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
/// ViewModel for CustomersForm (user customizations).
/// This file is preserved during reconversion - add your custom code here.
/// </summary>
public partial class CustomersFormViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    internal List<Customer> _customers = [];

    internal Customer? _current;

    internal async Task LoadCustomersAsync()
        {
            var searchText = searchTextBox.Text;
            _customers = await Task.Run(() =>
            {
                using var ctx = Db.CreateContext();
                var query = ctx.Customers.AsQueryable();
                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    query = query.Where(c => c.Name.Contains(searchText));
                }
                return query.OrderBy(c => c.Name).ToList();
            });
    
            bindingSourceControl.DataSource = _customers;
            recordCountLabel.Text = $"{_customers.Count} customer(s)";
            ClearDetails();
        }

    internal async void CustomersGrid_SelectionChanged(object? sender, EventArgs e)
        {
            if (bindingSourceControl.Current is not Customer customer)
            {
                return;
            }
    
            _current = customer;
            nameTextBox.Text = customer.Name;
            contactTextBox.Text = customer.ContactName;
            phoneMaskedTextBox.Text = customer.Phone;
            emailTextBox.Text = customer.Email;
            addressTextBox.Text = customer.Address;
            activeCheckBox.Checked = customer.IsActive;
            notesRichTextBox.Text = customer.Notes;
    
            await LoadOrdersForCustomerAsync(customer.Id);
        }

    internal async Task LoadOrdersForCustomerAsync(int customerId)
        {
            ordersListView.Items.Clear();
            var orders = await Task.Run(() =>
            {
                using var ctx = Db.CreateContext();
                return ctx.SalesOrders.Where(o => o.CustomerId == customerId).OrderByDescending(o => o.OrderDate).ToList();
            });
    
            foreach (var order in orders)
            {
                var item = new ListViewItem(order.OrderNumber);
                item.SubItems.Add(order.Status.ToString());
                item.SubItems.Add(order.OrderDate.ToString("d"));
                ordersListView.Items.Add(item);
            }
        }

    internal void NewCustomer()
        {
            _current = new Customer { IsActive = true };
            customersGrid.ClearSelection();
            ClearDetails(keepCurrent: true);
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
            notesRichTextBox.Clear();
            activeCheckBox.Checked = true;
            ordersListView.Items.Clear();
        }

    internal async Task SaveCustomerAsync()
        {
            _current ??= new Customer();
    
            if (string.IsNullOrWhiteSpace(nameTextBox.Text))
            {
                await WarehouseAvaloniaApp.Common.Dialogs.ShowAsync("Customer name is required.","Validation",WarehouseAvaloniaApp.Common.MessageBoxButtons.OK,WarehouseAvaloniaApp.Common.MessageBoxIcon.Warning);
                return;
            }
    
            _current.Name = nameTextBox.Text.Trim();
            _current.ContactName = contactTextBox.Text.Trim();
            _current.Phone = phoneMaskedTextBox.Text.Trim();
            _current.Email = emailTextBox.Text.Trim();
            _current.Address = addressTextBox.Text.Trim();
            _current.Notes = notesRichTextBox.Text;
            _current.IsActive = activeCheckBox.Checked;
    
            using var ctx = Db.CreateContext();
            if (_current.Id == 0)
            {
                ctx.Customers.Add(_current);
            }
            else
            {
                ctx.Customers.Attach(_current).State = EntityState.Modified;
            }
            await ctx.SaveChangesAsync();
    
            await LoadCustomersAsync();
        }

    internal async Task DeleteCustomerAsync()
        {
            if (_current is null || _current.Id == 0)
            {
                return;
            }
    
            var confirm = MessageBox.Show(this, $"Delete customer '{_current.Name}'?", "Confirm Delete",
                WarehouseAvaloniaApp.Common.MessageBoxButtons.YesNo, WarehouseAvaloniaApp.Common.MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
            if (confirm != WarehouseAvaloniaApp.Common.DialogResult.Yes)
            {
                return;
            }
    
            try
            {
                using var ctx = Db.CreateContext();
                var tracked = await ctx.Customers.FindAsync(_current.Id);
                if (tracked is not null)
                {
                    ctx.Customers.Remove(tracked);
                    await ctx.SaveChangesAsync();
                }
                await LoadCustomersAsync();
            }
            catch (Exception ex)
            {
                await WarehouseAvaloniaApp.Common.Dialogs.ShowAsync($"Could not delete customer: {ex.Message}","Delete Failed",                WarehouseAvaloniaApp.Common.MessageBoxButtons.OK,WarehouseAvaloniaApp.Common.MessageBoxIcon.Error);
            }
        }

}
