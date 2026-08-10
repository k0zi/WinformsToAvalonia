using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WarehouseApp.Common;
using WarehouseApp.Data.Models;

namespace ConvertedAvalonia.ViewModels;

/// <summary>
/// ViewModel for CategoriesForm (user customizations).
/// This file is preserved during reconversion - add your custom code here.
/// </summary>
public partial class CategoriesFormViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    internal Category? _current;

    internal int? _newParentId;

    internal bool _isNew;

    internal async Task LoadCategoriesAsync()
        {
            var categories = await Task.Run(() =>
            {
                using var ctx = Db.CreateContext();
                return ctx.Categories.ToList();
            });
    
            categoriesTreeView.Nodes.Clear();
            foreach (var category in categories.Where(c => c.ParentCategoryId is null).OrderBy(c => c.Name))
            {
                categoriesTreeView.Nodes.Add(BuildNode(category, categories));
            }
            categoriesTreeView.ExpandAll();
            recordCountLabel.Text = $"{categories.Count} categories";
            ClearDetails();
        }

    internal static TreeNode BuildNode(Category category, List<Category> all)
        {
            var children = all.Where(c => c.ParentCategoryId == category.Id).OrderBy(c => c.Name).ToList();
            var imageKey = children.Count > 0 ? "Folder" : "Leaf";
            var node = new TreeNode(category.Name) { Tag = category, ImageKey = imageKey, SelectedImageKey = imageKey };
            foreach (var child in children)
            {
                node.Nodes.Add(BuildNode(child, all));
            }
            return node;
        }

    internal void CategoriesTreeView_AfterSelect(object? sender, TreeViewEventArgs e)
        {
            if (SelectedCategory is not { } category)
            {
                return;
            }
    
            _current = category;
            _isNew = false;
            nameTextBox.Text = category.Name;
            descriptionTextBox.Text = category.Description;
            parentValueLabel.Text = e.Node?.Parent?.Text ?? "(none — root category)";
        }

    internal void NewCategory(Category? parent)
        {
            _current = null;
            _isNew = true;
            _newParentId = parent?.Id;
            nameTextBox.Clear();
            descriptionTextBox.Clear();
            parentValueLabel.Text = parent?.Name ?? "(none — root category)";
            nameTextBox.Focus();
        }

    internal void ClearDetails()
        {
            _current = null;
            _isNew = false;
            _newParentId = null;
            nameTextBox.Clear();
            descriptionTextBox.Clear();
            parentValueLabel.Text = "(none — root category)";
        }

    internal async Task SaveCategoryAsync()
        {
            if (string.IsNullOrWhiteSpace(nameTextBox.Text))
            {
                MessageBox.Show(this, "Category name is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
    
            using var ctx = Db.CreateContext();
            if (_isNew || _current is null)
            {
                ctx.Categories.Add(new Category
                {
                    Name = nameTextBox.Text.Trim(),
                    Description = descriptionTextBox.Text.Trim(),
                    ParentCategoryId = _newParentId
                });
            }
            else
            {
                var tracked = await ctx.Categories.FindAsync(_current.Id);
                if (tracked is not null)
                {
                    tracked.Name = nameTextBox.Text.Trim();
                    tracked.Description = descriptionTextBox.Text.Trim();
                }
            }
            await ctx.SaveChangesAsync();
            await LoadCategoriesAsync();
        }

    internal async Task DeleteCategoryAsync()
        {
            if (SelectedCategory is not { } category)
            {
                return;
            }
    
            var confirm = MessageBox.Show(this, $"Delete category '{category.Name}'?", "Confirm Delete",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
            if (confirm != DialogResult.Yes)
            {
                return;
            }
    
            try
            {
                using var ctx = Db.CreateContext();
                var tracked = await ctx.Categories.FindAsync(category.Id);
                if (tracked is not null)
                {
                    ctx.Categories.Remove(tracked);
                    await ctx.SaveChangesAsync();
                }
                await LoadCategoriesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Could not delete category — it may still have subcategories or products referencing it.\n\n{ex.Message}",
                    "Delete Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

}
