# Migration Guide: ConvertedAvalonia

**Generated**: 2026-08-10 06:37:18
**Converter Version**: 1.0.0

---

## Executive Summary

This document describes the migration of **ConvertedAvalonia** from Windows Forms to Avalonia UI.

### Conversion Statistics

- **Total Controls**: 203
- **Successfully Converted**: 203 (100%)
- **Partial Conversions**: 0
- **Placeholders**: 0
- **Total Properties Mapped**: 0/0
- **Events Converted to Commands**: 0
- **Styles Extracted**: 0

## Architectural Differences

### WinForms vs. Avalonia

| Aspect | Windows Forms | Avalonia |
|--------|--------------|----------|
| **Pattern** | Event-driven | MVVM with data binding |
| **Layout** | Absolute positioning | Declarative panels |
| **UI Thread** | STA thread required | Cross-platform threading |
| **Resources** | .resx files | .axaml dictionaries |
| **Styling** | Per-control properties | Styles and themes |
| **Platform** | Windows only | Cross-platform |

## Conversion Details

### Forms Converted

| WinForms Class | Avalonia Class | Controls | Layout | Status |
|----------------|----------------|----------|--------|--------|
| SalesOrdersListForm | SalesOrdersListForm | 1 | Canvas | Converted |
| StockOverviewForm | StockOverviewForm | 4 | Canvas | Converted |
| DashboardForm | DashboardForm | 26 | DockPanel | Converted |
| StockAdjustmentForm | StockAdjustmentForm | 2 | DockPanel | Converted |
| ReportsForm | ReportsForm | 2 | DockPanel | Converted |
| CategoriesForm | CategoriesForm | 4 | Canvas | Converted |
| CustomersForm | CustomersForm | 4 | Canvas | Converted |
| SalesOrderDetailForm | SalesOrderDetailForm | 28 | Grid | Converted |
| ProductDetailForm | ProductDetailForm | 22 | Grid | Converted |
| SettingsForm | SettingsForm | 2 | DockPanel | Converted |
| PurchaseOrdersListForm | PurchaseOrdersListForm | 1 | Canvas | Converted |
| UsersForm | UsersForm | 4 | Canvas | Converted |
| StockOutForm | StockOutForm | 14 | DockPanel | Converted |
| WarehousesForm | WarehousesForm | 12 | DockPanel | Converted |
| PurchaseOrderDetailForm | PurchaseOrderDetailForm | 25 | Grid | Converted |
| StockTransferForm | StockTransferForm | 17 | DockPanel | Converted |
| StockInForm | StockInForm | 16 | DockPanel | Converted |
| SuppliersForm | SuppliersForm | 4 | Canvas | Converted |
| LoginForm | LoginForm | 14 | Grid | Converted |
| ProductsListForm | ProductsListForm | 1 | Canvas | Converted |

## Layout Decisions

The converter analyzed control positioning to determine the best layout strategy for each form.

### SalesOrdersListForm

- **Layout Type**: Canvas
- **Confidence**: 85%
- **Reason**: Analyzed control positioning patterns

### StockOverviewForm

- **Layout Type**: Canvas
- **Confidence**: 85%
- **Reason**: Analyzed control positioning patterns

### DashboardForm

- **Layout Type**: DockPanel
- **Confidence**: 85%
- **Reason**: Analyzed control positioning patterns

### StockAdjustmentForm

- **Layout Type**: DockPanel
- **Confidence**: 85%
- **Reason**: Analyzed control positioning patterns

### ReportsForm

- **Layout Type**: DockPanel
- **Confidence**: 85%
- **Reason**: Analyzed control positioning patterns

### CategoriesForm

- **Layout Type**: Canvas
- **Confidence**: 85%
- **Reason**: Analyzed control positioning patterns

### CustomersForm

- **Layout Type**: Canvas
- **Confidence**: 85%
- **Reason**: Analyzed control positioning patterns

### SalesOrderDetailForm

- **Layout Type**: Grid
- **Confidence**: 85%
- **Reason**: Analyzed control positioning patterns

### ProductDetailForm

- **Layout Type**: Grid
- **Confidence**: 85%
- **Reason**: Analyzed control positioning patterns

### SettingsForm

- **Layout Type**: DockPanel
- **Confidence**: 85%
- **Reason**: Analyzed control positioning patterns

### PurchaseOrdersListForm

- **Layout Type**: Canvas
- **Confidence**: 85%
- **Reason**: Analyzed control positioning patterns

### UsersForm

- **Layout Type**: Canvas
- **Confidence**: 85%
- **Reason**: Analyzed control positioning patterns

### StockOutForm

- **Layout Type**: DockPanel
- **Confidence**: 85%
- **Reason**: Analyzed control positioning patterns

### WarehousesForm

- **Layout Type**: DockPanel
- **Confidence**: 85%
- **Reason**: Analyzed control positioning patterns

### PurchaseOrderDetailForm

- **Layout Type**: Grid
- **Confidence**: 85%
- **Reason**: Analyzed control positioning patterns

### StockTransferForm

- **Layout Type**: DockPanel
- **Confidence**: 85%
- **Reason**: Analyzed control positioning patterns

### StockInForm

- **Layout Type**: DockPanel
- **Confidence**: 85%
- **Reason**: Analyzed control positioning patterns

### SuppliersForm

- **Layout Type**: Canvas
- **Confidence**: 85%
- **Reason**: Analyzed control positioning patterns

### LoginForm

- **Layout Type**: Grid
- **Confidence**: 85%
- **Reason**: Analyzed control positioning patterns

### ProductsListForm

- **Layout Type**: Canvas
- **Confidence**: 85%
- **Reason**: Analyzed control positioning patterns

## Property Mappings

Common property mappings applied during conversion:

| WinForms Property | Avalonia Property | Notes |
|-------------------|-------------------|-------|
| `Text` | `Text` / `Content` | Direct mapping |
| `BackColor` | `Background` | Converted to Brush |
| `ForeColor` | `Foreground` | Converted to Brush |
| `Font` | `FontFamily`, `FontSize`, `FontWeight` | Split into multiple properties |
| `Location` | `Canvas.Left`, `Canvas.Top` | For Canvas layout |
| `Dock` | `DockPanel.Dock` | For DockPanel layout |
| `Anchor` | `Grid.Row`, `Grid.Column` | Converted to Grid positioning |

## Event to Command Conversions

Events have been converted to ICommand using CommunityToolkit.Mvvm:

```csharp
// WinForms
private void button1_Click(object sender, EventArgs e)
{
    // Handle click
}

// Avalonia ViewModel
[RelayCommand]
private void Button1Click()
{
    // Handle click
}
```

## Manual Steps Required

The following items require manual attention:

### Custom Property Logic

