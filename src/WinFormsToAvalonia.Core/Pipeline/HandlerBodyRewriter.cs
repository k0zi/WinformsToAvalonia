using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WinFormsToAvalonia.Core.Mapping;
using WinFormsToAvalonia.Core.Model;
using WinFormsToAvalonia.Core.Parsing;

namespace WinFormsToAvalonia.Core.Pipeline;

/// <summary>
/// Translates the statements of one WinForms event handler into real Avalonia code.
/// </summary>
/// <remarks>
/// <para>
/// The rule is the project's usual one, applied a statement at a time: emit real code only for
/// shapes that are <em>provably</em> equivalent, and leave everything else in the comment block
/// it lives in today. That is why this recognizes a small, closed set of statement forms rather
/// than attempting general WinForms-to-Avalonia expression translation.
/// </para>
/// <para>
/// Migration stops at the <b>first</b> statement it cannot translate, and everything from there
/// on stays commented. Translating statement 1 and 3 while silently dropping statement 2 would
/// produce a method that looks migrated but quietly skips work; a prefix, by contrast, is a
/// faithful partial execution of the original, and <c>MigrationTodo.NotMigrated</c> still fires
/// to say the handler is unfinished.
/// </para>
/// <para>
/// Two targets, because the same body means different things in the two places a handler can
/// land: in a View, <c>this.counterLabel.Text</c> is still a control property (<c>x:Name</c>
/// generates the field); in a ViewModel there is no control at all, and the same expression is
/// the generated [ObservableProperty] the plan bound to it.
/// </para>
/// </remarks>
public sealed class HandlerBodyRewriter
{
    /// <summary>
    /// Types whose static members are plain .NET and compile unchanged in an Avalonia project.
    /// A conservative list on purpose: an unrecognized receiver stops the migration rather than
    /// risking a WinForms API that does not exist any more.
    /// </summary>
    private static readonly HashSet<string> SafeStaticReceivers = new(StringComparer.Ordinal)
    {
        "int", "Int32", "long", "Int64", "short", "Int16", "byte", "Byte",
        "double", "Double", "float", "Single", "decimal", "Decimal",
        "bool", "Boolean", "char", "Char", "string", "String",
        "Math", "Convert", "DateTime", "DateTimeOffset", "TimeSpan", "Guid", "Environment",
    };

    private readonly ControlMappingRegistry _controlMappings;

    public HandlerBodyRewriter(ControlMappingRegistry controlMappings)
    {
        _controlMappings = controlMappings;
    }

    /// <summary>
    /// Rewrites for a View's code-behind, where the controls still exist as fields.
    /// </summary>
    public RewrittenBody RewriteForView(
        string body, FormModel formModel, ViewNavigationContext? navigation = null) =>
        Rewrite(body, new ViewTarget(formModel, _controlMappings, navigation ?? ViewNavigationContext.None));

    /// <summary>
    /// Rewrites for a promoted [RelayCommand], where every control property the body may touch
    /// has already been proved bindable and planned as an [ObservableProperty].
    /// </summary>
    public RewrittenBody RewriteForViewModel(
        string body, FormModel formModel, IReadOnlyList<BoundPropertyPlan> boundProperties) =>
        Rewrite(body, new ViewModelTarget(boundProperties, formModel));

    /// <summary>
    /// Translates a single *expression* against a ViewModel's properties - what a derived
    /// <c>CanExecute</c> guard needs, since it is a condition rather than a statement.
    /// </summary>
    public static bool TryRewriteConditionForViewModel(
        string expressionText,
        FormModel formModel,
        IReadOnlyList<BoundPropertyPlan> boundProperties,
        out string rewritten)
    {
        var expression = SyntaxFactory.ParseExpression(expressionText);

        if (expression.ContainsDiagnostics)
        {
            rewritten = "";
            return false;
        }

        return TryRewriteExpression(expression, new ViewModelTarget(boundProperties, formModel), out rewritten);
    }

