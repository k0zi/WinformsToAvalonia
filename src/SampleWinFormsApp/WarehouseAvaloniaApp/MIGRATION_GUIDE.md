# Migration Guide: WarehouseAvaloniaApp

**Generated**: 2026-08-12 00:05:07
**Converter Version**: 1.0.0

---

## Executive Summary

This document describes the migration of **WarehouseAvaloniaApp** from Windows Forms to Avalonia UI.

### Conversion Statistics

- **Total Controls**: 209
- **Successfully Converted**: 209 (100%)
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
| AutocompleteSearchBox | AutocompleteSearchBox | 2 | DockPanel | Converted |
| NumericStepperControl | NumericStepperControl | 4 | DockPanel | Converted |

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

### AutocompleteSearchBox

- **Layout Type**: DockPanel
- **Confidence**: 85%
- **Reason**: Analyzed control positioning patterns

### NumericStepperControl

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
  - Description: Custom-drawn control (derives from WinForms 'Control', overrides OnPaint, no InitializeComponent/child controls) - there is no control tree to convert into AXAML. Needs a hand-written Avalonia control with its own render logic (e.g. a Control subclass overriding Render(DrawingContext)).

- **StatusBadgeControl "statusBadge" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: Custom-drawn control (derives from WinForms 'Control', overrides OnPaint, no InitializeComponent/child controls) - there is no control tree to convert into AXAML. Needs a hand-written Avalonia control with its own render logic (e.g. a Control subclass overriding Render(DrawingContext)).

- **LoadingSpinnerControl "loadingSpinner" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/LoginForm.Designer.cs`
  - Description: Custom-drawn control (derives from WinForms 'Control', overrides OnPaint, no InitializeComponent/child controls) - there is no control tree to convert into AXAML. Needs a hand-written Avalonia control with its own render logic (e.g. a Control subclass overriding Render(DrawingContext)).

- **GaugeControl "capacityGauge" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/WarehousesForm.Designer.cs`
  - Description: Custom-drawn control (derives from WinForms 'Control', overrides OnPaint, no InitializeComponent/child controls) - there is no control tree to convert into AXAML. Needs a hand-written Avalonia control with its own render logic (e.g. a Control subclass overriding Render(DrawingContext)).

