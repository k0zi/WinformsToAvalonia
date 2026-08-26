using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using WinFormsToAvalonia.Core.Model;

namespace WinFormsToAvalonia.Core.Parsing;

/// <summary>
/// Roslyn-analyzes a Form's non-designer .cs file into a <see cref="CodeBehindModel"/>: which
/// methods are event handlers, what each handler's body actually touches (sender, EventArgs,
/// which control fields and which members on them, Form members, other Forms), which members
/// are ordinary helpers, and what the constructor does beyond InitializeComponent().
/// </summary>
/// <remarks>
/// This is deliberately a *syntactic* analysis - no compilation, no semantic model - matching
/// DesignerSyntaxWalker's approach and keeping the tool free of the need to resolve the source
/// project's references. The consequence is that a local variable shadowing a designer field
/// name would be misattributed to that control; in designer-generated code that shape does not
/// occur in practice, and every classification this feeds is fail-safe (an ambiguous handler
/// stays in code-behind rather than being promoted to a ViewModel command).
/// </remarks>
public sealed class CodeBehindAnalyzer
{
    /// <summary>
    /// Form members whose bare-identifier use (`Close();`, `Hide();`) can't be told apart from
    /// a helper call without a semantic model, but which unambiguously mean "this handler drives
    /// the window itself" - a hard blocker for ViewModel promotion.
    /// </summary>
    private static readonly HashSet<string> WellKnownFormMembers = new(StringComparer.Ordinal)
    {
        "Close", "Hide", "Show", "ShowDialog", "DialogResult", "Activate", "Focus", "Select",
        "Invalidate", "Refresh", "PerformLayout", "SuspendLayout", "ResumeLayout",
        "CreateGraphics", "BeginInvoke", "Invoke", "Controls", "WindowState", "TopMost",
    };

    /// <summary>
    /// True when an identifier names something in its own right, rather than being the member
    /// half of someone else's access.
    /// </summary>
    /// <remarks>
    /// Without this, <c>items.Select(...)</c> would read as a use of the Form's own
    /// <c>Select</c> - <c>DescendantNodes</c> visits the name of a member access as a plain
    /// identifier - and block promotion for a body doing ordinary LINQ.
    /// </remarks>
    private static bool IsStandaloneReference(IdentifierNameSyntax identifier) =>
        identifier.Parent is not MemberAccessExpressionSyntax memberAccess
        || memberAccess.Expression == identifier;

    public CodeBehindModel Analyze(string? primaryFilePath, FormModel formModel)
    {
        if (primaryFilePath is null || !File.Exists(primaryFilePath))
        {
            return CodeBehindModel.Empty(primaryFilePath ?? "");
        }

        return Analyze(File.ReadAllText(primaryFilePath), primaryFilePath, formModel);
    }

    public CodeBehindModel Analyze(string sourceText, string filePath, FormModel formModel)
    {
        var tree = CSharpSyntaxTree.ParseText(sourceText, path: filePath);
        var root = tree.GetRoot();

        var classDeclaration = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault(c => c.Identifier.ValueText == formModel.ClassName);

        if (classDeclaration is null)
        {
            return CodeBehindModel.Empty(filePath);
        }

        var controlFields = new HashSet<string>(formModel.Controls.Keys, StringComparer.Ordinal);
        var declaredMethodNames = classDeclaration.Members
            .OfType<MethodDeclarationSyntax>()
            .Select(m => m.Identifier.ValueText)
            .ToHashSet(StringComparer.Ordinal);

        var runtimeSubscriptions = CollectRuntimeSubscriptions(classDeclaration);
        var handlerNames = CollectDesignerHandlerNames(formModel);
        foreach (var subscription in runtimeSubscriptions)
        {
            handlerNames.Add(subscription.HandlerMethodName);
        }

        var handlers = new List<HandlerMethodModel>();
        var helpers = new List<HelperMemberModel>();
        var constructorExtraStatements = new List<string>();

        foreach (var member in classDeclaration.Members)
        {
            switch (member)
            {
                case ConstructorDeclarationSyntax constructor:
                    constructorExtraStatements.AddRange(ExtractConstructorExtraStatements(constructor));
                    break;

                case MethodDeclarationSyntax method when handlerNames.Contains(method.Identifier.ValueText):
                    handlers.Add(AnalyzeHandler(method, controlFields, declaredMethodNames, handlerNames));
                    break;

                case MethodDeclarationSyntax { Identifier.ValueText: "InitializeComponent" or "Dispose" }:
                    break;

                case MethodDeclarationSyntax method:
                    helpers.Add(new HelperMemberModel(method.Identifier.ValueText, HelperMemberKind.Method, GetSourceTextWithIndent(method)));
                    break;

                case PropertyDeclarationSyntax property:
                    helpers.Add(new HelperMemberModel(property.Identifier.ValueText, HelperMemberKind.Property, GetSourceTextWithIndent(property)));
                    break;

                case FieldDeclarationSyntax field:
                    helpers.Add(new HelperMemberModel(
                        field.Declaration.Variables.FirstOrDefault()?.Identifier.ValueText ?? "",
                        HelperMemberKind.Field,
                        GetSourceTextWithIndent(field)));
                    break;

                default:
                    helpers.Add(new HelperMemberModel("", HelperMemberKind.Other, GetSourceTextWithIndent(member)));
                    break;
            }
        }

        return new CodeBehindModel(filePath, handlers, helpers, constructorExtraStatements, runtimeSubscriptions);
    }

