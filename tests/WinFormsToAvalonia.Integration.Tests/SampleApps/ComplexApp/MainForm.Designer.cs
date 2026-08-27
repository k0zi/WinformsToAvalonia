namespace ComplexApp
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.statusTimer = new Timer(this.components);
            this.tabControl1 = new TabControl();
            this.generalTab = new TabPage();
            this.optionsGroup = new GroupBox();
            this.enabledCheckBox = new CheckBox();
            this.modeComboBox = new ComboBox();
            this.progressBar1 = new ProgressBar();
            this.amountUpDown = new NumericUpDown();
            this.advancedTab = new TabPage();
            this.flowLayoutPanel1 = new FlowLayoutPanel();
            this.saveButton = new Button();
            this.cancelButton = new Button();
            this.tableLayoutPanel1 = new TableLayoutPanel();
            this.nameLabel = new Label();
            this.nameTextBox = new TextBox();
            this.tabControl1.SuspendLayout();
            this.generalTab.SuspendLayout();
            this.optionsGroup.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.amountUpDown)).BeginInit();
            this.advancedTab.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            //
            // tabControl1
            //
            this.tabControl1.Controls.Add(this.generalTab);
            this.tabControl1.Controls.Add(this.advancedTab);
            this.tabControl1.Location = new System.Drawing.Point(12, 60);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.Size = new System.Drawing.Size(360, 220);
            this.tabControl1.TabIndex = 0;
            this.tabControl1.SelectedIndexChanged += new System.EventHandler(this.tabControl1_SelectedIndexChanged);
            //
            // generalTab
            //
            this.generalTab.Controls.Add(this.optionsGroup);
            this.generalTab.Location = new System.Drawing.Point(4, 24);
            this.generalTab.Name = "generalTab";
            this.generalTab.Size = new System.Drawing.Size(352, 192);
            this.generalTab.TabIndex = 0;
            this.generalTab.Text = "General";
            //
            // optionsGroup
            //
            this.optionsGroup.Controls.Add(this.enabledCheckBox);
            this.optionsGroup.Controls.Add(this.modeComboBox);
            this.optionsGroup.Controls.Add(this.progressBar1);
            this.optionsGroup.Controls.Add(this.amountUpDown);
            this.optionsGroup.Location = new System.Drawing.Point(8, 8);
            this.optionsGroup.Name = "optionsGroup";
            this.optionsGroup.Size = new System.Drawing.Size(330, 160);
            this.optionsGroup.TabIndex = 0;
            this.optionsGroup.Text = "Options";
            //
            // enabledCheckBox
            //
            this.enabledCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
            this.enabledCheckBox.Checked = true;
            this.enabledCheckBox.Location = new System.Drawing.Point(12, 24);
            this.enabledCheckBox.Name = "enabledCheckBox";
            this.enabledCheckBox.Size = new System.Drawing.Size(104, 24);
            this.enabledCheckBox.TabIndex = 0;
            this.enabledCheckBox.Text = "Enabled";
            //
            // modeComboBox
            //
            this.modeComboBox.Location = new System.Drawing.Point(12, 54);
            this.modeComboBox.Name = "modeComboBox";
            this.modeComboBox.Size = new System.Drawing.Size(150, 23);
            this.modeComboBox.TabIndex = 1;
            //
            // progressBar1
            //
            this.progressBar1.Location = new System.Drawing.Point(12, 84);
            this.progressBar1.Maximum = 200;
            this.progressBar1.Minimum = 0;
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(200, 23);
            this.progressBar1.TabIndex = 2;
            this.progressBar1.Value = 40;
            //
            // amountUpDown
            //
            this.amountUpDown.Location = new System.Drawing.Point(12, 114);
            this.amountUpDown.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            this.amountUpDown.Name = "amountUpDown";
            this.amountUpDown.Size = new System.Drawing.Size(80, 23);
            this.amountUpDown.TabIndex = 3;
            //
            // advancedTab
            //
            this.advancedTab.Controls.Add(this.flowLayoutPanel1);
            this.advancedTab.Controls.Add(this.tableLayoutPanel1);
            this.advancedTab.Location = new System.Drawing.Point(4, 24);
            this.advancedTab.Name = "advancedTab";
            this.advancedTab.Size = new System.Drawing.Size(352, 192);
            this.advancedTab.TabIndex = 1;
            this.advancedTab.Text = "Advanced";
            //
            // flowLayoutPanel1
            //
            this.flowLayoutPanel1.Controls.Add(this.saveButton);
            this.flowLayoutPanel1.Controls.Add(this.cancelButton);
            this.flowLayoutPanel1.Location = new System.Drawing.Point(8, 8);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(200, 40);
            this.flowLayoutPanel1.TabIndex = 0;
            //
            // saveButton
            //
            this.saveButton.Location = new System.Drawing.Point(3, 3);
            this.saveButton.Name = "saveButton";
            this.saveButton.Size = new System.Drawing.Size(75, 23);
            this.saveButton.TabIndex = 0;
            this.saveButton.Text = "Save";
            this.saveButton.Click += new System.EventHandler(this.saveButton_Click);
            //
            // cancelButton
            //
            this.cancelButton.Location = new System.Drawing.Point(84, 3);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 1;
            this.cancelButton.Text = "Cancel";
            //
            // tableLayoutPanel1
            //
            this.tableLayoutPanel1.Controls.Add(this.nameLabel);
            this.tableLayoutPanel1.Controls.Add(this.nameTextBox);
            this.tableLayoutPanel1.Location = new System.Drawing.Point(8, 60);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.Size = new System.Drawing.Size(300, 40);
            this.tableLayoutPanel1.TabIndex = 1;
            //
            // nameLabel
            //
            this.nameLabel.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.nameLabel.ForeColor = System.Drawing.Color.FromArgb(64, 64, 64);
            this.nameLabel.Location = new System.Drawing.Point(3, 8);
            this.nameLabel.Name = "nameLabel";
            this.nameLabel.Size = new System.Drawing.Size(80, 23);
            this.nameLabel.TabIndex = 0;
            this.nameLabel.Text = "Name:";
            //
            // nameTextBox
            //
            this.nameTextBox.Location = new System.Drawing.Point(89, 8);
            this.nameTextBox.Name = "nameTextBox";
            this.nameTextBox.Size = new System.Drawing.Size(180, 23);
            this.nameTextBox.TabIndex = 1;
            //
            // statusTimer
            //
            this.statusTimer.Interval = 1000;
            //
            // MainForm
            //
            this.ClientSize = new System.Drawing.Size(384, 292);
            this.Controls.Add(this.tabControl1);
            this.Name = "MainForm";
            this.Text = "Complex Demo";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.tabControl1.ResumeLayout(false);
            this.generalTab.ResumeLayout(false);
            this.optionsGroup.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.amountUpDown)).EndInit();
            this.advancedTab.ResumeLayout(false);
            this.flowLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        private Timer statusTimer;
        private TabControl tabControl1;
        private TabPage generalTab;
        private GroupBox optionsGroup;
        private CheckBox enabledCheckBox;
        private ComboBox modeComboBox;
        private ProgressBar progressBar1;
        private NumericUpDown amountUpDown;
        private TabPage advancedTab;
        private FlowLayoutPanel flowLayoutPanel1;
        private Button saveButton;
        private Button cancelButton;
        private TableLayoutPanel tableLayoutPanel1;
        private Label nameLabel;
        private TextBox nameTextBox;
    }
}
