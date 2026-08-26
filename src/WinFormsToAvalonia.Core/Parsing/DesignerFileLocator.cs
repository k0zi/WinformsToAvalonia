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
/// Classification is syntax-only - no compilation, no semantic model - matching
/// <see cref="DesignerSyntaxWalker"/>'s and <see cref="CodeBehindAnalyzer"/>'s approach, and
/// for the same reason: a semantic check would mean resolving the source project's references
/// (WinForms reference assemblies included), which would change what this tool needs installed
/// to run at all.
///
/// A class whose base list names <c>Form</c>/<c>UserControl</c>/<c>Component</c> directly is
/// classified from that alone. Otherwise the base list is followed *transitively* through the
/// other classes this project declares, so the common `MyBaseForm : Form` intermediate is
/// resolved rather than skipped. What syntax alone still cannot see is a base class defined in
/// a *referenced assembly* - that stays <see cref="WinFormsArtifactKind.Other"/>, but records
/// the unresolved names in <see cref="DesignerFilePairing.UnresolvedBaseTypes"/> so the
/// conversion reports it instead of dropping the artifact silently.
/// </remarks>
public sealed class DesignerFileLocator
{
    public IReadOnlyList<DesignerFilePairing> Locate(WinFormsProjectModel project)
    {
        // Parse every compile file exactly once: the same trees back both the per-group
        // classification below and the project-wide base-type map the transitive walk needs.
        var treesByFile = new Dictionary<string, SyntaxTree>(StringComparer.Ordinal);
        foreach (var file in project.CompileFiles)
        {
            if (!treesByFile.ContainsKey(file) && File.Exists(file))
            {
                treesByFile[file] = CSharpSyntaxTree.ParseText(File.ReadAllText(file), path: file);
            }
        }

        var baseTypesByClassName = BuildBaseTypeMap(treesByFile.Values);

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
            var pairing = ClassifyGroup(expectedClassName, files, treesByFile, baseTypesByClassName);
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

    /// <summary>
    /// Every class this project declares, mapped to its own base-list names - the graph the
    /// transitive walk follows.
    /// </summary>
    /// <remarks>
    /// Keyed by *simple* name, since that is all the base list gives us without a semantic
    /// model. Two same-named classes in different namespaces therefore merge their base sets;
    /// the effect is that such a name can only ever resolve to *more* base types than it really
    /// has, which at worst converts an artifact that should have been left alone - reported as
    /// a normal conversion, never a silent failure.
    /// </remarks>
    private static Dictionary<string, HashSet<string>> BuildBaseTypeMap(IEnumerable<SyntaxTree> trees)
    {
        var map = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var tree in trees)
        {
            foreach (var classDecl in tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                if (classDecl.BaseList is null)
                {
                    continue;
                }

                if (!map.TryGetValue(classDecl.Identifier.ValueText, out var baseTypes))
                {
                    baseTypes = new HashSet<string>(StringComparer.Ordinal);
                    map[classDecl.Identifier.ValueText] = baseTypes;
                }

                foreach (var baseType in classDecl.BaseList.Types)
                {
                    baseTypes.Add(RoslynTypeNameHelper.GetSimpleTypeName(baseType.Type));
                }
            }
        }

        return map;
    }

    private static DesignerFilePairing? ClassifyGroup(
        string expectedClassName,
        IReadOnlyList<string> files,
        IReadOnlyDictionary<string, SyntaxTree> treesByFile,
        IReadOnlyDictionary<string, HashSet<string>> baseTypesByClassName)
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

            if (!treesByFile.TryGetValue(file, out var tree))
            {
                continue;
            }

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
        var kind = ClassifyKind(baseIdentifiers, baseTypesByClassName, out var unresolvedBaseTypes);

        // Only worth reporting for something that looks like a designer artifact: a group with a
        // .Designer.cs that still didn't classify is a UI type this run is about to skip. Every
        // other Other-kind class (Program, a helper, a plain model) is Other on purpose.
        var reportableUnresolved = kind == WinFormsArtifactKind.Other && designerFile is not null
            ? unresolvedBaseTypes
            : [];

        return new DesignerFilePairing(
            expectedClassName, @namespace, kind, primaryFile, designerFile, resxFile, reportableUnresolved);
    }

    private static WinFormsArtifactKind ClassifyKind(
        HashSet<string> baseIdentifiers,
        IReadOnlyDictionary<string, HashSet<string>> baseTypesByClassName,
        out IReadOnlyList<string> unresolvedBaseTypes)
    {
        unresolvedBaseTypes = [];

        // Direct hit first, so nothing that already classified before the transitive walk
        // existed can change its answer because of graph traversal order.
        var direct = ClassifyDirect(baseIdentifiers);
        if (direct != WinFormsArtifactKind.Other)
        {
            return direct;
        }

        var visited = new HashSet<string>(StringComparer.Ordinal);
        var pending = new Queue<string>(baseIdentifiers);
        var unresolved = new List<string>();

        while (pending.Count > 0)
        {
            var name = pending.Dequeue();

            // Also the cycle guard: illegal in C#, but this walker runs on unresolved syntax
            // and must terminate on anything it is handed.
            if (!visited.Add(name))
            {
                continue;
            }

            if (!baseTypesByClassName.TryGetValue(name, out var parents))
            {
                unresolved.Add(name);
                continue;
            }

            var resolved = ClassifyDirect(parents);
            if (resolved != WinFormsArtifactKind.Other)
            {
                return resolved;
            }

            foreach (var parent in parents)
            {
                pending.Enqueue(parent);
            }
        }

        unresolvedBaseTypes = unresolved.OrderBy(n => n, StringComparer.Ordinal).ToList();
        return WinFormsArtifactKind.Other;
    }

    private static WinFormsArtifactKind ClassifyDirect(IReadOnlyCollection<string> baseIdentifiers)
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
