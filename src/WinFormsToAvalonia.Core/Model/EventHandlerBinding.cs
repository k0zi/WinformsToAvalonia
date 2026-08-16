namespace WinFormsToAvalonia.Core.Model;

/// <summary>
/// A `+=` event subscription captured from InitializeComponent(), e.g.
/// `this.button1.Click += new EventHandler(this.button1_Click);` or the lambda form
/// `this.Load += (s, e) => { ... };`. Exactly one of <see cref="HandlerMethodName"/> /
/// <see cref="InlineHandlerBody"/> is set.
/// </summary>
public sealed record EventHandlerBinding(string EventName, string? HandlerMethodName, string? InlineHandlerBody);
