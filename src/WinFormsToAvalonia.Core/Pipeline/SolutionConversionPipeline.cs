using System.Text;
using WinFormsToAvalonia.Core.Emission;
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
/// </remarks>
public sealed class SolutionConversionPipeline
{
    public SolutionConversionResult Run(ConversionOptions options)
    {
        var projectPaths = SolutionReader.ReadProjectPaths(options.SourceProjectPath);
        var converted = new List<ConvertedProject>();
        var skipped = new List<SkippedProject>();

        foreach (var projectPath in projectPaths)
        {
            if (!File.Exists(projectPath))
            {
                skipped.Add(new SkippedProject(projectPath, "the solution lists it, but the file is not there"));
                continue;
            }

            var outputDirectory = Path.Combine(options.OutputDirectory, Path.GetFileNameWithoutExtension(projectPath));

            try
            {
                var result = new ConversionPipeline().Run(options with
                {
                    SourceProjectPath = projectPath,
                    OutputDirectory = outputDirectory,
                });

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
            vfs.AddText(solutionFileName, BuildSolution(options.OutputDirectory, converted));
            vfs.WriteToDisk(
                options.OutputDirectory,
                options.OverwriteAll ? ExistingFileStrategy.Overwrite : ExistingFileStrategy.PreserveExisting);
        }

        return new SolutionConversionResult(solutionFileName, converted, skipped);
    }

    /// <summary>
    /// The `.slnx` format rather than the classic one: it needs no project GUIDs, which this
    /// converter would otherwise have to invent and then keep stable across re-runs.
    /// </summary>
    private static string BuildSolution(string outputDirectory, IReadOnlyList<ConvertedProject> converted)
    {
        var sb = new StringBuilder();
        sb.Append("<Solution>\n");

        foreach (var project in converted.OrderBy(p => p.OutputDirectory, StringComparer.Ordinal))
        {
            var name = NamingConventions.DeriveProjectName(project.OutputDirectory);
            var folder = Path.GetFileName(project.OutputDirectory.TrimEnd(Path.DirectorySeparatorChar, '/'));
            sb.Append($"  <Project Path=\"{folder}/{name}.csproj\" />\n");
        }

        sb.Append("</Solution>\n");
        return sb.ToString();
    }
}