- **StockOverviewForm.ClientSize requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockOverviewForm.Designer.cs`
  - Description: Maps toward 'Width,Height' but the automatic converter could not fully translate this property; review the generated AXAML.

- **mainSplitContainer.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockOverviewForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **mainToolStrip.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockOverviewForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **DashboardForm.ClientSize requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: Maps toward 'Width,Height' but the automatic converter could not fully translate this property; review the generated AXAML.

- **DashboardForm.MinimumSize requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: Maps toward 'MinWidth,MinHeight' but the automatic converter could not fully translate this property; review the generated AXAML.

- **DashboardForm.WindowState requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: Maps toward 'WindowState' but the automatic converter could not fully translate this property; review the generated AXAML.

- **tilesFlowPanel.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **productsTile.Margin requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: Maps toward 'Margin' but the automatic converter could not fully translate this property; review the generated AXAML.

- **productDetailTile.Margin requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: Maps toward 'Margin' but the automatic converter could not fully translate this property; review the generated AXAML.

- **categoriesTile.Margin requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: Maps toward 'Margin' but the automatic converter could not fully translate this property; review the generated AXAML.

- **suppliersTile.Margin requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: Maps toward 'Margin' but the automatic converter could not fully translate this property; review the generated AXAML.

- **customersTile.Margin requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: Maps toward 'Margin' but the automatic converter could not fully translate this property; review the generated AXAML.

- **warehousesTile.Margin requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: Maps toward 'Margin' but the automatic converter could not fully translate this property; review the generated AXAML.

- **stockOverviewTile.Margin requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: Maps toward 'Margin' but the automatic converter could not fully translate this property; review the generated AXAML.

- **stockInTile.Margin requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: Maps toward 'Margin' but the automatic converter could not fully translate this property; review the generated AXAML.

- **stockOutTile.Margin requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: Maps toward 'Margin' but the automatic converter could not fully translate this property; review the generated AXAML.

- **stockTransferTile.Margin requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: Maps toward 'Margin' but the automatic converter could not fully translate this property; review the generated AXAML.

- **stockAdjustmentTile.Margin requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: Maps toward 'Margin' but the automatic converter could not fully translate this property; review the generated AXAML.

- **purchaseOrdersTile.Margin requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: Maps toward 'Margin' but the automatic converter could not fully translate this property; review the generated AXAML.

- **purchaseOrderDetailTile.Margin requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: Maps toward 'Margin' but the automatic converter could not fully translate this property; review the generated AXAML.

- **salesOrdersTile.Margin requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: Maps toward 'Margin' but the automatic converter could not fully translate this property; review the generated AXAML.

- **salesOrderDetailTile.Margin requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: Maps toward 'Margin' but the automatic converter could not fully translate this property; review the generated AXAML.

- **usersTile.Margin requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: Maps toward 'Margin' but the automatic converter could not fully translate this property; review the generated AXAML.

- **reportsTile.Margin requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: Maps toward 'Margin' but the automatic converter could not fully translate this property; review the generated AXAML.

- **settingsTile.Margin requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: Maps toward 'Margin' but the automatic converter could not fully translate this property; review the generated AXAML.

- **sidePanel.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **capacityGauge.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **capacityLabel.AutoSize requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: Maps toward 'HorizontalAlignment,VerticalAlignment' but the automatic converter could not fully translate this property; review the generated AXAML.

- **capacityLabel.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **capacityLabel.TextAlign requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: Maps toward 'HorizontalContentAlignment,VerticalContentAlignment' but the automatic converter could not fully translate this property; review the generated AXAML.

- **capacityLabel.Font requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: Maps toward 'FontFamily,FontSize,FontWeight' but the automatic converter could not fully translate this property; review the generated AXAML.

- **mainToolStrip.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **menuStrip.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **StockAdjustmentForm.ClientSize requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockAdjustmentForm.Designer.cs`
  - Description: Maps toward 'Width,Height' but the automatic converter could not fully translate this property; review the generated AXAML.

- **mainSplitContainer.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockAdjustmentForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **ReportsForm.ClientSize requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/ReportsForm.Designer.cs`
  - Description: Maps toward 'Width,Height' but the automatic converter could not fully translate this property; review the generated AXAML.

- **mainTabControl.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/ReportsForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **CategoriesForm.ClientSize requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/CategoriesForm.Designer.cs`
  - Description: Maps toward 'Width,Height' but the automatic converter could not fully translate this property; review the generated AXAML.

- **mainSplitContainer.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/CategoriesForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **mainToolStrip.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/CategoriesForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **CustomersForm.ClientSize requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/CustomersForm.Designer.cs`
  - Description: Maps toward 'Width,Height' but the automatic converter could not fully translate this property; review the generated AXAML.

- **mainSplitContainer.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/CustomersForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **mainToolStrip.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/CustomersForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **SalesOrderDetailForm.ClientSize requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: Maps toward 'Width,Height' but the automatic converter could not fully translate this property; review the generated AXAML.

- **headerTableLayoutPanel.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **headerTableLayoutPanel.Size requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: Maps toward 'Width,Height' but the automatic converter could not fully translate this property; review the generated AXAML.

- **orderNumberLabel.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **orderNumberLabel.TextAlign requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: Maps toward 'HorizontalContentAlignment,VerticalContentAlignment' but the automatic converter could not fully translate this property; review the generated AXAML.

- **orderNumberValueLabel.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **orderNumberValueLabel.TextAlign requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: Maps toward 'HorizontalContentAlignment,VerticalContentAlignment' but the automatic converter could not fully translate this property; review the generated AXAML.

- **orderNumberValueLabel.Font requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: Maps toward 'FontFamily,FontSize,FontWeight' but the automatic converter could not fully translate this property; review the generated AXAML.

- **customerLabel.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **customerLabel.TextAlign requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: Maps toward 'HorizontalContentAlignment,VerticalContentAlignment' but the automatic converter could not fully translate this property; review the generated AXAML.

- **customerComboBox.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **customerComboBox.Margin requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: Maps toward 'Margin' but the automatic converter could not fully translate this property; review the generated AXAML.

- **warehouseLabel.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **warehouseLabel.TextAlign requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: Maps toward 'HorizontalContentAlignment,VerticalContentAlignment' but the automatic converter could not fully translate this property; review the generated AXAML.

- **warehouseComboBox.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **warehouseComboBox.Margin requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: Maps toward 'Margin' but the automatic converter could not fully translate this property; review the generated AXAML.

- **orderDateLabel.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **orderDateLabel.TextAlign requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: Maps toward 'HorizontalContentAlignment,VerticalContentAlignment' but the automatic converter could not fully translate this property; review the generated AXAML.

- **orderDatePicker.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **orderDatePicker.Margin requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: Maps toward 'Margin' but the automatic converter could not fully translate this property; review the generated AXAML.

- **requiredDateLabel.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **requiredDateLabel.TextAlign requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: Maps toward 'HorizontalContentAlignment,VerticalContentAlignment' but the automatic converter could not fully translate this property; review the generated AXAML.

- **requiredDatePicker.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **requiredDatePicker.Margin requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: Maps toward 'Margin' but the automatic converter could not fully translate this property; review the generated AXAML.

- **statusLabel.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **statusLabel.TextAlign requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: Maps toward 'HorizontalContentAlignment,VerticalContentAlignment' but the automatic converter could not fully translate this property; review the generated AXAML.

- **statusComboBox.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **statusComboBox.Margin requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: Maps toward 'Margin' but the automatic converter could not fully translate this property; review the generated AXAML.

- **satisfactionLabel.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **satisfactionLabel.TextAlign requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: Maps toward 'HorizontalContentAlignment,VerticalContentAlignment' but the automatic converter could not fully translate this property; review the generated AXAML.

- **satisfactionRatingControl.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **satisfactionRatingControl.Margin requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: Maps toward 'Margin' but the automatic converter could not fully translate this property; review the generated AXAML.

- **notesLabel.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **notesLabel.TextAlign requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: Maps toward 'HorizontalContentAlignment,VerticalContentAlignment' but the automatic converter could not fully translate this property; review the generated AXAML.

- **notesLabel.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **notesLabel.AutoSize requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: Maps toward 'HorizontalAlignment,VerticalAlignment' but the automatic converter could not fully translate this property; review the generated AXAML.

- **notesTextBox.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **notesTextBox.Margin requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: Maps toward 'Margin' but the automatic converter could not fully translate this property; review the generated AXAML.

- **notesTextBox.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **statusBadge.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **lineEntryPanel.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **lineEntryPanel.Size requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: Maps toward 'Width,Height' but the automatic converter could not fully translate this property; review the generated AXAML.

- **productLabel.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **productLabel.AutoSize requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: Maps toward 'HorizontalAlignment,VerticalAlignment' but the automatic converter could not fully translate this property; review the generated AXAML.

- **productSearchBox.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **qtyLabel.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **qtyLabel.AutoSize requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: Maps toward 'HorizontalAlignment,VerticalAlignment' but the automatic converter could not fully translate this property; review the generated AXAML.

- **qtyNumericUpDown.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **priceLabel.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **priceLabel.AutoSize requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: Maps toward 'HorizontalAlignment,VerticalAlignment' but the automatic converter could not fully translate this property; review the generated AXAML.

- **unitPriceNumericUpDown.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **addLineButton.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **addLineButton.Size requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: Maps toward 'Width,Height' but the automatic converter could not fully translate this property; review the generated AXAML.

- **linesGrid.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **linesGrid.Size requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: Maps toward 'Width,Height' but the automatic converter could not fully translate this property; review the generated AXAML.

- **ProductDetailForm.ClientSize requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/ProductDetailForm.Designer.cs`
  - Description: Maps toward 'Width,Height' but the automatic converter could not fully translate this property; review the generated AXAML.

