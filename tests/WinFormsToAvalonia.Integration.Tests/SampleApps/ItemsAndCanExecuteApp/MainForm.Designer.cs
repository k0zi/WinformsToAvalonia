namespace ItemsAndCanExecuteApp
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
            this.categoryComboBox = new System.Windows.Forms.ComboBox();
            this.tagsListBox = new System.Windows.Forms.ListBox();
            this.nameTextBox = new System.Windows.Forms.TextBox();
            this.submitButton = new System.Windows.Forms.Button();
            this.resultLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            //
            // categoryComboBox
            //
            this.categoryComboBox.Items.AddRange(new object[] { "Hardware", "Software", "Services" });
            this.categoryComboBox.Location = new System.Drawing.Point(12, 12);
            this.categoryComboBox.Name = "categoryComboBox";
            this.categoryComboBox.Size = new System.Drawing.Size(180, 27);
            this.categoryComboBox.TabIndex = 0;
            //
            // tagsListBox
            //
            this.tagsListBox.Items.Add("Urgent");
            this.tagsListBox.Items.Add("Later");
            this.tagsListBox.Location = new System.Drawing.Point(198, 12);
            this.tagsListBox.Name = "tagsListBox";
            this.tagsListBox.Size = new System.Drawing.Size(140, 80);
            this.tagsListBox.TabIndex = 1;
            //
            // nameTextBox
            //
            this.nameTextBox.Location = new System.Drawing.Point(12, 50);
            this.nameTextBox.Name = "nameTextBox";
            this.nameTextBox.Size = new System.Drawing.Size(180, 27);
            this.nameTextBox.TabIndex = 2;
            this.nameTextBox.TextChanged += new System.EventHandler(this.nameTextBox_TextChanged);
            //
            // submitButton
            //
            this.submitButton.Enabled = false;
            this.submitButton.Location = new System.Drawing.Point(12, 86);
            this.submitButton.Name = "submitButton";
            this.submitButton.Size = new System.Drawing.Size(96, 28);
            this.submitButton.TabIndex = 3;
            this.submitButton.Text = "Submit";
            this.submitButton.Click += new System.EventHandler(this.submitButton_Click);
            //
            // resultLabel
            //
            this.resultLabel.Location = new System.Drawing.Point(12, 122);
            this.resultLabel.Name = "resultLabel";
            this.resultLabel.Size = new System.Drawing.Size(326, 20);
            this.resultLabel.TabIndex = 4;
            //
            // MainForm
            //
            this.ClientSize = new System.Drawing.Size(350, 154);
            this.Controls.Add(this.categoryComboBox);
            this.Controls.Add(this.tagsListBox);
            this.Controls.Add(this.nameTextBox);
            this.Controls.Add(this.submitButton);
            this.Controls.Add(this.resultLabel);
            this.Name = "MainForm";
            this.Text = "Items and CanExecute";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.ComboBox categoryComboBox;
        private System.Windows.Forms.ListBox tagsListBox;
        private System.Windows.Forms.TextBox nameTextBox;
        private System.Windows.Forms.Button submitButton;
        private System.Windows.Forms.Label resultLabel;
    }
}
