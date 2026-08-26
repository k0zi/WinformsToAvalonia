using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using All_In_One_WinForms.Generated;

namespace All_In_One_WinForms.ViewModels;

public sealed partial class MainViewModel : ViewModelBase
{
    /// <summary>Bound to captionLabel.Text in the view.</summary>
    [ObservableProperty]
    public partial string CaptionLabelText { get; set; } = "Label - plain static text";

    /// <summary>Bound to titleTextBox.Text in the view.</summary>
    [ObservableProperty]
    public partial string TitleTextBoxText { get; set; } = "TextBox";

    /// <summary>Bound to enabledCheckBox.IsChecked in the view.</summary>
    [ObservableProperty]
    public partial bool EnabledCheckBoxIsChecked { get; set; } = true;

    /// <summary>Bound to amountUpDown.Value in the view.</summary>
    [ObservableProperty]
    public partial decimal? AmountUpDownValue { get; set; }

    /// <summary>Bound to itemsComboBox.SelectedIndex in the view.</summary>
    [ObservableProperty]
    public partial int ItemsComboBoxSelectedIndex { get; set; }

    [RelayCommand]
    private void ApplyButton()
    {
        CaptionLabelText = TitleTextBoxText;
        EnabledCheckBoxIsChecked = true;
    }

    [RelayCommand]
    private void ResetButton()
    {
        TitleTextBoxText = string.Empty;
        EnabledCheckBoxIsChecked = false;
        AmountUpDownValue = 0;
        ItemsComboBoxSelectedIndex = -1;
    }
}
