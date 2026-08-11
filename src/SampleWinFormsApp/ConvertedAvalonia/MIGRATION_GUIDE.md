# Migration Guide: ConvertedAvalonia

**Generated**: 2026-08-11 11:58:21
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
| SalesOrderDetailForm | SalesOrderDetailForm | 28 | Grid | Converted |
| LoginForm | LoginForm | 14 | Grid | Converted |
| ReportsForm | ReportsForm | 2 | DockPanel | Converted |
| WarehousesForm | WarehousesForm | 12 | DockPanel | Converted |
| CustomersForm | CustomersForm | 4 | Canvas | Converted |
| PurchaseOrderDetailForm | PurchaseOrderDetailForm | 25 | Grid | Converted |
| CategoriesForm | CategoriesForm | 4 | Canvas | Converted |
| StockInForm | StockInForm | 16 | DockPanel | Converted |
| SalesOrdersListForm | SalesOrdersListForm | 1 | Canvas | Converted |
| StockOverviewForm | StockOverviewForm | 4 | Canvas | Converted |
| StockTransferForm | StockTransferForm | 17 | DockPanel | Converted |
| ProductsListForm | ProductsListForm | 1 | Canvas | Converted |
| DashboardForm | DashboardForm | 26 | DockPanel | Converted |
| ProductDetailForm | ProductDetailForm | 22 | Grid | Converted |
| SuppliersForm | SuppliersForm | 4 | Canvas | Converted |
| StockOutForm | StockOutForm | 14 | DockPanel | Converted |
| PurchaseOrdersListForm | PurchaseOrdersListForm | 1 | Canvas | Converted |
| UsersForm | UsersForm | 4 | Canvas | Converted |
| SettingsForm | SettingsForm | 2 | DockPanel | Converted |
| StockAdjustmentForm | StockAdjustmentForm | 2 | DockPanel | Converted |

## Layout Decisions

The converter analyzed control positioning to determine the best layout strategy for each form.

### SalesOrderDetailForm

- **Layout Type**: Grid
- **Confidence**: 85%
- **Reason**: Analyzed control positioning patterns

### LoginForm

- **Layout Type**: Grid
- **Confidence**: 85%
- **Reason**: Analyzed control positioning patterns

### ReportsForm

- **Layout Type**: DockPanel
- **Confidence**: 85%
- **Reason**: Analyzed control positioning patterns

### WarehousesForm

- **Layout Type**: DockPanel
- **Confidence**: 85%
- **Reason**: Analyzed control positioning patterns

### CustomersForm

- **Layout Type**: Canvas
- **Confidence**: 85%
- **Reason**: Analyzed control positioning patterns

### PurchaseOrderDetailForm

- **Layout Type**: Grid
- **Confidence**: 85%
- **Reason**: Analyzed control positioning patterns

### CategoriesForm

- **Layout Type**: Canvas
- **Confidence**: 85%
- **Reason**: Analyzed control positioning patterns

### StockInForm

- **Layout Type**: DockPanel
- **Confidence**: 85%
- **Reason**: Analyzed control positioning patterns

### SalesOrdersListForm

- **Layout Type**: Canvas
- **Confidence**: 85%
- **Reason**: Analyzed control positioning patterns

### StockOverviewForm

- **Layout Type**: Canvas
- **Confidence**: 85%
- **Reason**: Analyzed control positioning patterns

### StockTransferForm

- **Layout Type**: DockPanel
- **Confidence**: 85%
- **Reason**: Analyzed control positioning patterns

### ProductsListForm

- **Layout Type**: Canvas
- **Confidence**: 85%
- **Reason**: Analyzed control positioning patterns

### DashboardForm

- **Layout Type**: DockPanel
- **Confidence**: 85%
- **Reason**: Analyzed control positioning patterns

### ProductDetailForm

- **Layout Type**: Grid
- **Confidence**: 85%
- **Reason**: Analyzed control positioning patterns

### SuppliersForm

- **Layout Type**: Canvas
- **Confidence**: 85%
- **Reason**: Analyzed control positioning patterns

### StockOutForm

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

### SettingsForm

- **Layout Type**: DockPanel
- **Confidence**: 85%
- **Reason**: Analyzed control positioning patterns

### StockAdjustmentForm

- **Layout Type**: DockPanel
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

### Unmapped Controls

- **StarRatingControl "satisfactionRatingControl" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: This control type has no built-in WinForms-to-Avalonia mapping; it was emitted as a TODO placeholder in the AXAML and needs a manual replacement.

