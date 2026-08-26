namespace WinFormsToAvalonia.Core.Model;

/// <summary>
/// What a handler's own parameters mean, so its body's uses of them can be translated.
/// </summary>
/// <param name="EventArgsParameterName">The original `e`, or null when the handler had no such parameter.</param>
/// <param name="EventArgsTypeName">The *Avalonia* args type the generated method will declare.</param>
/// <param name="SourceControlFieldName">
/// The control that raises the event, when the handler is wired to exactly one - which is what
/// a pointer-position translation needs. Null for a Form-level event or a shared handler.
/// </param>
public sealed record HandlerSignature(
    string? EventArgsParameterName,
    string EventArgsTypeName,
    string? SourceControlFieldName)
{
    /// <summary>No parameter information - nothing about `e` can be translated.</summary>
    public static HandlerSignature None { get; } = new(null, "EventArgs", null);
}