- **detailsGroupBox.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/ProductDetailForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **detailsGroupBox.Size requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/ProductDetailForm.Designer.cs`
  - Description: Maps toward 'Width,Height' but the automatic converter could not fully translate this property; review the generated AXAML.

- **fieldsTableLayoutPanel.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/ProductDetailForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **fieldsTableLayoutPanel.Size requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/ProductDetailForm.Designer.cs`
  - Description: Maps toward 'Width,Height' but the automatic converter could not fully translate this property; review the generated AXAML.

- **skuLabel.TextAlign requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/ProductDetailForm.Designer.cs`
  - Description: Maps toward 'HorizontalContentAlignment,VerticalContentAlignment' but the automatic converter could not fully translate this property; review the generated AXAML.

- **skuLabel.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/ProductDetailForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **skuTextBox.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/ProductDetailForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **skuTextBox.Margin requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/ProductDetailForm.Designer.cs`
  - Description: Maps toward 'Margin' but the automatic converter could not fully translate this property; review the generated AXAML.

- **nameLabel.TextAlign requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/ProductDetailForm.Designer.cs`
  - Description: Maps toward 'HorizontalContentAlignment,VerticalContentAlignment' but the automatic converter could not fully translate this property; review the generated AXAML.

- **nameLabel.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/ProductDetailForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **nameTextBox.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/ProductDetailForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **nameTextBox.Margin requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/ProductDetailForm.Designer.cs`
  - Description: Maps toward 'Margin' but the automatic converter could not fully translate this property; review the generated AXAML.

- **descriptionLabel.TextAlign requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/ProductDetailForm.Designer.cs`
  - Description: Maps toward 'HorizontalContentAlignment,VerticalContentAlignment' but the automatic converter could not fully translate this property; review the generated AXAML.

- **descriptionLabel.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/ProductDetailForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **descriptionTextBox.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/ProductDetailForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **descriptionTextBox.Margin requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/ProductDetailForm.Designer.cs`
  - Description: Maps toward 'Margin' but the automatic converter could not fully translate this property; review the generated AXAML.

- **categoryLabel.TextAlign requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/ProductDetailForm.Designer.cs`
  - Description: Maps toward 'HorizontalContentAlignment,VerticalContentAlignment' but the automatic converter could not fully translate this property; review the generated AXAML.

- **categoryLabel.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/ProductDetailForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **categoryComboBox.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/ProductDetailForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **categoryComboBox.Margin requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/ProductDetailForm.Designer.cs`
  - Description: Maps toward 'Margin' but the automatic converter could not fully translate this property; review the generated AXAML.

- **supplierLabel.TextAlign requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/ProductDetailForm.Designer.cs`
  - Description: Maps toward 'HorizontalContentAlignment,VerticalContentAlignment' but the automatic converter could not fully translate this property; review the generated AXAML.

- **supplierLabel.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/ProductDetailForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **supplierComboBox.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/ProductDetailForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **supplierComboBox.Margin requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/ProductDetailForm.Designer.cs`
  - Description: Maps toward 'Margin' but the automatic converter could not fully translate this property; review the generated AXAML.

- **unitOfMeasureLabel.TextAlign requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/ProductDetailForm.Designer.cs`
  - Description: Maps toward 'HorizontalContentAlignment,VerticalContentAlignment' but the automatic converter could not fully translate this property; review the generated AXAML.

- **unitOfMeasureLabel.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/ProductDetailForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **unitOfMeasureDomainUpDown.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/ProductDetailForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **unitOfMeasureDomainUpDown.Margin requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/ProductDetailForm.Designer.cs`
  - Description: Maps toward 'Margin' but the automatic converter could not fully translate this property; review the generated AXAML.

- **unitPriceLabel.TextAlign requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/ProductDetailForm.Designer.cs`
  - Description: Maps toward 'HorizontalContentAlignment,VerticalContentAlignment' but the automatic converter could not fully translate this property; review the generated AXAML.

- **unitPriceLabel.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/ProductDetailForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **unitPriceNumericUpDown.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/ProductDetailForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **unitPriceNumericUpDown.Margin requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/ProductDetailForm.Designer.cs`
  - Description: Maps toward 'Margin' but the automatic converter could not fully translate this property; review the generated AXAML.

