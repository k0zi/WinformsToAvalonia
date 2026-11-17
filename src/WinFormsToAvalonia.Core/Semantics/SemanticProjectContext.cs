using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.RegularExpressions;
using WinFormsToAvalonia.Core.Model;
using WinFormsToAvalonia.Core.Parsing;

namespace WinFormsToAvalonia.Core.Semantics;

/// <summary>A source file of the original project copied into the generated one unchanged.</summary>
/// <param name="RelativePath">Where it lands, relative to the generated project, with '/' separators.</param>
public sealed record CarriedSourceFile(string RelativePath, string Text, string OriginalPath);

/// <summary>A NuGet package of the original project the generated one references too.</summary>
public sealed record CarriedPackage(string Id, string Version);

/// <summary>
/// The original project compiled against the .NET base class library alone - the semantic half of
/// the conversion, which the syntactic stages consult when "could this be a WinForms API?" needs a
/// real answer instead of a refusal.
/// </summary>
/// <remarks>
/// <para>
/// Built once per project, before planning. It answers two questions:
/// </para>
/// <list type="number">
///   <item>Which of the project's <em>other</em> source files - models, services, a cache class, an
///     extension class - are UI-neutral as a whole, and therefore come across into the generated
///     project unchanged (<see cref="CarriedFiles"/>). Before this, everything that was not a Form,
///     UserControl or Component was simply left behind, so a handler calling into it could never
///     translate.</item>
///   <item>For one Form, whether a given field, helper signature or handler statement is UI-neutral
///     (<see cref="ForClass"/>), so it can be emitted verbatim.</item>
/// </list>
/// </remarks>
public sealed class SemanticProjectContext
{
    /// <summary>The generated namespaces a carried type may not live in - they would collide with what the scaffolder writes.</summary>
    private static readonly string[] GeneratedSubNamespaces = ["Views", "ViewModels", "Generated", "Controls", "Models", "Properties"];

    private static readonly string[] GeneratedRootTypeNames = ["App", "Program", "ViewLocator"];

    private const string ImplicitUsingsSource =
        "global using global::System;\n"
        + "global using global::System.Collections.Generic;\n"
        + "global using global::System.IO;\n"
        + "global using global::System.Linq;\n"
        + "global using global::System.Net.Http;\n"
        + "global using global::System.Threading;\n"
        + "global using global::System.Threading.Tasks;\n";

    private readonly HashSet<SyntaxTree> _carriedTrees;

    private SemanticProjectContext(
        CSharpCompilation? compilation,
        HashSet<SyntaxTree> carriedTrees,
        IReadOnlyList<CarriedSourceFile> carriedFiles,
        IReadOnlyList<string> warnings,
        string generatedRootNamespace,
        IReadOnlyList<CarriedPackage> carriedPackages,
        IReadOnlyList<NotCarriedPackage> notCarriedPackages)
    {
        NotCarriedPackages = notCarriedPackages;
        Compilation = compilation;
        _carriedTrees = carriedTrees;
        CarriedFiles = carriedFiles;
        CarriedPackages = carriedPackages;
        Warnings = warnings;
        GeneratedRootNamespace = generatedRootNamespace;
    }

    /// <summary>
    /// The namespaces whose types code passed through verbatim names by their simple name - every
    /// other type is written namespace-qualified. <c>Mapping.Tests</c> checks none of those simple
    /// names can mean something else inside a generated View.
    /// </summary>
    public static IReadOnlySet<string> SimpleNameNamespaces => NeutralityChecker.ImplicitNamespaces;

    /// <summary>No semantic information: every stage behaves exactly as it did before this existed.</summary>
    public static SemanticProjectContext None { get; } = new(null, [], [], [], "", [], []);

    internal CSharpCompilation? Compilation { get; }

    /// <summary>The project's own UI-neutral source files, copied into the generated project.</summary>
    public IReadOnlyList<CarriedSourceFile> CarriedFiles { get; }

    /// <summary>
    /// The NuGet packages the generated project references: the original's own (and what its
    /// meta-packages stand for) that a <c>net10.0</c> project can use and that its code really uses.
    /// </summary>
    public IReadOnlyList<CarriedPackage> CarriedPackages { get; }

    /// <summary>
    /// The declared packages the generated project does not reference, and why - kept apart from
    /// <see cref="Warnings"/> because the conversion may add one of them itself, for a component.
    /// </summary>
    public IReadOnlyList<NotCarriedPackage> NotCarriedPackages { get; }

    /// <summary>The project's other source files that could not come across, and why.</summary>
    public IReadOnlyList<string> Warnings { get; }

    internal string GeneratedRootNamespace { get; }

    internal bool IsCarried(SyntaxTree tree) => _carriedTrees.Contains(tree);

