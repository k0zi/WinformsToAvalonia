namespace WinFormsToAvalonia.Core.Mapping;

/// <summary>
/// Names a generated View or ViewModel already inherits, and which a preserved code-behind helper
/// therefore must not take.
/// </summary>
/// <remarks>
/// <para>
/// A helper is emitted as a real method on the generated class, so a WinForms helper called
/// <c>Tag</c> or <c>Refresh</c> lands beside Avalonia's <c>Control.Tag</c> and
/// <c>Visual.InvalidateVisual</c> - a CS0108 "hides inherited member" warning in the *generated*
/// project, which this converter's own build cannot see. Warning-free output is an invariant here,
/// so such a helper is left as a comment instead.
/// </para>
/// <para>
/// Hand-maintained and deliberately conservative, like every other table in this folder: the tool
/// has no Avalonia reference to reflect over (that is what keeps it, and everything depending on
/// it, free of a transitive Avalonia dependency). A name missing from this list costs one warning
/// in one generated project; a name wrongly on it costs one helper left un-migrated. Both are
/// recoverable, and only the second is visible.
/// </para>
/// </remarks>
public static class ReservedMemberNames
{
    private static readonly IReadOnlySet<string> Names = new HashSet<string>(StringComparer.Ordinal)
    {
        // Avalonia StyledElement / Visual / Control.
        "Name", "Tag", "Parent", "Classes", "Styles", "Resources", "DataContext", "DataTemplates",
        "Bounds", "Margin", "Padding", "Width", "Height", "MinWidth", "MinHeight", "MaxWidth", "MaxHeight",
        "Background", "Foreground", "Opacity", "IsVisible", "IsEnabled", "Cursor", "Focusable", "ZIndex",
        "Focus", "Measure", "Arrange", "Render", "InvalidateVisual", "InvalidateMeasure", "InvalidateArrange",
        "BeginInit", "EndInit", "GetValue", "SetValue", "ClearValue", "Bind", "InitializeComponent",

        // Window and TopLevel.
        "Title", "Icon", "Owner", "Show", "ShowDialog", "Hide", "Close", "Activate", "Clipboard",
        "StorageProvider", "WindowState", "Topmost", "ShowInTaskbar",

        // CommunityToolkit's ObservableObject, the generated ViewModels' base.
        "OnPropertyChanged", "OnPropertyChanging", "SetProperty", "PropertyChanged", "PropertyChanging",

        // Object.
        "ToString", "Equals", "GetHashCode", "GetType",
    };

    public static bool IsReserved(string memberName) => Names.Contains(memberName);
}
