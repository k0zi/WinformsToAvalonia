namespace HelperMethodApp
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
            this.startButton = new System.Windows.Forms.Button();
            this.warnButton = new System.Windows.Forms.Button();
            this.saveButton = new System.Windows.Forms.Button();
            this.statusLabel = new System.Windows.Forms.Label();
            this.itemsTreeView = new System.Windows.Forms.TreeView();
            this.tagButton = new System.Windows.Forms.Button();
            this.tagLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            //
            // startButton
            //
            this.startButton.Location = new System.Drawing.Point(12, 12);
            this.startButton.Name = "startButton";
            this.startButton.Size = new System.Drawing.Size(90, 28);
            this.startButton.TabIndex = 0;
            this.startButton.Text = "Start";
            this.startButton.Click += new System.EventHandler(this.startButton_Click);
            //
            // warnButton
            //
            this.warnButton.Location = new System.Drawing.Point(108, 12);
            this.warnButton.Name = "warnButton";
            this.warnButton.Size = new System.Drawing.Size(90, 28);
            this.warnButton.TabIndex = 1;
            this.warnButton.Text = "Warn";
            this.warnButton.Click += new System.EventHandler(this.warnButton_Click);
            //
            // saveButton
            //
            this.saveButton.Location = new System.Drawing.Point(204, 12);
            this.saveButton.Name = "saveButton";
            this.saveButton.Size = new System.Drawing.Size(90, 28);
            this.saveButton.TabIndex = 2;
            this.saveButton.Text = "Save";
            this.saveButton.Click += new System.EventHandler(this.saveButton_Click);
            //
            // statusLabel
            //
            this.statusLabel.Location = new System.Drawing.Point(12, 48);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(282, 23);
            this.statusLabel.TabIndex = 3;
            this.statusLabel.Text = "Ready";
            //
            // itemsTreeView
            //
            this.itemsTreeView.Location = new System.Drawing.Point(12, 80);
            this.itemsTreeView.Name = "itemsTreeView";
            this.itemsTreeView.Size = new System.Drawing.Size(282, 120);
            this.itemsTreeView.TabIndex = 4;
            //
            // MainForm
            //
            this.ClientSize = new System.Drawing.Size(306, 212);
            this.Controls.Add(this.startButton);
            this.Controls.Add(this.warnButton);
            this.Controls.Add(this.saveButton);
            this.Controls.Add(this.statusLabel);
            this.tagButton.Location = new System.Drawing.Point(300, 12);
            this.tagButton.Name = "tagButton";
            this.tagButton.Size = new System.Drawing.Size(90, 28);
            this.tagButton.Text = "Tag";
            this.tagButton.Click += new System.EventHandler(this.tagButton_Click);
            this.tagLabel.Location = new System.Drawing.Point(300, 48);
            this.tagLabel.Name = "tagLabel";
            this.tagLabel.Size = new System.Drawing.Size(120, 23);
            this.tagLabel.Text = "untagged";
            this.Controls.Add(this.itemsTreeView);
            this.Controls.Add(this.tagButton);
            this.Controls.Add(this.tagLabel);
            this.Name = "MainForm";
            this.Text = "Helper methods";
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.Button startButton;
        private System.Windows.Forms.Button warnButton;
        private System.Windows.Forms.Button saveButton;
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.TreeView itemsTreeView;
        private System.Windows.Forms.Button tagButton;
        private System.Windows.Forms.Label tagLabel;
    }
}
