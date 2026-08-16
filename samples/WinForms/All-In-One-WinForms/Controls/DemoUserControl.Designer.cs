namespace AllInOneWinForms.Controls
{
    partial class DemoUserControl
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
            this.captionLabel = new Label();
            this.counterLabel = new Label();
            this.incrementButton = new Button();
            this.SuspendLayout();
            //
            // captionLabel
            //
            this.captionLabel.Location = new System.Drawing.Point(8, 8);
            this.captionLabel.Name = "captionLabel";
            this.captionLabel.Size = new System.Drawing.Size(180, 20);
            this.captionLabel.TabIndex = 0;
            this.captionLabel.Text = "Demo user control";
            //
            // counterLabel
            //
            this.counterLabel.Location = new System.Drawing.Point(8, 34);
            this.counterLabel.Name = "counterLabel";
            this.counterLabel.Size = new System.Drawing.Size(60, 20);
            this.counterLabel.TabIndex = 1;
            this.counterLabel.Text = "0";
            //
            // incrementButton
            //
            this.incrementButton.Location = new System.Drawing.Point(74, 30);
            this.incrementButton.Name = "incrementButton";
            this.incrementButton.Size = new System.Drawing.Size(96, 26);
            this.incrementButton.TabIndex = 2;
            this.incrementButton.Text = "Increment";
            this.incrementButton.Click += new EventHandler(this.incrementButton_Click);
            //
            // DemoUserControl
            //
            this.Controls.Add(this.incrementButton);
            this.Controls.Add(this.counterLabel);
            this.Controls.Add(this.captionLabel);
            this.Name = "DemoUserControl";
            this.Size = new System.Drawing.Size(220, 70);
            this.ResumeLayout(false);
        }

        private Label captionLabel;
        private Label counterLabel;
        private Button incrementButton;
    }
}
