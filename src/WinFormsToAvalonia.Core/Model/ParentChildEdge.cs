namespace WinFormsToAvalonia.Core.Model;

/// <summary>
/// A `parent.Controls.Add(child)` / `parent.Controls.AddRange(new[] { child, ... })`
/// relationship captured from InitializeComponent(), before ControlGraphBuilder assembles
/// the actual parent/child tree. <see cref="ParentFieldName"/> is
/// <see cref="ParentChildEdge.FormOwner"/> for `this.Controls.Add(...)` (a direct child of
/// the Form/UserControl itself).
/// </summary>
public sealed record ParentChildEdge(string ParentFieldName, string ChildFieldName)
{
    public const string FormOwner = "this";
}
