using Avalonia.Controls;

namespace ConvertedAvalonia.Views;

public partial class AutocompleteSearchBox : UserControl
{
    public AutocompleteSearchBox()
    {
        InitializeComponent();
        DataContext = new ConvertedAvalonia.ViewModels.AutocompleteSearchBoxViewModel();
    }

    private ConvertedAvalonia.ViewModels.AutocompleteSearchBoxViewModel ViewModel => (ConvertedAvalonia.ViewModels.AutocompleteSearchBoxViewModel)DataContext!;

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
