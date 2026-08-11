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
/// ViewModel for ProductsListForm (user customizations).
/// This file is preserved during reconversion - add your custom code here.
/// </summary>
public partial class ProductsListFormViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    internal void PopulateCategoryFilter()
        {
            using var ctx = Db.CreateContext();
            categoryFilterComboBox.Items.Add("All Categories");
            foreach (var category in ctx.Categories.OrderBy(c => c.Name))
            {
                categoryFilterComboBox.Items.Add(category.Name);
            }
            categoryFilterComboBox.SelectedIndex = 0;
        }

    internal async Task<List<Product>> LoadDataAsync(string? searchText)
        {
            var selectedCategory = categoryFilterComboBox.SelectedIndex > 0
                ? categoryFilterComboBox.SelectedItem?.ToString()
                : null;
    
            return await Task.Run(() =>
            {
                using var ctx = Db.CreateContext();
                var query = ctx.Products.Include(p => p.Category).Include(p => p.Supplier).AsQueryable();
    
                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    query = query.Where(p => p.Sku.Contains(searchText) || p.Name.Contains(searchText));
                }
                if (!string.IsNullOrEmpty(selectedCategory))
                {
                    query = query.Where(p => p.Category.Name == selectedCategory);
                }
    
                return query.OrderBy(p => p.Sku).ToList();
            });
        }

    internal void AddNew()
        {
            using var form = new ProductDetailForm();
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                _ = ReloadAsync();
            }
        }

    internal void EditEntity(Product entity)
        {
            using var form = new ProductDetailForm(entity);
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                _ = ReloadAsync();
            }
        }

    internal async Task DeleteEntityAsync(Product entity)
        {
            using var ctx = Db.CreateContext();
            var tracked = await ctx.Products.FindAsync(entity.Id);
            if (tracked is not null)
            {
                ctx.Products.Remove(tracked);
                await ctx.SaveChangesAsync();
            }
        }

    internal void Grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (Grid.Rows[e.RowIndex].DataBoundItem is not Product product)
            {
                return;
            }
    
            if (Grid.Columns[e.ColumnIndex].Name == "Status")
            {
                e.Value = statusImageList.Images[product.IsActive ? "Active" : "Inactive"];
                e.FormattingApplied = true;
            }
            else if (Grid.Columns[e.ColumnIndex].Name == "Category")
            {
                e.Value = product.Category?.Name;
                e.FormattingApplied = true;
            }
            else if (Grid.Columns[e.ColumnIndex].Name == "Supplier")
            {
                e.Value = product.Supplier?.Name;
                e.FormattingApplied = true;
            }
        }

    internal void quickAddDuplicate_Click(object? sender, EventArgs e)
        {
            if (BindingSourceControl?.Current is not Product selected)
            {
                MessageBox.Show(this, "Select a product first.", "Quick Add", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
    
            using var form = new ProductDetailForm { SeedTemplate = selected };
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                _ = ReloadAsync();
            }
        }

}