- **StatusBadgeControl "statusBadge" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrderDetailForm.Designer.cs`
  - Description: Custom-drawn control (derives from WinForms 'Control', overrides OnPaint, no InitializeComponent/child controls) - there is no control tree to convert into AXAML. Needs a hand-written Avalonia control with its own render logic (e.g. a Control subclass overriding Render(DrawingContext)).

- **CardTileControl "productsTile" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: Custom-drawn control (derives from WinForms 'Control', overrides OnPaint, no InitializeComponent/child controls) - there is no control tree to convert into AXAML. Needs a hand-written Avalonia control with its own render logic (e.g. a Control subclass overriding Render(DrawingContext)).

- **CardTileControl "productDetailTile" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: Custom-drawn control (derives from WinForms 'Control', overrides OnPaint, no InitializeComponent/child controls) - there is no control tree to convert into AXAML. Needs a hand-written Avalonia control with its own render logic (e.g. a Control subclass overriding Render(DrawingContext)).

- **CardTileControl "categoriesTile" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: Custom-drawn control (derives from WinForms 'Control', overrides OnPaint, no InitializeComponent/child controls) - there is no control tree to convert into AXAML. Needs a hand-written Avalonia control with its own render logic (e.g. a Control subclass overriding Render(DrawingContext)).

- **CardTileControl "suppliersTile" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: Custom-drawn control (derives from WinForms 'Control', overrides OnPaint, no InitializeComponent/child controls) - there is no control tree to convert into AXAML. Needs a hand-written Avalonia control with its own render logic (e.g. a Control subclass overriding Render(DrawingContext)).

- **CardTileControl "customersTile" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: Custom-drawn control (derives from WinForms 'Control', overrides OnPaint, no InitializeComponent/child controls) - there is no control tree to convert into AXAML. Needs a hand-written Avalonia control with its own render logic (e.g. a Control subclass overriding Render(DrawingContext)).

- **CardTileControl "warehousesTile" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: Custom-drawn control (derives from WinForms 'Control', overrides OnPaint, no InitializeComponent/child controls) - there is no control tree to convert into AXAML. Needs a hand-written Avalonia control with its own render logic (e.g. a Control subclass overriding Render(DrawingContext)).

- **CardTileControl "stockOverviewTile" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: Custom-drawn control (derives from WinForms 'Control', overrides OnPaint, no InitializeComponent/child controls) - there is no control tree to convert into AXAML. Needs a hand-written Avalonia control with its own render logic (e.g. a Control subclass overriding Render(DrawingContext)).

- **CardTileControl "stockInTile" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: Custom-drawn control (derives from WinForms 'Control', overrides OnPaint, no InitializeComponent/child controls) - there is no control tree to convert into AXAML. Needs a hand-written Avalonia control with its own render logic (e.g. a Control subclass overriding Render(DrawingContext)).

- **CardTileControl "stockOutTile" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: Custom-drawn control (derives from WinForms 'Control', overrides OnPaint, no InitializeComponent/child controls) - there is no control tree to convert into AXAML. Needs a hand-written Avalonia control with its own render logic (e.g. a Control subclass overriding Render(DrawingContext)).

- **CardTileControl "stockTransferTile" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: Custom-drawn control (derives from WinForms 'Control', overrides OnPaint, no InitializeComponent/child controls) - there is no control tree to convert into AXAML. Needs a hand-written Avalonia control with its own render logic (e.g. a Control subclass overriding Render(DrawingContext)).

- **CardTileControl "stockAdjustmentTile" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: Custom-drawn control (derives from WinForms 'Control', overrides OnPaint, no InitializeComponent/child controls) - there is no control tree to convert into AXAML. Needs a hand-written Avalonia control with its own render logic (e.g. a Control subclass overriding Render(DrawingContext)).

- **CardTileControl "purchaseOrdersTile" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: Custom-drawn control (derives from WinForms 'Control', overrides OnPaint, no InitializeComponent/child controls) - there is no control tree to convert into AXAML. Needs a hand-written Avalonia control with its own render logic (e.g. a Control subclass overriding Render(DrawingContext)).

- **CardTileControl "purchaseOrderDetailTile" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: Custom-drawn control (derives from WinForms 'Control', overrides OnPaint, no InitializeComponent/child controls) - there is no control tree to convert into AXAML. Needs a hand-written Avalonia control with its own render logic (e.g. a Control subclass overriding Render(DrawingContext)).

- **CardTileControl "salesOrdersTile" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: Custom-drawn control (derives from WinForms 'Control', overrides OnPaint, no InitializeComponent/child controls) - there is no control tree to convert into AXAML. Needs a hand-written Avalonia control with its own render logic (e.g. a Control subclass overriding Render(DrawingContext)).

- **CardTileControl "salesOrderDetailTile" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: Custom-drawn control (derives from WinForms 'Control', overrides OnPaint, no InitializeComponent/child controls) - there is no control tree to convert into AXAML. Needs a hand-written Avalonia control with its own render logic (e.g. a Control subclass overriding Render(DrawingContext)).

- **CardTileControl "usersTile" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: Custom-drawn control (derives from WinForms 'Control', overrides OnPaint, no InitializeComponent/child controls) - there is no control tree to convert into AXAML. Needs a hand-written Avalonia control with its own render logic (e.g. a Control subclass overriding Render(DrawingContext)).

- **CardTileControl "reportsTile" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: Custom-drawn control (derives from WinForms 'Control', overrides OnPaint, no InitializeComponent/child controls) - there is no control tree to convert into AXAML. Needs a hand-written Avalonia control with its own render logic (e.g. a Control subclass overriding Render(DrawingContext)).

- **CardTileControl "settingsTile" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: Custom-drawn control (derives from WinForms 'Control', overrides OnPaint, no InitializeComponent/child controls) - there is no control tree to convert into AXAML. Needs a hand-written Avalonia control with its own render logic (e.g. a Control subclass overriding Render(DrawingContext)).

- **GaugeControl "capacityGauge" has no Avalonia mapping**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: Custom-drawn control (derives from WinForms 'Control', overrides OnPaint, no InitializeComponent/child controls) - there is no control tree to convert into AXAML. Needs a hand-written Avalonia control with its own render logic (e.g. a Control subclass overriding Render(DrawingContext)).

### Custom Control Instance

- **AutocompleteSearchBox "productSearchBox" was converted separately**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: Controls/AutocompleteSearchBox.axaml and its ViewModel were generated from this control's own Designer.cs. These properties set on this instance were not simple public auto-properties on AutocompleteSearchBox (or not found at all) and were not carried over: Location, Width. Wire them up manually if needed.

- **AutocompleteSearchBox "productSearchBox" was converted separately**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrderDetailForm.Designer.cs`
  - Description: Controls/AutocompleteSearchBox.axaml and its ViewModel were generated from this control's own Designer.cs. These properties set on this instance were not simple public auto-properties on AutocompleteSearchBox (or not found at all) and were not carried over: Location, Width. Wire them up manually if needed.

- **NumericStepperControl "quantityStepper" was converted separately**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockInForm.Designer.cs`
  - Description: Controls/NumericStepperControl.axaml and its ViewModel were generated from this control's own Designer.cs. These properties set on this instance were not simple public auto-properties on NumericStepperControl (or not found at all) and were not carried over: Location, Minimum, Maximum, Value. Wire them up manually if needed.

- **NumericStepperControl "quantityStepper" was converted separately**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockOutForm.Designer.cs`
  - Description: Controls/NumericStepperControl.axaml and its ViewModel were generated from this control's own Designer.cs. These properties set on this instance were not simple public auto-properties on NumericStepperControl (or not found at all) and were not carried over: Location, Minimum, Maximum, Value. Wire them up manually if needed.

### Command Logic References View-Only Control