- **StatusBadgeControl "statusBadge" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: This control type has no built-in WinForms-to-Avalonia mapping; it was emitted as a TODO placeholder in the AXAML and needs a manual replacement.

- **AutocompleteSearchBox "productSearchBox" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: This control type has no built-in WinForms-to-Avalonia mapping; it was emitted as a TODO placeholder in the AXAML and needs a manual replacement.

- **LoadingSpinnerControl "loadingSpinner" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/LoginForm.Designer.cs`
  - Description: This control type has no built-in WinForms-to-Avalonia mapping; it was emitted as a TODO placeholder in the AXAML and needs a manual replacement.

- **GaugeControl "capacityGauge" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/WarehousesForm.Designer.cs`
  - Description: This control type has no built-in WinForms-to-Avalonia mapping; it was emitted as a TODO placeholder in the AXAML and needs a manual replacement.

- **StatusBadgeControl "statusBadge" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrderDetailForm.Designer.cs`
  - Description: This control type has no built-in WinForms-to-Avalonia mapping; it was emitted as a TODO placeholder in the AXAML and needs a manual replacement.

- **AutocompleteSearchBox "productSearchBox" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrderDetailForm.Designer.cs`
  - Description: This control type has no built-in WinForms-to-Avalonia mapping; it was emitted as a TODO placeholder in the AXAML and needs a manual replacement.

- **NumericStepperControl "quantityStepper" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockInForm.Designer.cs`
  - Description: This control type has no built-in WinForms-to-Avalonia mapping; it was emitted as a TODO placeholder in the AXAML and needs a manual replacement.

- **CardTileControl "productsTile" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: This control type has no built-in WinForms-to-Avalonia mapping; it was emitted as a TODO placeholder in the AXAML and needs a manual replacement.

- **CardTileControl "productDetailTile" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: This control type has no built-in WinForms-to-Avalonia mapping; it was emitted as a TODO placeholder in the AXAML and needs a manual replacement.

- **CardTileControl "categoriesTile" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: This control type has no built-in WinForms-to-Avalonia mapping; it was emitted as a TODO placeholder in the AXAML and needs a manual replacement.

- **CardTileControl "suppliersTile" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: This control type has no built-in WinForms-to-Avalonia mapping; it was emitted as a TODO placeholder in the AXAML and needs a manual replacement.

- **CardTileControl "customersTile" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: This control type has no built-in WinForms-to-Avalonia mapping; it was emitted as a TODO placeholder in the AXAML and needs a manual replacement.

- **CardTileControl "warehousesTile" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: This control type has no built-in WinForms-to-Avalonia mapping; it was emitted as a TODO placeholder in the AXAML and needs a manual replacement.

- **CardTileControl "stockOverviewTile" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: This control type has no built-in WinForms-to-Avalonia mapping; it was emitted as a TODO placeholder in the AXAML and needs a manual replacement.

- **CardTileControl "stockInTile" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: This control type has no built-in WinForms-to-Avalonia mapping; it was emitted as a TODO placeholder in the AXAML and needs a manual replacement.

- **CardTileControl "stockOutTile" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: This control type has no built-in WinForms-to-Avalonia mapping; it was emitted as a TODO placeholder in the AXAML and needs a manual replacement.

- **CardTileControl "stockTransferTile" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: This control type has no built-in WinForms-to-Avalonia mapping; it was emitted as a TODO placeholder in the AXAML and needs a manual replacement.

- **CardTileControl "stockAdjustmentTile" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: This control type has no built-in WinForms-to-Avalonia mapping; it was emitted as a TODO placeholder in the AXAML and needs a manual replacement.

- **CardTileControl "purchaseOrdersTile" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: This control type has no built-in WinForms-to-Avalonia mapping; it was emitted as a TODO placeholder in the AXAML and needs a manual replacement.

- **CardTileControl "purchaseOrderDetailTile" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: This control type has no built-in WinForms-to-Avalonia mapping; it was emitted as a TODO placeholder in the AXAML and needs a manual replacement.

- **CardTileControl "salesOrdersTile" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: This control type has no built-in WinForms-to-Avalonia mapping; it was emitted as a TODO placeholder in the AXAML and needs a manual replacement.

- **CardTileControl "salesOrderDetailTile" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: This control type has no built-in WinForms-to-Avalonia mapping; it was emitted as a TODO placeholder in the AXAML and needs a manual replacement.

- **CardTileControl "usersTile" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: This control type has no built-in WinForms-to-Avalonia mapping; it was emitted as a TODO placeholder in the AXAML and needs a manual replacement.

- **CardTileControl "reportsTile" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: This control type has no built-in WinForms-to-Avalonia mapping; it was emitted as a TODO placeholder in the AXAML and needs a manual replacement.

- **CardTileControl "settingsTile" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: This control type has no built-in WinForms-to-Avalonia mapping; it was emitted as a TODO placeholder in the AXAML and needs a manual replacement.

- **GaugeControl "capacityGauge" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: This control type has no built-in WinForms-to-Avalonia mapping; it was emitted as a TODO placeholder in the AXAML and needs a manual replacement.

- **NumericStepperControl "quantityStepper" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockOutForm.Designer.cs`
  - Description: This control type has no built-in WinForms-to-Avalonia mapping; it was emitted as a TODO placeholder in the AXAML and needs a manual replacement.