- **reorderLevelLabel.TextAlign requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/ProductDetailForm.Designer.cs`
  - Description: Maps toward 'HorizontalContentAlignment,VerticalContentAlignment' but the automatic converter could not fully translate this property; review the generated AXAML.

- **reorderLevelLabel.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/ProductDetailForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **reorderLevelNumericUpDown.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/ProductDetailForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **reorderLevelNumericUpDown.Margin requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/ProductDetailForm.Designer.cs`
  - Description: Maps toward 'Margin' but the automatic converter could not fully translate this property; review the generated AXAML.

- **isActiveCheckBox.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/ProductDetailForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **isActiveCheckBox.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/ProductDetailForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **isActiveCheckBox.AutoSize requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/ProductDetailForm.Designer.cs`
  - Description: Maps toward 'HorizontalAlignment,VerticalAlignment' but the automatic converter could not fully translate this property; review the generated AXAML.

- **productPictureBox.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/ProductDetailForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **productPictureBox.Size requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/ProductDetailForm.Designer.cs`
  - Description: Maps toward 'Width,Height' but the automatic converter could not fully translate this property; review the generated AXAML.

- **productPictureBox.BorderStyle requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/ProductDetailForm.Designer.cs`
  - Description: Maps toward 'BorderBrush,BorderThickness' but the automatic converter could not fully translate this property; review the generated AXAML.

- **chooseImageButton.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/ProductDetailForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **chooseImageButton.Size requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/ProductDetailForm.Designer.cs`
  - Description: Maps toward 'Width,Height' but the automatic converter could not fully translate this property; review the generated AXAML.

- **SettingsForm.ClientSize requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SettingsForm.Designer.cs`
  - Description: Maps toward 'Width,Height' but the automatic converter could not fully translate this property; review the generated AXAML.

- **mainTabControl.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SettingsForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **UsersForm.ClientSize requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/UsersForm.Designer.cs`
  - Description: Maps toward 'Width,Height' but the automatic converter could not fully translate this property; review the generated AXAML.

- **mainSplitContainer.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/UsersForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **mainToolStrip.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/UsersForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **StockOutForm.ClientSize requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockOutForm.Designer.cs`
  - Description: Maps toward 'Width,Height' but the automatic converter could not fully translate this property; review the generated AXAML.

- **linesGrid.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockOutForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **actionPanel.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockOutForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **removeLineButton.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockOutForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **removeLineButton.Size requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockOutForm.Designer.cs`
  - Description: Maps toward 'Width,Height' but the automatic converter could not fully translate this property; review the generated AXAML.

- **postButton.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockOutForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **postButton.Size requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockOutForm.Designer.cs`
  - Description: Maps toward 'Width,Height' but the automatic converter could not fully translate this property; review the generated AXAML.

- **postButton.Font requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockOutForm.Designer.cs`
  - Description: Maps toward 'FontFamily,FontSize,FontWeight' but the automatic converter could not fully translate this property; review the generated AXAML.

- **statusLabel.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockOutForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **statusLabel.AutoSize requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockOutForm.Designer.cs`
  - Description: Maps toward 'HorizontalAlignment,VerticalAlignment' but the automatic converter could not fully translate this property; review the generated AXAML.

- **entryPanel.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockOutForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **warehouseLabel.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockOutForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **warehouseLabel.AutoSize requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockOutForm.Designer.cs`
  - Description: Maps toward 'HorizontalAlignment,VerticalAlignment' but the automatic converter could not fully translate this property; review the generated AXAML.

- **warehouseComboBox.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockOutForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **productLabel.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockOutForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **productLabel.AutoSize requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockOutForm.Designer.cs`
  - Description: Maps toward 'HorizontalAlignment,VerticalAlignment' but the automatic converter could not fully translate this property; review the generated AXAML.

- **productComboBox.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockOutForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **quantityLabel.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockOutForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **quantityLabel.AutoSize requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockOutForm.Designer.cs`
  - Description: Maps toward 'HorizontalAlignment,VerticalAlignment' but the automatic converter could not fully translate this property; review the generated AXAML.

- **quantityStepper.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockOutForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **addLineButton.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockOutForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **addLineButton.Size requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockOutForm.Designer.cs`
  - Description: Maps toward 'Width,Height' but the automatic converter could not fully translate this property; review the generated AXAML.

- **WarehousesForm.ClientSize requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/WarehousesForm.Designer.cs`
  - Description: Maps toward 'Width,Height' but the automatic converter could not fully translate this property; review the generated AXAML.

- **treeViewPanel.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/WarehousesForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **locationsTreeView.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/WarehousesForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **locationsTreeView.ImageList requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/WarehousesForm.Designer.cs`
  - Description: Maps toward 'Resources' but the automatic converter could not fully translate this property; review the generated AXAML.

- **splitter.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/WarehousesForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **detailFillPanel.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/WarehousesForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **shelfContentsListView.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/WarehousesForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **gaugePanel.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/WarehousesForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **selectedNameLabel.Font requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/WarehousesForm.Designer.cs`
  - Description: Maps toward 'FontFamily,FontSize,FontWeight' but the automatic converter could not fully translate this property; review the generated AXAML.

- **selectedNameLabel.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/WarehousesForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **selectedNameLabel.AutoSize requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/WarehousesForm.Designer.cs`
  - Description: Maps toward 'HorizontalAlignment,VerticalAlignment' but the automatic converter could not fully translate this property; review the generated AXAML.

- **capacityGauge.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/WarehousesForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **capacityDetailLabel.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/WarehousesForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **capacityDetailLabel.AutoSize requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/WarehousesForm.Designer.cs`
  - Description: Maps toward 'HorizontalAlignment,VerticalAlignment' but the automatic converter could not fully translate this property; review the generated AXAML.

- **mainToolStrip.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/WarehousesForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **PurchaseOrderDetailForm.ClientSize requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrderDetailForm.Designer.cs`
  - Description: Maps toward 'Width,Height' but the automatic converter could not fully translate this property; review the generated AXAML.

- **headerTableLayoutPanel.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrderDetailForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **headerTableLayoutPanel.Size requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrderDetailForm.Designer.cs`
  - Description: Maps toward 'Width,Height' but the automatic converter could not fully translate this property; review the generated AXAML.

- **orderNumberLabel.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrderDetailForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **orderNumberLabel.TextAlign requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrderDetailForm.Designer.cs`
  - Description: Maps toward 'HorizontalContentAlignment,VerticalContentAlignment' but the automatic converter could not fully translate this property; review the generated AXAML.

- **orderNumberValueLabel.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrderDetailForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **orderNumberValueLabel.TextAlign requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrderDetailForm.Designer.cs`
  - Description: Maps toward 'HorizontalContentAlignment,VerticalContentAlignment' but the automatic converter could not fully translate this property; review the generated AXAML.