- **addLineButton.Click handler "addLineButton_Click" references qtyNumericUpDown.Value**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: This handler was migrated as live code into the ViewModel, but it reads/writes another control's property directly, and that property has no DataBindings.Add(...) entry to rewrite into an [ObservableProperty] - it will not compile as-is (the ViewModel cannot reference the View). Either add a DataBindings.Add(...) for it in the source WinForms designer so it is auto-bound on the next conversion, or wire this up manually (e.g. an AXAML-side binding).

- **SalesOrderDetailForm.LoadFromEntity references customerComboBox.DataSource, customerComboBox.DisplayMember, customerComboBox.ValueMember, warehouseComboBox.DataSource, warehouseComboBox.DisplayMember, warehouseComboBox.ValueMember, productSearchBox.DataSource, statusComboBox.DataSource, orderNumberValueLabel.Text, statusComboBox.SelectedIndexChanged**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: This helper method was migrated as live code into the ViewModel, but it reads/writes another control's property directly, and that property has no DataBindings.Add(...) entry to rewrite into an [ObservableProperty] - it will not compile as-is (the ViewModel cannot reference the View). Either add a DataBindings.Add(...) for it in the source WinForms designer so it is auto-bound on the next conversion, or wire this up manually.

- **SalesOrderDetailForm.UpdateStatusBadge references statusBadge.Text, statusBadge.BadgeStyle**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: This helper method was migrated as live code into the ViewModel, but it reads/writes another control's property directly, and that property has no DataBindings.Add(...) entry to rewrite into an [ObservableProperty] - it will not compile as-is (the ViewModel cannot reference the View). Either add a DataBindings.Add(...) for it in the source WinForms designer so it is auto-bound on the next conversion, or wire this up manually.

- **SalesOrderDetailForm.ValidateInput references customerComboBox.SelectedItem, warehouseComboBox.SelectedItem**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SalesOrderDetailForm.Designer.cs`
  - Description: This helper method was migrated as live code into the ViewModel, but it reads/writes another control's property directly, and that property has no DataBindings.Add(...) entry to rewrite into an [ObservableProperty] - it will not compile as-is (the ViewModel cannot reference the View). Either add a DataBindings.Add(...) for it in the source WinForms designer so it is auto-bound on the next conversion, or wire this up manually.

- **loginButton.Click handler "loginButton_Click" references statusLabel.Text, usernameTextBox.Text, passwordTextBox.Text**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/LoginForm.Designer.cs`
  - Description: This handler was migrated as live code into the ViewModel, but it reads/writes another control's property directly, and that property has no DataBindings.Add(...) entry to rewrite into an [ObservableProperty] - it will not compile as-is (the ViewModel cannot reference the View). Either add a DataBindings.Add(...) for it in the source WinForms designer so it is auto-bound on the next conversion, or wire this up manually (e.g. an AXAML-side binding).

- **LoginForm.SetBusy references loginProgressBar.Visible, loadingSpinner.Spinning, loginButton.Enabled, usernameTextBox.Enabled, passwordTextBox.Enabled**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/LoginForm.Designer.cs`
  - Description: This helper method was migrated as live code into the ViewModel, but it reads/writes another control's property directly, and that property has no DataBindings.Add(...) entry to rewrite into an [ObservableProperty] - it will not compile as-is (the ViewModel cannot reference the View). Either add a DataBindings.Add(...) for it in the source WinForms designer so it is auto-bound on the next conversion, or wire this up manually.

- **WarehousesForm.LoadTreeAsync references locationsTreeView.Nodes, locationsTreeView.ExpandAll**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/WarehousesForm.Designer.cs`
  - Description: This helper method was migrated as live code into the ViewModel, but it reads/writes another control's property directly, and that property has no DataBindings.Add(...) entry to rewrite into an [ObservableProperty] - it will not compile as-is (the ViewModel cannot reference the View). Either add a DataBindings.Add(...) for it in the source WinForms designer so it is auto-bound on the next conversion, or wire this up manually.

- **addLineButton.Click handler "addLineButton_Click" references qtyNumericUpDown.Value**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrderDetailForm.Designer.cs`
  - Description: This handler was migrated as live code into the ViewModel, but it reads/writes another control's property directly, and that property has no DataBindings.Add(...) entry to rewrite into an [ObservableProperty] - it will not compile as-is (the ViewModel cannot reference the View). Either add a DataBindings.Add(...) for it in the source WinForms designer so it is auto-bound on the next conversion, or wire this up manually (e.g. an AXAML-side binding).

- **PurchaseOrderDetailForm.LoadFromEntity references supplierComboBox.DataSource, supplierComboBox.DisplayMember, supplierComboBox.ValueMember, productSearchBox.DataSource, statusComboBox.DataSource, statusComboBox.SelectedIndexChanged**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrderDetailForm.Designer.cs`
  - Description: This helper method was migrated as live code into the ViewModel, but it reads/writes another control's property directly, and that property has no DataBindings.Add(...) entry to rewrite into an [ObservableProperty] - it will not compile as-is (the ViewModel cannot reference the View). Either add a DataBindings.Add(...) for it in the source WinForms designer so it is auto-bound on the next conversion, or wire this up manually.

