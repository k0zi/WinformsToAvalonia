namespace ToolTipApp
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
            this.toolTip1 = new ToolTip(this.components);
            this.okButton = new Button();
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
            // toolTip1
            //
            this.toolTip1.SetToolTip(this.okButton, "Click to confirm");
            //
            // MainForm
            //
            this.ClientSize = new System.Drawing.Size(240, 140);
            this.Controls.Add(this.okButton);
            this.Name = "MainForm";
            this.Text = "ToolTip Demo";
            this.ResumeLayout(false);
        }

        private ToolTip toolTip1;
        private Button okButton;
    }
}
