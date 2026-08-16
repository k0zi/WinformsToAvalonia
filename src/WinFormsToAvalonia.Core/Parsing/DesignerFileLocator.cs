using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WinFormsToAvalonia.Core.Model;

namespace WinFormsToAvalonia.Core.Parsing;

/// <summary>
/// Pairs Foo.cs + Foo.Designer.cs (+ Foo.resx) by filename convention and classifies each
/// group as Form / UserControl / Component / Other.
/// </summary>
/// <remarks>
/// Classification is syntax-only: it looks at the immediate base-list identifiers of the
/// partial class declaration(s), not a fully resolved semantic model. A precise semantic
/// check (resolving transitively through custom intermediate base classes to confirm they
/// ultimately derive from System.Windows.Forms.Form) would require bundling WinForms
/// reference assemblies and compiling against them. Real designer-generated forms almost
/// always declare `: Form` / `: UserControl` / `: Component` directly, so this heuristic
/// covers the overwhelming majority of cases; forms with an intermediate custom base class
/// (`: MyBaseForm`) are classified as Other for manual review. See docs/known-limitations.md.
/// </remarks>
public sealed class DesignerFileLocator
{
    public IReadOnlyList<DesignerFilePairing> Locate(WinFormsProjectModel project)
    {
        var groups = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (var file in project.CompileFiles)
        {
            var fileName = Path.GetFileName(file);
            var isDesigner = fileName.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase);
            var baseName = isDesigner
                ? fileName[..^".Designer.cs".Length]
                : fileName[..^".cs".Length];

            var key = Path.Combine(Path.GetDirectoryName(file)!, baseName);
            if (!groups.TryGetValue(key, out var list))
            {
                list = [];
                groups[key] = list;
            }

            list.Add(file);
        }

        var results = new List<DesignerFilePairing>();

        foreach (var (key, files) in groups)
        {
            var expectedClassName = Path.GetFileName(key);
            var pairing = ClassifyGroup(expectedClassName, files);
            if (pairing is not null)
            {
                results.Add(pairing);
            }
        }

        return results
            .OrderBy(p => p.Namespace, StringComparer.Ordinal)
            .ThenBy(p => p.ClassName, StringComparer.Ordinal)
            .ToList();
    }

    private static DesignerFilePairing? ClassifyGroup(string expectedClassName, IReadOnlyList<string> files)
    {
        string? primaryFile = null;
        string? designerFile = null;
        string? @namespace = null;
        var baseIdentifiers = new HashSet<string>(StringComparer.Ordinal);
        var foundClass = false;

        foreach (var file in files)
        {
            var isDesigner = Path.GetFileName(file).EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase);
            if (isDesigner)
            {
                designerFile = file;
            }
            else
            {
                primaryFile = file;
            }

            var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(file), path: file);

            foreach (var classDecl in tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                if (classDecl.Identifier.ValueText != expectedClassName)
                {
                    continue;
                }

                foundClass = true;
                @namespace ??= GetNamespace(classDecl);

                if (classDecl.BaseList is not null)
                {
                    foreach (var baseType in classDecl.BaseList.Types)
                    {
                        baseIdentifiers.Add(RoslynTypeNameHelper.GetSimpleTypeName(baseType.Type));
                    }
                }
            }
        }

        if (!foundClass)
        {
            return null;
        }

        var resxFile = FindSiblingResx(primaryFile ?? designerFile!, expectedClassName);
        var kind = ClassifyKind(baseIdentifiers);

        return new DesignerFilePairing(expectedClassName, @namespace, kind, primaryFile, designerFile, resxFile);
    }

    private static WinFormsArtifactKind ClassifyKind(HashSet<string> baseIdentifiers)
    {
        if (baseIdentifiers.Contains("Form"))
        {
            return WinFormsArtifactKind.Form;
        }

        if (baseIdentifiers.Contains("UserControl"))
        {
            return WinFormsArtifactKind.UserControl;
        }

        if (baseIdentifiers.Contains("Component"))
        {
            return WinFormsArtifactKind.Component;
        }

        return WinFormsArtifactKind.Other;
    }

    private static string? FindSiblingResx(string anchorFile, string className)
    {
        var directory = Path.GetDirectoryName(anchorFile)!;
        var candidate = Path.Combine(directory, className + ".resx");
        return File.Exists(candidate) ? candidate : null;
    }

    private static string? GetNamespace(SyntaxNode node)
    {
        for (var current = node.Parent; current is not null; current = current.Parent)
        {
            switch (current)
            {
                case NamespaceDeclarationSyntax namespaceDecl:
                    return namespaceDecl.Name.ToString();
                case FileScopedNamespaceDeclarationSyntax fileScopedNamespaceDecl:
                    return fileScopedNamespaceDecl.Name.ToString();
            }
        }

        return null;
    }
}