- **orderNumberValueLabel.Font requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrderDetailForm.Designer.cs`
  - Description: Maps toward 'FontFamily,FontSize,FontWeight' but the automatic converter could not fully translate this property; review the generated AXAML.

- **supplierLabel.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrderDetailForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **supplierLabel.TextAlign requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrderDetailForm.Designer.cs`
  - Description: Maps toward 'HorizontalContentAlignment,VerticalContentAlignment' but the automatic converter could not fully translate this property; review the generated AXAML.

- **supplierComboBox.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrderDetailForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **supplierComboBox.Margin requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrderDetailForm.Designer.cs`
  - Description: Maps toward 'Margin' but the automatic converter could not fully translate this property; review the generated AXAML.

- **orderDateLabel.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrderDetailForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **orderDateLabel.TextAlign requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrderDetailForm.Designer.cs`
  - Description: Maps toward 'HorizontalContentAlignment,VerticalContentAlignment' but the automatic converter could not fully translate this property; review the generated AXAML.

- **orderDatePicker.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrderDetailForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **orderDatePicker.Margin requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrderDetailForm.Designer.cs`
  - Description: Maps toward 'Margin' but the automatic converter could not fully translate this property; review the generated AXAML.

- **expectedDateLabel.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrderDetailForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **expectedDateLabel.TextAlign requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrderDetailForm.Designer.cs`
  - Description: Maps toward 'HorizontalContentAlignment,VerticalContentAlignment' but the automatic converter could not fully translate this property; review the generated AXAML.

- **expectedDatePicker.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrderDetailForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **expectedDatePicker.Margin requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrderDetailForm.Designer.cs`
  - Description: Maps toward 'Margin' but the automatic converter could not fully translate this property; review the generated AXAML.

- **statusLabel.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrderDetailForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **statusLabel.TextAlign requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrderDetailForm.Designer.cs`
  - Description: Maps toward 'HorizontalContentAlignment,VerticalContentAlignment' but the automatic converter could not fully translate this property; review the generated AXAML.

- **statusComboBox.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrderDetailForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **statusComboBox.Margin requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrderDetailForm.Designer.cs`
  - Description: Maps toward 'Margin' but the automatic converter could not fully translate this property; review the generated AXAML.

- **notesLabel.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrderDetailForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **notesLabel.TextAlign requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrderDetailForm.Designer.cs`
  - Description: Maps toward 'HorizontalContentAlignment,VerticalContentAlignment' but the automatic converter could not fully translate this property; review the generated AXAML.

- **notesTextBox.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrderDetailForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **notesTextBox.Margin requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrderDetailForm.Designer.cs`
  - Description: Maps toward 'Margin' but the automatic converter could not fully translate this property; review the generated AXAML.

- **statusBadge.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrderDetailForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **lineEntryPanel.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrderDetailForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **lineEntryPanel.Size requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrderDetailForm.Designer.cs`
  - Description: Maps toward 'Width,Height' but the automatic converter could not fully translate this property; review the generated AXAML.

- **productLabel.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrderDetailForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **productLabel.AutoSize requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrderDetailForm.Designer.cs`
  - Description: Maps toward 'HorizontalAlignment,VerticalAlignment' but the automatic converter could not fully translate this property; review the generated AXAML.

- **productSearchBox.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrderDetailForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **qtyLabel.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrderDetailForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **qtyLabel.AutoSize requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrderDetailForm.Designer.cs`
  - Description: Maps toward 'HorizontalAlignment,VerticalAlignment' but the automatic converter could not fully translate this property; review the generated AXAML.

- **qtyNumericUpDown.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrderDetailForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **priceLabel.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrderDetailForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **priceLabel.AutoSize requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrderDetailForm.Designer.cs`
  - Description: Maps toward 'HorizontalAlignment,VerticalAlignment' but the automatic converter could not fully translate this property; review the generated AXAML.

- **unitPriceNumericUpDown.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrderDetailForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **addLineButton.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrderDetailForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **addLineButton.Size requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrderDetailForm.Designer.cs`
  - Description: Maps toward 'Width,Height' but the automatic converter could not fully translate this property; review the generated AXAML.

- **linesGrid.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrderDetailForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **linesGrid.Size requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrderDetailForm.Designer.cs`
  - Description: Maps toward 'Width,Height' but the automatic converter could not fully translate this property; review the generated AXAML.

- **printButton.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrderDetailForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **printButton.Size requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrderDetailForm.Designer.cs`
  - Description: Maps toward 'Width,Height' but the automatic converter could not fully translate this property; review the generated AXAML.

- **StockTransferForm.ClientSize requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockTransferForm.Designer.cs`
  - Description: Maps toward 'Width,Height' but the automatic converter could not fully translate this property; review the generated AXAML.

- **linesGrid.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockTransferForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **actionPanel.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockTransferForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **removeLineButton.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockTransferForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **removeLineButton.Size requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockTransferForm.Designer.cs`
  - Description: Maps toward 'Width,Height' but the automatic converter could not fully translate this property; review the generated AXAML.

- **postButton.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockTransferForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **postButton.Size requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockTransferForm.Designer.cs`
  - Description: Maps toward 'Width,Height' but the automatic converter could not fully translate this property; review the generated AXAML.

- **postButton.Font requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockTransferForm.Designer.cs`
  - Description: Maps toward 'FontFamily,FontSize,FontWeight' but the automatic converter could not fully translate this property; review the generated AXAML.

- **statusLabel.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockTransferForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **statusLabel.AutoSize requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockTransferForm.Designer.cs`
  - Description: Maps toward 'HorizontalAlignment,VerticalAlignment' but the automatic converter could not fully translate this property; review the generated AXAML.

- **entryPanel.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockTransferForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **fromLabel.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockTransferForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **fromLabel.AutoSize requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockTransferForm.Designer.cs`
  - Description: Maps toward 'HorizontalAlignment,VerticalAlignment' but the automatic converter could not fully translate this property; review the generated AXAML.

- **fromWarehouseComboBox.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockTransferForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **swapButton.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockTransferForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **swapButton.Size requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockTransferForm.Designer.cs`
  - Description: Maps toward 'Width,Height' but the automatic converter could not fully translate this property; review the generated AXAML.

- **toLabel.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockTransferForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **toLabel.AutoSize requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockTransferForm.Designer.cs`
  - Description: Maps toward 'HorizontalAlignment,VerticalAlignment' but the automatic converter could not fully translate this property; review the generated AXAML.

- **toWarehouseComboBox.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockTransferForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **productLabel.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockTransferForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **productLabel.AutoSize requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockTransferForm.Designer.cs`
  - Description: Maps toward 'HorizontalAlignment,VerticalAlignment' but the automatic converter could not fully translate this property; review the generated AXAML.

- **productComboBox.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockTransferForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **quantityLabel.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockTransferForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **quantityLabel.AutoSize requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockTransferForm.Designer.cs`
  - Description: Maps toward 'HorizontalAlignment,VerticalAlignment' but the automatic converter could not fully translate this property; review the generated AXAML.

