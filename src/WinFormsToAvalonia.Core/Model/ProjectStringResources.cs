namespace WinFormsToAvalonia.Core.Model;

/// <summary>
/// The string entries of the project's <c>Properties/Resources.resx</c>, as a handler body may
/// read them - <c>Properties.Resources.Greeting</c> - and the generated class that carries them.
/// </summary>
/// <remarks>
/// Only strings. An image resource becomes a <c>Bitmap</c> in the generated class, which is a
/// different type from the <c>System.Drawing.Image</c> the original code assigned it to - so a body
/// naming one still stops there, rather than producing a statement that compiles against the
/// wrong thing.
/// </remarks>
/// <param name="ClassExpression">The generated class, fully qualified - <c>App.Properties.Resources</c>; not <c>global::</c>-qualified, because a <c>::</c> inside an interpolation hole reads as a format specifier.</param>
public sealed record ProjectStringResources(string ClassExpression, IReadOnlySet<string> Names)
{
    public static ProjectStringResources None { get; } = new("", new HashSet<string>(StringComparer.Ordinal));
}
