using System;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia;

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
/// <para>
/// It does navigate, but it owns no data. <see cref="Position"/> and <see cref="Count"/> are the
/// BindingSource's two properties, and the conversion binds them: <c>Count</c> to the generated
/// <c>ObservableCollection</c>'s own count, <c>Position</c> two-way to a ViewModel property the
/// bound control's <c>SelectedIndex</c> shares - which is what <c>BindingSource.Position</c>
/// actually meant.
/// </para>
/// <para>
/// The four <c>Move*</c> methods below are what the navigator's buttons did in WinForms, and the
/// generated View wires each designer-recorded button to one of them. They are here rather than in
/// the generated code so the clamping lives in one testable place - and so an empty collection
/// lands on -1, which is both what <c>BindingSource.Position</c> reported and what Avalonia reads
/// as "nothing selected".
/// </para>
/// </remarks>
public class BindingNavigatorFallback : StackPanel
{
    public static readonly StyledProperty<int> PositionProperty =
        AvaloniaProperty.Register<BindingNavigatorFallback, int>(nameof(Position));

    public static readonly StyledProperty<int> CountProperty =
        AvaloniaProperty.Register<BindingNavigatorFallback, int>(nameof(Count));

    /// <remarks>
    /// The spacing and the centring are the strip: without them a horizontal StackPanel butts
    /// its children straight up against each other and stretches them to its full height, so
    /// the sample's two status labels rendered as the single word "ReadyAll-In-One WinForms
    /// control gallery" pinned to the top edge. WinForms lays these out with a margin per item
    /// and centres them in the strip; this is that, in the two properties Avalonia spells it
    /// with.
    /// </remarks>
    public BindingNavigatorFallback()
    {
        Orientation = Avalonia.Layout.Orientation.Horizontal;
        Spacing = 6;
        Background = Brushes.WhiteSmoke;

        Styles.Add(new Style(x => x.OfType<BindingNavigatorFallback>().Child().Is<Control>())
        {
            Setters = { new Setter(VerticalAlignmentProperty, VerticalAlignment.Center) },
        });
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

    /// <summary>Moves to the first record, or to none at all when the collection is empty.</summary>
    public void MoveFirst() => Position = Count > 0 ? 0 : -1;

    /// <summary>Moves back one record, stopping at the first.</summary>
    public void MovePrevious() => Position = Count > 0 ? Math.Max(0, Position - 1) : -1;

    /// <summary>Moves on one record, stopping at the last.</summary>
    public void MoveNext() => Position = Count > 0 ? Math.Min(Count - 1, Position + 1) : -1;

    /// <summary>Moves to the last record, or to none at all when the collection is empty.</summary>
    public void MoveLast() => Position = Count - 1;
}
