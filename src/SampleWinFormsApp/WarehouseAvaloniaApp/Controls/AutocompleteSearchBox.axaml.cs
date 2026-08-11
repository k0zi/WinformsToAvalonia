using Avalonia.Controls;

namespace WarehouseAvaloniaApp.Controls;

public partial class AutocompleteSearchBox : UserControl
{
    public AutocompleteSearchBox()
    {
        InitializeComponent();
        DataContext = new WarehouseAvaloniaApp.ViewModels.AutocompleteSearchBoxViewModel();
    }

    private WarehouseAvaloniaApp.ViewModels.AutocompleteSearchBoxViewModel ViewModel => (WarehouseAvaloniaApp.ViewModels.AutocompleteSearchBoxViewModel)DataContext!;

    public static readonly Avalonia.StyledProperty<string> DisplayMemberProperty =
        Avalonia.AvaloniaProperty.Register<AutocompleteSearchBox, string>(nameof(DisplayMember));

    public string DisplayMember
    {
        get => GetValue(DisplayMemberProperty);
        set => SetValue(DisplayMemberProperty, value);
    }

    public static readonly Avalonia.StyledProperty<object?> SelectedItemProperty =
        Avalonia.AvaloniaProperty.Register<AutocompleteSearchBox, object?>(nameof(SelectedItem));

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public string Text
    {
        get => _textBox.Text;
        set => _textBox.Text = value ?? string.Empty;
    }

    public string PlaceholderText
    {
        get => _textBox.PlaceholderText;
        set => _textBox.PlaceholderText = value;
    }

    private void TextBox_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
    {
            if (ViewModel._popupList is null || !ViewModel._popupList.Visible)
            {
                return;
            }
    
            switch (e.KeyCode)
            {
                case Keys.Down:
                    if (ViewModel._popupList.SelectedIndex < ViewModel._popupList.Items.Count - 1)
                    {
                        ViewModel._popupList.SelectedIndex++;
                    }
                    e.Handled = true;
                    break;
                case Keys.Up:
                    if (ViewModel._popupList.SelectedIndex > 0)
                    {
                        ViewModel._popupList.SelectedIndex--;
                    }
                    e.Handled = true;
                    break;
                case Keys.Enter:
                    if (ViewModel._popupList.SelectedItem is object item)
                    {
                        ViewModel.CommitSelection(item);
                    }
                    e.Handled = true;
                    break;
                case Keys.Escape:
                    ViewModel.HidePopup();
                    e.Handled = true;
                    break;
            }
        }

    private void _textBox_LostFocus_InlineHandler(object? sender, Avalonia.Input.FocusChangedEventArgs e)
    {
        ViewModel.ClosePopupSoon();
    }
}
