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
        CounterLabelText = (int.Parse(CounterLabelText) + 1).ToString();
    }
}
