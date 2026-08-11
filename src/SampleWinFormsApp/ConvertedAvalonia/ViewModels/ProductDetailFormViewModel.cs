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

    internal void LoadFromEntity()
        {
            using var ctx = Db.CreateContext();
            _categories = ctx.Categories.OrderBy(c => c.Name).ToList();
            _suppliers = ctx.Suppliers.Where(s => s.IsActive).OrderBy(s => s.Name).ToList();
    
            categoryComboBox.DataSource = _categories;
            categoryComboBox.DisplayMember = nameof(Category.Name);
            categoryComboBox.ValueMember = nameof(Category.Id);
    
            supplierComboBox.DataSource = _suppliers;
            supplierComboBox.DisplayMember = nameof(Supplier.Name);
            supplierComboBox.ValueMember = nameof(Supplier.Id);
    
            var seed = IsNew ? SeedTemplate : null;
    
            skuTextBox.Text = seed is not null ? seed.Sku + "-COPY" : Entity.Sku;
            nameTextBox.Text = seed is not null ? seed.Name + " (Copy)" : Entity.Name;
            descriptionTextBox.Text = seed?.Description ?? Entity.Description;
            unitPriceNumericUpDown.Value = seed?.UnitPrice ?? Entity.UnitPrice;
            reorderLevelNumericUpDown.Value = seed?.ReorderLevel ?? Entity.ReorderLevel;
            isActiveCheckBox.Checked = IsNew || Entity.IsActive;
    
            if (!IsNew || seed is not null)
            {
                var source = seed ?? Entity;
                categoryComboBox.SelectedValue = source.CategoryId;
                supplierComboBox.SelectedValue = source.SupplierId;
                var uomIndex = unitOfMeasureDomainUpDown.Items.IndexOf(source.UnitOfMeasure.ToString());
                if (uomIndex >= 0)
                {
                    unitOfMeasureDomainUpDown.SelectedIndex = uomIndex;
                }
            }
        }

    internal bool ValidateInput()
        {
            var valid = true;
            if (string.IsNullOrWhiteSpace(skuTextBox.Text))
            {
                Validation.SetError(skuTextBox, "SKU is required.");
                valid = false;
            }
            if (string.IsNullOrWhiteSpace(nameTextBox.Text))
            {
                Validation.SetError(nameTextBox, "Name is required.");
                valid = false;
            }
            if (categoryComboBox.SelectedItem is null)
            {
                Validation.SetError(categoryComboBox, "Choose a category.");
                valid = false;
            }
            if (supplierComboBox.SelectedItem is null)
            {
                Validation.SetError(supplierComboBox, "Choose a supplier.");
                valid = false;
            }
            return valid;
        }

    internal void SaveToEntity()
        {
            Entity.Sku = skuTextBox.Text.Trim();
            Entity.Name = nameTextBox.Text.Trim();
            Entity.Description = descriptionTextBox.Text.Trim();
            Entity.CategoryId = (int)categoryComboBox.SelectedValue!;
            Entity.SupplierId = (int)supplierComboBox.SelectedValue!;
            Entity.UnitOfMeasure = Enum.Parse<UnitOfMeasure>((string)unitOfMeasureDomainUpDown.SelectedItem!);
            Entity.UnitPrice = unitPriceNumericUpDown.Value;
            Entity.ReorderLevel = (int)reorderLevelNumericUpDown.Value;
            Entity.IsActive = isActiveCheckBox.Checked;
            if (IsNew)
            {
                Entity.CreatedAt = DateTime.UtcNow;
            }
        }

    internal async Task PersistAsync()
        {
            using var ctx = Db.CreateContext();
            if (IsNew)
            {
                ctx.Products.Add(Entity);
            }
            else
            {
                ctx.Products.Attach(Entity).State = EntityState.Modified;
            }
            await ctx.SaveChangesAsync();
        }

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
