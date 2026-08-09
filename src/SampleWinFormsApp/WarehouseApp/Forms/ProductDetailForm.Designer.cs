namespace WarehouseApp.Forms;

partial class ProductDetailForm
{
    private GroupBox detailsGroupBox = null!;
    private TableLayoutPanel fieldsTableLayoutPanel = null!;
    private Label skuLabel = null!;
    private TextBox skuTextBox = null!;
    private Label nameLabel = null!;
    private TextBox nameTextBox = null!;
    private Label descriptionLabel = null!;
    private TextBox descriptionTextBox = null!;
    private Label categoryLabel = null!;
    private ComboBox categoryComboBox = null!;
    private Label supplierLabel = null!;
    private ComboBox supplierComboBox = null!;
    private Label unitOfMeasureLabel = null!;
    private DomainUpDown unitOfMeasureDomainUpDown = null!;
    private Label unitPriceLabel = null!;
    private NumericUpDown unitPriceNumericUpDown = null!;
    private Label reorderLevelLabel = null!;
    private NumericUpDown reorderLevelNumericUpDown = null!;
    private CheckBox isActiveCheckBox = null!;
    private PictureBox productPictureBox = null!;
    private Button chooseImageButton = null!;
    private OpenFileDialog openFileDialog = null!;

    private void InitializeComponent()
    {
        this.SuspendLayout();

        this.detailsGroupBox = new System.Windows.Forms.GroupBox();
        this.detailsGroupBox.Text = "Product Details";
        this.detailsGroupBox.Location = new System.Drawing.Point(12, 12);
        this.detailsGroupBox.Size = new System.Drawing.Size(430, 330);

        this.fieldsTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
        this.fieldsTableLayoutPanel.Location = new System.Drawing.Point(15, 25);
        this.fieldsTableLayoutPanel.Size = new System.Drawing.Size(400, 290);
        this.fieldsTableLayoutPanel.ColumnCount = 2;
        this.fieldsTableLayoutPanel.RowCount = 8;
        this.fieldsTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 110));
        this.fieldsTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100));
        for (var i = 0; i < 8; i++)
        {
            this.fieldsTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34));
        }

        this.skuLabel = new System.Windows.Forms.Label();
        this.skuLabel.Text = "SKU:";
        this.skuLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        this.skuLabel.Dock = System.Windows.Forms.DockStyle.Fill;
        this.skuTextBox = new System.Windows.Forms.TextBox();
        this.skuTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
        this.skuTextBox.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);

        this.nameLabel = new System.Windows.Forms.Label();
        this.nameLabel.Text = "Name:";
        this.nameLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        this.nameLabel.Dock = System.Windows.Forms.DockStyle.Fill;
        this.nameTextBox = new System.Windows.Forms.TextBox();
        this.nameTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
        this.nameTextBox.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);

        this.descriptionLabel = new System.Windows.Forms.Label();
        this.descriptionLabel.Text = "Description:";
        this.descriptionLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        this.descriptionLabel.Dock = System.Windows.Forms.DockStyle.Fill;
        this.descriptionTextBox = new System.Windows.Forms.TextBox();
        this.descriptionTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
        this.descriptionTextBox.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);

        this.categoryLabel = new System.Windows.Forms.Label();
        this.categoryLabel.Text = "Category:";
        this.categoryLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        this.categoryLabel.Dock = System.Windows.Forms.DockStyle.Fill;
        this.categoryComboBox = new System.Windows.Forms.ComboBox();
        this.categoryComboBox.Dock = System.Windows.Forms.DockStyle.Fill;
        this.categoryComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.categoryComboBox.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);

        this.supplierLabel = new System.Windows.Forms.Label();
        this.supplierLabel.Text = "Supplier:";
        this.supplierLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        this.supplierLabel.Dock = System.Windows.Forms.DockStyle.Fill;
        this.supplierComboBox = new System.Windows.Forms.ComboBox();
        this.supplierComboBox.Dock = System.Windows.Forms.DockStyle.Fill;
        this.supplierComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.supplierComboBox.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);

        this.unitOfMeasureLabel = new System.Windows.Forms.Label();
        this.unitOfMeasureLabel.Text = "Unit:";
        this.unitOfMeasureLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        this.unitOfMeasureLabel.Dock = System.Windows.Forms.DockStyle.Fill;
        this.unitOfMeasureDomainUpDown = new System.Windows.Forms.DomainUpDown();
        this.unitOfMeasureDomainUpDown.Dock = System.Windows.Forms.DockStyle.Fill;
        this.unitOfMeasureDomainUpDown.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
        foreach (var uom in Enum.GetNames<WarehouseApp.Data.Models.UnitOfMeasure>())
        {
            this.unitOfMeasureDomainUpDown.Items.Add(uom);
        }
        this.unitOfMeasureDomainUpDown.SelectedIndex = 0;

        this.unitPriceLabel = new System.Windows.Forms.Label();
        this.unitPriceLabel.Text = "Unit Price ($):";
        this.unitPriceLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        this.unitPriceLabel.Dock = System.Windows.Forms.DockStyle.Fill;
        this.unitPriceNumericUpDown = new System.Windows.Forms.NumericUpDown();
        this.unitPriceNumericUpDown.Dock = System.Windows.Forms.DockStyle.Fill;
        this.unitPriceNumericUpDown.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
        this.unitPriceNumericUpDown.Minimum = 0;
        this.unitPriceNumericUpDown.Maximum = 100000;
        this.unitPriceNumericUpDown.DecimalPlaces = 2;
        this.unitPriceNumericUpDown.Increment = 0.5m;

        this.reorderLevelLabel = new System.Windows.Forms.Label();
        this.reorderLevelLabel.Text = "Reorder Level:";
        this.reorderLevelLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        this.reorderLevelLabel.Dock = System.Windows.Forms.DockStyle.Fill;
        this.reorderLevelNumericUpDown = new System.Windows.Forms.NumericUpDown();
        this.reorderLevelNumericUpDown.Dock = System.Windows.Forms.DockStyle.Fill;
        this.reorderLevelNumericUpDown.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
        this.reorderLevelNumericUpDown.Minimum = 0;
        this.reorderLevelNumericUpDown.Maximum = 10000;

        this.isActiveCheckBox = new System.Windows.Forms.CheckBox();
        this.isActiveCheckBox.Text = "Active";
        this.isActiveCheckBox.Dock = System.Windows.Forms.DockStyle.Fill;
        this.isActiveCheckBox.Checked = true;

        this.fieldsTableLayoutPanel.Controls.Add(this.skuLabel, 0, 0);
        this.fieldsTableLayoutPanel.Controls.Add(this.skuTextBox, 1, 0);
        this.fieldsTableLayoutPanel.Controls.Add(this.nameLabel, 0, 1);
        this.fieldsTableLayoutPanel.Controls.Add(this.nameTextBox, 1, 1);
        this.fieldsTableLayoutPanel.Controls.Add(this.descriptionLabel, 0, 2);
        this.fieldsTableLayoutPanel.Controls.Add(this.descriptionTextBox, 1, 2);
        this.fieldsTableLayoutPanel.Controls.Add(this.categoryLabel, 0, 3);
        this.fieldsTableLayoutPanel.Controls.Add(this.categoryComboBox, 1, 3);
        this.fieldsTableLayoutPanel.Controls.Add(this.supplierLabel, 0, 4);
        this.fieldsTableLayoutPanel.Controls.Add(this.supplierComboBox, 1, 4);
        this.fieldsTableLayoutPanel.Controls.Add(this.unitOfMeasureLabel, 0, 5);
        this.fieldsTableLayoutPanel.Controls.Add(this.unitOfMeasureDomainUpDown, 1, 5);
        this.fieldsTableLayoutPanel.Controls.Add(this.unitPriceLabel, 0, 6);
        this.fieldsTableLayoutPanel.Controls.Add(this.unitPriceNumericUpDown, 1, 6);
        this.fieldsTableLayoutPanel.Controls.Add(this.reorderLevelLabel, 0, 7);
        this.fieldsTableLayoutPanel.Controls.Add(this.reorderLevelNumericUpDown, 1, 7);

        this.detailsGroupBox.Controls.Add(this.fieldsTableLayoutPanel);
        this.detailsGroupBox.Controls.Add(this.isActiveCheckBox);
        this.isActiveCheckBox.Dock = System.Windows.Forms.DockStyle.None;
        this.isActiveCheckBox.Location = new System.Drawing.Point(15, 300);
        this.isActiveCheckBox.AutoSize = true;

        this.productPictureBox = new System.Windows.Forms.PictureBox();
        this.productPictureBox.Location = new System.Drawing.Point(456, 12);
        this.productPictureBox.Size = new System.Drawing.Size(120, 120);
        this.productPictureBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
        this.productPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
        this.productPictureBox.Image = Common.AppIcons.CreatePlaceholderProductImage();
        this.chooseImageButton = new System.Windows.Forms.Button();
        this.chooseImageButton.Text = "Choose Image...";
        this.chooseImageButton.Location = new System.Drawing.Point(456, 138);
        this.chooseImageButton.Size = new System.Drawing.Size(120, 28);
        this.chooseImageButton.Click += this.chooseImageButton_Click;
        this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
        this.openFileDialog.Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp|All Files|*.*";

        this.ClientSize = new System.Drawing.Size(600, 380);
        this.Controls.Add(this.detailsGroupBox);
        this.Controls.Add(this.productPictureBox);
        this.Controls.Add(this.chooseImageButton);
        this.Text = "Product Detail — WarehouseApp";

        this.ResumeLayout(false);
        this.PerformLayout();
    }
}