- **PurchaseOrderDetailForm.UpdateStatusBadge references statusBadge.Text, statusBadge.BadgeStyle**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrderDetailForm.Designer.cs`
  - Description: This helper method was migrated as live code into the ViewModel, but it reads/writes another control's property directly, and that property has no DataBindings.Add(...) entry to rewrite into an [ObservableProperty] - it will not compile as-is (the ViewModel cannot reference the View). Either add a DataBindings.Add(...) for it in the source WinForms designer so it is auto-bound on the next conversion, or wire this up manually.

- **PurchaseOrderDetailForm.ValidateInput references supplierComboBox.SelectedItem**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrderDetailForm.Designer.cs`
  - Description: This helper method was migrated as live code into the ViewModel, but it reads/writes another control's property directly, and that property has no DataBindings.Add(...) entry to rewrite into an [ObservableProperty] - it will not compile as-is (the ViewModel cannot reference the View). Either add a DataBindings.Add(...) for it in the source WinForms designer so it is auto-bound on the next conversion, or wire this up manually.

- **PurchaseOrderDetailForm.PrintDocument_PrintPage references supplierComboBox.Text**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/PurchaseOrderDetailForm.Designer.cs`
  - Description: This helper method was migrated as live code into the ViewModel, but it reads/writes another control's property directly, and that property has no DataBindings.Add(...) entry to rewrite into an [ObservableProperty] - it will not compile as-is (the ViewModel cannot reference the View). Either add a DataBindings.Add(...) for it in the source WinForms designer so it is auto-bound on the next conversion, or wire this up manually.

- **addLineButton.Click handler "addLineButton_Click" references productComboBox.SelectedItem, warehouseComboBox.SelectedItem, quantityStepper.Value**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockInForm.Designer.cs`
  - Description: This handler was migrated as live code into the ViewModel, but it reads/writes another control's property directly, and that property has no DataBindings.Add(...) entry to rewrite into an [ObservableProperty] - it will not compile as-is (the ViewModel cannot reference the View). Either add a DataBindings.Add(...) for it in the source WinForms designer so it is auto-bound on the next conversion, or wire this up manually (e.g. an AXAML-side binding).

- **StockInForm.LoadLookupsAsync references productComboBox.DataSource, productComboBox.DisplayMember, productComboBox.ValueMember, warehouseComboBox.DataSource, warehouseComboBox.DisplayMember, warehouseComboBox.ValueMember**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockInForm.Designer.cs`
  - Description: This helper method was migrated as live code into the ViewModel, but it reads/writes another control's property directly, and that property has no DataBindings.Add(...) entry to rewrite into an [ObservableProperty] - it will not compile as-is (the ViewModel cannot reference the View). Either add a DataBindings.Add(...) for it in the source WinForms designer so it is auto-bound on the next conversion, or wire this up manually.

- **StockInForm.RemoveSelectedLine references linesGrid.CurrentRow**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockInForm.Designer.cs`
  - Description: This helper method was migrated as live code into the ViewModel, but it reads/writes another control's property directly, and that property has no DataBindings.Add(...) entry to rewrite into an [ObservableProperty] - it will not compile as-is (the ViewModel cannot reference the View). Either add a DataBindings.Add(...) for it in the source WinForms designer so it is auto-bound on the next conversion, or wire this up manually.

- **StockInForm.PostReceiptAsync references postButton.Enabled, receiptDatePicker.Value**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockInForm.Designer.cs`
  - Description: This helper method was migrated as live code into the ViewModel, but it reads/writes another control's property directly, and that property has no DataBindings.Add(...) entry to rewrite into an [ObservableProperty] - it will not compile as-is (the ViewModel cannot reference the View). Either add a DataBindings.Add(...) for it in the source WinForms designer so it is auto-bound on the next conversion, or wire this up manually.

- **addLineButton.Click handler "addLineButton_Click" references productComboBox.SelectedItem, fromWarehouseComboBox.SelectedItem, toWarehouseComboBox.SelectedItem, quantityNumericUpDown.Value**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockTransferForm.Designer.cs`
  - Description: This handler was migrated as live code into the ViewModel, but it reads/writes another control's property directly, and that property has no DataBindings.Add(...) entry to rewrite into an [ObservableProperty] - it will not compile as-is (the ViewModel cannot reference the View). Either add a DataBindings.Add(...) for it in the source WinForms designer so it is auto-bound on the next conversion, or wire this up manually (e.g. an AXAML-side binding).

