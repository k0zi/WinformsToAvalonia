namespace UserControlApp.Controls
{
    partial class CounterControl
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
            this.counterLabel = new System.Windows.Forms.Label();
            this.incrementButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // counterLabel
            //
            this.counterLabel.Location = new System.Drawing.Point(8, 8);
            this.counterLabel.Name = "counterLabel";
            this.counterLabel.Size = new System.Drawing.Size(60, 20);
            this.counterLabel.Text = "0";
            //
            // incrementButton
            //
            this.incrementButton.Location = new System.Drawing.Point(74, 4);
            this.incrementButton.Name = "incrementButton";
            this.incrementButton.Size = new System.Drawing.Size(96, 26);
            this.incrementButton.Text = "Increment";
            this.incrementButton.Click += new System.EventHandler(this.incrementButton_Click);
            //
            // CounterControl
            //
            this.Controls.Add(this.incrementButton);
            this.Controls.Add(this.counterLabel);
            this.Name = "CounterControl";
            this.Size = new System.Drawing.Size(180, 36);
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.Label counterLabel;
        private System.Windows.Forms.Button incrementButton;
    }
}
