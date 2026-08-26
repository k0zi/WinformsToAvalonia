namespace SenderAndDragApp
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
            this.renameButton = new System.Windows.Forms.Button();
            this.sharedOneButton = new System.Windows.Forms.Button();
            this.sharedTwoButton = new System.Windows.Forms.Button();
            this.copyButton = new System.Windows.Forms.Button();
            this.dropPanel = new System.Windows.Forms.Panel();
            this.statusLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            //
            // renameButton
            //
            this.renameButton.Location = new System.Drawing.Point(12, 12);
            this.renameButton.Name = "renameButton";
            this.renameButton.Size = new System.Drawing.Size(100, 28);
            this.renameButton.TabIndex = 0;
            this.renameButton.Text = "Rename me";
            this.renameButton.Click += new System.EventHandler(this.renameButton_Click);
            //
            // sharedOneButton
            //
            this.sharedOneButton.Location = new System.Drawing.Point(118, 12);
            this.sharedOneButton.Name = "sharedOneButton";
            this.sharedOneButton.Size = new System.Drawing.Size(100, 28);
            this.sharedOneButton.TabIndex = 1;
            this.sharedOneButton.Text = "Shared one";
            this.sharedOneButton.Click += new System.EventHandler(this.sharedClick);
            //
            // sharedTwoButton
            //
            this.sharedTwoButton.Location = new System.Drawing.Point(224, 12);
            this.sharedTwoButton.Name = "sharedTwoButton";
            this.sharedTwoButton.Size = new System.Drawing.Size(100, 28);
            this.sharedTwoButton.TabIndex = 2;
            this.sharedTwoButton.Text = "Shared two";
            this.sharedTwoButton.Click += new System.EventHandler(this.sharedClick);
            //
            // copyButton
            //
            this.copyButton.Location = new System.Drawing.Point(12, 46);
            this.copyButton.Name = "copyButton";
            this.copyButton.Size = new System.Drawing.Size(100, 28);
            this.copyButton.TabIndex = 3;
            this.copyButton.Text = "Copy";
            this.copyButton.Click += new System.EventHandler(this.copyButton_Click);
            //
            // dropPanel
            //
            this.dropPanel.AllowDrop = true;
            this.dropPanel.Location = new System.Drawing.Point(12, 80);
            this.dropPanel.Name = "dropPanel";
            this.dropPanel.Size = new System.Drawing.Size(312, 90);
            this.dropPanel.TabIndex = 4;
            this.dropPanel.DragEnter += new System.Windows.Forms.DragEventHandler(this.dropPanel_DragEnter);
            this.dropPanel.DragDrop += new System.Windows.Forms.DragEventHandler(this.dropPanel_DragDrop);
            //
            // statusLabel
            //
            this.statusLabel.Location = new System.Drawing.Point(12, 178);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(312, 23);
            this.statusLabel.TabIndex = 5;
            this.statusLabel.Text = "Ready";
            //
            // MainForm
            //
            this.ClientSize = new System.Drawing.Size(336, 212);
            this.Controls.Add(this.renameButton);
            this.Controls.Add(this.sharedOneButton);
            this.Controls.Add(this.sharedTwoButton);
            this.Controls.Add(this.copyButton);
            this.Controls.Add(this.dropPanel);
            this.Controls.Add(this.statusLabel);
            this.Name = "MainForm";
            this.Text = "Sender and drag";
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.Button renameButton;
        private System.Windows.Forms.Button sharedOneButton;
        private System.Windows.Forms.Button sharedTwoButton;
        private System.Windows.Forms.Button copyButton;
        private System.Windows.Forms.Panel dropPanel;
        private System.Windows.Forms.Label statusLabel;
    }
}
