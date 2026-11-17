using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WinFormsToAvalonia.Core.Model;

namespace WinFormsToAvalonia.Core.Pipeline;

/// <summary>
/// Names <em>why</em> a handler stopped migrating, from the first statement of its un-migrated
/// remainder: the measure that says which missing translation would carry the most code next.
/// </summary>
/// <remarks>
/// <para>
/// Deliberately after the fact and purely syntactic. The rewriter refuses in hundreds of places,
/// and threading a reason out of each would put a second opinion beside every one of them; the
/// prefix rule already guarantees the remainder's first statement is the one that was refused, so
/// classifying that statement answers the same question without touching the rewriter.
/// </para>
/// <para>
/// The answer is a bucket for a histogram, not a diagnosis: <c>"Graphics: DrawCurve"</c> says where
/// to look, not what exactly the rewriter disliked. The categories are checked most-specific first,
/// so a <c>MessageBox.Show(comboBox1.Text)</c> counts as a dialog rather than a control read.
/// </para>
/// </remarks>
public static class MigrationBlockerClassifier
{
    private static readonly HashSet<string> GraphicsTypeNames = new(StringComparer.Ordinal)
    {
        "Graphics", "Pen", "Pens", "Brush", "Brushes", "SolidBrush", "LinearGradientBrush", "HatchBrush",
        "TextureBrush", "GraphicsPath", "Region", "Matrix", "StringFormat", "Font", "FontFamily", "Bitmap",
        "Image", "Icon", "TextRenderer", "ControlPaint", "SystemBrushes", "SystemPens", "DashStyle", "SmoothingMode",
    };

    private static readonly HashSet<string> DialogNames = new(StringComparer.Ordinal)
    {
        "MessageBox", "ShowDialog", "Show", "ShowAsync", "InputBox",
    };

    /// <summary>
    /// The bucket for <paramref name="remainingBody"/>'s first statement, or null when there is none
    /// (the remainder is only comments or trivia).
    /// </summary>
    /// <param name="controls">The Form's controls by field name, to tell a control member from any other.</param>
    /// <param name="controlTypeNames">Every WinForms type the run can map - what a runtime <c>new</c> creates.</param>
    public static string? Classify(
        string remainingBody, IReadOnlyDictionary<string, ControlModel> controls, IReadOnlySet<string> controlTypeNames)
    {
        if (SyntaxFactory.ParseStatement("{\n" + remainingBody + "\n}") is not BlockSyntax { Statements: [var first, ..] })
        {
            return null;
        }

        var nodes = first.DescendantNodesAndSelf(n => n is not (AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax)).ToList();
        var invocations = nodes.OfType<InvocationExpressionSyntax>().ToList();

        // Dialogs before anything they mention - their arguments are often control reads, and the
        // dialog is often itself created on the spot (`new Form2().ShowDialog()`).
        foreach (var invocation in invocations)
        {
            var name = InvokedName(invocation);
            var receiver = invocation.Expression is MemberAccessExpressionSyntax access ? LastIdentifier(access.Expression) : null;
            if (receiver == "MessageBox" || (name is not null && DialogNames.Contains(name) && receiver != "MessageBox"))
            {
                return receiver == "MessageBox" ? $"Dialog: MessageBox.{name}" : $"Dialog: {name}";
            }
        }

        // A control the original creates at run time - the designer never saw it.
        foreach (var creation in nodes.OfType<ObjectCreationExpressionSyntax>())
        {
            if (LastIdentifier(creation.Type) is { } created && controlTypeNames.Contains(created))
            {
                return $"Runtime control creation: new {created}";
            }
        }

        foreach (var invocation in invocations)
        {
            if (invocation.Expression is MemberAccessExpressionSyntax
                {
                    Name.Identifier.ValueText: "Add" or "Remove" or "Clear" or "AddRange" or "RemoveAt",
                    Expression: var collection,
                } controlsCall
                && LastIdentifier(collection) == "Controls")
            {
                return $"Runtime control creation: Controls.{controlsCall.Name.Identifier.ValueText}";
            }
        }

        // Drawing code: anything that names a GDI+ type or a Graphics member.
        foreach (var node in nodes)
        {
            if (node is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Graphics" } graphicsAccess
                && graphicsAccess.Parent is MemberAccessExpressionSyntax { Name: var drawCall })
            {
                return $"Graphics: {drawCall.Identifier.ValueText}";
            }

            // As a type or a static receiver - not as a member name: `label1.Font` is a control read.
            if (node is IdentifierNameSyntax { Identifier.ValueText: var typeName }
                && GraphicsTypeNames.Contains(typeName)
                && !(node.Parent is MemberAccessExpressionSyntax memberParent && memberParent.Name == node))
            {
                return $"Graphics: {typeName}";
            }
        }

        foreach (var node in nodes.OfType<MemberAccessExpressionSyntax>())
        {
            if (node.Name.Identifier.ValueText is "Items" or "SelectedItems" or "SelectedIndices" or "CheckedItems" or "Nodes"
                && node.Parent is MemberAccessExpressionSyntax { Name.Identifier.ValueText: var itemsMember } parent
                && parent.Expression == node)
            {
                return $"Items collection: {node.Name.Identifier.ValueText}.{itemsMember}";
            }
        }

        foreach (var node in nodes)
        {
            switch (node)
            {
                case CastExpressionSyntax cast:
                    return $"Cast: ({LastIdentifier(cast.Type) ?? cast.Type.ToString()})";
                case BinaryExpressionSyntax { RawKind: (int)SyntaxKind.AsExpression or (int)SyntaxKind.IsExpression } test:
                    return $"Type test: {test.OperatorToken.ValueText} {LastIdentifier(test.Right) ?? test.Right.ToString()}";
                case IsPatternExpressionSyntax { Pattern: DeclarationPatternSyntax or TypePatternSyntax or RecursivePatternSyntax } pattern:
                    return $"Type test: is {PatternTypeName(pattern.Pattern)}";
            }
        }

        // A member of one of the Form's controls - most specific first: the outermost access on a field.
        foreach (var access in nodes.OfType<MemberAccessExpressionSyntax>())
        {
            if (ControlReceiver(access.Expression) is { } field
                && controls.TryGetValue(field, out var control))
            {
                return $"Control member: {control.ClrTypeName}.{access.Name.Identifier.ValueText}";
            }
        }

        // A call nothing translated - most often a helper of the Form's own that did not translate whole.
        foreach (var invocation in invocations)
        {
            switch (invocation.Expression)
            {
                case IdentifierNameSyntax { Identifier.ValueText: var helper }:
                    return $"Call: {helper}()";
                case MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax, Name.Identifier.ValueText: var thisHelper }:
                    return $"Call: {thisHelper}()";
                case MemberAccessExpressionSyntax { Name.Identifier.ValueText: var method } call:
                    return $"Call: {LastIdentifier(call.Expression) ?? "(expression)"}.{method}()";
            }
        }