### Migrated Logic May Not Compile

- **SalesOrderDetailForm.ValidateInput references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: This method was migrated as live code, but its body references MessageBox, MessageBoxButtons, MessageBoxIcon - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

- **SalesOrderDetailForm.PersistAsync references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: This method was migrated as live code, but its body references DataGridViewRow - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

- **ReportsForm.LoadAuditLogAsync references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/ReportsForm.Designer.cs`
  - Description: This method was migrated as live code, but its body references ListViewItem - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

- **ReportsForm.exportCsvButton_Click references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/ReportsForm.Designer.cs`
  - Description: This method was migrated as live code, but its body references DialogResult, MessageBox, MessageBoxButtons, MessageBoxIcon - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

- **ReportsForm.chooseFontButton_Click references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/ReportsForm.Designer.cs`
  - Description: This method was migrated as live code, but its body references DialogResult - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

- **ReportsForm.PrintDocument_PrintPage references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/ReportsForm.Designer.cs`
  - Description: This method was migrated as live code, but its body references PrintPageEventArgs - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

- **WarehousesForm.LoadTreeAsync references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/WarehousesForm.Designer.cs`
  - Description: This method was migrated as live code, but its body references TreeNode - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

- **WarehousesForm.LoadShelfContentsAsync references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/WarehousesForm.Designer.cs`
  - Description: This method was migrated as live code, but its body references ListViewItem - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

- **WarehousesForm.AddZone references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/WarehousesForm.Designer.cs`
  - Description: This method was migrated as live code, but its body references MessageBox, MessageBoxButtons, MessageBoxIcon - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

- **WarehousesForm.AddShelf references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/WarehousesForm.Designer.cs`
  - Description: This method was migrated as live code, but its body references MessageBox, MessageBoxButtons, MessageBoxIcon - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

- **WarehousesForm.DeleteSelectedNodeAsync references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/WarehousesForm.Designer.cs`
  - Description: This method was migrated as live code, but its body references MessageBox, MessageBoxButtons, MessageBoxIcon, DialogResult - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

- **CustomersForm.LoadOrdersForCustomerAsync references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/CustomersForm.Designer.cs`
  - Description: This method was migrated as live code, but its body references ListViewItem - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

- **CustomersForm.SaveCustomerAsync references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/CustomersForm.Designer.cs`
  - Description: This method was migrated as live code, but its body references MessageBox, MessageBoxButtons, MessageBoxIcon - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

- **CustomersForm.DeleteCustomerAsync references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/CustomersForm.Designer.cs`
  - Description: This method was migrated as live code, but its body references MessageBox, MessageBoxButtons, MessageBoxIcon, DialogResult - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

- **PurchaseOrderDetailForm.ValidateInput references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrderDetailForm.Designer.cs`
  - Description: This method was migrated as live code, but its body references MessageBox, MessageBoxButtons, MessageBoxIcon - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

- **PurchaseOrderDetailForm.PersistAsync references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrderDetailForm.Designer.cs`
  - Description: This method was migrated as live code, but its body references DataGridViewRow - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

- **PurchaseOrderDetailForm.PrintDocument_PrintPage references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrderDetailForm.Designer.cs`
  - Description: This method was migrated as live code, but its body references PrintPageEventArgs, DataGridViewRow - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

- **CategoriesForm.BuildNode references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/CategoriesForm.Designer.cs`
  - Description: This method was migrated as live code, but its body references TreeNode - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

- **CategoriesForm.CategoriesTreeView_AfterSelect references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/CategoriesForm.Designer.cs`
  - Description: This method was migrated as live code, but its body references TreeViewEventArgs - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

- **CategoriesForm.SaveCategoryAsync references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/CategoriesForm.Designer.cs`
  - Description: This method was migrated as live code, but its body references MessageBox, MessageBoxButtons, MessageBoxIcon - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

