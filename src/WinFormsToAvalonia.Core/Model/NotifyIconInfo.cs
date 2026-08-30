namespace WinFormsToAvalonia.Core.Model;

/// <summary>
/// A NotifyIcon component collected across all of a project's Forms, aggregated by
/// ConversionPipeline.Run for AvaloniaProjectScaffolder to emit into App.axaml's
/// TrayIcon.Icons - App-level, not per-View, so it doesn't fit ConvertedFormOutput's
/// per-form shape.
/// </summary>
/// <param name="IconAssetPath">
/// The generated project's asset path for the icon (e.g. "Assets/app.ico"), set only when the
/// icon file was actually resolved from Designer.cs <em>and</em> copied into the output.
/// <see langword="null"/> otherwise - which is the common case, since real Designer.cs rarely
/// assigns a literal icon path (it is usually a resx resource or a computed Icon).
/// Avalonia's TrayIcon resolves its Icon at run time, so emitting a path to a file that was
/// never produced is a startup FileNotFoundException, not a build error - hence the null.
/// </param>
/// <param name="MenuItems">
/// The NotifyIcon's <c>ContextMenuStrip</c>, as Avalonia's <c>TrayIcon.Menu</c> needs it. Empty
/// when the designer wired none, and empty is what the emitter checks - a <c>NativeMenu</c> with
/// no items is a menu that opens onto nothing.
/// </param>
public sealed record NotifyIconInfo(
    string FieldName,
    string? IconAssetPath,
    string? TooltipText,
    IReadOnlyList<TrayMenuItemInfo>? MenuItems = null)
{
    public IReadOnlyList<TrayMenuItemInfo> MenuItems { get; } = MenuItems ?? [];
}

/// <summary>
/// One entry of a tray icon's menu, flattened out of the WinForms <c>ToolStripMenuItem</c> tree
/// into what a <c>NativeMenuItem</c> can carry.
/// </summary>
/// <remarks>
/// A tray menu is a <b>native</b> menu - the OS draws it, not Avalonia - so it takes far less
/// than a <c>ContextMenu</c> does: a header, an enabled/visible flag, and a submenu. There is no
/// styling, no arbitrary content and, notably, no way to point it at a code-behind handler from
/// XAML: <c>NativeMenuItem.Click</c> is an event, not a bindable attribute. Designer-wired Click
/// handlers are therefore reported rather than emitted.
/// </remarks>
public sealed record TrayMenuItemInfo(
    string Header,
    bool IsSeparator = false,
    bool IsEnabled = true,
    IReadOnlyList<TrayMenuItemInfo>? Children = null)
{
    public IReadOnlyList<TrayMenuItemInfo> Children { get; } = Children ?? [];
}
