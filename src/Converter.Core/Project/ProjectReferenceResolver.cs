using System.Xml.Linq;

namespace Converter.Core.Project;

/// <summary>
/// A sibling project referenced by the source WinForms project's own .csproj that can be
/// referenced as-is from the generated Avalonia project (a plain class library with no
/// WinForms dependency - e.g. a data/domain layer).
/// </summary>
public sealed record ResolvedProjectReference(string AbsolutePath, string ProjectName);

/// <summary>
/// Result of ProjectReferenceResolver.Resolve: sibling projects split into ones safe to
/// reference directly and ones that look like WinForms projects themselves (and therefore need
/// converting separately before they can be referenced).
/// </summary>
public sealed class ProjectReferenceResolution
{
    public IReadOnlyList<ResolvedProjectReference> Referenceable { get; init; } = [];
    public IReadOnlyList<string> SkippedWinFormsProjectNames { get; init; } = [];

    public static readonly ProjectReferenceResolution Empty = new();
}

/// <summary>
/// Finds the source WinForms project's own .csproj (the one directly inside the folder passed
/// to `convert -i`) and resolves its &lt;ProjectReference&gt; items, so the generated Avalonia
/// project can reference the same non-UI dependencies (a data/business layer, typically) -
/// without this, code migrated into ViewModels that references the app's own domain types
/// (e.g. "Product", "Customer") compiles the "using" but not the type itself. Hand-rolled
/// System.Xml.Linq, same as ResxDocument - no MSBuild API dependency. Best-effort throughout:
/// a missing/malformed source .csproj, or a reference that itself doesn't resolve on disk,
/// simply yields nothing for that entry, never a hard failure of the whole conversion.
/// </summary>
public static class ProjectReferenceResolver
{
    public static ProjectReferenceResolution Resolve(string sourcePath)
    {
        var referenceable = new List<ResolvedProjectReference>();
        var skipped = new List<string>();

        try
        {
            var sourceCsproj = Directory.GetFiles(sourcePath, "*.csproj", SearchOption.TopDirectoryOnly).FirstOrDefault();
            if (sourceCsproj == null)
            {
                return ProjectReferenceResolution.Empty;
            }

            var sourceDir = Path.GetDirectoryName(sourceCsproj)!;
            var doc = XDocument.Load(sourceCsproj);

            var includes = doc.Descendants("ProjectReference")
                .Select(e => e.Attribute("Include")?.Value)
                .Where(v => !string.IsNullOrWhiteSpace(v));

            foreach (var include in includes)
            {
                var normalized = include!.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
                var referencedPath = Path.GetFullPath(Path.Combine(sourceDir, normalized));
                if (!File.Exists(referencedPath))
                {
                    // Best-effort: a reference that's already broken in the source project has
                    // nothing for us to migrate.
                    continue;
                }

                var projectName = Path.GetFileNameWithoutExtension(referencedPath);
                if (IsWinFormsProject(referencedPath))
                {
                    skipped.Add(projectName);
                    continue;
                }

                referenceable.Add(new ResolvedProjectReference(referencedPath, projectName));
            }
        }
        catch
        {
            // Best-effort: a malformed/unreadable source .csproj means no references get
            // resolved, not a failed conversion.
        }

        return new ProjectReferenceResolution { Referenceable = referenceable, SkippedWinFormsProjectNames = skipped };
    }

    /// <summary>
    /// A referenced project that itself sets UseWindowsForms=true is a WinForms project too -
    /// referencing it as-is from a cross-platform Avalonia project wouldn't compile (no
    /// System.Windows.Forms assembly available), and it needs its own separate conversion run
    /// rather than being silently wired in.
    /// </summary>
    private static bool IsWinFormsProject(string csprojPath)
    {
        try
        {
            var doc = XDocument.Load(csprojPath);
            return doc.Descendants("UseWindowsForms")
                .Any(e => string.Equals(e.Value.Trim(), "true", StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return false;
        }
    }
}
