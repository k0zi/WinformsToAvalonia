using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace __TARGET_NAMESPACE__;

/// <summary>
/// Fallback for WinForms BindingNavigator: Avalonia has no record-navigator control (nor a
/// BindingSource - the MVVM equivalent is an ObservableCollection plus a SelectedItem on the
/// ViewModel). A horizontal StackPanel, the same shape as ToolStripFallback, so the
/// navigator's own designer-declared ToolStripItems (the move-first/previous/next/last
/// buttons, the position box, the separators) render as real children instead of being
/// dropped - a BindingNavigator is a ToolStrip subclass, and its items are parsed like any
/// other ToolStrip's.
/// </summary>
/// <remarks>
/// It navigates nothing on its own. <see cref="Position"/> and <see cref="Count"/> are here so
/// the converted code has the BindingSource's two properties to bind to; wiring them - and the
/// buttons' Click handlers - to your collection is the manual step, since Designer.cs records
/// the navigator's items but not the data behind them.
/// </remarks>
public class BindingNavigatorFallback : StackPanel
{
    public static readonly StyledProperty<int> PositionProperty =
        AvaloniaProperty.Register<BindingNavigatorFallback, int>(nameof(Position));

    public static readonly StyledProperty<int> CountProperty =
        AvaloniaProperty.Register<BindingNavigatorFallback, int>(nameof(Count));

    public BindingNavigatorFallback()
    {
        Orientation = Avalonia.Layout.Orientation.Horizontal;
        Background = Brushes.WhiteSmoke;
    }

    /// <summary>Zero-based current record index - the WinForms BindingSource.Position equivalent.</summary>
    public int Position
    {
        get => GetValue(PositionProperty);
        set => SetValue(PositionProperty, value);
    }

    /// <summary>Total record count - the WinForms BindingSource.Count equivalent.</summary>
    public int Count
    {
        get => GetValue(CountProperty);
        set => SetValue(CountProperty, value);
    }
}