    private static HashSet<string> CollectDesignerHandlerNames(FormModel formModel)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);

        foreach (var binding in formModel.FormEvents)
        {
            if (binding.HandlerMethodName is { } name)
            {
                names.Add(name);
            }
        }

        foreach (var control in formModel.Controls.Values)
        {
            foreach (var binding in control.Events)
            {
                if (binding.HandlerMethodName is { } name)
                {
                    names.Add(name);
                }
            }
        }

        return names;
    }

    /// <summary>
    /// `this.timer.Tick += this.Timer_Tick;` / `button.Click += OnClick;` written by hand in
    /// the code-behind (typically from a Load handler), which InitializeComponent() - and
    /// therefore DesignerSyntaxWalker - never sees.
    /// </summary>
    private static List<RuntimeEventSubscription> CollectRuntimeSubscriptions(ClassDeclarationSyntax classDeclaration)
    {
        var subscriptions = new List<RuntimeEventSubscription>();

        foreach (var assignment in classDeclaration.DescendantNodes().OfType<AssignmentExpressionSyntax>())
        {
            if (!assignment.IsKind(SyntaxKind.AddAssignmentExpression))
            {
                continue;
            }

            if (!TryGetSimpleName(assignment.Right, out var handlerName))
            {
                continue;
            }

            switch (assignment.Left)
            {
                // `this.timer.Tick += ...` / `timer.Tick += ...`
                case MemberAccessExpressionSyntax { Name.Identifier.ValueText: var eventName } target
                    when TryGetFieldReference(target.Expression, out var fieldName):
                    subscriptions.Add(new RuntimeEventSubscription(fieldName, eventName, handlerName));
                    break;

                // `this.Load += ...`
                case MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax, Name.Identifier.ValueText: var formEventName }:
                    subscriptions.Add(new RuntimeEventSubscription(null, formEventName, handlerName));
                    break;
            }
        }

        return subscriptions;
    }

    private static HandlerMethodModel AnalyzeHandler(
        MethodDeclarationSyntax method,
        HashSet<string> controlFields,
        HashSet<string> declaredMethodNames,
        HashSet<string> handlerNames)
    {
        var parameters = method.ParameterList.Parameters;
        var senderParameter = parameters.Count > 0 ? parameters[0].Identifier.ValueText : null;
        var eventArgsParameter = parameters.Count > 1 ? parameters[1].Identifier.ValueText : null;
        var eventArgsTypeName = parameters.Count > 1 && parameters[1].Type is { } type
            ? RoslynTypeNameHelper.GetSimpleTypeName(StripNullable(type))
            : "EventArgs";

        var bodyNode = (SyntaxNode?)method.Body ?? method.ExpressionBody;
        var bodyText = method.Body is { } block
            ? ExtractBlockBodyText(block)
            : method.ExpressionBody is { } arrow
                ? $"{arrow.Expression};"
                : "";

        var usesSender = false;
        var usesEventArgs = false;
        var createsOtherForms = false;
        var callsDialogApis = false;
        var referencedControlFields = new List<string>();
        var seenControlFields = new HashSet<string>(StringComparer.Ordinal);
        var controlMemberAccesses = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var touchedFormMembers = new SortedSet<string>(StringComparer.Ordinal);
        var calledHelperMethods = new SortedSet<string>(StringComparer.Ordinal);

        void NoteControlField(string fieldName)
        {
            if (seenControlFields.Add(fieldName))
            {
                referencedControlFields.Add(fieldName);
            }
        }

        if (bodyNode is not null)
        {
            foreach (var node in bodyNode.DescendantNodes())
            {
                switch (node)
                {
                    case IdentifierNameSyntax identifier:
                    {
                        var name = identifier.Identifier.ValueText;
                        if (name == senderParameter)
                        {
                            usesSender = true;
                        }
                        else if (name == eventArgsParameter)
                        {
                            usesEventArgs = true;
                        }
                        else if (WellKnownFormMembers.Contains(name)
                            && !controlFields.Contains(name)
                            && !declaredMethodNames.Contains(name)
                            && IsStandaloneReference(identifier))
                        {
                            // The bare `DialogResult = DialogResult.OK;` / `WindowState = ...`
                            // designer-era code writes, which `this.`-qualified code reaches
                            // through the member-access arm below. Both drive the window, and a
                            // ViewModel has no window - so both must block promotion, or a
                            // handler whose whole point is closing the dialog would be promoted
                            // to a place where it cannot be translated at all.
                            touchedFormMembers.Add(name);
                        }

                        break;
                    }

                    case MemberAccessExpressionSyntax memberAccess
                        when TryGetFieldReference(memberAccess.Expression, out var fieldName) && controlFields.Contains(fieldName):
                    {
                        NoteControlField(fieldName);
                        if (!controlMemberAccesses.TryGetValue(fieldName, out var members))
                        {
                            members = [];
                            controlMemberAccesses[fieldName] = members;
                        }

                        var memberName = memberAccess.Name.Identifier.ValueText;
                        if (!members.Contains(memberName, StringComparer.Ordinal))
                        {
                            members.Add(memberName);
                        }

                        break;
                    }

                    case MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax, Name.Identifier.ValueText: var thisMember }:
                    {
                        if (controlFields.Contains(thisMember))
                        {
                            NoteControlField(thisMember);
                        }
                        else if (declaredMethodNames.Contains(thisMember) && !handlerNames.Contains(thisMember))
                        {
                            calledHelperMethods.Add(thisMember);
                        }
                        else if (!declaredMethodNames.Contains(thisMember))
                        {
                            touchedFormMembers.Add(thisMember);
                        }

                        break;
                    }

                    case InvocationExpressionSyntax { Expression: IdentifierNameSyntax callee }:
                    {
                        var name = callee.Identifier.ValueText;
                        if (declaredMethodNames.Contains(name) && !handlerNames.Contains(name))
                        {
                            calledHelperMethods.Add(name);
                        }
                        else if (WellKnownFormMembers.Contains(name))
                        {
                            touchedFormMembers.Add(name);
                        }

                        break;
                    }

                    case ObjectCreationExpressionSyntax creation
                        when IsFormOrDialogTypeName(RoslynTypeNameHelper.GetSimpleTypeName(creation.Type)):
                        createsOtherForms = true;
                        break;

                    // `MessageBox.Show(...)`. Recorded separately from CreatesOtherForms because
                    // it is not a Form construction, but it has the same consequence: showing a
                    // dialog needs a TopLevel to own it, which is a View, never a ViewModel.
                    case InvocationExpressionSyntax
                    {
                        Expression: MemberAccessExpressionSyntax
                        {
                            Expression: IdentifierNameSyntax { Identifier.ValueText: "MessageBox" },
                            Name.Identifier.ValueText: "Show",
                        },
                    }:
                        callsDialogApis = true;
                        break;
                }
            }
        }

        // A control field can also be referenced bare (`treeView1.Nodes`), which the member-access
        // arm above already covers, or passed as a plain argument (`errorProvider.SetError(usernameTextBox, ...)`).
        if (bodyNode is not null)
        {
            foreach (var identifier in bodyNode.DescendantNodes().OfType<IdentifierNameSyntax>())
            {
                var name = identifier.Identifier.ValueText;
                if (controlFields.Contains(name))
                {
                    NoteControlField(name);
                }
            }
        }

        return new HandlerMethodModel
        {
            MethodName = method.Identifier.ValueText,
            SenderParameterName = senderParameter,
            EventArgsParameterName = eventArgsParameter,
            EventArgsTypeName = eventArgsTypeName,
            IsAsync = method.Modifiers.Any(m => m.IsKind(SyntaxKind.AsyncKeyword)),
            BodyText = bodyText,
            UsesSender = usesSender,
            UsesEventArgs = usesEventArgs,
            CreatesOtherForms = createsOtherForms,
            CallsDialogApis = callsDialogApis,
            ReferencedControlFields = referencedControlFields,
            ControlMemberAccesses = controlMemberAccesses.ToDictionary(
                kvp => kvp.Key,
                kvp => (IReadOnlyList<string>)kvp.Value,
                StringComparer.Ordinal),
            TouchedFormMembers = [.. touchedFormMembers],
            CalledHelperMethods = [.. calledHelperMethods],
        };
    }

    private static IEnumerable<string> ExtractConstructorExtraStatements(ConstructorDeclarationSyntax constructor)
    {
        if (constructor.Body is null)
        {
            yield break;
        }

        foreach (var statement in constructor.Body.Statements)
        {
            if (statement is ExpressionStatementSyntax
                {
                    Expression: InvocationExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.ValueText: "InitializeComponent" } },
                })
            {
                continue;
            }

            yield return statement.ToString();
        }
    }

    private static bool IsFormOrDialogTypeName(string typeName) =>
        typeName.EndsWith("Form", StringComparison.Ordinal) || typeName.EndsWith("Dialog", StringComparison.Ordinal);

    private static TypeSyntax StripNullable(TypeSyntax type) =>
        type is NullableTypeSyntax nullable ? nullable.ElementType : type;

    /// <summary>Recognizes `this.field` and bare `field` as a reference to the same field name.</summary>
    private static bool TryGetFieldReference(ExpressionSyntax expression, out string fieldName)
    {
        switch (expression)
        {
            case MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax, Name.Identifier.ValueText: var name }:
                fieldName = name;
                return true;
            case IdentifierNameSyntax identifier:
                fieldName = identifier.Identifier.ValueText;
                return true;
            default:
                fieldName = "";
                return false;
        }
    }

    private static bool TryGetSimpleName(ExpressionSyntax expression, out string name)
    {
        switch (expression)
        {
            case MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax, Name.Identifier.ValueText: var memberName }:
                name = memberName;
                return true;
            case IdentifierNameSyntax identifier:
                name = identifier.Identifier.ValueText;
                return true;
            case ObjectCreationExpressionSyntax { ArgumentList.Arguments.Count: 1 } creation:
                return TryGetSimpleName(creation.ArgumentList!.Arguments[0].Expression, out name);
            default:
                name = "";
                return false;
        }
    }

    private static string ExtractBlockBodyText(BlockSyntax block)
    {
        var text = block.SyntaxTree.GetText();
        var span = TextSpan.FromBounds(block.OpenBraceToken.Span.End, block.CloseBraceToken.Span.Start);
        return Dedent(text.ToString(span));
    }

    /// <summary>
    /// Returns the node's source text extended back to the start of its first line when only
    /// whitespace precedes it, so <see cref="Dedent"/> sees a consistent indentation baseline
    /// across every line of the member (a bare ToString() drops the first line's indent only).
    /// </summary>
    private static string GetSourceTextWithIndent(SyntaxNode node)
    {
        var text = node.SyntaxTree.GetText();
        var line = text.Lines.GetLineFromPosition(node.Span.Start);
        var prefix = text.ToString(TextSpan.FromBounds(line.Start, node.Span.Start));
        var start = string.IsNullOrWhiteSpace(prefix) ? line.Start : node.Span.Start;
        return Dedent(text.ToString(TextSpan.FromBounds(start, node.Span.End)));
    }

    /// <summary>Trims blank leading/trailing lines and removes the common leading indentation.</summary>
    private static string Dedent(string text)
    {
        var lines = text.Replace("\r\n", "\n").Split('\n').ToList();

        while (lines.Count > 0 && lines[0].Trim().Length == 0)
        {
            lines.RemoveAt(0);
        }

        while (lines.Count > 0 && lines[^1].Trim().Length == 0)
        {
            lines.RemoveAt(lines.Count - 1);
        }

        if (lines.Count == 0)
        {
            return "";
        }

        var commonIndent = lines
            .Where(l => l.Trim().Length > 0)
            .Select(l => l.Length - l.TrimStart().Length)
            .DefaultIfEmpty(0)
            .Min();

        return string.Join("\n", lines.Select(l => l.Length >= commonIndent ? l[commonIndent..] : l.TrimStart()));
    }
}
