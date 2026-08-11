using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Converter.Plugin.Abstractions;

namespace Converter.Core.Parsing;

/// <summary>
/// WinFormsParser.ParseEventSubscription only sees "+=" event wiring written inside a
/// Designer.cs's InitializeComponent - a very common alternative idiom (subscribing to a
/// form's own lifecycle events in the regular, hand-written constructor instead, e.g. "Load
/// += Form_Load; FormClosing += Form_FormClosing;") is otherwise completely invisible to the
/// pipeline: the handler method still gets migrated (as a CodeBehindMemberExtractor helper
/// method), but with no wiring anywhere in the generated output, it's either silently dead
/// code or - if its signature uses a WinForms-only EventArgs type Designer.cs-driven wiring
/// would have mapped away (e.g. FormClosingEventArgs) - a build error. This scans the
/// non-Designer code-behind's constructor for the same shape and merges any matches into
/// ControlNode.EventHandlers, the exact same dictionary WinFormsParser populates - so every
/// downstream mechanism (AXAML event-attribute wiring, EventSignatureRegistry-correct stub
/// generation, ConvertToCommand RelayCommand generation) picks these up for free.
/// </summary>
public static class CodeBehindEventSubscriptionDetector
{
    /// <summary>
    /// WinForms event names recognized for this detection, kept in sync loosely with
    /// Converter.Mappings.BuiltIn.EventMappingRegistry's key set but duplicated here rather
    /// than referenced, to avoid a Converter.Mappings -> Converter.Core -> Converter.Mappings
    /// project reference cycle (Converter.Mappings already references Converter.Core) - the
    /// same constraint and precedent as WinFormsParser.KnownControlTypeNames. Gating on a
    /// recognized event name (rather than matching any "+=" at all) is what keeps false
    /// positives near-zero: an unrelated "total += x;" on a coincidentally event-named local
    /// won't match anything here.
    /// </summary>
    private static readonly HashSet<string> KnownEventNames = new(StringComparer.Ordinal)
    {
        "Click", "DoubleClick", "MouseDown", "MouseUp", "MouseMove", "MouseEnter", "MouseLeave",
        "MouseWheel", "KeyDown", "KeyUp", "KeyPress", "TextChanged", "SelectedIndexChanged",
        "CheckedChanged", "ValueChanged", "Load", "FormClosing", "FormClosed", "Resize",
        "Paint", "Enter", "Leave", "GotFocus", "LostFocus", "Validating", "Validated",
        "DragEnter", "DragDrop", "DragOver", "DragLeave", "Scroll", "Tick", "NodeClick",
        "CellClick"
    };

    public static async Task MergeConstructorEventSubscriptionsAsync(ControlNode root, string codeBehindPath)
    {
        string source;
        try
        {
            source = await File.ReadAllTextAsync(codeBehindPath);
        }
        catch (IOException)
        {
            return;
        }

        SyntaxNode syntaxRoot;
        try
        {
            syntaxRoot = CSharpSyntaxTree.ParseText(source).GetRoot();
        }
        catch
        {
            return;
        }

        var controlsByName = new Dictionary<string, ControlNode>(StringComparer.Ordinal);
        CollectControlsByName(root, controlsByName);

        // Only the form's own class's constructor(s) - ignores unrelated nested/partial types
        // that might live in the same file.
        var constructors = syntaxRoot.DescendantNodes()
            .OfType<ConstructorDeclarationSyntax>()
            .Where(c => c.Identifier.Text == root.Name);

        foreach (var ctor in constructors)
        {
            if (ctor.Body == null)
            {
                continue;
            }

            foreach (var assignment in ctor.Body.DescendantNodes().OfType<AssignmentExpressionSyntax>())
            {
                if (assignment.IsKind(SyntaxKind.AddAssignmentExpression))
                {
                    TryMergeSubscription(assignment, root, controlsByName);
                }
            }
        }
    }

    private static void TryMergeSubscription(
        AssignmentExpressionSyntax assignment, ControlNode root, IReadOnlyDictionary<string, ControlNode> controlsByName)
    {
        var (target, eventName) = ResolveTarget(assignment.Left, root, controlsByName);
        if (target == null || eventName == null || !KnownEventNames.Contains(eventName))
        {
            return;
        }

        // Designer.cs-driven wiring (if any) always wins over a constructor-discovered guess.
        if (target.EventHandlers.ContainsKey(eventName))
        {
            return;
        }

        var handlerName = ResolveHandlerName(assignment.Right);
        if (!string.IsNullOrEmpty(handlerName))
        {
            target.EventHandlers[eventName] = handlerName;
        }
    }

    private static (ControlNode? Target, string? EventName) ResolveTarget(
        ExpressionSyntax left, ControlNode root, IReadOnlyDictionary<string, ControlNode> controlsByName)
    {
        switch (left)
        {
            // "Load += Form_Load;" - implicit this, the root form itself.
            case IdentifierNameSyntax identifier:
                return (root, identifier.Identifier.Text);

            // "this.Load += Form_Load;" - explicit this.
            case MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax } thisAccess:
                return (root, thisAccess.Name.Identifier.Text);

            // "someControl.Click += Handler;" - a named child control field.
            case MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax controlId } controlAccess
                when controlsByName.TryGetValue(controlId.Identifier.Text, out var control):
                return (control, controlAccess.Name.Identifier.Text);

            default:
                return (null, null);
        }
    }

    /// <summary>
    /// Only the bare method-group cases (mirroring WinFormsParser.GetEventHandlerName's
    /// non-lambda branches) - an inline lambda here would need the same body-registration
    /// treatment WinFormsParser.RegisterInlineLambdaHandler gives Designer.cs lambdas, which
    /// is out of scope for this narrower pass.
    /// </summary>
    private static string? ResolveHandlerName(ExpressionSyntax right) => right switch
    {
        IdentifierNameSyntax handler => handler.Identifier.Text,
        MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax } handler => handler.Name.Identifier.Text,
        _ => null
    };

    private static void CollectControlsByName(ControlNode node, Dictionary<string, ControlNode> map)
    {
        map[node.Name] = node;
        foreach (var child in node.Children)
        {
            CollectControlsByName(child, map);
        }
    }
}
