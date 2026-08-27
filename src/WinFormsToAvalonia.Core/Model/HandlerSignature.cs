namespace WinFormsToAvalonia.Core.Model;

/// <summary>
/// What a handler's own parameters mean, so its body's uses of them can be translated.
/// </summary>
/// <param name="EventArgsParameterName">The original `e`, or null when the handler had no such parameter.</param>
/// <param name="EventArgsTypeName">The *Avalonia* args type the generated method will declare.</param>
/// <param name="SourceControlFieldNames">
/// Every control wired to this handler. Usually one - and a pointer-position translation needs
/// exactly that, since it has to name the control the coordinates are relative to. Several means
/// a shared handler, which only `sender` can tell apart; empty means a Form-level event.
/// </param>
public sealed record HandlerSignature(
    string? EventArgsParameterName,
    string EventArgsTypeName,
    IReadOnlyList<string> SourceControlFieldNames)
{
    /// <summary>No parameter information - nothing about `e` can be translated.</summary>
    public static HandlerSignature None { get; } = new(null, "EventArgs", []);

    /// <summary>The one control that raises the event, or null if it is not exactly one.</summary>
    public string? SourceControlFieldName =>
        SourceControlFieldNames is [var single] ? single : null;
}
