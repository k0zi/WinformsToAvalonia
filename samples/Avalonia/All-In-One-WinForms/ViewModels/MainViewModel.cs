using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using All_In_One_WinForms.Generated;

namespace All_In_One_WinForms.ViewModels;

public sealed partial class MainViewModel : ViewModelBase
{
    /// <summary>Bound to titleTextBox.Text in the view.</summary>
    [ObservableProperty]
    public partial string TitleTextBoxText { get; set; } = "TextBox";

    /// <summary>Bound to captionLabel.Text in the view.</summary>
    [ObservableProperty]
    public partial string CaptionLabelText { get; set; } = "Label - plain static text";

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
    private void DemoButton()
    {
        /* ORIGINAL WINFORMS BODY of 'demoButton_Click' - TODO(Winforms2Avalonia): rewrite it against this ViewModel's properties.
        MessageBox.Show(this, $"Hello, {this.titleTextBox.Text}!", "All-In-One");
        */
        MigrationTodo.NotMigrated(nameof(DemoButton), "demoButton_Click");
    }

    [RelayCommand]
    private void ApplyButton()
    {
        /* ORIGINAL WINFORMS BODY of 'applyButton_Click' - TODO(Winforms2Avalonia): rewrite it against this ViewModel's properties.
        this.captionLabel.Text = this.titleTextBox.Text;
        this.enabledCheckBox.Checked = true;
        */
        MigrationTodo.NotMigrated(nameof(ApplyButton), "applyButton_Click");
    }

    [RelayCommand]
    private void ResetButton()
    {
        /* ORIGINAL WINFORMS BODY of 'resetButton_Click' - TODO(Winforms2Avalonia): rewrite it against this ViewModel's properties.
        this.titleTextBox.Text = string.Empty;
        this.enabledCheckBox.Checked = false;
        this.amountUpDown.Value = 0;
        this.itemsComboBox.SelectedIndex = -1;
        */
        MigrationTodo.NotMigrated(nameof(ResetButton), "resetButton_Click");
    }
}
