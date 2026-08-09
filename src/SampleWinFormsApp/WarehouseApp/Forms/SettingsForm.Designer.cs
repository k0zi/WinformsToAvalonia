using WarehouseApp.Controls;

namespace WarehouseApp.Forms;

partial class SettingsForm
{
    private System.ComponentModel.IContainer components = null!;
    private TabControl mainTabControl = null!;
    private TabPage generalTabPage = null!;
    private TabPage advancedTabPage = null!;
    private TabPage aboutTabPage = null!;

    private Label companyNameLabel = null!;
    private TextBox companyNameTextBox = null!;
    private Label darkModeLabel = null!;
    private ToggleSwitchControl darkModeToggle = null!;
    private GroupBox accentGroupBox = null!;
    private RadioButton presetColorRadioButton = null!;
    private RadioButton customColorRadioButton = null!;
    private Panel colorPreviewPanel = null!;
    private Button chooseColorButton = null!;
    private ColorDialog colorDialog = null!;
    private Label backupFolderLabel = null!;
    private TextBox backupFolderTextBox = null!;
    private Button browseFolderButton = null!;
    private FolderBrowserDialog folderBrowserDialog = null!;
    private Button saveGeneralButton = null!;

    private PropertyGrid advancedPropertyGrid = null!;
    private Button saveAdvancedButton = null!;

    private WebBrowser aboutWebBrowser = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.components?.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        this.components = new System.ComponentModel.Container();
        this.SuspendLayout();

        this.companyNameLabel = new System.Windows.Forms.Label();
        this.companyNameLabel.Text = "Company Name:";
        this.companyNameLabel.Location = new System.Drawing.Point(12, 15);
        this.companyNameLabel.AutoSize = true;
        this.companyNameTextBox = new System.Windows.Forms.TextBox();
        this.companyNameTextBox.Location = new System.Drawing.Point(140, 12);
        this.companyNameTextBox.Width = 260;

        this.darkModeLabel = new System.Windows.Forms.Label();
        this.darkModeLabel.Text = "Dark Mode:";
        this.darkModeLabel.Location = new System.Drawing.Point(12, 52);
        this.darkModeLabel.AutoSize = true;
        this.darkModeToggle = new WarehouseApp.Controls.ToggleSwitchControl();
        this.darkModeToggle.Location = new System.Drawing.Point(140, 46);

        this.accentGroupBox = new System.Windows.Forms.GroupBox();
        this.accentGroupBox.Text = "Accent Color";
        this.accentGroupBox.Location = new System.Drawing.Point(12, 90);
        this.accentGroupBox.Size = new System.Drawing.Size(390, 90);
        this.presetColorRadioButton = new System.Windows.Forms.RadioButton();
        this.presetColorRadioButton.Text = "Use preset (blue)";
        this.presetColorRadioButton.Location = new System.Drawing.Point(12, 25);
        this.presetColorRadioButton.AutoSize = true;
        this.presetColorRadioButton.Checked = true;
        this.customColorRadioButton = new System.Windows.Forms.RadioButton();
        this.customColorRadioButton.Text = "Use custom color";
        this.customColorRadioButton.Location = new System.Drawing.Point(12, 50);
        this.customColorRadioButton.AutoSize = true;
        this.colorPreviewPanel = new System.Windows.Forms.Panel();
        this.colorPreviewPanel.Location = new System.Drawing.Point(160, 48);
        this.colorPreviewPanel.Size = new System.Drawing.Size(24, 20);
        this.colorPreviewPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
        this.colorPreviewPanel.BackColor = System.Drawing.Color.FromArgb(0x2D, 0x6C, 0xDF);
        this.chooseColorButton = new System.Windows.Forms.Button();
        this.chooseColorButton.Text = "Choose...";
        this.chooseColorButton.Location = new System.Drawing.Point(196, 46);
        this.chooseColorButton.Size = new System.Drawing.Size(80, 26);
        this.chooseColorButton.Click += this.chooseColorButton_Click;
        this.colorDialog = new System.Windows.Forms.ColorDialog();
        this.accentGroupBox.Controls.Add(this.presetColorRadioButton);
        this.accentGroupBox.Controls.Add(this.customColorRadioButton);
        this.accentGroupBox.Controls.Add(this.colorPreviewPanel);
        this.accentGroupBox.Controls.Add(this.chooseColorButton);

