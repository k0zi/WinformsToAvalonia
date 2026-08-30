using System.Text;
using WinFormsToAvalonia.Core.Emission;
using WinFormsToAvalonia.Core.Model;
using WinFormsToAvalonia.Core.Parsing;
using WinFormsToAvalonia.Core.Scaffolding;

namespace WinFormsToAvalonia.Core.Pipeline;

/// <param name="OutputDirectory">Where the generated project folder for this one landed.</param>
public sealed record ConvertedProject(string SourceProjectPath, string OutputDirectory, ConversionRunResult Result);

public sealed record SkippedProject(string SourceProjectPath, string Reason);

public sealed record SolutionConversionResult(
    string SolutionFileName,
    IReadOnlyList<ConvertedProject> Converted,
    IReadOnlyList<SkippedProject> Skipped);

/// <summary>
/// Converts every WinForms project a solution lists, into one output solution.
/// </summary>
/// <remarks>
/// <para>
/// The workaround this replaces is checked into this repo: <c>samples/convert.sh</c> loops over
/// the projects by hand and stitches a solution together afterwards, because <c>--source</c> only
/// ever accepted a single <c>.csproj</c>. A real multi-project WinForms solution hits that limit
/// immediately.
/// </para>
/// <para>
/// Each project still goes through the ordinary single-project pipeline, unchanged - this only
/// decides what to run it on and what to write around the results. A project the pipeline finds
/// nothing convertible in (a class library, a test project) is reported and skipped rather than
/// failing the run: a solution with one non-WinForms project in it is the normal case, not an
/// error.
/// </para>
/// <para>
/// The one thing it does have to decide up front is what each project's UserControls will be
/// called once converted, because a Form in one project may host a UserControl from another and
/// the hosting run has to name a View that does not exist yet. That is the same problem
/// <c>ConversionPipeline.BuildFormViews</c> solves for Forms within a project, one level up: a
/// discovery pass over the whole solution before any project is converted.
/// </para>
/// </remarks>
public sealed class SolutionConversionPipeline
{
    public SolutionConversionResult Run(ConversionOptions options)
    {
        var projectPaths = SolutionReader.ReadProjectPaths(options.SourceProjectPath);
        var converted = new List<ConvertedProject>();
        var skipped = new List<SkippedProject>();

        var existingProjects = projectPaths.Where(File.Exists).ToList();
        var outputDirectories = existingProjects.ToDictionary(
            p => p,
            p => Path.Combine(options.OutputDirectory, Path.GetFileNameWithoutExtension(p)),
            StringComparer.OrdinalIgnoreCase);
        var userControlsByProject = existingProjects.ToDictionary(
            p => p,
            p => DiscoverUserControls(p, outputDirectories[p]),
            StringComparer.OrdinalIgnoreCase);

        foreach (var projectPath in projectPaths)
        {
            if (!File.Exists(projectPath))
            {
                skipped.Add(new SkippedProject(projectPath, "the solution lists it, but the file is not there"));
                continue;
            }

            var outputDirectory = outputDirectories[projectPath];

            try
            {
                var result = new ConversionPipeline().Run(
                    options with
                    {
                        SourceProjectPath = projectPath,
                        OutputDirectory = outputDirectory,
                    },
                    BuildContext(
                        projectPath, outputDirectory, outputDirectories, userControlsByProject, options.WithWeb));

                converted.Add(new ConvertedProject(projectPath, outputDirectory, result));
            }
            catch (NoConvertibleArtifactsException)
            {
                skipped.Add(new SkippedProject(projectPath, "no WinForms Form, UserControl or Component in it"));
            }
        }

        var solutionFileName = $"{Path.GetFileNameWithoutExtension(options.SourceProjectPath)}.slnx";

        if (!options.DryRun && converted.Count > 0)
        {
            var vfs = new VirtualFileSystem();
            vfs.AddText(solutionFileName, BuildSolution(converted, options.WithWeb));
            vfs.WriteToDisk(
                options.OutputDirectory,
                options.OverwriteAll ? ExistingFileStrategy.Overwrite : ExistingFileStrategy.PreserveExisting);
        }

        return new SolutionConversionResult(solutionFileName, converted, skipped);
    }