- **StockTransferForm.LoadLookupsAsync references productComboBox.DataSource, productComboBox.DisplayMember, productComboBox.ValueMember, fromWarehouseComboBox.DataSource, fromWarehouseComboBox.DisplayMember, fromWarehouseComboBox.ValueMember, toWarehouseComboBox.DataSource, toWarehouseComboBox.DisplayMember, toWarehouseComboBox.ValueMember, toWarehouseComboBox.SelectedIndex**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockTransferForm.Designer.cs`
  - Description: This helper method was migrated as live code into the ViewModel, but it reads/writes another control's property directly, and that property has no DataBindings.Add(...) entry to rewrite into an [ObservableProperty] - it will not compile as-is (the ViewModel cannot reference the View). Either add a DataBindings.Add(...) for it in the source WinForms designer so it is auto-bound on the next conversion, or wire this up manually.

- **StockTransferForm.SwapWarehouses references fromWarehouseComboBox.SelectedValue, toWarehouseComboBox.SelectedValue**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockTransferForm.Designer.cs`
  - Description: This helper method was migrated as live code into the ViewModel, but it reads/writes another control's property directly, and that property has no DataBindings.Add(...) entry to rewrite into an [ObservableProperty] - it will not compile as-is (the ViewModel cannot reference the View). Either add a DataBindings.Add(...) for it in the source WinForms designer so it is auto-bound on the next conversion, or wire this up manually.

- **StockTransferForm.RemoveSelectedLine references linesGrid.CurrentRow**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockTransferForm.Designer.cs`
  - Description: This helper method was migrated as live code into the ViewModel, but it reads/writes another control's property directly, and that property has no DataBindings.Add(...) entry to rewrite into an [ObservableProperty] - it will not compile as-is (the ViewModel cannot reference the View). Either add a DataBindings.Add(...) for it in the source WinForms designer so it is auto-bound on the next conversion, or wire this up manually.

- **StockTransferForm.PostTransferAsync references postButton.Enabled**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockTransferForm.Designer.cs`
  - Description: This helper method was migrated as live code into the ViewModel, but it reads/writes another control's property directly, and that property has no DataBindings.Add(...) entry to rewrite into an [ObservableProperty] - it will not compile as-is (the ViewModel cannot reference the View). Either add a DataBindings.Add(...) for it in the source WinForms designer so it is auto-bound on the next conversion, or wire this up manually.

- **DashboardForm.RefreshCapacityAsync references capacityGauge.Value**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: This helper method was migrated as live code into the ViewModel, but it reads/writes another control's property directly, and that property has no DataBindings.Add(...) entry to rewrite into an [ObservableProperty] - it will not compile as-is (the ViewModel cannot reference the View). Either add a DataBindings.Add(...) for it in the source WinForms designer so it is auto-bound on the next conversion, or wire this up manually.

- **chooseImageButton.Click handler "chooseImageButton_Click" references productPictureBox.Image**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/ProductDetailForm.Designer.cs`
  - Description: This handler was migrated as live code into the ViewModel, but it reads/writes another control's property directly, and that property has no DataBindings.Add(...) entry to rewrite into an [ObservableProperty] - it will not compile as-is (the ViewModel cannot reference the View). Either add a DataBindings.Add(...) for it in the source WinForms designer so it is auto-bound on the next conversion, or wire this up manually (e.g. an AXAML-side binding).

- **ProductDetailForm.LoadFromEntity references categoryComboBox.DataSource, categoryComboBox.DisplayMember, categoryComboBox.ValueMember, supplierComboBox.DataSource, supplierComboBox.DisplayMember, supplierComboBox.ValueMember, unitOfMeasureDomainUpDown.Items, unitOfMeasureDomainUpDown.SelectedIndex**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/ProductDetailForm.Designer.cs`
  - Description: This helper method was migrated as live code into the ViewModel, but it reads/writes another control's property directly, and that property has no DataBindings.Add(...) entry to rewrite into an [ObservableProperty] - it will not compile as-is (the ViewModel cannot reference the View). Either add a DataBindings.Add(...) for it in the source WinForms designer so it is auto-bound on the next conversion, or wire this up manually.

- **ProductDetailForm.ValidateInput references categoryComboBox.SelectedItem, supplierComboBox.SelectedItem**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/ProductDetailForm.Designer.cs`
  - Description: This helper method was migrated as live code into the ViewModel, but it reads/writes another control's property directly, and that property has no DataBindings.Add(...) entry to rewrite into an [ObservableProperty] - it will not compile as-is (the ViewModel cannot reference the View). Either add a DataBindings.Add(...) for it in the source WinForms designer so it is auto-bound on the next conversion, or wire this up manually.

- **ProductDetailForm.SaveToEntity references unitOfMeasureDomainUpDown.SelectedItem**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/ProductDetailForm.Designer.cs`
  - Description: This helper method was migrated as live code into the ViewModel, but it reads/writes another control's property directly, and that property has no DataBindings.Add(...) entry to rewrite into an [ObservableProperty] - it will not compile as-is (the ViewModel cannot reference the View). Either add a DataBindings.Add(...) for it in the source WinForms designer so it is auto-bound on the next conversion, or wire this up manually.