        foreach (var access in nodes.OfType<MemberAccessExpressionSyntax>())
        {
            if (access.Expression is ThisExpressionSyntax)
            {
                return $"Form member: {access.Name.Identifier.ValueText}";
            }
        }

        // Last before giving up: the first member read off a plain name - a field of a type the
        // conversion does not know (`label.Text` on a ToolStripStatusLabel created at run time),
        // or an event-args member with no Avalonia counterpart (`e.Cancel`).
        foreach (var access in nodes.OfType<MemberAccessExpressionSyntax>())
        {
            if (access.Expression is IdentifierNameSyntax { Identifier.ValueText: var receiver })
            {
                return $"Member: {receiver}.{access.Name.Identifier.ValueText}";
            }
        }

        return $"Statement: {StatementKind(first)}";
    }

    /// <summary>The <paramref name="classify"/> buckets of several remainders, counted and ordered most frequent first.</summary>
    public static IReadOnlyList<KeyValuePair<string, int>> Histogram(IEnumerable<string> buckets) =>
        [.. buckets
            .GroupBy(b => b, StringComparer.Ordinal)
            .Select(g => new KeyValuePair<string, int>(g.Key, g.Count()))
            .OrderByDescending(p => p.Value)
            .ThenBy(p => p.Key, StringComparer.Ordinal)];

    private static string? ControlReceiver(ExpressionSyntax expression) => expression switch
    {
        IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
        MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax, Name: var name } => name.Identifier.ValueText,
        _ => null,
    };

    private static string? InvokedName(InvocationExpressionSyntax invocation) => invocation.Expression switch
    {
        IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
        MemberAccessExpressionSyntax access => access.Name.Identifier.ValueText,
        _ => null,
    };

    private static string? LastIdentifier(SyntaxNode node) => node switch
    {
        IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
        GenericNameSyntax generic => generic.Identifier.ValueText,
        QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
        AliasQualifiedNameSyntax alias => alias.Name.Identifier.ValueText,
        MemberAccessExpressionSyntax access => access.Name.Identifier.ValueText,
        _ => null,
    };

    private static string PatternTypeName(PatternSyntax pattern) => pattern switch
    {
        DeclarationPatternSyntax declaration => LastIdentifier(declaration.Type) ?? declaration.Type.ToString(),
        TypePatternSyntax type => LastIdentifier(type.Type) ?? type.Type.ToString(),
        RecursivePatternSyntax { Type: { } type } => LastIdentifier(type) ?? type.ToString(),
        _ => "pattern",
    };

    private static string StatementKind(StatementSyntax statement) => statement switch
    {
        LocalDeclarationStatementSyntax => "local declaration",
        ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax } => "assignment",
        ExpressionStatementSyntax => "expression",
        IfStatementSyntax => "if",
        ForEachStatementSyntax => "foreach",
        ForStatementSyntax => "for",
        WhileStatementSyntax or DoStatementSyntax => "loop",
        SwitchStatementSyntax => "switch",
        TryStatementSyntax => "try",
        UsingStatementSyntax => "using",
        ReturnStatementSyntax => "return",
        ThrowStatementSyntax => "throw",
        _ => statement.Kind().ToString(),
    };
}
