namespace NonVisualComponentsApp
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
            this.okButton = new Button();
            this.refreshTimer = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            //
            // okButton
            //
            this.okButton.Location = new System.Drawing.Point(12, 12);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(75, 23);
            this.okButton.TabIndex = 0;
            this.okButton.Text = "OK";
            //
            // refreshTimer
            //
            this.refreshTimer.Interval = 1000;
            this.refreshTimer.Tick += new EventHandler(this.refreshTimer_Tick);
            //
            // MainForm
            //
            this.ClientSize = new System.Drawing.Size(320, 240);
            this.Controls.Add(this.okButton);
            this.Name = "MainForm";
            this.Text = "Main Form";
            this.ResumeLayout(false);
        }

        private Button okButton;
        private System.Windows.Forms.Timer refreshTimer;
    }
}
