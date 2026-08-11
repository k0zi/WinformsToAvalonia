using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace WarehouseAvaloniaApp.ViewModels;

/// <summary>
/// ViewModel for ProductDetailForm (auto-generated - observable properties only).
/// </summary>
public partial class ProductDetailFormViewModel
{
    [ObservableProperty]
    private string sku = string.Empty;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string description = string.Empty;

    [ObservableProperty]
    private int unitPrice = 0;

    [ObservableProperty]
    private int reorderLevel = 0;

    [ObservableProperty]
    private bool isActive = false;

    [ObservableProperty]
    private string category = string.Empty;

    [ObservableProperty]
    private string supplier = string.Empty;

}
