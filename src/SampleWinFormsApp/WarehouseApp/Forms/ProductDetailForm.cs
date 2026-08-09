using Microsoft.EntityFrameworkCore;
using WarehouseApp.Common;
using WarehouseApp.Data.Models;

namespace WarehouseApp.Forms;

public partial class ProductDetailForm : DetailFormBase<Product>
{
    private List<Category> _categories = [];
    private List<Supplier> _suppliers = [];

    /// <summary>When set on a new (IsNew) form, pre-fills fields from another product for a "duplicate" flow.</summary>
    public Product? SeedTemplate { get; set; }

    public ProductDetailForm(Product? product = null) : base(product)
    {
        InitializeComponent();
        Text = IsNew ? "New Product — WarehouseApp" : $"Edit Product — {product!.Sku}";
    }

    protected override void LoadFromEntity()
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

    protected override bool ValidateInput()
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

    protected override void SaveToEntity()
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

    protected override async Task PersistAsync()
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

    private void chooseImageButton_Click(object? sender, EventArgs e)
    {
        if (openFileDialog.ShowDialog(this) == DialogResult.OK)
        {
            Entity.ImagePath = openFileDialog.FileName;
            productPictureBox.Image = Image.FromFile(openFileDialog.FileName);
        }
    }
}
