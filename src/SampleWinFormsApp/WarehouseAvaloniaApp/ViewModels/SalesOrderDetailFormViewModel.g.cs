using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace WarehouseAvaloniaApp.ViewModels;

/// <summary>
/// ViewModel for SalesOrderDetailForm (auto-generated - observable properties only).
/// </summary>
public partial class SalesOrderDetailFormViewModel
{
    [ObservableProperty]
    private string productSearchBox = string.Empty;

    [ObservableProperty]
    private int unitPrice = 0;

    [ObservableProperty]
    private int orderDatePicker = 0;

    [ObservableProperty]
    private int requiredDatePicker = 0;

    [ObservableProperty]
    private string notes = string.Empty;

    [ObservableProperty]
    private int satisfactionRatingControl = 0;

    [ObservableProperty]
    private string status = string.Empty;

    [ObservableProperty]
    private string customer = string.Empty;

    [ObservableProperty]
    private string warehouse = string.Empty;

    [ObservableProperty]
    private string linesGrid = string.Empty;

}
