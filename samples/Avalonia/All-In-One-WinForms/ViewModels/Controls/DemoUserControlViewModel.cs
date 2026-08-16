using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using All_In_One_WinForms.Generated;

namespace All_In_One_WinForms.ViewModels.Controls;

public sealed partial class DemoUserControlViewModel : ViewModelBase
{
    /// <summary>Bound to counterLabel.Text in the view.</summary>
    [ObservableProperty]
    public partial string CounterLabelText { get; set; } = "0";

    [RelayCommand]
    private void IncrementButton()
    {
        /* ORIGINAL WINFORMS BODY of 'incrementButton_Click' - TODO(Winforms2Avalonia): rewrite it against this ViewModel's properties.
        this.counterLabel.Text = (int.Parse(this.counterLabel.Text) + 1).ToString();
        */
        MigrationTodo.NotMigrated(nameof(IncrementButton), "incrementButton_Click");
    }
}
