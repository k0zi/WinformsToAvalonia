using CommunityToolkit.Mvvm.ComponentModel;

namespace All_In_One_WinForms.Models;

/// <summary>One row of 'checkedListBox1', which was a WinForms CheckedListBox.</summary>
public sealed partial class CheckedListBox1Item : ObservableObject
{
    /// <summary>The caption - what the WinForms item's ToString() showed.</summary>
    [ObservableProperty]
    public partial string Text { get; set; } = string.Empty;

    /// <summary>The tick. This is the state WinForms kept separately from selection.</summary>
    [ObservableProperty]
    public partial bool IsChecked { get; set; }
}
