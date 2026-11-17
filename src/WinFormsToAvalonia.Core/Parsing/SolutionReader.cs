using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace WinFormsToAvalonia.Core.Parsing;

/// <summary>
/// The C# projects a solution file lists, for both formats a WinForms solution can be in.
/// </summary>
/// <remarks>
/// Parsed as text rather than through <c>Microsoft.Build</c>: both formats are simple enough to
/// read directly, and taking that dependency would pull the MSBuild engine - and its version
/// resolution - into a tool whose whole parsing story is deliberately syntactic. The cost is that
/// a solution using MSBuild properties in a project path is not understood, which is reported
/// rather than guessed at.
/// </remarks>
public static class SolutionReader
{
    /// <summary>Classic <c>Project("{type-guid}") = "Name", "relative\path.csproj", "{guid}"</c> lines.</summary>
    private static readonly Regex ClassicProjectLine = new(
        """^Project\("\{[^}]+\}"\)\s*=\s*"[^"]*",\s*"([^"]+)",""",
        RegexOptions.Multiline | RegexOptions.Compiled);

    public static bool IsSolutionPath(string path) =>
        path.EndsWith(".sln", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase);

    /// <summary>Absolute paths to the C# projects the solution lists, in solution order.</summary>
    public static IReadOnlyList<string> ReadProjectPaths(string solutionPath)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(solutionPath)) ?? ".";
        var text = File.ReadAllText(solutionPath);

        var relativePaths = solutionPath.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase)
            ? ReadXmlProjectPaths(text)
            : ClassicProjectLine.Matches(text).Select(m => m.Groups[1].Value);

        return [.. relativePaths
            // Solution files are authored on Windows more often than not, and carry its separator
            // whatever the machine reading them.
            .Select(p => p.Replace('\\', Path.DirectorySeparatorChar))
            .Where(p => p.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            .Select(p => Path.GetFullPath(Path.Combine(directory, p)))
            .Distinct(StringComparer.Ordinal)];
    }

    /// <summary>The `.slnx` format: nested <c>&lt;Project Path="..." /&gt;</c> elements.</summary>
    private static IEnumerable<string> ReadXmlProjectPaths(string text) =>
        XDocument.Parse(text)
            .Descendants("Project")
            .Select(e => e.Attribute("Path")?.Value)
            .OfType<string>();
}