    private static RewrittenBody Rewrite(string body, IRewriteTarget target)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return RewrittenBody.NothingMigrated(body);
        }

        // The stored body is dedented and brace-stripped, so it parses as a block once wrapped.
        if (SyntaxFactory.ParseStatement("{\n" + body + "\n}") is not BlockSyntax block
            || block.ContainsDiagnostics)
        {
            return RewrittenBody.NothingMigrated(body);
        }

        var statements = block.Statements;
        var migrated = new List<string>();
        var usings = new HashSet<string>(StringComparer.Ordinal);
        var fallbackKeys = new HashSet<string>(StringComparer.Ordinal);
        var requiresAsync = false;

        var migratedCount = 0;
        foreach (var statement in statements)
        {
            if (!TryRewriteStatement(statement, target, out var rewritten))
            {
                break;
            }

            migrated.Add(rewritten.Text);
            usings.UnionWith(rewritten.RequiredUsings);
            fallbackKeys.UnionWith(rewritten.RequiredFallbackKeys);
            requiresAsync |= rewritten.RequiresAsync;
            migratedCount++;
        }

        if (migratedCount == 0)
        {
            return RewrittenBody.NothingMigrated(body, statements.Count);
        }

        // The untouched suffix, taken verbatim from the original text rather than from the
        // syntax tree, so comments and formatting inside it survive exactly as before.
        var remaining = migratedCount == statements.Count
            ? ""
            : RemainingText(body, block, statements[migratedCount]);

        return new RewrittenBody(migrated, remaining, statements.Count, usings, fallbackKeys, requiresAsync);
    }

    /// <summary>The original body text from the first un-migrated statement onwards.</summary>
    private static string RemainingText(string body, BlockSyntax block, StatementSyntax firstRemaining)
    {
        // ParseStatement was handed "{\n" + body + "\n}", so a span offset in the block maps back
        // to the body by subtracting that two-character prefix.
        const int prefixLength = 2;
        var start = firstRemaining.SpanStart - prefixLength;

        return start >= 0 && start < body.Length ? body[start..].TrimEnd() : body;
    }

    private static bool TryRewriteStatement(StatementSyntax statement, IRewriteTarget target, out RewrittenStatement rewritten)
    {
        rewritten = default;

        if (statement is not ExpressionStatementSyntax expressionStatement)
        {
            return false;
        }

        return expressionStatement.Expression switch
        {
            AssignmentExpressionSyntax assignment => TryRewriteAssignment(assignment, target, out rewritten),
            InvocationExpressionSyntax invocation => TryRewriteInvocation(invocation, target, out rewritten),
            _ => false,
        };
    }

    /// <summary>`this.label1.Text = ...;` - the single most common statement in WinForms handlers.</summary>
    private static bool TryRewriteAssignment(
        AssignmentExpressionSyntax assignment, IRewriteTarget target, out RewrittenStatement rewritten)
    {
        rewritten = default;

        if (assignment.OperatorToken.IsKind(SyntaxKind.EqualsToken) is false
            || !TryResolveControlProperty(assignment.Left, target, forWrite: true, out var left)
            || !TryRewriteExpression(assignment.Right, target, out var right))
        {
            return false;
        }

        rewritten = new RewrittenStatement($"{left} = {right};");
        return true;
    }

    private static bool TryRewriteInvocation(
        InvocationExpressionSyntax invocation, IRewriteTarget target, out RewrittenStatement rewritten)
    {
        rewritten = default;

        if (!TrySplitInvocation(invocation, out var receiver, out var methodName))
        {
            return false;
        }

        // `new SettingsForm().ShowDialog();` / `.Show();` - opening another converted Form.
        if (receiver is ObjectCreationExpressionSyntax creation)
        {
            return TryRewriteFormNavigation(creation, methodName, invocation, target, out rewritten);
        }

        // `control.Focus();` - Avalonia's Control.Focus() is the same call, on the same field.
        if (methodName == "Focus"
            && invocation.ArgumentList.Arguments.Count == 0
            && receiver is not null
            && target.TryResolveControlField(receiver, out var controlField))
        {
            rewritten = new RewrittenStatement($"{controlField}.Focus();");
            return true;
        }

        if (!target.AllowsWindowApis)
        {
            return false;
        }

        // Form lifetime members, called bare or through `this`. The View *is* the Window.
        if (receiver is null || receiver is ThisExpressionSyntax)
        {
            if (invocation.ArgumentList.Arguments.Count > 0)
            {
                return false;
            }

            switch (methodName)
            {
                case "Close":
                    rewritten = new RewrittenStatement("Close();");
                    return true;
                case "Show":
                    rewritten = new RewrittenStatement("Show();");
                    return true;
                case "Hide":
                    // Avalonia's Window.Hide() exists, but WinForms' Hide() on a control means
                    // Visible = false; using IsVisible keeps one meaning for both.
                    rewritten = new RewrittenStatement("IsVisible = false;");
                    return true;
            }

            return false;
        }

        if (receiver is IdentifierNameSyntax { Identifier.ValueText: "MessageBox" } && methodName == "Show")
        {
            return TryRewriteMessageBox(invocation, target, out rewritten);
        }

        if (receiver is IdentifierNameSyntax { Identifier.ValueText: "Application" }
            && methodName == "Exit"
            && invocation.ArgumentList.Arguments.Count == 0)
        {
            rewritten = new RewrittenStatement(
                "(Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown();",
                RequiredUsings: ["Avalonia", "Avalonia.Controls.ApplicationLifetimes"]);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Opening another converted Form. The generated View sets its own DataContext in its
    /// constructor, so the translated call needs nothing the original did not have.
    /// </summary>
    /// <remarks>
    /// Only the bare statement forms are taken. `if (new F().ShowDialog() == DialogResult.OK)`
    /// - the shape most WinForms code actually uses - is left alone on purpose: Avalonia's
    /// ShowDialog returns a Task whose result is whatever the dialog passed to Close(), so
    /// translating the call without the branch around it would silently change the control flow.
    /// </remarks>
    private static bool TryRewriteFormNavigation(
        ObjectCreationExpressionSyntax creation,
        string methodName,
        InvocationExpressionSyntax invocation,
        IRewriteTarget target,
        out RewrittenStatement rewritten)
    {
        rewritten = default;

        // The generated View constructor takes no arguments, so a Form built with any would not
        // survive the translation intact.
        if (creation.ArgumentList is { Arguments.Count: > 0 }
            || !target.TryResolveFormView(RoslynTypeNameHelper.GetSimpleTypeName(creation.Type), out var view))
        {
            return false;
        }

        // ShowDialog's only argument is the owner, which the translated call supplies itself.
        if (invocation.ArgumentList.Arguments.Count > 1)
        {
            return false;
        }

        switch (methodName)
        {
            case "ShowDialog" when target.HostIsWindow:
                rewritten = new RewrittenStatement(
                    $"await new {view.ViewClassName}().ShowDialog(this);",
                    RequiredUsings: [view.ViewNamespace],
                    RequiresAsync: true);
                return true;

            case "Show" when invocation.ArgumentList.Arguments.Count == 0:
                rewritten = new RewrittenStatement(
                    $"new {view.ViewClassName}().Show();",
                    RequiredUsings: [view.ViewNamespace]);
                return true;

            default:
                return false;
        }
    }

    /// <summary>
    /// `MessageBox.Show(...)` has no Avalonia equivalent, so it maps to the bundled
    /// MessageBoxFallback - which is a dialog, and therefore async. Only the two argument shapes
    /// whose meaning is unambiguous are taken; the overloads carrying buttons/icons/defaults
    /// would need a return value the caller usually inspects, so they stay for a human.
    /// </summary>
    private static bool TryRewriteMessageBox(
        InvocationExpressionSyntax invocation, IRewriteTarget target, out RewrittenStatement rewritten)
    {
        rewritten = default;

        var arguments = invocation.ArgumentList.Arguments;
        if (arguments.Count is not (1 or 2))
        {
            return false;
        }

        var rewrittenArguments = new List<string>();
        foreach (var argument in arguments)
        {
            if (!TryRewriteExpression(argument.Expression, target, out var text))
            {
                return false;
            }

            rewrittenArguments.Add(text);
        }

        var caption = rewrittenArguments.Count == 2 ? rewrittenArguments[1] : "\"\"";
        rewritten = new RewrittenStatement(
            $"await MessageBoxFallback.ShowAsync(this, {rewrittenArguments[0]}, {caption});",
            RequiredFallbackKeys: ["MessageBoxFallback"],
            RequiresAsync: true);
        return true;
    }

    /// <summary>
    /// Rebuilds an expression with every control-property access replaced by its target-side
    /// equivalent, refusing anything whose meaning cannot be established syntactically.
    /// </summary>
    private static bool TryRewriteExpression(ExpressionSyntax expression, IRewriteTarget target, out string text)
    {
        text = "";

        switch (expression)
        {
            case LiteralExpressionSyntax:
                text = expression.ToString();
                return true;

            case ParenthesizedExpressionSyntax parenthesized:
                if (!TryRewriteExpression(parenthesized.Expression, target, out var inner))
                {
                    return false;
                }

                text = $"({inner})";
                return true;

            case PrefixUnaryExpressionSyntax prefix:
                if (!TryRewriteExpression(prefix.Operand, target, out var operand))
                {
                    return false;
                }

                text = $"{prefix.OperatorToken}{operand}";
                return true;

            case BinaryExpressionSyntax binary:
                if (!TryRewriteExpression(binary.Left, target, out var binaryLeft)
                    || !TryRewriteExpression(binary.Right, target, out var binaryRight))
                {
                    return false;
                }

                text = $"{binaryLeft} {binary.OperatorToken} {binaryRight}";
                return true;

            case ConditionalExpressionSyntax conditional:
                if (!TryRewriteExpression(conditional.Condition, target, out var condition)
                    || !TryRewriteExpression(conditional.WhenTrue, target, out var whenTrue)
                    || !TryRewriteExpression(conditional.WhenFalse, target, out var whenFalse))
                {
                    return false;
                }

                text = $"{condition} ? {whenTrue} : {whenFalse}";
                return true;

            case InterpolatedStringExpressionSyntax interpolated:
                return TryRewriteInterpolatedString(interpolated, target, out text);

            case MemberAccessExpressionSyntax memberAccess:
                return TryRewriteMemberAccess(memberAccess, target, out text);

            case InvocationExpressionSyntax invocation:
                return TryRewriteCallExpression(invocation, target, out text);

            default:
                return false;
        }
    }

    /// <summary>
    /// `$"TrackBar value: {this.trackBar1.Value}"` - the literal parts pass through untouched and
    /// each hole is rewritten like any other expression, so one un-translatable hole rejects the
    /// whole string rather than producing a half-converted message.
    /// </summary>
    /// <remarks>
    /// The delimiters are taken from the original tokens rather than rebuilt, so verbatim
    /// (<c>$@"..."</c>) and raw (<c>$"""..."""</c>) forms survive, as does <c>{{</c> escaping
    /// inside the text. Alignment and format clauses (<c>{x,5:N2}</c>) are plain .NET and are
    /// copied verbatim.
    /// </remarks>
    private static bool TryRewriteInterpolatedString(
        InterpolatedStringExpressionSyntax interpolated, IRewriteTarget target, out string text)
    {
        text = "";
        var builder = new System.Text.StringBuilder(interpolated.StringStartToken.ToString());

        foreach (var content in interpolated.Contents)
        {
            switch (content)
            {
                case InterpolatedStringTextSyntax literalText:
                    builder.Append(literalText.ToString());
                    break;

                case InterpolationSyntax interpolation:
                    if (!TryRewriteExpression(interpolation.Expression, target, out var hole))
                    {
                        return false;
                    }

                    builder.Append('{').Append(hole)
                        .Append(interpolation.AlignmentClause?.ToString())
                        .Append(interpolation.FormatClause?.ToString())
                        .Append('}');
                    break;

                default:
                    return false;
            }
        }

        text = builder.Append(interpolated.StringEndToken.ToString()).ToString();
        return true;
    }

    private static bool TryRewriteMemberAccess(MemberAccessExpressionSyntax memberAccess, IRewriteTarget target, out string text)
    {
        if (TryResolveControlProperty(memberAccess, target, forWrite: false, out text))
        {
            return true;
        }

        // `string.Empty`, `Math.PI`, `DateTime.Now`, ... - plain .NET, unchanged in Avalonia.
        if (IsSafeStaticReceiver(memberAccess.Expression, out _))
        {
            text = memberAccess.ToString();
            return true;
        }

        // `nameTextBox.Text.Length` - a member *of* a control property. Safe because every type
        // in BindablePropertyCatalog is a plain BCL type (string/bool/int/...), so anything
        // hanging off one is ordinary .NET rather than a WinForms API. The receiver goes through
        // the same read path as anywhere else, so it is null-guarded too.
        if (TryResolveControlProperty(memberAccess.Expression, target, forWrite: false, out var propertyAccess))
        {
            text = $"{propertyAccess}.{memberAccess.Name}";
            return true;
        }

        text = "";
        return false;
    }

    /// <summary>
    /// A receiver that names one of the <see cref="SafeStaticReceivers"/>. Both spellings count:
    /// `int.Parse` parses as a predefined-type keyword, `Int32.Parse` as an identifier.
    /// </summary>
    private static bool IsSafeStaticReceiver(ExpressionSyntax expression, out string name)
    {
        name = expression switch
        {
            PredefinedTypeSyntax predefined => predefined.Keyword.ValueText,
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            _ => "",
        };

        return name.Length > 0 && SafeStaticReceivers.Contains(name);
    }

    /// <summary>
    /// A call used as a value: either on a safe BCL type (`int.Parse(...)`) or on something this
    /// rewriter already understands (`this.textBox1.Text.Trim()`).
    /// </summary>
    private static bool TryRewriteCallExpression(InvocationExpressionSyntax invocation, IRewriteTarget target, out string text)
    {
        text = "";

        if (invocation.Expression is not MemberAccessExpressionSyntax { Name: var name } call)
        {
            return false;
        }

        string receiverText;
        if (IsSafeStaticReceiver(call.Expression, out var receiverName))
        {
            receiverText = receiverName;
        }
        else if (!TryRewriteExpression(call.Expression, target, out receiverText))
        {
            return false;
        }

        var arguments = new List<string>();
        foreach (var argument in invocation.ArgumentList.Arguments)
        {
            // A ref/out/named argument changes the call's meaning in ways this is not trying to
            // reason about.
            if (argument.NameColon is not null || !argument.RefKindKeyword.IsKind(SyntaxKind.None)
                || !TryRewriteExpression(argument.Expression, target, out var argumentText))
            {
                return false;
            }

            arguments.Add(argumentText);
        }

        text = $"{receiverText}.{name}({string.Join(", ", arguments)})";
        return true;
    }

    /// <summary>`this.label1.Text` / `label1.Text` resolved to whatever it is on the target side.</summary>
    private static bool TryResolveControlProperty(
        ExpressionSyntax expression, IRewriteTarget target, bool forWrite, out string text)
    {
        text = "";

        if (expression is not MemberAccessExpressionSyntax { Name.Identifier.ValueText: var propertyName } access
            || !target.TryResolveControlField(access.Expression, out var fieldName))
        {
            return false;
        }

        return target.TryResolveProperty(fieldName, propertyName, forWrite, out text);
    }

    /// <summary>Splits `a.B(...)` into receiver `a` and name `B`; `B(...)` yields a null receiver.</summary>
    private static bool TrySplitInvocation(
        InvocationExpressionSyntax invocation, out ExpressionSyntax? receiver, out string methodName)
    {
        switch (invocation.Expression)
        {
            case MemberAccessExpressionSyntax { Name.Identifier.ValueText: var name } access:
                receiver = access.Expression;
                methodName = name;
                return true;
            case IdentifierNameSyntax identifier:
                receiver = null;
                methodName = identifier.Identifier.ValueText;
                return true;
            default:
                receiver = null;
                methodName = "";
                return false;
        }
    }

    private readonly record struct RewrittenStatement(
        string Text,
        IReadOnlyList<string>? RequiredUsings = null,
        IReadOnlyList<string>? RequiredFallbackKeys = null,
        bool RequiresAsync = false)
    {
        public IReadOnlyList<string> RequiredUsings { get; } = RequiredUsings ?? [];

        public IReadOnlyList<string> RequiredFallbackKeys { get; } = RequiredFallbackKeys ?? [];
    }

    /// <summary>Where the rewritten body is going to live, and what a control property means there.</summary>
    private interface IRewriteTarget
    {
        /// <summary>False in a ViewModel, which has no Window to close and no dialog owner.</summary>
        bool AllowsWindowApis { get; }

        /// <summary>False in a converted UserControl, which cannot own a modal dialog.</summary>
        bool HostIsWindow { get; }

        /// <summary>Resolves a WinForms Form type name to the View this run generates for it.</summary>
        bool TryResolveFormView(string winFormsTypeName, out FormViewInfo view);

        bool TryResolveControlField(ExpressionSyntax expression, out string fieldName);

        /// <param name="forWrite">True for an assignment target, where the expression must stay assignable.</param>
        bool TryResolveProperty(string fieldName, string winFormsPropertyName, bool forWrite, out string text);
    }

    private sealed class ViewTarget(
        FormModel formModel, ControlMappingRegistry controlMappings, ViewNavigationContext navigation) : IRewriteTarget
    {
        public bool AllowsWindowApis => true;

        public bool HostIsWindow => navigation.HostIsWindow;

        public bool TryResolveFormView(string winFormsTypeName, out FormViewInfo view) =>
            navigation.FormViews.TryGetValue(winFormsTypeName, out view!);

        public bool TryResolveControlField(ExpressionSyntax expression, out string fieldName)
        {
            fieldName = expression switch
            {
                MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax, Name.Identifier.ValueText: var name } => name,
                IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
                _ => "",
            };

            return fieldName.Length > 0 && formModel.Controls.ContainsKey(fieldName);
        }

        /// <summary>
        /// The control still exists as an x:Name field, so the access survives - but only for a
        /// property with a proven Avalonia counterpart, on a control that really is that Avalonia
        /// element. A fallback control does not necessarily expose it.
        /// </summary>
        public bool TryResolveProperty(string fieldName, string winFormsPropertyName, bool forWrite, out string text)
        {
            text = "";

            if (!formModel.Controls.TryGetValue(fieldName, out var control)
                || controlMappings.Map(control).Status != MappingStatus.Direct
                || !BindablePropertyCatalog.TryGet(control.ClrTypeName, winFormsPropertyName, out var bindable))
            {
                return false;
            }

            var access = $"{fieldName}.{bindable.AvaloniaPropertyName}";

            // WinForms' string properties never return null - Avalonia's are `string?`. Reading
            // one straight into something like int.Parse would be a CS8604 in the generated
            // project, which enables nullable; `?? string.Empty` is both warning-free and the
            // faithful translation of the WinForms behaviour.
            text = !forWrite && bindable.ClrTypeName == "string" ? $"({access} ?? string.Empty)" : access;
            return true;
        }
    }

    private sealed class ViewModelTarget(
        IReadOnlyList<BoundPropertyPlan> boundProperties, FormModel formModel) : IRewriteTarget
    {
        public bool AllowsWindowApis => false;

        public bool HostIsWindow => false;

        /// <summary>A ViewModel has no business constructing Views; navigation needs a service.</summary>
        public bool TryResolveFormView(string winFormsTypeName, out FormViewInfo view)
        {
            view = null!;
            return false;
        }

        public bool TryResolveControlField(ExpressionSyntax expression, out string fieldName)
        {
            fieldName = expression switch
            {
                MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax, Name.Identifier.ValueText: var name } => name,
                IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
                _ => "",
            };

            var candidate = fieldName;
            return candidate.Length > 0 && boundProperties.Any(p => p.ControlFieldName == candidate);
        }

        /// <summary>
        /// There is no control here: the only thing a promoted body may name is an
        /// [ObservableProperty] the plan created for it, which is exactly the set of
        /// BoundPropertyPlans - so nothing else can resolve, by construction.
        /// </summary>
        /// <remarks>
        /// No null-guard needed on reads, unlike the View: the generated [ObservableProperty] for
        /// a string is non-nullable and initialized to string.Empty.
        /// </remarks>
        public bool TryResolveProperty(string fieldName, string winFormsPropertyName, bool forWrite, out string text)
        {
            text = "";

            // BoundPropertyPlan is keyed by the *Avalonia* property, so the WinForms name has to
            // go through the catalog first - with the control's own type, which only the
            // FormModel still knows at this point.
            if (!formModel.Controls.TryGetValue(fieldName, out var control)
                || !BindablePropertyCatalog.TryGet(control.ClrTypeName, winFormsPropertyName, out var bindable))
            {
                return false;
            }

            var bound = boundProperties.FirstOrDefault(p =>
                p.ControlFieldName == fieldName && p.AvaloniaPropertyName == bindable.AvaloniaPropertyName);

            if (bound is null)
            {
                return false;
            }

            text = bound.ViewModelPropertyName;
            return true;
        }
    }
}
