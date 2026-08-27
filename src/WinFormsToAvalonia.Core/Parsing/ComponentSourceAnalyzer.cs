using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WinFormsToAvalonia.Core.Model;

namespace WinFormsToAvalonia.Core.Parsing;

/// <param name="Files">The component's source files, already rewritten into the target namespace.</param>
/// <param name="Events">
/// The component's own events and the args type each carries, for the ones whose shape a
/// generated handler can be declared against. Read from the component's own declarations, since
/// nothing else knows them - an in-box component gets this from <c>ComponentFieldCatalog</c>.
/// </param>
public sealed record CarriedOverComponent(
    string ClassName,
    string TargetNamespace,
    IReadOnlyList<(string RelativePath, string Text)> Files,
    IReadOnlyDictionary<string, string> Events);

/// <summary>
/// Decides whether a <c>Component</c> the source project defines can be carried into the
/// generated project as-is.
/// </summary>
/// <remarks>
/// <para>
/// The same observation that produced <c>ComponentFieldCatalog</c>, applied to the user's own
/// class: a component deriving from <c>System.ComponentModel.Component</c> and touching no
/// WinForms type is plain .NET, and plain .NET compiles unchanged in an Avalonia project. So the
/// source is copied and the component gets a real field, instead of the whole artifact kind being
/// dropped with guidance.
/// </para>
/// <para>
/// The test is syntactic and deliberately over-rejects, because the cost is asymmetric: a
/// component wrongly refused is reported and left alone, exactly as before this existed, while one
/// wrongly accepted breaks the generated build - and this converter has no semantic model to be
/// sure with. Every simple name the file mentions is checked, not just the ones in type position:
/// a local called <c>Timer</c> will refuse the component, which is a false negative nobody is
/// harmed by.
/// </para>
/// </remarks>
public static class ComponentSourceAnalyzer
{
    public static bool TryCarryOver(
        DesignerFilePairing pairing,
        string targetNamespace,
        IReadOnlySet<string> winFormsTypeNames,
        IReadOnlySet<string> otherProjectClassNames,
        out CarriedOverComponent component,
        out string reason)
    {
        component = null!;
        reason = "";

        var sourcePaths = new[] { pairing.PrimaryFilePath, pairing.DesignerFilePath }
            .OfType<string>()
            .Where(File.Exists)
            .ToList();

        if (sourcePaths.Count == 0)
        {
            reason = "its source file was not found";
            return false;
        }

        var files = new List<(string RelativePath, string Text)>();
        var events = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var path in sourcePaths)
        {
            var root = CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path).GetRoot();

            if (root.ContainsDiagnostics)
            {
                reason = $"'{Path.GetFileName(path)}' did not parse cleanly";
                return false;
            }

            if (FindDisqualifyingName(root, pairing.ClassName, winFormsTypeNames, otherProjectClassNames) is { } offender)
            {
                reason = $"it mentions '{offender}', which does not come across";
                return false;
            }

            files.Add(($"Components/{Path.GetFileName(path)}", RewriteNamespace(root, targetNamespace)));

            foreach (var (eventName, argsTypeName) in DeclaredEvents(root))
            {
                events[eventName] = argsTypeName;
            }
        }

        component = new CarriedOverComponent(pairing.ClassName, targetNamespace, files, events);
        return true;
    }

    /// <summary>
    /// The first name in the file that this converter cannot promise survives: a WinForms
    /// namespace, a WinForms control type, or another class of this project that is not itself
    /// carried over.
    /// </summary>
    private static string? FindDisqualifyingName(
        SyntaxNode root,
        string ownClassName,
        IReadOnlySet<string> winFormsTypeNames,
        IReadOnlySet<string> otherProjectClassNames)
    {
        foreach (var directive in root.DescendantNodes().OfType<UsingDirectiveSyntax>())
        {
            if (directive.Name?.ToString().StartsWith("System.Windows.Forms", StringComparison.Ordinal) == true)
            {
                return directive.Name.ToString();
            }
        }

        foreach (var qualified in root.DescendantNodes().OfType<QualifiedNameSyntax>())
        {
            if (qualified.ToString().StartsWith("System.Windows.Forms", StringComparison.Ordinal))
            {
                return qualified.ToString();
            }
        }

        foreach (var identifier in root.DescendantNodes().OfType<IdentifierNameSyntax>())
        {
            var name = identifier.Identifier.ValueText;
            if (name == ownClassName)
            {
                continue;
            }

            if (winFormsTypeNames.Contains(name) || otherProjectClassNames.Contains(name))
            {
                return name;
            }
        }

        return null;
    }

    /// <summary>
    /// The component's events whose handler signature this converter can write down.
    /// </summary>
    /// <remarks>
    /// Only <c>EventHandler</c> and <c>EventHandler&lt;T&gt;</c>. A custom delegate would need its
    /// own signature read out, and a generated handler declared against the wrong one is a compile
    /// error in the generated project - so it is left unsubscribed instead, which is what happened
    /// to every component event before any of this existed.
    /// </remarks>
    private static IEnumerable<(string EventName, string ArgsTypeName)> DeclaredEvents(SyntaxNode root)
    {
        foreach (var declaration in root.DescendantNodes().OfType<EventFieldDeclarationSyntax>())
        {
            var type = declaration.Declaration.Type is NullableTypeSyntax nullable ? nullable.ElementType : declaration.Declaration.Type;

            var argsTypeName = type switch
            {
                IdentifierNameSyntax { Identifier.ValueText: "EventHandler" } => "EventArgs",
                GenericNameSyntax { Identifier.ValueText: "EventHandler", TypeArgumentList.Arguments: [var argument] } =>
                    RoslynTypeNameHelper.GetSimpleTypeName(argument),
                _ => null,
            };

            if (argsTypeName is null)
            {
                continue;
            }

            foreach (var variable in declaration.Declaration.Variables)
            {
                yield return (variable.Identifier.ValueText, argsTypeName);
            }
        }
    }

    /// <summary>
    /// Points the file at the generated project's namespace. Handles both the file-scoped and the
    /// block-scoped form, since a WinForms project of any age can contain either.
    /// </summary>
    private static string RewriteNamespace(SyntaxNode root, string targetNamespace)
    {
        var declaration = root.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault();
        if (declaration is null)
        {
            return root.ToFullString();
        }

        var renamed = declaration.WithName(
            SyntaxFactory.ParseName(targetNamespace).WithTriviaFrom(declaration.Name));

        return root.ReplaceNode(declaration, renamed).ToFullString();
    }
}
