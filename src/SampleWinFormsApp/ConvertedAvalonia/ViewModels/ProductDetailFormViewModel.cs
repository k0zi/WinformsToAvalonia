using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WarehouseApp.Common;
using WarehouseApp.Data.Models;

namespace ConvertedAvalonia.ViewModels;

/// <summary>
/// ViewModel for ProductDetailForm (user customizations).
/// This file is preserved during reconversion - add your custom code here.
/// </summary>
public partial class ProductDetailFormViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    internal List<Category> _categories = [];

    internal List<Supplier> _suppliers = [];

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private void chooseImageButtonClick()
    {
            if (openFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                Entity.ImagePath = openFileDialog.FileName;
                productPictureBox.Image = Image.FromFile(openFileDialog.FileName);
            }
        }

}
