namespace WinFormsToAvalonia.Core.Model;

/// <summary>
/// A `+=` event subscription found in the *non-designer* code-behind rather than in
/// InitializeComponent() - e.g. ProgressHost's <c>this.timer.Tick += this.Timer_Tick;</c>
/// inside Form1_Load. DesignerSyntaxWalker never sees these, so without capturing them here
/// the referenced method would look like an ordinary helper instead of a handler.
/// </summary>
/// <param name="TargetFieldName">The subscribed object's field name, or null for a Form-level (`this.X += ...`) subscription.</param>
public sealed record RuntimeEventSubscription(string? TargetFieldName, string EventName, string HandlerMethodName);
