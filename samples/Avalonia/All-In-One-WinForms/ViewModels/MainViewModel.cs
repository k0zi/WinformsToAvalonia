using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using All_In_One_WinForms.Generated;

namespace All_In_One_WinForms.ViewModels;

public sealed partial class MainViewModel : ViewModelBase
{
    /// <summary>Bound to dataGridView1.ItemsSource in the view, replacing
    /// the WinForms BindingSource 'bindingSource1'. Generated empty: the wiring is
    /// here, the rows are not - populate it where the WinForms code set DataSource.</summary>
    public ObservableCollection<object> DataGridView1Items { get; } = [];

    /// <summary>Bound to toolStripProgressBar1.Value in the view.</summary>
    [ObservableProperty]
    public partial double ToolStripProgressBar1Value { get; set; }

    /// <summary>Bound to statusLabel.Text in the view.</summary>
    [ObservableProperty]
    public partial string StatusLabelText { get; set; } = "Ready";

    /// <summary>Bound to linkLabel1.IsVisited in the view.</summary>
    [ObservableProperty]
    public partial bool LinkLabel1IsVisited { get; set; }

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
    private void ToolStripNewButton()
    {
        ToolStripProgressBar1Value = Math.Min(100, ToolStripProgressBar1Value + 10);
        StatusLabelText = "Toolbar: new";
    }

    [RelayCommand]
    private void LinkLabel1()
    {
        LinkLabel1IsVisited = true;
        StatusLabelText = "Link clicked";
    }

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
