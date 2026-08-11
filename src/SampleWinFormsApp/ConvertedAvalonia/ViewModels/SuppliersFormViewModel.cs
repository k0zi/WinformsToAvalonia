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
/// ViewModel for SuppliersForm (user customizations).
/// This file is preserved during reconversion - add your custom code here.
/// </summary>
public partial class SuppliersFormViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    internal List<Supplier> _suppliers = [];

    internal Supplier? _current;

    internal async Task LoadSuppliersAsync(string? searchText = null)
        {
            searchText ??= searchTextBox.Text;
            _suppliers = await Task.Run(() =>
            {
                using var ctx = Db.CreateContext();
                var query = ctx.Suppliers.AsQueryable();
                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    query = query.Where(s => s.Name.Contains(searchText));
                }
                return query.OrderBy(s => s.Name).ToList();
            });
    
            suppliersListView.Items.Clear();
            foreach (var supplier in _suppliers)
            {
                var item = new ListViewItem(supplier.Name, "Supplier");
                item.SubItems.Add(supplier.Phone ?? "—");
                item.SubItems.Add(supplier.Rating.ToString());
                item.Tag = supplier;
                suppliersListView.Items.Add(item);
            }
    
            recordCountLabel.Text = $"{_suppliers.Count} supplier(s)";
            ClearDetails();
        }

    internal void SuppliersListView_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (suppliersListView.SelectedItems.Count == 0)
            {
                return;
            }
    
            _current = (Supplier)suppliersListView.SelectedItems[0].Tag!;
            nameTextBox.Text = _current.Name;
            contactTextBox.Text = _current.ContactName;
            phoneMaskedTextBox.Text = _current.Phone;
            emailTextBox.Text = _current.Email;
            addressTextBox.Text = _current.Address;
            ratingControl.Value = _current.Rating;
            activeCheckBox.Checked = _current.IsActive;
        }

    internal void NewSupplier()
        {
            _current = new Supplier { IsActive = true };
            ClearDetails(keepCurrent: true);
            suppliersListView.SelectedItems.Clear();
            nameTextBox.Focus();
        }

    internal void ClearDetails(bool keepCurrent = false)
        {
            if (!keepCurrent)
            {
                _current = null;
            }
            nameTextBox.Clear();
            contactTextBox.Clear();
            phoneMaskedTextBox.Clear();
            emailTextBox.Clear();
            addressTextBox.Clear();
            ratingControl.Value = 0;
            activeCheckBox.Checked = true;
        }

    internal async Task SaveSupplierAsync()
        {
            if (_current is null)
            {
                _current = new Supplier();
            }
    
            if (string.IsNullOrWhiteSpace(nameTextBox.Text))
            {
                MessageBox.Show(this, "Supplier name is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
    
            _current.Name = nameTextBox.Text.Trim();
            _current.ContactName = contactTextBox.Text.Trim();
            _current.Phone = phoneMaskedTextBox.Text.Trim();
            _current.Email = emailTextBox.Text.Trim();
            _current.Address = addressTextBox.Text.Trim();
            _current.Rating = ratingControl.Value;
            _current.IsActive = activeCheckBox.Checked;
    
            using var ctx = Db.CreateContext();
            if (_current.Id == 0)
            {
                ctx.Suppliers.Add(_current);
            }
            else
            {
                ctx.Suppliers.Attach(_current).State = EntityState.Modified;
            }
            await ctx.SaveChangesAsync();
    
            await LoadSuppliersAsync();
        }

    internal async Task DeleteSupplierAsync()
        {
            if (_current is null || _current.Id == 0)
            {
                return;
            }
    
            var confirm = MessageBox.Show(this, $"Delete supplier '{_current.Name}'?", "Confirm Delete",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
            if (confirm != DialogResult.Yes)
            {
                return;
            }
    
            try
            {
                using var ctx = Db.CreateContext();
                var tracked = await ctx.Suppliers.FindAsync(_current.Id);
                if (tracked is not null)
                {
                    ctx.Suppliers.Remove(tracked);
                    await ctx.SaveChangesAsync();
                }
                await LoadSuppliersAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Could not delete supplier: {ex.Message}", "Delete Failed",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

    internal void EmailLinkLabel_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(emailTextBox.Text))
            {
                MessageBox.Show(this, "No email address on file.", "Send Email", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
    
            MessageBox.Show(this, $"Would open mail client for: {emailTextBox.Text}", "Send Email",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

}