- **quantityNumericUpDown.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockTransferForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **addLineButton.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockTransferForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **addLineButton.Size requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockTransferForm.Designer.cs`
  - Description: Maps toward 'Width,Height' but the automatic converter could not fully translate this property; review the generated AXAML.

- **StockInForm.ClientSize requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockInForm.Designer.cs`
  - Description: Maps toward 'Width,Height' but the automatic converter could not fully translate this property; review the generated AXAML.

- **linesGrid.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockInForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **actionPanel.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockInForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **removeLineButton.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockInForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **removeLineButton.Size requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockInForm.Designer.cs`
  - Description: Maps toward 'Width,Height' but the automatic converter could not fully translate this property; review the generated AXAML.

- **postButton.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockInForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **postButton.Size requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockInForm.Designer.cs`
  - Description: Maps toward 'Width,Height' but the automatic converter could not fully translate this property; review the generated AXAML.

- **postButton.Font requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockInForm.Designer.cs`
  - Description: Maps toward 'FontFamily,FontSize,FontWeight' but the automatic converter could not fully translate this property; review the generated AXAML.

- **statusLabel.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockInForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **statusLabel.AutoSize requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockInForm.Designer.cs`
  - Description: Maps toward 'HorizontalAlignment,VerticalAlignment' but the automatic converter could not fully translate this property; review the generated AXAML.

- **entryPanel.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockInForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **warehouseLabel.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockInForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **warehouseLabel.AutoSize requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockInForm.Designer.cs`
  - Description: Maps toward 'HorizontalAlignment,VerticalAlignment' but the automatic converter could not fully translate this property; review the generated AXAML.

- **warehouseComboBox.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockInForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **productLabel.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockInForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **productLabel.AutoSize requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockInForm.Designer.cs`
  - Description: Maps toward 'HorizontalAlignment,VerticalAlignment' but the automatic converter could not fully translate this property; review the generated AXAML.

- **productComboBox.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockInForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **dateLabel.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockInForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **dateLabel.AutoSize requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockInForm.Designer.cs`
  - Description: Maps toward 'HorizontalAlignment,VerticalAlignment' but the automatic converter could not fully translate this property; review the generated AXAML.

- **receiptDatePicker.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockInForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **quantityLabel.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockInForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **quantityLabel.AutoSize requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockInForm.Designer.cs`
  - Description: Maps toward 'HorizontalAlignment,VerticalAlignment' but the automatic converter could not fully translate this property; review the generated AXAML.

- **quantityStepper.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockInForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **addLineButton.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockInForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **addLineButton.Size requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockInForm.Designer.cs`
  - Description: Maps toward 'Width,Height' but the automatic converter could not fully translate this property; review the generated AXAML.

- **SuppliersForm.ClientSize requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SuppliersForm.Designer.cs`
  - Description: Maps toward 'Width,Height' but the automatic converter could not fully translate this property; review the generated AXAML.

- **mainSplitContainer.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SuppliersForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **mainToolStrip.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SuppliersForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **LoginForm.ClientSize requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/LoginForm.Designer.cs`
  - Description: Maps toward 'Width,Height' but the automatic converter could not fully translate this property; review the generated AXAML.

- **LoginForm.FormBorderStyle requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/LoginForm.Designer.cs`
  - Description: Maps toward 'CanResize' but the automatic converter could not fully translate this property; review the generated AXAML.

- **logoPictureBox.Size requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/LoginForm.Designer.cs`
  - Description: Maps toward 'Width,Height' but the automatic converter could not fully translate this property; review the generated AXAML.

- **logoPictureBox.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/LoginForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **titleLabel.Font requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/LoginForm.Designer.cs`
  - Description: Maps toward 'FontFamily,FontSize,FontWeight' but the automatic converter could not fully translate this property; review the generated AXAML.

- **titleLabel.AutoSize requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/LoginForm.Designer.cs`
  - Description: Maps toward 'HorizontalAlignment,VerticalAlignment' but the automatic converter could not fully translate this property; review the generated AXAML.

- **titleLabel.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/LoginForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **subtitleLabel.AutoSize requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/LoginForm.Designer.cs`
  - Description: Maps toward 'HorizontalAlignment,VerticalAlignment' but the automatic converter could not fully translate this property; review the generated AXAML.

- **subtitleLabel.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/LoginForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **layoutPanel.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/LoginForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **layoutPanel.Size requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/LoginForm.Designer.cs`
  - Description: Maps toward 'Width,Height' but the automatic converter could not fully translate this property; review the generated AXAML.

- **usernameLabel.TextAlign requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/LoginForm.Designer.cs`
  - Description: Maps toward 'HorizontalContentAlignment,VerticalContentAlignment' but the automatic converter could not fully translate this property; review the generated AXAML.

- **usernameLabel.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/LoginForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **usernameTextBox.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/LoginForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **usernameTextBox.Margin requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/LoginForm.Designer.cs`
  - Description: Maps toward 'Margin' but the automatic converter could not fully translate this property; review the generated AXAML.

- **passwordLabel.TextAlign requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/LoginForm.Designer.cs`
  - Description: Maps toward 'HorizontalContentAlignment,VerticalContentAlignment' but the automatic converter could not fully translate this property; review the generated AXAML.

- **passwordLabel.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/LoginForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **passwordTextBox.Dock requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/LoginForm.Designer.cs`
  - Description: Maps toward 'DockPanel.Dock' but the automatic converter could not fully translate this property; review the generated AXAML.

- **passwordTextBox.Margin requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/LoginForm.Designer.cs`
  - Description: Maps toward 'Margin' but the automatic converter could not fully translate this property; review the generated AXAML.

- **rememberMeCheckBox.AutoSize requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/LoginForm.Designer.cs`
  - Description: Maps toward 'HorizontalAlignment,VerticalAlignment' but the automatic converter could not fully translate this property; review the generated AXAML.

- **rememberMeCheckBox.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/LoginForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **loginButton.Size requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/LoginForm.Designer.cs`
  - Description: Maps toward 'Width,Height' but the automatic converter could not fully translate this property; review the generated AXAML.

- **loginButton.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/LoginForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **loginProgressBar.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/LoginForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **loginProgressBar.Size requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/LoginForm.Designer.cs`
  - Description: Maps toward 'Width,Height' but the automatic converter could not fully translate this property; review the generated AXAML.

- **loadingSpinner.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/LoginForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **loadingSpinner.Size requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/LoginForm.Designer.cs`
  - Description: Maps toward 'Width,Height' but the automatic converter could not fully translate this property; review the generated AXAML.