- **addLineButton.Click handler "addLineButton_Click" references productComboBox.SelectedItem, warehouseComboBox.SelectedItem, quantityStepper.Value**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockOutForm.Designer.cs`
  - Description: This handler was migrated as live code into the ViewModel, but it reads/writes another control's property directly, and that property has no DataBindings.Add(...) entry to rewrite into an [ObservableProperty] - it will not compile as-is (the ViewModel cannot reference the View). Either add a DataBindings.Add(...) for it in the source WinForms designer so it is auto-bound on the next conversion, or wire this up manually (e.g. an AXAML-side binding).

- **StockOutForm.LoadLookupsAsync references productComboBox.DataSource, productComboBox.DisplayMember, productComboBox.ValueMember, warehouseComboBox.DataSource, warehouseComboBox.DisplayMember, warehouseComboBox.ValueMember**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockOutForm.Designer.cs`
  - Description: This helper method was migrated as live code into the ViewModel, but it reads/writes another control's property directly, and that property has no DataBindings.Add(...) entry to rewrite into an [ObservableProperty] - it will not compile as-is (the ViewModel cannot reference the View). Either add a DataBindings.Add(...) for it in the source WinForms designer so it is auto-bound on the next conversion, or wire this up manually.

- **StockOutForm.RemoveSelectedLine references linesGrid.CurrentRow**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockOutForm.Designer.cs`
  - Description: This helper method was migrated as live code into the ViewModel, but it reads/writes another control's property directly, and that property has no DataBindings.Add(...) entry to rewrite into an [ObservableProperty] - it will not compile as-is (the ViewModel cannot reference the View). Either add a DataBindings.Add(...) for it in the source WinForms designer so it is auto-bound on the next conversion, or wire this up manually.

- **StockOutForm.PostIssueAsync references postButton.Enabled**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockOutForm.Designer.cs`
  - Description: This helper method was migrated as live code into the ViewModel, but it reads/writes another control's property directly, and that property has no DataBindings.Add(...) entry to rewrite into an [ObservableProperty] - it will not compile as-is (the ViewModel cannot reference the View). Either add a DataBindings.Add(...) for it in the source WinForms designer so it is auto-bound on the next conversion, or wire this up manually.

- **AutocompleteSearchBox.ShowPopup references _textBox.Focus**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Controls/AutocompleteSearchBox.cs`
  - Description: This helper method was migrated as live code into the ViewModel, but it reads/writes another control's property directly, and that property has no DataBindings.Add(...) entry to rewrite into an [ObservableProperty] - it will not compile as-is (the ViewModel cannot reference the View). Either add a DataBindings.Add(...) for it in the source WinForms designer so it is auto-bound on the next conversion, or wire this up manually.

- **AutocompleteSearchBox.CommitSelection references _textBox.SelectionStart**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Controls/AutocompleteSearchBox.cs`
  - Description: This helper method was migrated as live code into the ViewModel, but it reads/writes another control's property directly, and that property has no DataBindings.Add(...) entry to rewrite into an [ObservableProperty] - it will not compile as-is (the ViewModel cannot reference the View). Either add a DataBindings.Add(...) for it in the source WinForms designer so it is auto-bound on the next conversion, or wire this up manually.

- **NumericStepperControl.UpdateLabel references _valueLabel.Text**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Controls/NumericStepperControl.cs`
  - Description: This helper method was migrated as live code into the ViewModel, but it reads/writes another control's property directly, and that property has no DataBindings.Add(...) entry to rewrite into an [ObservableProperty] - it will not compile as-is (the ViewModel cannot reference the View). Either add a DataBindings.Add(...) for it in the source WinForms designer so it is auto-bound on the next conversion, or wire this up manually.

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

- **CustomersForm.LoadOrdersForCustomerAsync references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/CustomersForm.Designer.cs`
  - Description: This method was migrated as live code, but its body references ListViewItem - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

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

- **StockInForm.PostReceiptAsync references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockInForm.Designer.cs`
  - Description: This method was migrated as live code, but its body references DataGridViewRow - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

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
  - Description: This method was migrated as live code, but its body references DataGridViewRow - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

- **ProductsListForm.AddNew references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/ProductsListForm.Designer.cs`
  - Description: This method was migrated as live code, but its body references DialogResult - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

- **ProductsListForm.EditEntity references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/ProductsListForm.Designer.cs`
  - Description: This method was migrated as live code, but its body references DialogResult - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

- **ProductsListForm.Grid_CellFormatting references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/ProductsListForm.Designer.cs`
  - Description: This method was migrated as live code, but its body references DataGridViewCellFormattingEventArgs - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

- **DashboardForm.OpenForm references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: This method was migrated as live code, but its body references Form - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

- **SuppliersForm.LoadSuppliersAsync references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SuppliersForm.Designer.cs`
  - Description: This method was migrated as live code, but its body references ListViewItem - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

- **SuppliersForm.EmailLinkLabel_LinkClicked references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SuppliersForm.Designer.cs`
  - Description: This method was migrated as live code, but its body references LinkLabelLinkClickedEventArgs - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

- **StockOutForm.PostIssueAsync references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockOutForm.Designer.cs`
  - Description: This method was migrated as live code, but its body references DataGridViewRow - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

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

- **SettingsForm.chooseColorButton_Click references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SettingsForm.Designer.cs`
  - Description: This method was migrated as live code, but its body references DialogResult - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

