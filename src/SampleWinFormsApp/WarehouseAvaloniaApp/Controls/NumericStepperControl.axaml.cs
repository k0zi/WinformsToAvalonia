using Avalonia.Controls;

namespace WarehouseAvaloniaApp.Controls;

public partial class NumericStepperControl : UserControl
{
    public NumericStepperControl()
    {
        InitializeComponent();
        DataContext = new WarehouseAvaloniaApp.ViewModels.NumericStepperControlViewModel();
    }

    private WarehouseAvaloniaApp.ViewModels.NumericStepperControlViewModel ViewModel => (WarehouseAvaloniaApp.ViewModels.NumericStepperControlViewModel)DataContext!;

    public static readonly Avalonia.StyledProperty<decimal> IncrementProperty =
        Avalonia.AvaloniaProperty.Register<NumericStepperControl, decimal>(nameof(Increment));

    public decimal Increment
    {
        get => GetValue(IncrementProperty);
        set => SetValue(IncrementProperty, value);
    }

    private void _incrementButton_MouseDown_InlineHandler(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        ViewModel.StartRepeat(1);
    }

    private void _incrementButton_MouseUp_InlineHandler(object? sender, Avalonia.Input.PointerReleasedEventArgs e)
    {
        ViewModel.StopRepeat();
    }

    private void _decrementButton_MouseDown_InlineHandler(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        ViewModel.StartRepeat(-1);
    }

    private void _decrementButton_MouseUp_InlineHandler(object? sender, Avalonia.Input.PointerReleasedEventArgs e)
    {
        ViewModel.StopRepeat();
    }
}
