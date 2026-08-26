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

        // System.IO: plain .NET, and exactly what a file-dialog handler reaches for next.
        "File", "Directory", "Path",
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
        string body,
        FormModel formModel,
        ViewNavigationContext? navigation = null,
        HandlerSignature? signature = null) =>
        Rewrite(
            body,
            new ViewTarget(
                formModel,
                _controlMappings,
                navigation ?? ViewNavigationContext.None,
                signature ?? HandlerSignature.None));

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
            var snapshot = target.Requirements.Snapshot();
            if (!TryRewriteStatement(statement, target, out var rewritten))
            {
                // Undo anything a half-translated expression recorded, or the method could end up
                // `async` with nothing to await.
                target.Requirements.Restore(snapshot);
                break;
            }

            migrated.Add(rewritten.Text);
            usings.UnionWith(rewritten.RequiredUsings);
            fallbackKeys.UnionWith(rewritten.RequiredFallbackKeys);
            requiresAsync |= rewritten.RequiresAsync;
            migratedCount++;
        }

        // A local left at the very end of a *partial* prefix can have no user: the statements that
        // would have used it are exactly the ones that did not translate. Dropping it keeps the
        // generated method free of dead declarations - and of CS0219, which a constant
        // initializer would raise in a project that builds warning-free.
        if (migratedCount < statements.Count)
        {
            while (migratedCount > 0 && statements[migratedCount - 1] is LocalDeclarationStatementSyntax)
            {
                migratedCount--;
                migrated.RemoveAt(migrated.Count - 1);
            }
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

        usings.UnionWith(target.Requirements.RequiredUsings);
        requiresAsync |= target.Requirements.RequiresAsync;

        return new RewrittenBody(
            migrated, remaining, statements.Count, usings, fallbackKeys, requiresAsync,
            target.Requirements.InlinedDialogFields);
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

        switch (statement)
        {
            case ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax assignment }:
                return TryRewriteAssignment(assignment, target, out rewritten);

            case ExpressionStatementSyntax { Expression: InvocationExpressionSyntax invocation }:
                return TryRewriteInvocation(invocation, target, out rewritten);

            // `i++;` on its own line, not just as a `for` incrementor.
            case ExpressionStatementSyntax { Expression: PostfixUnaryExpressionSyntax or PrefixUnaryExpressionSyntax } increment
                when TryRewriteIncrement(increment.Expression, target, out var incrementText):
                rewritten = new RewrittenStatement($"{incrementText};");
                return true;

            case IfStatementSyntax ifStatement:
                return TryRewriteIf(ifStatement, target, out rewritten);

            case BlockSyntax block:
                return TryRewriteBlock(block, target, out rewritten);

            case ForEachStatementSyntax forEachStatement:
                return TryRewriteForEach(forEachStatement, target, out rewritten);

            case ForStatementSyntax forStatement:
                return TryRewriteFor(forStatement, target, out rewritten);

            case WhileStatementSyntax whileStatement:
                return TryRewriteWhile(whileStatement, target, out rewritten);

            case LocalDeclarationStatementSyntax declaration:
                return TryRewriteLocalDeclaration(declaration, target, out rewritten);

            // `return;` in a void handler - an early exit with nothing to translate.
            case ReturnStatementSyntax { Expression: null }:
                rewritten = new RewrittenStatement("return;");
                return true;

            default:
                return false;
        }
    }

    /// <summary>
    /// An <c>if</c> whose condition and every branch translate. Braces are always emitted, even
    /// where the original had none, so a rewritten branch can never change what the `else` binds to.
    /// </summary>
    /// <summary>
    /// `if (openFileDialog1.ShowDialog(this) == DialogResult.OK) { ... openFileDialog1.FileName ... }`
    /// - the one translation in this rewriter that changes an expression's *shape* rather than
    /// re-spelling it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Avalonia has no dialog object to ask afterwards: the picker returns the selection itself.
    /// A list pattern keeps that inline - <c>is [var f, ..]</c> both tests for a selection and
    /// binds it - so no statement has to be inserted before the `if`, and the binding is scoped
    /// exactly to the branch that used to read the dialog's property.
    /// </para>
    /// <para>
    /// The picker is opened with default options: the designer's `Filter` string would have to be
    /// parsed into `FileTypeFilter` entries, and getting that subtly wrong is worse than leaving
    /// it out. Only the path property is translated, not `FileNames` (multi-select) or the
    /// dialogs with no Avalonia equivalent at all (Color/Font/Print).
    /// </para>
    /// </remarks>
    private static bool TryRewriteFileDialogIf(
        IfStatementSyntax ifStatement, IRewriteTarget target, out RewrittenStatement rewritten)
    {
        rewritten = default;

        if (!target.AllowsWindowApis
            || ifStatement.Condition is not BinaryExpressionSyntax comparison
            || !comparison.IsKind(SyntaxKind.EqualsExpression)
            || comparison.Right is not MemberAccessExpressionSyntax
            {
                Expression: IdentifierNameSyntax { Identifier.ValueText: "DialogResult" },
                Name.Identifier.ValueText: "OK",
            }
            || comparison.Left is not InvocationExpressionSyntax
            {
                Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "ShowDialog" } call,
            } invocation
            || invocation.ArgumentList.Arguments.Count > 1
            || !target.TryResolveControlField(call.Expression, out var dialogField)
            || !target.TryResolveFileDialog(dialogField, out var kind))
        {
            return false;
        }

        var variableName = $"{dialogField}{kind.SelectionSuffix}";
        var selectionKey = $"{dialogField}.{kind.PathMemberName}";

        target.DialogSelections[selectionKey] = $"{variableName}.Path.LocalPath";
        try
        {
            if (!TryRewriteBranch(ifStatement.Statement, target, out var thenBranch))
            {
                return false;
            }

            // An `else` runs when the user cancelled; nothing about that needs the selection.
            var parts = new List<RewrittenStatement> { thenBranch };
            var pattern = string.Format(kind.SelectionPattern, variableName);
            var text =
                $"if (await StorageProvider.{kind.PickerMethodName}(new {kind.OptionsTypeName}()) is {pattern})"
                + $"\n{{\n{Indent(thenBranch.Text)}\n}}";

            if (ifStatement.Else is { } elseClause)
            {
                target.DialogSelections.Remove(selectionKey);
                if (!TryRewriteBranch(elseClause.Statement, target, out var elseBranch))
                {
                    return false;
                }

                parts.Add(elseBranch);
                text += $"\nelse\n{{\n{Indent(elseBranch.Text)}\n}}";
            }

            target.Requirements.RequiresAsync = true;
            target.Requirements.RequiredUsings.Add("Avalonia.Platform.Storage");
            target.Requirements.InlinedDialogFields.Add(dialogField);

            rewritten = Merge(text, parts);
            return true;
        }
        finally
        {
            target.DialogSelections.Remove(selectionKey);
        }
    }

    private static bool TryRewriteIf(IfStatementSyntax ifStatement, IRewriteTarget target, out RewrittenStatement rewritten)
    {
        if (TryRewriteFileDialogIf(ifStatement, target, out rewritten))
        {
            return true;
        }

        rewritten = default;

        if (!TryRewriteExpression(ifStatement.Condition, target, out var condition)
            || !TryRewriteBranch(ifStatement.Statement, target, out var thenBranch))
        {
            return false;
        }

        var parts = new List<RewrittenStatement> { thenBranch };
        var text = $"if ({condition})\n{{\n{Indent(thenBranch.Text)}\n}}";

        if (ifStatement.Else is { } elseClause)
        {
            if (!TryRewriteBranch(elseClause.Statement, target, out var elseBranch))
            {
                return false;
            }

            parts.Add(elseBranch);

            // `else if` reads better than a nested braced if, and the recursion already produced
            // exactly that text.
            text += elseClause.Statement is IfStatementSyntax
                ? $"\nelse {elseBranch.Text}"
                : $"\nelse\n{{\n{Indent(elseBranch.Text)}\n}}";
        }

        rewritten = Merge(text, parts);
        return true;
    }

    /// <summary>
    /// `foreach (var item in <collection>)`. The loop variable is a
    /// <see cref="LocalKind.Value"/> for the same reason any other local is: the collection
    /// expression had to translate, so it is a plain .NET value, and so are its elements.
    /// </summary>
    private static bool TryRewriteForEach(
        ForEachStatementSyntax forEach, IRewriteTarget target, out RewrittenStatement rewritten)
    {
        rewritten = default;

        if (DeclaredTypeText(forEach.Type) is not { } typeText
            || !TryRewriteExpression(forEach.Expression, target, out var collection))
        {
            return false;
        }

        var name = forEach.Identifier.ValueText;
        return TryRewriteLoopBody(
            $"foreach ({typeText} {name} in {collection})",
            forEach.Statement,
            target,
            [(name, LocalKind.Value)],
            out rewritten);
    }

    /// <summary>
    /// `for (var i = 0; i < n; i++)`. Only the single-declaration shape is taken - a comma-separated
    /// initializer list is rare enough in designer-era handler code not to be worth the ambiguity.
    /// </summary>
    private static bool TryRewriteFor(ForStatementSyntax forStatement, IRewriteTarget target, out RewrittenStatement rewritten)
    {
        rewritten = default;

        if (forStatement.Initializers.Count > 0)
        {
            return false;
        }

        // The loop variable is in scope for the condition and incrementors too, so it has to be
        // declared before either is translated - and popped again by TryRewriteLoopBody.
        var declared = new List<(string Name, LocalKind Kind)>();
        var header = "for (";

        if (forStatement.Declaration is { Variables: [{ Initializer.Value: { } initial } variable] } declaration)
        {
            if (DeclaredTypeText(declaration.Type) is not { } typeText
                || !TryRewriteExpression(initial, target, out var initialValue))
            {
                return false;
            }

            declared.Add((variable.Identifier.ValueText, LocalKind.Value));
            header += $"{typeText} {variable.Identifier.ValueText} = {initialValue}";
        }
        else if (forStatement.Declaration is not null)
        {
            return false;
        }

        // Declared up front so the condition and incrementors below can see the loop variable;
        // TryRewriteLoopBody pushes its own scope and re-declares them for the body.
        target.Locals.Push();
        try
        {
            foreach (var (name, kind) in declared)
            {
                target.Locals.Declare(name, kind);
            }

            var condition = "";
            if (forStatement.Condition is { } conditionExpression
                && !TryRewriteExpression(conditionExpression, target, out condition))
            {
                return false;
            }

            var incrementors = new List<string>();
            foreach (var incrementor in forStatement.Incrementors)
            {
                if (!TryRewriteIncrement(incrementor, target, out var text)
                    && !TryRewriteSideEffect(incrementor, target, out text))
                {
                    return false;
                }

                incrementors.Add(text);
            }

            header += $"; {condition}; {string.Join(", ", incrementors)})";
        }
        finally
        {
            target.Locals.Pop();
        }

        return TryRewriteLoopBody(header, forStatement.Statement, target, declared, out rewritten);
    }

    private static bool TryRewriteWhile(WhileStatementSyntax whileStatement, IRewriteTarget target, out RewrittenStatement rewritten)
    {
        rewritten = default;

        return TryRewriteExpression(whileStatement.Condition, target, out var condition)
            && TryRewriteLoopBody($"while ({condition})", whileStatement.Statement, target, [], out rewritten);
    }

    /// <summary>
    /// The shared tail of every loop: declare the loop variables, translate the body
    /// all-or-nothing (as with an <c>if</c> branch), and brace it.
    /// </summary>
    private static bool TryRewriteLoopBody(
        string header,
        StatementSyntax body,
        IRewriteTarget target,
        IReadOnlyList<(string Name, LocalKind Kind)> loopVariables,
        out RewrittenStatement rewritten)
    {
        rewritten = default;

        target.Locals.Push();
        try
        {
            foreach (var (name, kind) in loopVariables)
            {
                target.Locals.Declare(name, kind);
            }

            if (!TryRewriteStatement(body, target, out var translatedBody))
            {
                return false;
            }

            rewritten = Merge($"{header}\n{{\n{Indent(translatedBody.Text)}\n}}", [translatedBody]);
            return true;
        }
        finally
        {
            target.Locals.Pop();
        }
    }

    /// <summary>An expression used for its side effect - an assignment, in a `for` incrementor.</summary>
    private static bool TryRewriteSideEffect(ExpressionSyntax expression, IRewriteTarget target, out string text)
    {
        text = "";

        if (expression is not AssignmentExpressionSyntax assignment
            || !TryRewriteAssignment(assignment, target, out var statement))
        {
            return false;
        }

        text = statement.Text.TrimEnd(';');
        return true;
    }

    /// <summary>
    /// `var` or a keyword type is kept; a named type could be a WinForms type whose translation
    /// is a different type entirely, so it is refused.
    /// </summary>
    private static string? DeclaredTypeText(TypeSyntax type) =>
        type.IsVar || type is PredefinedTypeSyntax ? type.ToString() : null;

    /// <summary>
    /// One branch of an <c>if</c>, or the contents of a block - translated <b>whole or not at
    /// all</b>, unlike the top level.
    /// </summary>
    /// <remarks>
    /// The prefix rule cannot apply inside a compound statement: the un-migrated remainder is
    /// emitted as a comment *after* the whole statement, so a partially translated branch would
    /// silently drop its own tail with nothing at that spot to say so. All-or-nothing keeps the
    /// emitted code a faithful whole.
    /// </remarks>
    private static bool TryRewriteBranch(StatementSyntax branch, IRewriteTarget target, out RewrittenStatement rewritten) =>
        branch is IfStatementSyntax nestedIf
            ? TryRewriteIf(nestedIf, target, out rewritten)
            : TryRewriteStatement(branch, target, out rewritten);

    private static bool TryRewriteBlock(BlockSyntax block, IRewriteTarget target, out RewrittenStatement rewritten)
    {
        rewritten = default;
        var parts = new List<RewrittenStatement>();

        // Locals declared inside the block go out of scope with it, exactly as in C#.
        target.Locals.Push();
        try
        {
            foreach (var statement in block.Statements)
            {
                if (!TryRewriteStatement(statement, target, out var part))
                {
                    return false;
                }

                parts.Add(part);
            }
        }
        finally
        {
            target.Locals.Pop();
        }

        rewritten = Merge(string.Join("\n", parts.Select(p => p.Text)), parts);
        return true;
    }

    /// <summary>
    /// `var x = <expr>;` - the local becomes usable by every later statement in its scope.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Two initializer shapes are accepted. A translatable *expression* yields a
    /// <see cref="LocalKind.Value"/>: since such an expression can only be built from literals,
    /// catalog properties and safe BCL statics, its result is always a plain .NET value, so
    /// members of it are plain .NET too. `new SomeForm()` yields a
    /// <see cref="LocalKind.FormView"/>, which only the navigation calls accept - that shape is
    /// handled here rather than in the general expression grammar so `new` cannot leak into
    /// arbitrary expressions.
    /// </para>
    /// <para>
    /// `using var dialog = new SomeForm();` drops the `using`: an Avalonia Window is not
    /// IDisposable, so there is no disposal to preserve. On any other initializer a `using` is
    /// refused rather than silently dropped, since that would discard a real Dispose call.
    /// </para>
    /// </remarks>
    private static bool TryRewriteLocalDeclaration(
        LocalDeclarationStatementSyntax declaration, IRewriteTarget target, out RewrittenStatement rewritten)
    {
        rewritten = default;

        if (declaration.Declaration.Variables is not [{ Initializer.Value: { } initializer } variable]
            || declaration.Modifiers.Any(m => m.IsKind(SyntaxKind.ConstKeyword)))
        {
            return false;
        }

        var name = variable.Identifier.ValueText;
        var isUsing = !declaration.UsingKeyword.IsKind(SyntaxKind.None);

        // `new SomeForm()` - the only construction this rewriter understands.
        if (initializer is ObjectCreationExpressionSyntax { ArgumentList: null or { Arguments.Count: 0 } } creation
            && target.TryResolveFormView(RoslynTypeNameHelper.GetSimpleTypeName(creation.Type), out var view))
        {
            target.Locals.Declare(name, LocalKind.FormView);
            rewritten = new RewrittenStatement(
                $"var {name} = new {view.ViewClassName}();",
                RequiredUsings: [view.ViewNamespace]);
            return true;
        }

        if (isUsing || !TryRewriteExpression(initializer, target, out var value))
        {
            return false;
        }

        // The declared type is kept only when it is a keyword type; a named one could be a
        // WinForms type whose translation is a different type entirely.
        if (DeclaredTypeText(declaration.Declaration.Type) is not { } typeText)
        {
            return false;
        }

        target.Locals.Declare(name, LocalKind.Value);
        rewritten = new RewrittenStatement($"{typeText} {name} = {value};");
        return true;
    }

    /// <summary>Combines the children's text with the union of everything they need.</summary>
    private static RewrittenStatement Merge(string text, IReadOnlyList<RewrittenStatement> parts) =>
        new(
            text,
            [.. parts.SelectMany(p => p.RequiredUsings).Distinct(StringComparer.Ordinal)],
            [.. parts.SelectMany(p => p.RequiredFallbackKeys).Distinct(StringComparer.Ordinal)],
            parts.Any(p => p.RequiresAsync));

    private static string Indent(string text) =>
        string.Join("\n", text.Split('\n').Select(l => l.Length == 0 ? l : "    " + l));

    /// <summary>`this.label1.Text = ...;` - the single most common statement in WinForms handlers.</summary>
    private static bool TryRewriteAssignment(
        AssignmentExpressionSyntax assignment, IRewriteTarget target, out RewrittenStatement rewritten)
    {
        rewritten = default;

        if (!TryRewriteExpression(assignment.Right, target, out var right))
        {
            return false;
        }

        // A local holds a plain .NET value, so any compound operator on it is ordinary .NET.
        // A control property is only assigned with `=`: a compound operator would read it too,
        // and the read path (which null-guards) cannot be spliced into the left of an assignment.
        if (assignment.Left is IdentifierNameSyntax { Identifier.ValueText: var localName }
            && target.Locals.TryGet(localName, out var localKind)
            && localKind == LocalKind.Value)
        {
            rewritten = new RewrittenStatement($"{localName} {assignment.OperatorToken} {right};");
            return true;
        }

        if (!assignment.OperatorToken.IsKind(SyntaxKind.EqualsToken))
        {
            return false;
        }

        // `e.Cancel = true;` - settable only through a plain member path; a translation that
        // computes something (the pointer position) is a read, and cannot be assigned to.
        if (assignment.Left is MemberAccessExpressionSyntax { Name.Identifier.ValueText: var argsMemberName } argsAccess
            && target.TryResolveEventArgsMember(argsAccess.Expression, argsMemberName, out var argsTarget)
            && !argsTarget.Contains('(', StringComparison.Ordinal))
        {
            rewritten = new RewrittenStatement($"{argsTarget} = {right};");
            return true;
        }

        if (!TryResolveControlProperty(assignment.Left, target, forWrite: true, out var left))
        {
            return false;
        }

        rewritten = new RewrittenStatement($"{left} = {right};");
        return true;
    }

    /// <summary>`i++` / `--i` on a local - the shape a `for` incrementor almost always is.</summary>
    private static bool TryRewriteIncrement(ExpressionSyntax expression, IRewriteTarget target, out string text)
    {
        text = "";

        var (operand, format) = expression switch
        {
            PostfixUnaryExpressionSyntax postfix => (postfix.Operand, $"{{0}}{postfix.OperatorToken}"),
            PrefixUnaryExpressionSyntax prefix when prefix.OperatorToken.IsKind(SyntaxKind.PlusPlusToken)
                || prefix.OperatorToken.IsKind(SyntaxKind.MinusMinusToken) => (prefix.Operand, $"{prefix.OperatorToken}{{0}}"),
            _ => (null, ""),
        };

        if (operand is not IdentifierNameSyntax { Identifier.ValueText: var name }
            || !target.Locals.TryGet(name, out var kind)
            || kind != LocalKind.Value)
        {
            return false;
        }

        text = string.Format(format, name);
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

        // The same call on a local the pass already resolved to a View - the
        // `var dialog = new SettingsForm(); dialog.ShowDialog(this);` shape.
        if (receiver is IdentifierNameSyntax { Identifier.ValueText: var dialogLocal }
            && target.Locals.TryGet(dialogLocal, out var dialogKind)
            && dialogKind == LocalKind.FormView)
        {
            return TryRewriteViewNavigationCall(dialogLocal, methodName, invocation, target, out rewritten);
        }

        // `control.Clear();` / `control.Focus();` - a zero-argument method with an exact Avalonia
        // equivalent, per ControlMethodCatalog.
        if (invocation.ArgumentList.Arguments.Count == 0
            && receiver is not null
            && target.TryResolveControlField(receiver, out var controlField)
            && target.TryResolveControlMethod(controlField, methodName, out var call))
        {
            rewritten = new RewrittenStatement(call);
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

        return TryRewriteViewNavigationCall(
            $"new {view.ViewClassName}()", methodName, invocation, target, out rewritten, view.ViewNamespace);
    }

    /// <summary>
    /// Emits the navigation call itself, given whatever already evaluates to the target View -
    /// a fresh `new SomeView()` or a local that holds one.
    /// </summary>
    private static bool TryRewriteViewNavigationCall(
        string viewExpression,
        string methodName,
        InvocationExpressionSyntax invocation,
        IRewriteTarget target,
        out RewrittenStatement rewritten,
        string? requiredUsing = null)
    {
        rewritten = default;
        IReadOnlyList<string> usings = requiredUsing is null ? [] : [requiredUsing];

        // ShowDialog's only argument is the owner, which the translated call supplies itself.
        if (invocation.ArgumentList.Arguments.Count > 1)
        {
            return false;
        }

        switch (methodName)
        {
            case "ShowDialog" when target.HostIsWindow:
                rewritten = new RewrittenStatement(
                    $"await {viewExpression}.ShowDialog(this);", RequiredUsings: usings, RequiresAsync: true);
                return true;

            case "Show" when invocation.ArgumentList.Arguments.Count == 0:
                rewritten = new RewrittenStatement($"{viewExpression}.Show();", RequiredUsings: usings);
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

            // `dlg.ShowDialog(this) == DialogResult.OK` - checked before the generic binary case,
            // because neither operand translates on its own.
            case BinaryExpressionSyntax comparison
                when TryRewriteDialogResultComparison(comparison, target, out text):
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

            // A local this pass declared earlier. Only Value locals are usable as expressions -
            // a FormView is not a value, it is something you open.
            case IdentifierNameSyntax identifier
                when target.Locals.TryGet(identifier.Identifier.ValueText, out var kind) && kind == LocalKind.Value:
                text = identifier.Identifier.ValueText;
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

    /// <summary>
    /// The WinForms modal-dialog idiom: `if (new SettingsForm().ShowDialog(this) == DialogResult.OK)`.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Avalonia's <c>ShowDialog&lt;T&gt;</c> returns whatever the dialog passed to
    /// <c>Close(result)</c>, and a converted dialog closes with a <c>bool</c> - see
    /// <c>FormMigrationPlanner.PlanDialogResultButtons</c>, which synthesizes that from the
    /// designer-set <c>DialogResult</c>. So the whole comparison collapses into the awaited call,
    /// negated when the test was for Cancel.
    /// </para>
    /// <para>
    /// Only OK and Cancel are taken. A three-way Yes/No/Cancel dialog cannot be expressed as a
    /// bool, and inventing a wider result type would change what the converted dialog returns.
    /// </para>
    /// </remarks>
    private static bool TryRewriteDialogResultComparison(
        BinaryExpressionSyntax comparison, IRewriteTarget target, out string text)
    {
        text = "";

        var isEquals = comparison.IsKind(SyntaxKind.EqualsExpression);
        if ((!isEquals && !comparison.IsKind(SyntaxKind.NotEqualsExpression)) || !target.HostIsWindow)
        {
            return false;
        }

        // Either order: `dlg.ShowDialog() == DialogResult.OK` or the reverse.
        var (call, expected) = comparison.Left is InvocationExpressionSyntax leftCall
            ? (leftCall, comparison.Right)
            : comparison.Right is InvocationExpressionSyntax rightCall ? (rightCall, comparison.Left) : (null, null);

        if (call is null
            || expected is not MemberAccessExpressionSyntax
            {
                Expression: IdentifierNameSyntax { Identifier.ValueText: "DialogResult" },
                Name.Identifier.ValueText: var resultName,
            }
            || resultName is not ("OK" or "Cancel")
            || !TryResolveShowDialogTarget(call, target, out var viewExpression, out var viewNamespace))
        {
            return false;
        }

        // `== OK` and `!= Cancel` both mean "the user accepted"; the other two mean the opposite.
        var meansAccepted = isEquals == (resultName == "OK");

        target.Requirements.RequiresAsync = true;
        if (viewNamespace is not null)
        {
            target.Requirements.RequiredUsings.Add(viewNamespace);
        }

        text = $"{(meansAccepted ? "" : "!")}await {viewExpression}.ShowDialog<bool>(this)";
        return true;
    }

    /// <summary>
    /// The `X` of `X.ShowDialog(...)`, when X is a converted Form - either freshly constructed or
    /// a local already holding its View.
    /// </summary>
    private static bool TryResolveShowDialogTarget(
        InvocationExpressionSyntax call, IRewriteTarget target, out string viewExpression, out string? viewNamespace)
    {
        viewExpression = "";
        viewNamespace = null;

        if (call.Expression is not MemberAccessExpressionSyntax { Name.Identifier.ValueText: "ShowDialog" } access
            || call.ArgumentList.Arguments.Count > 1)
        {
            return false;
        }

        switch (access.Expression)
        {
            case ObjectCreationExpressionSyntax { ArgumentList: null or { Arguments.Count: 0 } } creation
                when target.TryResolveFormView(RoslynTypeNameHelper.GetSimpleTypeName(creation.Type), out var view):
                viewExpression = $"new {view.ViewClassName}()";
                viewNamespace = view.ViewNamespace;
                return true;

            case IdentifierNameSyntax { Identifier.ValueText: var localName }
                when target.Locals.TryGet(localName, out var kind) && kind == LocalKind.FormView:
                viewExpression = localName;
                return true;

            default:
                return false;
        }
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

        // `openFileDialog1.FileName` inside an inlined picker branch - the chosen item is a
        // pattern variable now, not a property of a dialog object that no longer exists.
        if (memberAccess.Expression is IdentifierNameSyntax or MemberAccessExpressionSyntax
            && target.TryResolveControlField(memberAccess.Expression, out var dialogField)
            && target.DialogSelections.TryGetValue(
                $"{dialogField}.{memberAccess.Name.Identifier.ValueText}", out var selection))
        {
            text = selection;
            return true;
        }

        // `e.NewValue`, `e.X`, ... - the handler's own EventArgs parameter.
        if (target.TryResolveEventArgsMember(memberAccess.Expression, memberAccess.Name.Identifier.ValueText, out var argsMember))
        {
            text = argsMember;
            return true;
        }

        // `text.Length` on a local. Same argument: a Value local can only hold what a
        // translatable expression produced, which is always a plain .NET value.
        if (memberAccess.Expression is IdentifierNameSyntax { Identifier.ValueText: var localName }
            && target.Locals.TryGet(localName, out var localKind)
            && localKind == LocalKind.Value)
        {
            text = $"{localName}.{memberAccess.Name}";
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

    /// <summary>What a translated local variable holds, which is what decides how it may be used.</summary>
    private enum LocalKind
    {
        /// <summary>
        /// A plain .NET value. Safe to take members of: a translatable initializer can only be
        /// built from literals, catalog properties and safe BCL statics, so its result is always
        /// a BCL value - the same argument that allows members of a control property.
        /// </summary>
        Value,

        /// <summary>A converted Form's View. Only the navigation calls are allowed on it.</summary>
        FormView,
    }

    /// <summary>
    /// The locals in scope while one body is translated. Block-scoped, so a declaration inside an
    /// `if` branch cannot leak past it - matching C#, and matching the fact that a branch is
    /// translated all-or-nothing.
    /// </summary>
    private sealed class LocalScope
    {
        private readonly List<Dictionary<string, LocalKind>> _scopes = [new(StringComparer.Ordinal)];

        public void Push() => _scopes.Add(new Dictionary<string, LocalKind>(StringComparer.Ordinal));

        public void Pop() => _scopes.RemoveAt(_scopes.Count - 1);

        public void Declare(string name, LocalKind kind) => _scopes[^1][name] = kind;

        public bool TryGet(string name, out LocalKind kind)
        {
            for (var i = _scopes.Count - 1; i >= 0; i--)
            {
                if (_scopes[i].TryGetValue(name, out kind))
                {
                    return true;
                }
            }

            kind = default;
            return false;
        }
    }

    /// <summary>
    /// What the translation so far has obliged the generated method to do. Statements report this
    /// through <see cref="RewrittenStatement"/>; an *expression* has no such channel, so a
    /// translation like the dialog-result comparison - which introduces an `await` from inside a
    /// condition - records it here instead.
    /// </summary>
    /// <remarks>
    /// Mutable and scoped to a single Rewrite call. The top-level loop snapshots it before each
    /// statement and restores it if that statement turns out to be untranslatable, so a partially
    /// rewritten expression cannot leave the method marked `async` with nothing to await.
    /// </remarks>
    private sealed class RewriteRequirements
    {
        public bool RequiresAsync { get; set; }

        public HashSet<string> RequiredUsings { get; } = new(StringComparer.Ordinal);

        /// <summary>Dialog fields a body opened inline, so no separate method is generated for them.</summary>
        public HashSet<string> InlinedDialogFields { get; } = new(StringComparer.Ordinal);

        public (bool Async, string[] Usings, string[] Dialogs) Snapshot() =>
            (RequiresAsync, [.. RequiredUsings], [.. InlinedDialogFields]);

        public void Restore((bool Async, string[] Usings, string[] Dialogs) snapshot)
        {
            RequiresAsync = snapshot.Async;
            RequiredUsings.Clear();
            RequiredUsings.UnionWith(snapshot.Usings);
            InlinedDialogFields.Clear();
            InlinedDialogFields.UnionWith(snapshot.Dialogs);
        }
    }

    /// <summary>Where the rewritten body is going to live, and what a control property means there.</summary>
    private interface IRewriteTarget
    {
        /// <summary>Locals declared so far. Mutable, and scoped to a single Rewrite call.</summary>
        LocalScope Locals { get; }

        /// <summary>What expression-level translations have obliged the method to do.</summary>
        RewriteRequirements Requirements { get; }

        /// <summary>
        /// While an inlined file-dialog branch is being translated, what that dialog's path
        /// property now reads as. Scoped to the branch, like the pattern variable it names.
        /// </summary>
        Dictionary<string, string> DialogSelections { get; }

        /// <summary>False in a ViewModel, which has no Window to close and no dialog owner.</summary>
        bool AllowsWindowApis { get; }

        /// <summary>False in a converted UserControl, which cannot own a modal dialog.</summary>
        bool HostIsWindow { get; }

        /// <summary>Resolves a WinForms Form type name to the View this run generates for it.</summary>
        bool TryResolveFormView(string winFormsTypeName, out FormViewInfo view);

        /// <summary>`e.X` and friends, rewritten against the Avalonia args type. False in a ViewModel.</summary>
        bool TryResolveEventArgsMember(ExpressionSyntax receiver, string memberName, out string text);

        bool TryResolveControlField(ExpressionSyntax expression, out string fieldName);

        /// <param name="forWrite">True for an assignment target, where the expression must stay assignable.</param>
        bool TryResolveProperty(string fieldName, string winFormsPropertyName, bool forWrite, out string text);

        /// <summary>A zero-argument control method with an exact Avalonia equivalent.</summary>
        bool TryResolveControlMethod(string fieldName, string methodName, out string statement);

        /// <summary>The field's file-dialog kind, when it is one this converter can open inline.</summary>
        bool TryResolveFileDialog(string fieldName, out FileDialogKind kind);
    }

    private sealed class ViewTarget(
        FormModel formModel,
        ControlMappingRegistry controlMappings,
        ViewNavigationContext navigation,
        HandlerSignature signature) : IRewriteTarget
    {
        public LocalScope Locals { get; } = new();

        public RewriteRequirements Requirements { get; } = new();

        public Dictionary<string, string> DialogSelections { get; } = new(StringComparer.Ordinal);

        /// <summary>
        /// The generated method keeps the original parameter name, so a translated member reads
        /// naturally against the same `e` the developer wrote.
        /// </summary>
        public bool TryResolveEventArgsMember(ExpressionSyntax receiver, string memberName, out string text)
        {
            text = "";

            if (signature.EventArgsParameterName is not { } parameterName
                || receiver is not IdentifierNameSyntax { Identifier.ValueText: var receiverName }
                || receiverName != parameterName
                || !EventArgsMemberCatalog.TryGet(signature.EventArgsTypeName, memberName, out var member))
            {
                return false;
            }

            // A translation that needs the raising control is only possible when there is exactly
            // one - a shared handler has no single answer.
            if (member.NeedsSourceControl && signature.SourceControlFieldName is null)
            {
                return false;
            }

            text = string.Format(member.Format, parameterName, signature.SourceControlFieldName);
            return true;
        }

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
                || !BindablePropertyCatalog.TryGet(control.ClrTypeName, winFormsPropertyName, out var bindable)
                || !IsReachable(control, bindable.AvaloniaPropertyName))
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

        public bool TryResolveFileDialog(string fieldName, out FileDialogKind kind)
        {
            kind = null!;

            return formModel.Controls.TryGetValue(fieldName, out var control)
                && FileDialogCatalog.TryGet(control.ClrTypeName, out kind);
        }

        public bool TryResolveControlMethod(string fieldName, string methodName, out string statement)
        {
            statement = "";

            if (!formModel.Controls.TryGetValue(fieldName, out var control)
                || !ControlMethodCatalog.TryGet(control.ClrTypeName, methodName, out var method)
                || !IsReachable(control, methodName))
            {
                return false;
            }

            statement = string.Format(method.StatementFormat, fieldName);
            return true;
        }

        /// <summary>
        /// A Direct-mapped element is the Avalonia control itself, so the catalog's answer is
        /// authoritative. A Fallback element is one of this repo's own templates, and only the
        /// members it demonstrably exposes are safe to touch.
        /// </summary>
        private bool IsReachable(ControlModel control, string avaloniaMemberName)
        {
            var mapped = controlMappings.Map(control);

            return mapped.Status == MappingStatus.Direct
                || (mapped.Status == MappingStatus.Fallback
                    && FallbackControlMemberSupport.Exposes(mapped.FallbackTemplateKey, avaloniaMemberName));
        }
    }

    private sealed class ViewModelTarget(
        IReadOnlyList<BoundPropertyPlan> boundProperties, FormModel formModel) : IRewriteTarget
    {
        public LocalScope Locals { get; } = new();

        public RewriteRequirements Requirements { get; } = new();

        public Dictionary<string, string> DialogSelections { get; } = new(StringComparer.Ordinal);

        public bool AllowsWindowApis => false;

        public bool HostIsWindow => false;

        /// <summary>A ViewModel has no business constructing Views; navigation needs a service.</summary>
        public bool TryResolveFormView(string winFormsTypeName, out FormViewInfo view)
        {
            view = null!;
            return false;
        }

        /// <summary>There is no control here to call anything on.</summary>
        public bool TryResolveControlMethod(string fieldName, string methodName, out string statement)
        {
            statement = "";
            return false;
        }

        /// <summary>StorageProvider hangs off the TopLevel, which a ViewModel is not.</summary>
        public bool TryResolveFileDialog(string fieldName, out FileDialogKind kind)
        {
            kind = null!;
            return false;
        }

        /// <summary>A promoted handler cannot use EventArgs at all - that is one of the promotion conditions.</summary>
        public bool TryResolveEventArgsMember(ExpressionSyntax receiver, string memberName, out string text)
        {
            text = "";
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