- **SettingsForm.browseFolderButton_Click references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/SettingsForm.Designer.cs`
  - Description: This method was migrated as live code, but its body references DialogResult - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

- **StockAdjustmentForm.PostAdjustmentAsync references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/StockAdjustmentForm.Designer.cs`
  - Description: This method was migrated as live code, but its body references DataGridViewRow - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

- **AutocompleteSearchBox.ShowPopup references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Controls/AutocompleteSearchBox.cs`
  - Description: This method was migrated as live code, but its body references ListBox, Form - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this logic manually.

- **AutocompleteSearchBox._popup references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Controls/AutocompleteSearchBox.cs`
  - Description: This field was migrated as live code, but its declared type references Form - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this field's type manually.

- **AutocompleteSearchBox._popupList references WinForms type(s) with no Avalonia equivalent**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Controls/AutocompleteSearchBox.cs`
  - Description: This field was migrated as live code, but its declared type references ListBox - which have no Avalonia equivalent (a different UI/control model entirely). It will not compile as-is; review and redesign this field's type manually.

### Custom Property Logic

- **locationsTreeView.ImageList requires custom conversion logic**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/WarehousesForm.Designer.cs`
  - Description: Maps toward 'Resources' but the automatic converter could not fully translate this property; review the generated AXAML.

### Preserved Event Handlers

- **DashboardForm.Load handler "DashboardForm_Load" needs manual review**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: Maps to Avalonia's 'Loaded' event. The original handler body was embedded as live code, with best-effort identifier rewriting for any fields/methods that moved to the ViewModel - verify this compiles (the original code may call WinForms-only APIs that don't exist in Avalonia) and double-check the rewritten identifiers before shipping.

- **DashboardForm.FormClosing handler "DashboardForm_FormClosing" needs manual review**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: Maps to Avalonia's 'Closing' event. The original handler body was embedded as live code, with best-effort identifier rewriting for any fields/methods that moved to the ViewModel - verify this compiles (the original code may call WinForms-only APIs that don't exist in Avalonia) and double-check the rewritten identifiers before shipping.

- **DashboardForm.Resize handler "DashboardForm_Resize" needs manual review**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Forms/DashboardForm.Designer.cs`
  - Description: Maps to Avalonia's 'SizeChanged' event. The original handler body was embedded as live code, with best-effort identifier rewriting for any fields/methods that moved to the ViewModel - verify this compiles (the original code may call WinForms-only APIs that don't exist in Avalonia) and double-check the rewritten identifiers before shipping.

- **_textBox.KeyDown handler "TextBox_KeyDown" needs manual review**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Controls/AutocompleteSearchBox.cs`
  - Description: Maps to Avalonia's 'KeyDown' event. The original handler body was embedded as live code, with best-effort identifier rewriting for any fields/methods that moved to the ViewModel - verify this compiles (the original code may call WinForms-only APIs that don't exist in Avalonia) and double-check the rewritten identifiers before shipping.

- **_textBox.LostFocus handler "_textBox_LostFocus_InlineHandler" needs manual review**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Controls/AutocompleteSearchBox.cs`
  - Description: Maps to Avalonia's 'LostFocus' event. The original handler body was embedded as live code, with best-effort identifier rewriting for any fields/methods that moved to the ViewModel - verify this compiles (the original code may call WinForms-only APIs that don't exist in Avalonia) and double-check the rewritten identifiers before shipping.

- **_incrementButton.MouseDown handler "_incrementButton_MouseDown_InlineHandler" needs manual review**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Controls/NumericStepperControl.cs`
  - Description: Maps to Avalonia's 'PointerPressed' event. The original handler body was embedded as live code, with best-effort identifier rewriting for any fields/methods that moved to the ViewModel - verify this compiles (the original code may call WinForms-only APIs that don't exist in Avalonia) and double-check the rewritten identifiers before shipping.

- **_incrementButton.MouseUp handler "_incrementButton_MouseUp_InlineHandler" needs manual review**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Controls/NumericStepperControl.cs`
  - Description: Maps to Avalonia's 'PointerReleased' event. The original handler body was embedded as live code, with best-effort identifier rewriting for any fields/methods that moved to the ViewModel - verify this compiles (the original code may call WinForms-only APIs that don't exist in Avalonia) and double-check the rewritten identifiers before shipping.

- **_decrementButton.MouseDown handler "_decrementButton_MouseDown_InlineHandler" needs manual review**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Controls/NumericStepperControl.cs`
  - Description: Maps to Avalonia's 'PointerPressed' event. The original handler body was embedded as live code, with best-effort identifier rewriting for any fields/methods that moved to the ViewModel - verify this compiles (the original code may call WinForms-only APIs that don't exist in Avalonia) and double-check the rewritten identifiers before shipping.

- **_decrementButton.MouseUp handler "_decrementButton_MouseUp_InlineHandler" needs manual review**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Controls/NumericStepperControl.cs`
  - Description: Maps to Avalonia's 'PointerReleased' event. The original handler body was embedded as live code, with best-effort identifier rewriting for any fields/methods that moved to the ViewModel - verify this compiles (the original code may call WinForms-only APIs that don't exist in Avalonia) and double-check the rewritten identifiers before shipping.

### Custom Event Logic

- **_textBox.TextChanged handler "TextBox_TextChanged" requires custom conversion logic**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Controls/AutocompleteSearchBox.cs`
  - Description: Bind to Text property changes