- **CategoriesForm.DeleteCategoryAsync references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/CategoriesForm.Designer.cs`
  - Description: This method was migrated as live code, but its body references MessageBox, MessageBoxButtons, MessageBoxIcon, DialogResult - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

- **StockInForm.PostReceiptAsync references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockInForm.Designer.cs`
  - Description: This method was migrated as live code, but its body references MessageBox, MessageBoxButtons, MessageBoxIcon, DataGridViewRow - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

- **SalesOrdersListForm.AddNew references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrdersListForm.Designer.cs`
  - Description: This method was migrated as live code, but its body references DialogResult - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

- **SalesOrdersListForm.EditEntity references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrdersListForm.Designer.cs`
  - Description: This method was migrated as live code, but its body references DialogResult - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

- **SalesOrdersListForm.Grid_CellFormatting references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrdersListForm.Designer.cs`
  - Description: This method was migrated as live code, but its body references DataGridViewCellFormattingEventArgs - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

- **StockOverviewForm.StockGrid_CellFormatting references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockOverviewForm.Designer.cs`
  - Description: This method was migrated as live code, but its body references DataGridViewCellFormattingEventArgs - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

- **StockTransferForm.PostTransferAsync references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockTransferForm.Designer.cs`
  - Description: This method was migrated as live code, but its body references MessageBox, MessageBoxButtons, MessageBoxIcon, DataGridViewRow - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

- **ProductsListForm.AddNew references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/ProductsListForm.Designer.cs`
  - Description: This method was migrated as live code, but its body references DialogResult - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

- **ProductsListForm.EditEntity references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/ProductsListForm.Designer.cs`
  - Description: This method was migrated as live code, but its body references DialogResult - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

- **ProductsListForm.Grid_CellFormatting references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/ProductsListForm.Designer.cs`
  - Description: This method was migrated as live code, but its body references DataGridViewCellFormattingEventArgs - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

- **ProductsListForm.quickAddDuplicate_Click references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/ProductsListForm.Designer.cs`
  - Description: This method was migrated as live code, but its body references MessageBox, MessageBoxButtons, MessageBoxIcon, DialogResult - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

- **DashboardForm.DashboardForm_FormClosing references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: This method was migrated as live code, but its body references FormClosingEventArgs - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

- **DashboardForm.OpenForm references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: This method was migrated as live code, but its body references Form - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

- **SuppliersForm.LoadSuppliersAsync references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SuppliersForm.Designer.cs`
  - Description: This method was migrated as live code, but its body references ListViewItem - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

- **SuppliersForm.SaveSupplierAsync references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SuppliersForm.Designer.cs`
  - Description: This method was migrated as live code, but its body references MessageBox, MessageBoxButtons, MessageBoxIcon - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

- **SuppliersForm.DeleteSupplierAsync references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SuppliersForm.Designer.cs`
  - Description: This method was migrated as live code, but its body references MessageBox, MessageBoxButtons, MessageBoxIcon, DialogResult - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

- **SuppliersForm.EmailLinkLabel_LinkClicked references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SuppliersForm.Designer.cs`
  - Description: This method was migrated as live code, but its body references LinkLabelLinkClickedEventArgs, MessageBox, MessageBoxButtons, MessageBoxIcon - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

- **StockOutForm.PostIssueAsync references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockOutForm.Designer.cs`
  - Description: This method was migrated as live code, but its body references MessageBox, MessageBoxButtons, MessageBoxIcon, DataGridViewRow - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

- **PurchaseOrdersListForm.AddNew references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrdersListForm.Designer.cs`
  - Description: This method was migrated as live code, but its body references DialogResult - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

- **PurchaseOrdersListForm.EditEntity references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrdersListForm.Designer.cs`
  - Description: This method was migrated as live code, but its body references DialogResult - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

- **PurchaseOrdersListForm.Grid_CellFormatting references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrdersListForm.Designer.cs`
  - Description: This method was migrated as live code, but its body references DataGridViewCellFormattingEventArgs - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

- **UsersForm.UsersGrid_CellFormatting references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/UsersForm.Designer.cs`
  - Description: This method was migrated as live code, but its body references DataGridViewCellFormattingEventArgs - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

- **UsersForm.SaveUserAsync references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/UsersForm.Designer.cs`
  - Description: This method was migrated as live code, but its body references MessageBox, MessageBoxButtons, MessageBoxIcon - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

- **UsersForm.DeleteUserAsync references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/UsersForm.Designer.cs`
  - Description: This method was migrated as live code, but its body references MessageBox, MessageBoxButtons, MessageBoxIcon, DialogResult - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