- **statusLabel.AutoSize requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/LoginForm.Designer.cs`
  - Description: Maps toward 'HorizontalAlignment,VerticalAlignment' but the automatic converter could not fully translate this property; review the generated AXAML.

- **statusLabel.Location requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/LoginForm.Designer.cs`
  - Description: Maps toward 'Canvas.Left,Canvas.Top' but the automatic converter could not fully translate this property; review the generated AXAML.

- **statusLabel.Size requires custom conversion logic**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/LoginForm.Designer.cs`
  - Description: Maps toward 'Width,Height' but the automatic converter could not fully translate this property; review the generated AXAML.

### Unmapped Controls

- **CardTileControl "productsTile" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: This control type has no built-in WinForms-to-Avalonia mapping; it was emitted as a TODO placeholder in the AXAML and needs a manual replacement.

- **CardTileControl "productDetailTile" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: This control type has no built-in WinForms-to-Avalonia mapping; it was emitted as a TODO placeholder in the AXAML and needs a manual replacement.

- **CardTileControl "categoriesTile" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: This control type has no built-in WinForms-to-Avalonia mapping; it was emitted as a TODO placeholder in the AXAML and needs a manual replacement.

- **CardTileControl "suppliersTile" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: This control type has no built-in WinForms-to-Avalonia mapping; it was emitted as a TODO placeholder in the AXAML and needs a manual replacement.

- **CardTileControl "customersTile" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: This control type has no built-in WinForms-to-Avalonia mapping; it was emitted as a TODO placeholder in the AXAML and needs a manual replacement.

- **CardTileControl "warehousesTile" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: This control type has no built-in WinForms-to-Avalonia mapping; it was emitted as a TODO placeholder in the AXAML and needs a manual replacement.

- **CardTileControl "stockOverviewTile" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: This control type has no built-in WinForms-to-Avalonia mapping; it was emitted as a TODO placeholder in the AXAML and needs a manual replacement.

- **CardTileControl "stockInTile" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: This control type has no built-in WinForms-to-Avalonia mapping; it was emitted as a TODO placeholder in the AXAML and needs a manual replacement.

- **CardTileControl "stockOutTile" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: This control type has no built-in WinForms-to-Avalonia mapping; it was emitted as a TODO placeholder in the AXAML and needs a manual replacement.

- **CardTileControl "stockTransferTile" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: This control type has no built-in WinForms-to-Avalonia mapping; it was emitted as a TODO placeholder in the AXAML and needs a manual replacement.

- **CardTileControl "stockAdjustmentTile" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: This control type has no built-in WinForms-to-Avalonia mapping; it was emitted as a TODO placeholder in the AXAML and needs a manual replacement.

- **CardTileControl "purchaseOrdersTile" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: This control type has no built-in WinForms-to-Avalonia mapping; it was emitted as a TODO placeholder in the AXAML and needs a manual replacement.

- **CardTileControl "purchaseOrderDetailTile" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: This control type has no built-in WinForms-to-Avalonia mapping; it was emitted as a TODO placeholder in the AXAML and needs a manual replacement.

- **CardTileControl "salesOrdersTile" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: This control type has no built-in WinForms-to-Avalonia mapping; it was emitted as a TODO placeholder in the AXAML and needs a manual replacement.

- **CardTileControl "salesOrderDetailTile" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: This control type has no built-in WinForms-to-Avalonia mapping; it was emitted as a TODO placeholder in the AXAML and needs a manual replacement.

- **CardTileControl "usersTile" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: This control type has no built-in WinForms-to-Avalonia mapping; it was emitted as a TODO placeholder in the AXAML and needs a manual replacement.

- **CardTileControl "reportsTile" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: This control type has no built-in WinForms-to-Avalonia mapping; it was emitted as a TODO placeholder in the AXAML and needs a manual replacement.

- **CardTileControl "settingsTile" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: This control type has no built-in WinForms-to-Avalonia mapping; it was emitted as a TODO placeholder in the AXAML and needs a manual replacement.

- **GaugeControl "capacityGauge" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: This control type has no built-in WinForms-to-Avalonia mapping; it was emitted as a TODO placeholder in the AXAML and needs a manual replacement.

- **StarRatingControl "satisfactionRatingControl" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: This control type has no built-in WinForms-to-Avalonia mapping; it was emitted as a TODO placeholder in the AXAML and needs a manual replacement.

- **StatusBadgeControl "statusBadge" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: This control type has no built-in WinForms-to-Avalonia mapping; it was emitted as a TODO placeholder in the AXAML and needs a manual replacement.

- **AutocompleteSearchBox "productSearchBox" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: This control type has no built-in WinForms-to-Avalonia mapping; it was emitted as a TODO placeholder in the AXAML and needs a manual replacement.

- **NumericStepperControl "quantityStepper" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockOutForm.Designer.cs`
  - Description: This control type has no built-in WinForms-to-Avalonia mapping; it was emitted as a TODO placeholder in the AXAML and needs a manual replacement.

- **GaugeControl "capacityGauge" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/WarehousesForm.Designer.cs`
  - Description: This control type has no built-in WinForms-to-Avalonia mapping; it was emitted as a TODO placeholder in the AXAML and needs a manual replacement.

- **StatusBadgeControl "statusBadge" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrderDetailForm.Designer.cs`
  - Description: This control type has no built-in WinForms-to-Avalonia mapping; it was emitted as a TODO placeholder in the AXAML and needs a manual replacement.

- **AutocompleteSearchBox "productSearchBox" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrderDetailForm.Designer.cs`
  - Description: This control type has no built-in WinForms-to-Avalonia mapping; it was emitted as a TODO placeholder in the AXAML and needs a manual replacement.

- **NumericStepperControl "quantityStepper" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockInForm.Designer.cs`
  - Description: This control type has no built-in WinForms-to-Avalonia mapping; it was emitted as a TODO placeholder in the AXAML and needs a manual replacement.