    /// <summary>
    /// What the projects <paramref name="projectPath"/> references contribute to its conversion.
    /// </summary>
    /// <remarks>
    /// The reference graph comes from the source csproj, not from the solution: naming every
    /// other project would both invent build dependencies that were never there and let a
    /// UserControl resolve in a project that could not actually see it.
    /// </remarks>
    private static SolutionConversionContext? BuildContext(
        string projectPath,
        string outputDirectory,
        IReadOnlyDictionary<string, string> outputDirectories,
        IReadOnlyDictionary<string, IReadOnlyList<ExternalUserControl>> userControlsByProject,
        bool withWeb)
    {
        var referenced = new WinFormsProjectLoader().Load(projectPath).ProjectReferences
            .Where(outputDirectories.ContainsKey)
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();

        if (referenced.Count == 0)
        {
            return null;
        }

        var externals = referenced.SelectMany(r => userControlsByProject[r]).ToList();
        if (externals.Count == 0)
        {
            // Nothing here can name anything there, so a ProjectReference would only add an
            // unused build dependency.
            return null;
        }

        var references = referenced
            .Select(r => RelativeCsprojPath(outputDirectory, outputDirectories[r], withWeb))
            .ToList();

        return new SolutionConversionContext(externals, references);
    }

    /// <summary>
    /// The Views a project's UserControls will be emitted as, predicted without converting it.
    /// </summary>
    /// <remarks>
    /// This mirrors <c>ConversionPipeline.BuildUserControlViews</c> and has to keep mirroring it:
    /// both derive the View name and namespace through <see cref="NamingConventions"/>, which is
    /// what keeps the prediction and the eventual emission in agreement.
    /// </remarks>
    private static IReadOnlyList<ExternalUserControl> DiscoverUserControls(string projectPath, string outputDirectory)
    {
        WinFormsProjectModel project;
        try
        {
            project = new WinFormsProjectLoader().Load(projectPath);
        }
        catch (Exception e) when (e is IOException or InvalidOperationException or System.Xml.XmlException)
        {
            // A project that will not even load is reported by the conversion attempt below; it
            // simply contributes no UserControls.
            return [];
        }

        var assemblyName = NamingConventions.DeriveProjectName(outputDirectory);

        return new DesignerFileLocator().Locate(project)
            .Where(p => p.Kind == WinFormsArtifactKind.UserControl && p.DesignerFilePath is not null)
            .Select(p => new ExternalUserControl(
                p.ClassName,
                NamingConventions.DeriveViewName(p.ClassName),
                NamingConventions.NamespaceOf(
                    $"{assemblyName}.Views",
                    ConversionPipeline.GetRelativeFolder(project.ProjectDirectory, p.DesignerFilePath!)),
                assemblyName))
            .ToList();
    }

    /// <remarks>
    /// The reference is written relative to the *referencing csproj's own folder*, which
    /// <c>--with-web</c> moves one level deeper: the shared library sits in
    /// <c>&lt;output&gt;/&lt;Proj&gt;/&lt;Proj&gt;/</c> rather than <c>&lt;output&gt;/&lt;Proj&gt;/</c>,
    /// so both ends of the relative path gain a segment.
    /// </remarks>
    private static string RelativeCsprojPath(
        string fromOutputDirectory, string referencedOutputDirectory, bool withWeb)
    {
        var fromName = NamingConventions.DeriveProjectName(fromOutputDirectory);
        var name = NamingConventions.DeriveProjectName(referencedOutputDirectory);

        var from = withWeb ? Path.Combine(fromOutputDirectory, fromName) : fromOutputDirectory;
        var to = withWeb ? Path.Combine(referencedOutputDirectory, name) : referencedOutputDirectory;

        var relative = Path.GetRelativePath(from, to);
        return $"{relative.Replace(Path.DirectorySeparatorChar, '/')}/{name}.csproj";
    }

    /// <summary>
    /// The `.slnx` format rather than the classic one: it needs no project GUIDs, which this
    /// converter would otherwise have to invent and then keep stable across re-runs.
    /// </summary>
    private static string BuildSolution(IReadOnlyList<ConvertedProject> converted, bool withWeb)
    {
        var sb = new StringBuilder();
        sb.Append("<Solution>\n");

        foreach (var project in converted.OrderBy(p => p.OutputDirectory, StringComparer.Ordinal))
        {
            var name = NamingConventions.DeriveProjectName(project.OutputDirectory);
            var folder = Path.GetFileName(project.OutputDirectory.TrimEnd(Path.DirectorySeparatorChar, '/'));

            // Each converted project is three of them under --with-web, and every one has to be
            // in the solution or the heads are unbuildable from it.
            var names = withWeb
                ?
                [
                    name,
                    AvaloniaProjectScaffolder.BrowserHeadFolder(name),
                    AvaloniaProjectScaffolder.DesktopHeadFolder(name),
                ]
                : new[] { name };

            foreach (var projectName in names.OrderBy(n => n, StringComparer.Ordinal))
            {
                var path = withWeb
                    ? $"{folder}/{projectName}/{projectName}.csproj"
                    : $"{folder}/{projectName}.csproj";
                sb.Append($"  <Project Path=\"{path}\" />\n");
            }
        }

        sb.Append("</Solution>\n");
        return sb.ToString();
    }
}