    /// <summary>
    /// The namespaces a generated View's simple names and extension methods resolve in without a
    /// using: the implicit global usings, and the generated root namespace that encloses it.
    /// </summary>
    internal IReadOnlySet<string> GeneratedNamespacesInScope =>
        new HashSet<string>(NeutralityChecker.ImplicitNamespaces.Append(GeneratedRootNamespace), StringComparer.Ordinal);

    /// <param name="artifactTypeNames">
    /// Full names of every type discovery classified (Forms, UserControls, Components, and the
    /// rest it saw a designer for): their files are converted, never copied.
    /// </param>
    /// <param name="packageStore">Where the project's NuGet packages are looked up; none are when null.</param>
    public static SemanticProjectContext Build(
        WinFormsProjectModel project,
        string generatedRootNamespace,
        IReadOnlySet<string> artifactTypeNames,
        NuGetPackageStore? packageStore = null)
    {
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest);
        var trees = new List<SyntaxTree>();

        // Where each file lands if it comes across: the project's own at their own relative path,
        // a referenced class library's under a folder named after it.
        var origins = new Dictionary<SyntaxTree, (string ProjectDirectory, string OutputFolder)>();

        void AddSources(WinFormsProjectModel source, string outputFolder)
        {
            foreach (var file in source.CompileFiles.Order(StringComparer.Ordinal))
            {
                string text;
                try
                {
                    text = File.ReadAllText(file);
                }
                catch (IOException)
                {
                    continue;
                }

                var tree = CSharpSyntaxTree.ParseText(text, parseOptions, path: file);
                trees.Add(tree);
                origins[tree] = (source.ProjectDirectory, outputFolder);
            }
        }

        AddSources(project, "");
        var libraries = ReferencedClassLibraries(project);
        foreach (var library in libraries)
        {
            AddSources(library, Path.GetFileNameWithoutExtension(library.ProjectFilePath) + "/");
        }

