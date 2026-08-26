namespace LocalizedApp
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

        // Exactly the shape the WinForms designer emits for a Localizable=true form: every
        // property - Text, Location, Size, Font - comes from the .resx, not from C#.
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.greetingLabel = new System.Windows.Forms.Label();
            this.nameTextBox = new System.Windows.Forms.TextBox();
            this.submitButton = new System.Windows.Forms.Button();
            this.logoPictureBox = new System.Windows.Forms.PictureBox();
            this.SuspendLayout();
            //
            // greetingLabel
            //
            resources.ApplyResources(this.greetingLabel, "greetingLabel");
            this.greetingLabel.Name = "greetingLabel";
            //
            // nameTextBox
            //
            resources.ApplyResources(this.nameTextBox, "nameTextBox");
            this.nameTextBox.Name = "nameTextBox";
            //
            // submitButton
            //
            resources.ApplyResources(this.submitButton, "submitButton");
            this.submitButton.Name = "submitButton";
            this.submitButton.UseVisualStyleBackColor = true;
            //
            // logoPictureBox
            //
            resources.ApplyResources(this.logoPictureBox, "logoPictureBox");
            this.logoPictureBox.Name = "logoPictureBox";
            this.logoPictureBox.TabStop = false;
            //
            // MainForm
            //
            resources.ApplyResources(this, "$this");
            this.Controls.Add(this.greetingLabel);
            this.Controls.Add(this.nameTextBox);
            this.Controls.Add(this.submitButton);
            this.Controls.Add(this.logoPictureBox);
            this.Name = "MainForm";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Label greetingLabel;
        private System.Windows.Forms.TextBox nameTextBox;
        private System.Windows.Forms.Button submitButton;
        private System.Windows.Forms.PictureBox logoPictureBox;
    }
}
