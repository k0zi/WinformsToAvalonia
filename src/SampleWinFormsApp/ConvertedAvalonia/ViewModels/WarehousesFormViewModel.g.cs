using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace ConvertedAvalonia.ViewModels;

/// <summary>
/// ViewModel for WarehousesForm (auto-generated - observable properties only).
/// </summary>
public partial class WarehousesFormViewModel
{
    [ObservableProperty]
    private ObservableCollection<object> shelfContents = new();

    [ObservableProperty]
    private string selectedName = string.Empty;

    [ObservableProperty]
    private int capacityGauge = 0;

    [ObservableProperty]
    private string capacityDetail = string.Empty;

    [ObservableProperty]
    private string locations = string.Empty;

}
