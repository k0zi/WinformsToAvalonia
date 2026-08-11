using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace ConvertedAvalonia.ViewModels;

/// <summary>
/// ViewModel for PurchaseOrderDetailForm (auto-generated - observable properties only).
/// </summary>
public partial class PurchaseOrderDetailFormViewModel
{
    [ObservableProperty]
    private string productSearchBox = string.Empty;

    [ObservableProperty]
    private int unitPrice = 0;

    [ObservableProperty]
    private int orderDatePicker = 0;

    [ObservableProperty]
    private int expectedDatePicker = 0;

    [ObservableProperty]
    private string notes = string.Empty;

    [ObservableProperty]
    private string orderNumberValue = string.Empty;

    [ObservableProperty]
    private string status = string.Empty;

    [ObservableProperty]
    private string supplier = string.Empty;

    [ObservableProperty]
    private string linesGrid = string.Empty;

}
