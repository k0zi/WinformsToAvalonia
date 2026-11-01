namespace SettingsApp
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        private void InitializeComponent()
        {
            this.nameBox = new System.Windows.Forms.TextBox();
            this.urlLabel = new System.Windows.Forms.Label();
            this.countLabel = new System.Windows.Forms.Label();
            this.saveButton = new System.Windows.Forms.Button();
            this.resetButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // nameBox
            //
            this.nameBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::SettingsApp.Properties.Settings.Default, "UserName", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.nameBox.Location = new System.Drawing.Point(12, 12);
            this.nameBox.Name = "nameBox";
            this.nameBox.Size = new System.Drawing.Size(200, 23);
            this.nameBox.TabIndex = 0;
            this.nameBox.Text = global::SettingsApp.Properties.Settings.Default.UserName;
            //
            // urlLabel
            //
            this.urlLabel.Location = new System.Drawing.Point(12, 45);
            this.urlLabel.Name = "urlLabel";
            this.urlLabel.Size = new System.Drawing.Size(200, 23);
            this.urlLabel.TabIndex = 1;
            //
            // countLabel
            //
            this.countLabel.Location = new System.Drawing.Point(12, 75);
            this.countLabel.Name = "countLabel";
            this.countLabel.Size = new System.Drawing.Size(200, 23);
            this.countLabel.TabIndex = 2;
            //
            // saveButton
            //
            this.saveButton.Location = new System.Drawing.Point(12, 105);
            this.saveButton.Name = "saveButton";
            this.saveButton.Size = new System.Drawing.Size(90, 23);
            this.saveButton.TabIndex = 3;
            this.saveButton.Text = "Save";
            this.saveButton.UseVisualStyleBackColor = true;
            this.saveButton.Click += new System.EventHandler(this.saveButton_Click);
            //
            // resetButton
            //
            this.resetButton.Location = new System.Drawing.Point(110, 105);
            this.resetButton.Name = "resetButton";
            this.resetButton.Size = new System.Drawing.Size(90, 23);
            this.resetButton.TabIndex = 4;
            this.resetButton.Text = "Reset";
            this.resetButton.UseVisualStyleBackColor = true;
            this.resetButton.Click += new System.EventHandler(this.resetButton_Click);
            //
            // MainForm
            //
            this.ClientSize = new System.Drawing.Size(240, 150);
            this.Controls.Add(this.resetButton);
            this.Controls.Add(this.saveButton);
            this.Controls.Add(this.countLabel);
            this.Controls.Add(this.urlLabel);
            this.Controls.Add(this.nameBox);
            this.Name = "MainForm";
            this.Text = "Settings";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.TextBox nameBox;
        private System.Windows.Forms.Label urlLabel;
        private System.Windows.Forms.Label countLabel;
        private System.Windows.Forms.Button saveButton;
        private System.Windows.Forms.Button resetButton;
    }
}