- **LoadingSpinnerControl "loadingSpinner" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/LoginForm.Designer.cs`
  - Description: This control type has no built-in WinForms-to-Avalonia mapping; it was emitted as a TODO placeholder in the AXAML and needs a manual replacement.

### Inline Lambda Event Handlers

- **productsTile.TileClicked is subscribed with an inline lambda**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: The original WinForms code subscribes this event with an inline lambda (no stable method name to extract), so it was skipped entirely instead of emitting broken generated code. Wire up the equivalent Avalonia event/command manually.

- **productDetailTile.TileClicked is subscribed with an inline lambda**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: The original WinForms code subscribes this event with an inline lambda (no stable method name to extract), so it was skipped entirely instead of emitting broken generated code. Wire up the equivalent Avalonia event/command manually.

- **categoriesTile.TileClicked is subscribed with an inline lambda**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: The original WinForms code subscribes this event with an inline lambda (no stable method name to extract), so it was skipped entirely instead of emitting broken generated code. Wire up the equivalent Avalonia event/command manually.

- **suppliersTile.TileClicked is subscribed with an inline lambda**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: The original WinForms code subscribes this event with an inline lambda (no stable method name to extract), so it was skipped entirely instead of emitting broken generated code. Wire up the equivalent Avalonia event/command manually.

- **customersTile.TileClicked is subscribed with an inline lambda**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: The original WinForms code subscribes this event with an inline lambda (no stable method name to extract), so it was skipped entirely instead of emitting broken generated code. Wire up the equivalent Avalonia event/command manually.

- **warehousesTile.TileClicked is subscribed with an inline lambda**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: The original WinForms code subscribes this event with an inline lambda (no stable method name to extract), so it was skipped entirely instead of emitting broken generated code. Wire up the equivalent Avalonia event/command manually.

- **stockOverviewTile.TileClicked is subscribed with an inline lambda**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: The original WinForms code subscribes this event with an inline lambda (no stable method name to extract), so it was skipped entirely instead of emitting broken generated code. Wire up the equivalent Avalonia event/command manually.

- **stockInTile.TileClicked is subscribed with an inline lambda**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: The original WinForms code subscribes this event with an inline lambda (no stable method name to extract), so it was skipped entirely instead of emitting broken generated code. Wire up the equivalent Avalonia event/command manually.

- **stockOutTile.TileClicked is subscribed with an inline lambda**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: The original WinForms code subscribes this event with an inline lambda (no stable method name to extract), so it was skipped entirely instead of emitting broken generated code. Wire up the equivalent Avalonia event/command manually.

- **stockTransferTile.TileClicked is subscribed with an inline lambda**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: The original WinForms code subscribes this event with an inline lambda (no stable method name to extract), so it was skipped entirely instead of emitting broken generated code. Wire up the equivalent Avalonia event/command manually.

- **stockAdjustmentTile.TileClicked is subscribed with an inline lambda**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: The original WinForms code subscribes this event with an inline lambda (no stable method name to extract), so it was skipped entirely instead of emitting broken generated code. Wire up the equivalent Avalonia event/command manually.

- **purchaseOrdersTile.TileClicked is subscribed with an inline lambda**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: The original WinForms code subscribes this event with an inline lambda (no stable method name to extract), so it was skipped entirely instead of emitting broken generated code. Wire up the equivalent Avalonia event/command manually.

- **purchaseOrderDetailTile.TileClicked is subscribed with an inline lambda**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: The original WinForms code subscribes this event with an inline lambda (no stable method name to extract), so it was skipped entirely instead of emitting broken generated code. Wire up the equivalent Avalonia event/command manually.

- **salesOrdersTile.TileClicked is subscribed with an inline lambda**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: The original WinForms code subscribes this event with an inline lambda (no stable method name to extract), so it was skipped entirely instead of emitting broken generated code. Wire up the equivalent Avalonia event/command manually.

- **salesOrderDetailTile.TileClicked is subscribed with an inline lambda**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: The original WinForms code subscribes this event with an inline lambda (no stable method name to extract), so it was skipped entirely instead of emitting broken generated code. Wire up the equivalent Avalonia event/command manually.

- **usersTile.TileClicked is subscribed with an inline lambda**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: The original WinForms code subscribes this event with an inline lambda (no stable method name to extract), so it was skipped entirely instead of emitting broken generated code. Wire up the equivalent Avalonia event/command manually.

- **reportsTile.TileClicked is subscribed with an inline lambda**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: The original WinForms code subscribes this event with an inline lambda (no stable method name to extract), so it was skipped entirely instead of emitting broken generated code. Wire up the equivalent Avalonia event/command manually.

- **settingsTile.TileClicked is subscribed with an inline lambda**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: The original WinForms code subscribes this event with an inline lambda (no stable method name to extract), so it was skipped entirely instead of emitting broken generated code. Wire up the equivalent Avalonia event/command manually.

- **removeLineButton.Click is subscribed with an inline lambda**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockOutForm.Designer.cs`
  - Description: The original WinForms code subscribes this event with an inline lambda (no stable method name to extract), so it was skipped entirely instead of emitting broken generated code. Wire up the equivalent Avalonia event/command manually.

- **postButton.Click is subscribed with an inline lambda**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockOutForm.Designer.cs`
  - Description: The original WinForms code subscribes this event with an inline lambda (no stable method name to extract), so it was skipped entirely instead of emitting broken generated code. Wire up the equivalent Avalonia event/command manually.

- **removeLineButton.Click is subscribed with an inline lambda**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockTransferForm.Designer.cs`
  - Description: The original WinForms code subscribes this event with an inline lambda (no stable method name to extract), so it was skipped entirely instead of emitting broken generated code. Wire up the equivalent Avalonia event/command manually.

- **postButton.Click is subscribed with an inline lambda**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockTransferForm.Designer.cs`
  - Description: The original WinForms code subscribes this event with an inline lambda (no stable method name to extract), so it was skipped entirely instead of emitting broken generated code. Wire up the equivalent Avalonia event/command manually.

- **swapButton.Click is subscribed with an inline lambda**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockTransferForm.Designer.cs`
  - Description: The original WinForms code subscribes this event with an inline lambda (no stable method name to extract), so it was skipped entirely instead of emitting broken generated code. Wire up the equivalent Avalonia event/command manually.

- **removeLineButton.Click is subscribed with an inline lambda**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockInForm.Designer.cs`
  - Description: The original WinForms code subscribes this event with an inline lambda (no stable method name to extract), so it was skipped entirely instead of emitting broken generated code. Wire up the equivalent Avalonia event/command manually.

- **postButton.Click is subscribed with an inline lambda**
  - Location: `/home/k0zi/Develop/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockInForm.Designer.cs`
  - Description: The original WinForms code subscribes this event with an inline lambda (no stable method name to extract), so it was skipped entirely instead of emitting broken generated code. Wire up the equivalent Avalonia event/command manually.

## Recommendations

### MVVM Best Practices

1. **Use Data Binding**: Leverage Avalonia's binding system instead of direct control manipulation
2. **Implement INotifyPropertyChanged**: Use `[ObservableProperty]` from CommunityToolkit.Mvvm
3. **Commands Over Events**: Convert remaining events to ICommand for better testability
4. **Dependency Injection**: Consider using DI for ViewModels and services

### Testing

1. **UI Testing**: Use Avalonia.Headless for automated UI tests
2. **ViewModel Testing**: Test ViewModels independently of views
3. **Integration Testing**: Test the full application flow

## Next Steps

- [ ] Review and test all converted forms
- [ ] Implement TODO comments in ViewModels
- [ ] Replace placeholder controls with Avalonia alternatives
- [ ] Test cross-platform compatibility (if applicable)
- [ ] Optimize layouts and styling
- [ ] Add unit tests for ViewModels
- [ ] Update deployment process

## Appendix

### Resources

- [Avalonia Documentation](https://docs.avaloniaui.net/)
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/)
- [Avalonia Samples](https://github.com/AvaloniaUI/Avalonia.Samples)

