using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;

namespace __TARGET_NAMESPACE__;

/// <summary>
/// Fallback for WinForms DomainUpDown: a text-based spinner that cycles through a fixed list
/// of strings (Avalonia has no built-in equivalent - NumericUpDown only cycles numbers).
/// Composed directly (no separate XAML template, same convention as every other bundled
/// fallback control) from a read-only TextBox display plus two step buttons docked to the
/// right, matching WinForms DomainUpDown's editable-look/spin-button shape closely enough to
/// be a drop-in functional replacement. <see cref="Items"/> isn't populated automatically -
/// WinForms' `this.domainUpDown1.Items.Add(...)` calls aren't parsed yet (same limitation as
/// ToolStripItem/DataGridView column trees, see docs/known-limitations.md) - populate it by
/// hand or bind it from the ViewModel.
/// </summary>
public class DomainUpDownFallback : DockPanel
{
    public static readonly StyledProperty<int> SelectedIndexProperty =
        AvaloniaProperty.Register<DomainUpDownFallback, int>(nameof(SelectedIndex), -1);

    public static readonly StyledProperty<bool> WrapProperty =
        AvaloniaProperty.Register<DomainUpDownFallback, bool>(nameof(Wrap), true);

    private readonly TextBox _display = new() { IsReadOnly = true };

    public AvaloniaList<string> Items { get; } = [];

    public int SelectedIndex
    {
        get => GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    public bool Wrap
    {
        get => GetValue(WrapProperty);
        set => SetValue(WrapProperty, value);
    }

    public DomainUpDownFallback()
    {
        var upButton = new Button { Content = "▲" };
        upButton.Click += (_, _) => Step(1);

        var downButton = new Button { Content = "▼" };
        downButton.Click += (_, _) => Step(-1);

        var buttonsPanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Vertical };
        buttonsPanel.Children.Add(upButton);
        buttonsPanel.Children.Add(downButton);
        DockPanel.SetDock(buttonsPanel, Dock.Right);

        Children.Add(buttonsPanel);
        Children.Add(_display);

        UpdateDisplayText();
    }

    private void Step(int direction)
    {
        if (Items.Count == 0)
        {
            return;
        }

        var next = SelectedIndex + direction;
        SelectedIndex = Wrap
            ? ((next % Items.Count) + Items.Count) % Items.Count
            : Math.Clamp(next, 0, Items.Count - 1);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SelectedIndexProperty)
        {
            UpdateDisplayText();
        }
    }

    private void UpdateDisplayText()
    {
        _display.Text = SelectedIndex >= 0 && SelectedIndex < Items.Count ? Items[SelectedIndex] : "";
    }
}
