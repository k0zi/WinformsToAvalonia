using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AvaloniaForms.ViewModels;

/// <summary>
/// ViewModel for ProductDetailForm (auto-generated).
/// </summary>
public partial class ProductDetailFormViewModel : ObservableObject
{
    [RelayCommand]
    private void chooseImageButtonClick()
    {
        // Original WinForms handler "chooseImageButton_Click", preserved for reference - review and adapt:
        // private void chooseImageButton_Click(object? sender, EventArgs e)
        //     {
        //         if (openFileDialog.ShowDialog(this) == DialogResult.OK)
        //         {
        //             Entity.ImagePath = openFileDialog.FileName;
        //             productPictureBox.Image = Image.FromFile(openFileDialog.FileName);
        //         }
        //     }
    }

}
