using Avalonia.Controls;

namespace WarehouseAvaloniaApp.Views;

public partial class ProductDetailForm : Window
{
    public ProductDetailForm()
    {
        InitializeComponent();
        DataContext = new WarehouseAvaloniaApp.ViewModels.ProductDetailFormViewModel();
    }

    private WarehouseAvaloniaApp.ViewModels.ProductDetailFormViewModel ViewModel => (WarehouseAvaloniaApp.ViewModels.ProductDetailFormViewModel)DataContext!;

    private void chooseImageButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
            if (openFileDialog.ShowDialog(this) == WarehouseAvaloniaApp.Common.DialogResult.OK)
            {
                Entity.ImagePath = openFileDialog.FileName;
                productPictureBox.Image = Image.FromFile(openFileDialog.FileName);
            }
        }
}
