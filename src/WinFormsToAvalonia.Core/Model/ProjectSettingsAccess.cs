namespace WinFormsToAvalonia.Core.Model;

/// <summary>
/// The project's <c>Properties.Settings.Default</c> as a handler body may use it: which settings it
/// may read, which it may assign (user scope only - an application-scoped one has no setter, in
/// WinForms or in the generated class), and the generated instance they now live on.
/// </summary>
/// <param name="InstanceExpression">The generated instance, fully qualified - <c>App.Properties.Settings.Default</c> (see <see cref="ProjectStringResources.ClassExpression"/> for why not <c>global::</c>).</param>
public sealed record ProjectSettingsAccess(
    string InstanceExpression,
    IReadOnlySet<string> Readable,
    IReadOnlySet<string> Writable)
{
    public static ProjectSettingsAccess None { get; } = new(
        "", new HashSet<string>(StringComparer.Ordinal), new HashSet<string>(StringComparer.Ordinal));

    /// <summary>The instance methods the generated class declares, all parameterless.</summary>
    public static IReadOnlySet<string> Methods { get; } = new HashSet<string>(StringComparer.Ordinal) { "Save", "Reload", "Reset" };

    public bool IsEmpty => Readable.Count == 0;
}