- **SettingsForm.chooseColorButton_Click references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SettingsForm.Designer.cs`
  - Description: This method was migrated as live code, but its body references DialogResult - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

- **SettingsForm.browseFolderButton_Click references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SettingsForm.Designer.cs`
  - Description: This method was migrated as live code, but its body references DialogResult - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

- **SettingsForm.SaveGeneralAsync references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SettingsForm.Designer.cs`
  - Description: This method was migrated as live code, but its body references MessageBox, MessageBoxButtons, MessageBoxIcon - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

- **SettingsForm.SaveAdvancedAsync references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SettingsForm.Designer.cs`
  - Description: This method was migrated as live code, but its body references MessageBox, MessageBoxButtons, MessageBoxIcon - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

- **StockAdjustmentForm.StartCountSession references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockAdjustmentForm.Designer.cs`
  - Description: This method was migrated as live code, but its body references MessageBox, MessageBoxButtons, MessageBoxIcon - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

- **StockAdjustmentForm.PostAdjustmentAsync references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockAdjustmentForm.Designer.cs`
  - Description: This method was migrated as live code, but its body references MessageBox, MessageBoxButtons, MessageBoxIcon, DataGridViewRow - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

### Custom Property Logic

- **locationsTreeView.ImageList requires custom conversion logic**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/WarehousesForm.Designer.cs`
  - Description: Maps toward 'Resources' but the automatic converter could not fully translate this property; review the generated AXAML.

### Unconverted Support Files

- **"Common/DetailFormBase.cs" was not copied**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Common/DetailFormBase.cs`
  - Description: Declares a type deriving from WinForms 'Form' - needs a manual Avalonia port (a different rendering/control model entirely), not a plain file copy.

- **"Common/InputBoxHelper.cs" was not copied**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Common/InputBoxHelper.cs`
  - Description: Uses WinForms type(s) with no Avalonia equivalent (IWin32Window, Form, Label, TextBox, Button, DialogResult) - needs a manual Avalonia port, not a plain file copy.

- **"Common/ListFormBase.cs" was not copied**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Common/ListFormBase.cs`
  - Description: Declares a type deriving from WinForms 'Form' - needs a manual Avalonia port (a different rendering/control model entirely), not a plain file copy.

- **"Controls/AutocompleteSearchBox.cs" was not copied**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Controls/AutocompleteSearchBox.cs`
  - Description: Declares a type deriving from WinForms 'UserControl' - needs a manual Avalonia port (a different rendering/control model entirely), not a plain file copy.

- **"Controls/CardTileControl.cs" was not copied**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Controls/CardTileControl.cs`
  - Description: Declares a type deriving from WinForms 'Control' - needs a manual Avalonia port (a different rendering/control model entirely), not a plain file copy.

- **"Controls/ChartControl.cs" was not copied**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Controls/ChartControl.cs`
  - Description: Declares a type deriving from WinForms 'Control' - needs a manual Avalonia port (a different rendering/control model entirely), not a plain file copy.

- **"Controls/GaugeControl.cs" was not copied**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Controls/GaugeControl.cs`
  - Description: Declares a type deriving from WinForms 'Control' - needs a manual Avalonia port (a different rendering/control model entirely), not a plain file copy.

- **"Controls/LoadingSpinnerControl.cs" was not copied**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Controls/LoadingSpinnerControl.cs`
  - Description: Declares a type deriving from WinForms 'Control' - needs a manual Avalonia port (a different rendering/control model entirely), not a plain file copy.

- **"Controls/NumericStepperControl.cs" was not copied**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Controls/NumericStepperControl.cs`
  - Description: Declares a type deriving from WinForms 'UserControl' - needs a manual Avalonia port (a different rendering/control model entirely), not a plain file copy.

- **"Controls/StarRatingControl.cs" was not copied**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Controls/StarRatingControl.cs`
  - Description: Declares a type deriving from WinForms 'Control' - needs a manual Avalonia port (a different rendering/control model entirely), not a plain file copy.

- **"Controls/StatusBadgeControl.cs" was not copied**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Controls/StatusBadgeControl.cs`
  - Description: Declares a type deriving from WinForms 'Control' - needs a manual Avalonia port (a different rendering/control model entirely), not a plain file copy.

- **"Controls/ToggleSwitchControl.cs" was not copied**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Controls/ToggleSwitchControl.cs`
  - Description: Declares a type deriving from WinForms 'Control' - needs a manual Avalonia port (a different rendering/control model entirely), not a plain file copy.

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

