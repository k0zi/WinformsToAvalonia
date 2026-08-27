using System.Reflection;

namespace WinFormsToAvalonia.Mapping.Tests;

/// <summary>
/// WinForms' own API, read as metadata - the other side of the question
/// <see cref="AvaloniaMetadata"/> answers.
/// </summary>
/// <remarks>
/// <para>
/// The mapping tables are claims about two frameworks, and until now only one of them could be
/// checked. "Is every event mapped?" was a question nobody could answer without reading the table
/// and remembering what WinForms has; with this it is a test.
/// </para>
/// <para>
/// The reference assembly comes from the <c>Microsoft.WindowsDesktop.App.Ref</c> package, copied
/// beside the test assembly by this project's csproj - pinned by version, not found by groping
/// around the NuGet cache at run time. Nothing here is executed: the converter runs on Linux, and
/// so does this.
/// </para>
/// </remarks>
public static class WinFormsMetadata
{
    private static readonly Lazy<Loaded> Context = new(Load, isThreadSafe: true);

    private sealed record Loaded(MetadataLoadContext Mlc, Assembly WindowsForms);

    /// <summary>A WinForms type by simple name, e.g. <c>"Control"</c> or <c>"ComboBox"</c>.</summary>
    public static Type? FindType(string simpleTypeName) =>
        Context.Value.WindowsForms.GetExportedTypes()
            .FirstOrDefault(t => string.Equals(t.Name, simpleTypeName, StringComparison.Ordinal));

    /// <summary>
    /// The events a type declares itself, not counting what it inherits - which is what makes
    /// "the events on Control" and "the events on Form" two separate, finite questions.
    /// </summary>
    public static IEnumerable<string> DeclaredEventNames(Type type) =>
        type.GetEvents(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(e => e.Name)
            .Order(StringComparer.Ordinal);

    /// <summary>
    /// The major version of the WinForms this suite reads. It has to match the framework the
    /// generated projects target, or the tables are being checked against a different API than
    /// the one they describe.
    /// </summary>
    public static int MajorVersion => Context.Value.WindowsForms.GetName().Version?.Major ?? 0;

    private static Loaded Load()
    {
        var assemblyPath = Path.Combine(AppContext.BaseDirectory, "System.Windows.Forms.dll");
        Assert.True(
            File.Exists(assemblyPath),
            $"System.Windows.Forms.dll is not beside the test assembly ({AppContext.BaseDirectory}). "
            + "This suite is meaningless without it - check the Microsoft.WindowsDesktop.App.Ref reference.");

        var candidates = Directory
            .GetFiles(AppContext.BaseDirectory, "*.dll")
            .Concat(Directory.GetFiles(System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory(), "*.dll"))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var mlc = new MetadataLoadContext(new PathAssemblyResolver(candidates));
        return new Loaded(mlc, mlc.LoadFromAssemblyPath(assemblyPath));
    }
}
