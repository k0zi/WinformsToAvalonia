using Microsoft.EntityFrameworkCore;
using WarehouseApp.Common;
using WarehouseApp.Data.Models;

namespace WarehouseApp.Forms;

public partial class CustomersForm : Form
{
    private List<Customer> _customers = [];
    private Customer? _current;

    public CustomersForm()
    {
        InitializeComponent();
        Load += async (_, _) => await LoadCustomersAsync();
    }

    private async Task LoadCustomersAsync()
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

    private async void CustomersGrid_SelectionChanged(object? sender, EventArgs e)
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

    private async Task LoadOrdersForCustomerAsync(int customerId)
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

    private void NewCustomer()
    {
        _current = new Customer { IsActive = true };
        customersGrid.ClearSelection();
        ClearDetails(keepCurrent: true);
        nameTextBox.Focus();
    }

    private void ClearDetails(bool keepCurrent = false)
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

    private async Task SaveCustomerAsync()
    {
        _current ??= new Customer();

        if (string.IsNullOrWhiteSpace(nameTextBox.Text))
        {
            MessageBox.Show(this, "Customer name is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

    private async Task DeleteCustomerAsync()
    {
        if (_current is null || _current.Id == 0)
        {
            return;
        }

        var confirm = MessageBox.Show(this, $"Delete customer '{_current.Name}'?", "Confirm Delete",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
        if (confirm != DialogResult.Yes)
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
            MessageBox.Show(this, $"Could not delete customer: {ex.Message}", "Delete Failed",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