### Custom Control Property Not Auto-Bound

- **AutocompleteSearchBox.DataSource was not auto-bound**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Controls/AutocompleteSearchBox.cs`
  - Description: This public property type 'IEnumerable<object>' is not supported for auto-binding, so it was left as a plain C# property instead of a bindable Avalonia StyledProperty - it can't be set from a parent's AXAML as an attribute. Wire it up manually if a consumer needs to.

- **NumericStepperControl.Value was not auto-bound**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Controls/NumericStepperControl.cs`
  - Description: This public property has custom getter/setter logic, so it was left as a plain C# property instead of a bindable Avalonia StyledProperty - it can't be set from a parent's AXAML as an attribute. Wire it up manually if a consumer needs to.

- **NumericStepperControl.Minimum was not auto-bound**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Controls/NumericStepperControl.cs`
  - Description: This public property has custom getter/setter logic, so it was left as a plain C# property instead of a bindable Avalonia StyledProperty - it can't be set from a parent's AXAML as an attribute. Wire it up manually if a consumer needs to.

- **NumericStepperControl.Maximum was not auto-bound**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Controls/NumericStepperControl.cs`
  - Description: This public property has custom getter/setter logic, so it was left as a plain C# property instead of a bindable Avalonia StyledProperty - it can't be set from a parent's AXAML as an attribute. Wire it up manually if a consumer needs to.

### Skipped Override Methods

- **AutocompleteSearchBox.Dispose was not migrated (Form-lifecycle override)**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Controls/AutocompleteSearchBox.cs`
  - Description: This method overrides a base Form/Control member (e.g. OnClosing, OnLoad) and has no clean 1:1 ViewModel equivalent, so it was intentionally left out of the generated ViewModel. Port its logic manually into the Window's own lifecycle override or a suitable code-behind/ViewModel hook.

- **NumericStepperControl.Dispose was not migrated (Form-lifecycle override)**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Controls/NumericStepperControl.cs`
  - Description: This method overrides a base Form/Control member (e.g. OnClosing, OnLoad) and has no clean 1:1 ViewModel equivalent, so it was intentionally left out of the generated ViewModel. Port its logic manually into the Window's own lifecycle override or a suitable code-behind/ViewModel hook.

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

- **"Controls/CardTileControl.cs" was not copied**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Controls/CardTileControl.cs`
  - Description: Custom-drawn control (derives from WinForms 'Control', overrides OnPaint, no InitializeComponent/child controls) - there is no control tree to convert into AXAML. Needs a hand-written Avalonia control with its own render logic (e.g. a Control subclass overriding Render(DrawingContext)).

- **"Controls/ChartControl.cs" was not copied**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Controls/ChartControl.cs`
  - Description: Partially copied - ChartControl was removed (derives from WinForms 'Control') and need a manual Avalonia port; every other type declared in this file was copied unchanged.

- **"Controls/GaugeControl.cs" was not copied**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Controls/GaugeControl.cs`
  - Description: Custom-drawn control (derives from WinForms 'Control', overrides OnPaint, no InitializeComponent/child controls) - there is no control tree to convert into AXAML. Needs a hand-written Avalonia control with its own render logic (e.g. a Control subclass overriding Render(DrawingContext)).

- **"Controls/LoadingSpinnerControl.cs" was not copied**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Controls/LoadingSpinnerControl.cs`
  - Description: Custom-drawn control (derives from WinForms 'Control', overrides OnPaint, no InitializeComponent/child controls) - there is no control tree to convert into AXAML. Needs a hand-written Avalonia control with its own render logic (e.g. a Control subclass overriding Render(DrawingContext)).

- **"Controls/StarRatingControl.cs" was not copied**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Controls/StarRatingControl.cs`
  - Description: Custom-drawn control (derives from WinForms 'Control', overrides OnPaint, no InitializeComponent/child controls) - there is no control tree to convert into AXAML. Needs a hand-written Avalonia control with its own render logic (e.g. a Control subclass overriding Render(DrawingContext)).

- **"Controls/StatusBadgeControl.cs" was not copied**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Controls/StatusBadgeControl.cs`
  - Description: Partially copied - StatusBadgeControl was removed (derives from WinForms 'Control') and need a manual Avalonia port; every other type declared in this file was copied unchanged.

- **"Controls/ToggleSwitchControl.cs" was not copied**
  - Location: `/home/k0zi/Develop/Sources/GitHub/WinformsToAvalonia/src/SampleWinFormsApp/WarehouseApp/Controls/ToggleSwitchControl.cs`
  - Description: Custom-drawn control (derives from WinForms 'Control', overrides OnPaint, no InitializeComponent/child controls) - there is no control tree to convert into AXAML. Needs a hand-written Avalonia control with its own render logic (e.g. a Control subclass overriding Render(DrawingContext)).

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

