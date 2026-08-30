namespace WinFormsToAvalonia.Core.Model;

/// <summary>
/// The Avalonia element a converted View is rooted at, which is <i>not</i> the same question as
/// <see cref="WinFormsArtifactKind"/>.
/// </summary>
/// <remarks>
/// <para>
/// A Form is a <see cref="Window"/> and a UserControl a <see cref="UserControl"/>, and for a long
/// time one enum answered both questions at once. Avalonia's browser backend breaks that: it only
/// offers a single-view lifetime, and a <c>Window</c> cannot be instantiated there at all. So
/// under <c>--with-web</c> the main Form's View is rooted at a <c>UserControl</c> - while staying
/// a Form in every other respect, notably taking its size from <c>ClientSize</c> - and a thin
/// generated <c>Window</c> hosts it for the desktop head.
/// </para>
/// <para>
/// This only ever describes the root element and the code-behind base type. Whether the View can
/// call <c>Close()</c> or own a dialog is <see cref="ViewNavigationContext"/>'s business.
/// </para>
/// </remarks>
public enum ViewRootKind
{
    Window,
    UserControl,
}

public static class ViewRootKindExtensions
{
    /// <summary>The root a given artifact gets when nothing asks for anything else.</summary>
    public static ViewRootKind DefaultRootKind(this WinFormsArtifactKind kind) =>
        kind == WinFormsArtifactKind.UserControl ? ViewRootKind.UserControl : ViewRootKind.Window;

    public static string ElementName(this ViewRootKind kind) =>
        kind == ViewRootKind.UserControl ? "UserControl" : "Window";
}
