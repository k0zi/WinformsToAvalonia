namespace HandlerMigrationApp
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
            this.okButton = new System.Windows.Forms.Button();
            this.aboutButton = new System.Windows.Forms.Button();
            this.resetButton = new System.Windows.Forms.Button();
            this.saveButton = new System.Windows.Forms.Button();
            this.nameTextBox = new System.Windows.Forms.TextBox();
            this.statusLabel = new System.Windows.Forms.Label();
            this.counterLabel = new System.Windows.Forms.Label();
            this.validateButton = new System.Windows.Forms.Button();
            this.countButton = new System.Windows.Forms.Button();
            this.canvas = new System.Windows.Forms.Panel();
            this.errorProvider1 = new System.Windows.Forms.ErrorProvider();
            this.flagButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // flagButton
            //
            this.flagButton.Location = new System.Drawing.Point(320, 12);
            this.flagButton.Name = "flagButton";
            this.flagButton.Size = new System.Drawing.Size(90, 28);
            this.flagButton.Text = "Flag";
            this.flagButton.Click += new System.EventHandler(this.flagButton_Click);
            //
            // okButton
            //
            this.okButton.Location = new System.Drawing.Point(12, 12);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(90, 28);
            this.okButton.TabIndex = 0;
            this.okButton.Text = "OK";
            this.okButton.Click += new System.EventHandler(this.okButton_Click);
            //
            // aboutButton
            //
            this.aboutButton.Location = new System.Drawing.Point(108, 12);
            this.aboutButton.Name = "aboutButton";
            this.aboutButton.Size = new System.Drawing.Size(90, 28);
            this.aboutButton.TabIndex = 1;
            this.aboutButton.Text = "About";
            this.aboutButton.Click += new System.EventHandler(this.aboutButton_Click);
            //
            // resetButton
            //
            this.resetButton.Location = new System.Drawing.Point(204, 12);
            this.resetButton.Name = "resetButton";
            this.resetButton.Size = new System.Drawing.Size(90, 28);
            this.resetButton.TabIndex = 2;
            this.resetButton.Text = "Increment";
            this.resetButton.Click += new System.EventHandler(this.resetButton_Click);
            //
            // saveButton
            //
            this.saveButton.Location = new System.Drawing.Point(300, 12);
            this.saveButton.Name = "saveButton";
            this.saveButton.Size = new System.Drawing.Size(90, 28);
            this.saveButton.TabIndex = 3;
            this.saveButton.Text = "Save";
            this.saveButton.Click += new System.EventHandler(this.saveButton_Click);
            //
            // validateButton
            //
            this.validateButton.Location = new System.Drawing.Point(300, 50);
            this.validateButton.Name = "validateButton";
            this.validateButton.Size = new System.Drawing.Size(90, 28);
            this.validateButton.TabIndex = 8;
            this.validateButton.Text = "Validate";
            this.validateButton.Click += new System.EventHandler(this.validateButton_Click);
            //
            // countButton
            //
            this.countButton.Location = new System.Drawing.Point(300, 84);
            this.countButton.Name = "countButton";
            this.countButton.Size = new System.Drawing.Size(90, 28);
            this.countButton.TabIndex = 9;
            this.countButton.Text = "Count";
            this.countButton.Click += new System.EventHandler(this.countButton_Click);
            //
            // nameTextBox
            //
            this.nameTextBox.Location = new System.Drawing.Point(12, 50);
            this.nameTextBox.Name = "nameTextBox";
            this.nameTextBox.Size = new System.Drawing.Size(200, 27);
            this.nameTextBox.TabIndex = 4;
            //
            // statusLabel
            //
            this.statusLabel.Location = new System.Drawing.Point(12, 86);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(200, 20);
            this.statusLabel.TabIndex = 5;
            this.statusLabel.Text = "Ready";
            //
            // counterLabel
            //
            this.counterLabel.Location = new System.Drawing.Point(228, 86);
            this.counterLabel.Name = "counterLabel";
            this.counterLabel.Size = new System.Drawing.Size(60, 20);
            this.counterLabel.TabIndex = 6;
            this.counterLabel.Text = "0";
            //
            // canvas
            //
            this.canvas.Location = new System.Drawing.Point(12, 116);
            this.canvas.Name = "canvas";
            this.canvas.Size = new System.Drawing.Size(378, 80);
            this.canvas.TabIndex = 7;
            this.canvas.MouseDown += new System.Windows.Forms.MouseEventHandler(this.canvas_MouseDown);
            this.okButton.MouseDown += new System.Windows.Forms.MouseEventHandler(this.sharedMouseDown);
            this.saveButton.MouseDown += new System.Windows.Forms.MouseEventHandler(this.sharedMouseDown);
            //
            // MainForm
            //
            this.ClientSize = new System.Drawing.Size(404, 210);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.aboutButton);
            this.Controls.Add(this.resetButton);
            this.Controls.Add(this.saveButton);
            this.Controls.Add(this.nameTextBox);
            this.Controls.Add(this.statusLabel);
            this.Controls.Add(this.counterLabel);
            this.Controls.Add(this.validateButton);
            this.Controls.Add(this.countButton);
            this.Controls.Add(this.canvas);
            this.Controls.Add(this.flagButton);
            this.Name = "MainForm";
            this.Text = "Handler Migration Demo";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.ErrorProvider errorProvider1;
        private System.Windows.Forms.Button flagButton;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button aboutButton;
        private System.Windows.Forms.Button resetButton;
        private System.Windows.Forms.Button saveButton;
        private System.Windows.Forms.TextBox nameTextBox;
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.Label counterLabel;
        private System.Windows.Forms.Button validateButton;
        private System.Windows.Forms.Button countButton;
        private System.Windows.Forms.Panel canvas;
    }
}
