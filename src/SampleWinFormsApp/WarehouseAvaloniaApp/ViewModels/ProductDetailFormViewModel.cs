using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WarehouseApp.Common;
using WarehouseApp.Data.Models;

namespace WarehouseAvaloniaApp.ViewModels;

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
    
            Sku = seed is not null ? seed.Sku + "-COPY" : Entity.Sku;
            Name = seed is not null ? seed.Name + " (Copy)" : Entity.Name;
            Description = seed?.Description ?? Entity.Description;
            UnitPrice = seed?.UnitPrice ?? Entity.UnitPrice;
            ReorderLevel = seed?.ReorderLevel ?? Entity.ReorderLevel;
            IsActive = IsNew || Entity.IsActive;
    
            if (!IsNew || seed is not null)
            {
                var source = seed ?? Entity;
                Category = source.CategoryId;
                Supplier = source.SupplierId;
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
            if (string.IsNullOrWhiteSpace(Sku))
            {
                Validation.SetError(skuTextBox, "SKU is required.");
                valid = false;
            }
            if (string.IsNullOrWhiteSpace(Name))
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
            Entity.Sku = Sku.Trim();
            Entity.Name = Name.Trim();
            Entity.Description = Description.Trim();
            Entity.CategoryId = (int)Category!;
            Entity.SupplierId = (int)Supplier!;
            Entity.UnitOfMeasure = Enum.Parse<UnitOfMeasure>((string)unitOfMeasureDomainUpDown.SelectedItem!);
            Entity.UnitPrice = UnitPrice;
            Entity.ReorderLevel = (int)ReorderLevel;
            Entity.IsActive = IsActive;
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

}