        this.backupFolderLabel = new System.Windows.Forms.Label();
        this.backupFolderLabel.Text = "Backup Folder:";
        this.backupFolderLabel.Location = new System.Drawing.Point(12, 195);
        this.backupFolderLabel.AutoSize = true;
        this.backupFolderTextBox = new System.Windows.Forms.TextBox();
        this.backupFolderTextBox.Location = new System.Drawing.Point(140, 192);
        this.backupFolderTextBox.Width = 260;
        this.backupFolderTextBox.ReadOnly = true;
        this.browseFolderButton = new System.Windows.Forms.Button();
        this.browseFolderButton.Text = "Browse...";
        this.browseFolderButton.Location = new System.Drawing.Point(140, 222);
        this.browseFolderButton.Size = new System.Drawing.Size(90, 26);
        this.browseFolderButton.Click += this.browseFolderButton_Click;
        this.folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();

        this.saveGeneralButton = new System.Windows.Forms.Button();
        this.saveGeneralButton.Text = "Save";
        this.saveGeneralButton.Location = new System.Drawing.Point(12, 270);
        this.saveGeneralButton.Size = new System.Drawing.Size(100, 30);
        this.saveGeneralButton.Font = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold);
        this.saveGeneralButton.Click += async (_, _) => await this.SaveGeneralAsync();

        this.generalTabPage = new System.Windows.Forms.TabPage("General");
        this.generalTabPage.Controls.Add(this.companyNameLabel);
        this.generalTabPage.Controls.Add(this.companyNameTextBox);
        this.generalTabPage.Controls.Add(this.darkModeLabel);
        this.generalTabPage.Controls.Add(this.darkModeToggle);
        this.generalTabPage.Controls.Add(this.accentGroupBox);
        this.generalTabPage.Controls.Add(this.backupFolderLabel);
        this.generalTabPage.Controls.Add(this.backupFolderTextBox);
        this.generalTabPage.Controls.Add(this.browseFolderButton);
        this.generalTabPage.Controls.Add(this.saveGeneralButton);

        this.advancedPropertyGrid = new System.Windows.Forms.PropertyGrid();
        this.advancedPropertyGrid.Location = new System.Drawing.Point(12, 12);
        this.advancedPropertyGrid.Size = new System.Drawing.Size(420, 380);
        this.saveAdvancedButton = new System.Windows.Forms.Button();
        this.saveAdvancedButton.Text = "Save";
        this.saveAdvancedButton.Location = new System.Drawing.Point(12, 400);
        this.saveAdvancedButton.Size = new System.Drawing.Size(100, 30);
        this.saveAdvancedButton.Font = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold);
        this.saveAdvancedButton.Click += async (_, _) => await this.SaveAdvancedAsync();

        this.advancedTabPage = new System.Windows.Forms.TabPage("Advanced");
        this.advancedTabPage.Controls.Add(this.advancedPropertyGrid);
        this.advancedTabPage.Controls.Add(this.saveAdvancedButton);

        this.aboutWebBrowser = new System.Windows.Forms.WebBrowser();
        this.aboutWebBrowser.Dock = System.Windows.Forms.DockStyle.Fill;
        this.aboutTabPage = new System.Windows.Forms.TabPage("About");
        this.aboutTabPage.Controls.Add(this.aboutWebBrowser);

        this.mainTabControl = new System.Windows.Forms.TabControl();
        this.mainTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
        this.mainTabControl.TabPages.AddRange(new System.Windows.Forms.TabPage[] { this.generalTabPage, this.advancedTabPage, this.aboutTabPage });

        this.ClientSize = new System.Drawing.Size(460, 460);
        this.Controls.Add(this.mainTabControl);
        this.Text = "Settings — WarehouseApp";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;

        this.ResumeLayout(false);
        this.PerformLayout();
    }
}