        // The packages the project and its libraries declare: whatever a net10.0 project can use
        // joins the compilation, so code calling into it binds - and is then carried like the BCL.
        var (packages, notCarriedPackages) = NuGetPackageResolver.Resolve(
            libraries.Prepend(project).SelectMany(p => p.Packages.Select(package => (package, p.ProjectDirectory))),
            packageStore ?? NuGetPackageStore.None,
            ReferenceAssemblies.AssemblyNames);
        var packageReferences = new Dictionary<MetadataReference, ResolvedPackage>();
        var packageAssemblyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var package in packages)
        {
            foreach (var assembly in package.CompileAssemblies)
            {
                if (packageAssemblyNames.Add(Path.GetFileNameWithoutExtension(assembly)))
                {
                    packageReferences[MetadataReference.CreateFromFile(assembly)] = package;
                }
            }
        }

        var implicitUsings = CSharpSyntaxTree.ParseText(ImplicitUsingsSource, parseOptions, path: "<implicit-usings>");

        var compilation = CSharpCompilation.Create(
            "W2aSemanticModel",
            [.. trees, implicitUsings],
            [.. ReferenceAssemblies.All, .. packageReferences.Keys],
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Disable,
                warningLevel: 9999,
                concurrentBuild: false,
                deterministic: true));

        string OutputPathOf(SyntaxTree tree) =>
            origins[tree].OutputFolder + RelativePathOf(origins[tree].ProjectDirectory, tree.FilePath);

        var (carried, warnings) = DecideCarriedTrees(compilation, trees, generatedRootNamespace, artifactTypeNames, OutputPathOf);

        // Only what the code really uses: a package nothing calls is a restore - and possibly an
        // audit warning - the generated project does not need. Un-migrated code counts as a use,
        // so its TODO compiles once a human writes it.
        var used = packageReferences.Count == 0 ? [] : AssembliesNamedBy(compilation, trees);
        var carriedPackages = packageReferences
            .Where(r => compilation.GetAssemblyOrModuleSymbol(r.Key) is IAssemblySymbol assembly && used.Contains(assembly))
            .Select(r => r.Value)
            .Distinct()
            .Select(p => new CarriedPackage(p.Id, p.Version))
            .OrderBy(p => p.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var nullableDisable = !project.NullableEnabled;
        var files = carried
            .OrderBy(OutputPathOf, StringComparer.Ordinal)
            .Select(t => new CarriedSourceFile(
                OutputPathOf(t),
                RenderCarriedFile(compilation, t, nullableDisable),
                t.FilePath))
            .ToList();

        return new SemanticProjectContext(compilation, carried, files, warnings, generatedRootNamespace, carriedPackages, notCarriedPackages);
    }

    /// <summary>The scope of one Form/UserControl class, or null when it cannot be found unambiguously.</summary>
    public SemanticClassScope? ForClass(string fullyQualifiedName)
    {
        if (Compilation?.GetTypeByMetadataName(fullyQualifiedName) is not { } type)
        {
            return null;
        }

        return new SemanticClassScope(this, type);
    }

    /// <summary>
    /// The assemblies the project's code names a type or member of. Roslyn's own
    /// <c>GetUsedAssemblyReferences</c> cannot answer this here: in a compilation with errors - every
    /// WinForms name is one - it reports every reference as used.
    /// </summary>
    private static HashSet<IAssemblySymbol> AssembliesNamedBy(CSharpCompilation compilation, IEnumerable<SyntaxTree> trees)
    {
        var assemblies = new HashSet<IAssemblySymbol>(SymbolEqualityComparer.Default);
        foreach (var tree in trees)
        {
            var model = compilation.GetSemanticModel(tree);
            foreach (var name in tree.GetRoot().DescendantNodes().OfType<SimpleNameSyntax>())
            {
                var info = model.GetSymbolInfo(name);
                foreach (var symbol in info.CandidateSymbols.Prepend(info.Symbol).OfType<ISymbol>())
                {
                    var definition = symbol is IMethodSymbol { ReducedFrom: { } extension } ? extension : symbol;
                    if (definition.ContainingAssembly is { } assembly)
                    {
                        assemblies.Add(assembly);
                    }
                }
            }
        }

        return assemblies;
    }

    private static (HashSet<SyntaxTree> Carried, List<string> Warnings) DecideCarriedTrees(
        CSharpCompilation compilation,
        IReadOnlyList<SyntaxTree> trees,
        string generatedRootNamespace,
        IReadOnlySet<string> artifactTypeNames,
        Func<SyntaxTree, string> outputPathOf)
    {
        var warnings = new List<string>();
        var candidates = new List<(SyntaxTree Tree, SemanticModel Model)>();

        foreach (var tree in trees)
        {
            var root = (CompilationUnitSyntax)tree.GetRoot();
            var model = compilation.GetSemanticModel(tree);
            var declaredTypes = root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>()
                .Select(d => model.GetDeclaredSymbol(d))
                .OfType<INamedTypeSymbol>()
                .ToList();
            var fileName = Path.GetFileName(tree.FilePath);

            // What the conversion writes itself, or what has no place in an Avalonia project at all.
            if (fileName.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase)
                || string.Equals(fileName, "AssemblyInfo.cs", StringComparison.OrdinalIgnoreCase)
                || declaredTypes.Count == 0
                || declaredTypes.Any(t => artifactTypeNames.Contains(t.ToDisplayString())
                    || IsFormsRelatedName(t))
                || root.Members.OfType<GlobalStatementSyntax>().Any()
                || root.AttributeLists.Any()
                || declaredTypes.SelectMany(t => t.GetMembers("Main")).Any(m => m.IsStatic))
            {
                continue;
            }

            if (declaredTypes.FirstOrDefault(t => CollidesWithGenerated(t, generatedRootNamespace)) is { } colliding)
            {
                warnings.Add(
                    $"'{outputPathOf(tree)}' was not carried over: it declares "
                    + $"'{colliding.ToDisplayString()}', a name the generated project uses itself.");
                continue;
            }

            // Errors and warnings alike: a carried file has to compile warning-free there too. A
            // using directive that does not bind (`using System.Windows.Forms;` left over from a
            // template) is not counted - it is dropped when the file is copied.
            var problem = model.GetDiagnostics()
                .Where(d => d.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning)
                .FirstOrDefault(d => !IsInsideUsing(root, d));
            if (problem is not null)
            {
                warnings.Add(NotCarried(outputPathOf(tree), problem.GetMessage(System.Globalization.CultureInfo.InvariantCulture)));
                continue;
            }

            candidates.Add((tree, model));
        }

        // A fixed point: a file may only use types of files that come across too, and dropping one
        // file can strand another that used it.
        var carried = candidates.Select(c => c.Tree).ToHashSet();
        var reasons = new Dictionary<SyntaxTree, string>();
        bool changed;
        do
        {
            changed = false;
            foreach (var (tree, model) in candidates.Where(c => carried.Contains(c.Tree)))
            {
                var checker = new NeutralityChecker(
                    model, owner: null, _ => false, refuseMethodParameters: false, carried.Contains,
                    NeutralityChecker.ImplicitNamespaces);

                if (!checker.Check(tree.GetRoot()))
                {
                    carried.Remove(tree);
                    reasons[tree] = checker.FirstProblem ?? "it uses a type the generated project does not have";
                    changed = true;
                }
            }
        }
        while (changed);

        foreach (var (tree, reason) in reasons.OrderBy(r => r.Key.FilePath, StringComparer.Ordinal))
        {
            warnings.Add(NotCarried(outputPathOf(tree), reason));
        }

        return (carried, warnings);
    }

    private static string NotCarried(string outputPath, string reason) =>
        $"'{outputPath}' was not carried over into the generated project - {reason}. "
        + "Code that uses it stays un-migrated.";

    /// <summary>
    /// The class libraries this project references, transitively - the ones that are not WinForms
    /// projects themselves (a WinForms project is converted on its own, and referenced as such).
    /// </summary>
    /// <remarks>
    /// Their UI-neutral sources come across <em>into</em> the generated project, under a folder
    /// named after the library, rather than as a reference to the original csproj: a .NET
    /// Framework library (the common case beside a WinForms app) cannot be referenced from a
    /// <c>net10.0</c> project as it stands, and the converted app should not depend on the source
    /// tree it was converted from.
    /// </remarks>
    private static List<WinFormsProjectModel> ReferencedClassLibraries(WinFormsProjectModel project)
    {
        var loader = new WinFormsProjectLoader();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { project.ProjectFilePath };
        var libraries = new List<WinFormsProjectModel>();
        var pending = new Queue<string>(project.ProjectReferences);

        while (pending.TryDequeue(out var path))
        {
            if (!visited.Add(path) || !File.Exists(path) || IsWinFormsProject(path))
            {
                continue;
            }

            WinFormsProjectModel library;
            try
            {
                library = loader.Load(path);
            }
            catch (Exception ex) when (ex is IOException or System.Xml.XmlException or InvalidOperationException)
            {
                continue;
            }

            libraries.Add(library);
            foreach (var reference in library.ProjectReferences)
            {
                pending.Enqueue(reference);
            }
        }

        return libraries;
    }

    private static bool IsWinFormsProject(string projectFilePath)
    {
        var text = File.ReadAllText(projectFilePath);
        return Regex.IsMatch(text, @"<UseWindowsForms>\s*true", RegexOptions.IgnoreCase)
            || text.Contains("Include=\"System.Windows.Forms", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Program.cs-style plumbing: <c>Application.Run</c> lives there, and nothing in it is ever called.</summary>
    private static bool IsFormsRelatedName(INamedTypeSymbol type) => type.Name == "Program" && type.IsStatic;

    private static bool CollidesWithGenerated(INamedTypeSymbol type, string generatedRootNamespace)
    {
        var ns = type.ContainingNamespace?.ToDisplayString() ?? "";
        if (string.Equals(ns, generatedRootNamespace, StringComparison.Ordinal)
            && GeneratedRootTypeNames.Contains(type.Name, StringComparer.Ordinal))
        {
            return true;
        }

        return GeneratedSubNamespaces.Any(sub =>
            string.Equals(ns, $"{generatedRootNamespace}.{sub}", StringComparison.Ordinal));
    }

    private static bool IsInsideUsing(CompilationUnitSyntax root, Diagnostic diagnostic) =>
        diagnostic.Location.IsInSource
        && root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true)
            .AncestorsAndSelf().Any(n => n is UsingDirectiveSyntax);

    /// <summary>
    /// The file as written, minus the using directives that name WinForms or nothing at all, and -
    /// unless the original project already had nullable reference types on - under
    /// <c>#nullable disable</c>: the generated project enables them, and code written without them
    /// would otherwise be a page of CS86xx warnings it never had.
    /// </summary>
    private static string RenderCarriedFile(CSharpCompilation compilation, SyntaxTree tree, bool nullableDisable)
    {
        var root = (CompilationUnitSyntax)tree.GetRoot();
        var model = compilation.GetSemanticModel(tree);
        var dead = root.DescendantNodes().OfType<UsingDirectiveSyntax>()
            .Where(u => u.Name is { } name && model.GetSymbolInfo(name).Symbol switch
            {
                INamespaceSymbol ns => IsUiNamespaceName(ns.ToDisplayString()),
                null => true,
                _ => false,
            })
            .ToList();

        var text = root.RemoveNodes(dead, SyntaxRemoveOptions.KeepLeadingTrivia)!.ToFullString();
        var lineEnding = text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";

        return nullableDisable ? $"#nullable disable{lineEnding}{text}" : text;
    }

    private static bool IsUiNamespaceName(string name) =>
        name == "System.Windows.Forms" || name.StartsWith("System.Windows.Forms.", StringComparison.Ordinal)
        || name == "System.Drawing" || name.StartsWith("System.Drawing.", StringComparison.Ordinal);

    internal static string RelativePathOf(string projectDirectory, string filePath)
    {
        var relative = Path.GetRelativePath(projectDirectory, filePath);
        if (relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative))
        {
            // A linked file lands at the root, as a linked Form does.
            relative = Path.GetFileName(filePath);
        }

        return relative.Replace('\\', '/');
    }
}
