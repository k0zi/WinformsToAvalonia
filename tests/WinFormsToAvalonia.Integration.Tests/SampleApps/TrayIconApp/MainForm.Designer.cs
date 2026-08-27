namespace TrayIconApp
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
            this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
            this.notifyIcon2 = new System.Windows.Forms.NotifyIcon(this.components);
            this.hideButton = new System.Windows.Forms.Button();
            this.otherButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // notifyIcon1
            //
            this.notifyIcon1.Icon = new System.Drawing.Icon("app.ico");
            this.notifyIcon1.Text = "Tray demo";
            this.notifyIcon1.Visible = true;
            //
            // notifyIcon2 - no icon the conversion can resolve, so no TrayIcon is emitted for it.
            //
            this.notifyIcon2.Text = "No icon";
            //
            // hideButton
            //
            this.hideButton.Location = new System.Drawing.Point(12, 12);
            this.hideButton.Name = "hideButton";
            this.hideButton.Size = new System.Drawing.Size(120, 28);
            this.hideButton.Text = "Hide the icon";
            this.hideButton.Click += new System.EventHandler(this.hideButton_Click);
            //
            // otherButton
            //
            this.otherButton.Location = new System.Drawing.Point(12, 46);
            this.otherButton.Name = "otherButton";
            this.otherButton.Size = new System.Drawing.Size(120, 28);
            this.otherButton.Text = "Hide the other";
            this.otherButton.Click += new System.EventHandler(this.otherButton_Click);
            //
            // MainForm
            //
            this.ClientSize = new System.Drawing.Size(240, 100);
            this.Controls.Add(this.hideButton);
            this.Controls.Add(this.otherButton);
            this.Name = "MainForm";
            this.Text = "Tray icon";
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.NotifyIcon notifyIcon1;
        private System.Windows.Forms.NotifyIcon notifyIcon2;
        private System.Windows.Forms.Button hideButton;
        private System.Windows.Forms.Button otherButton;
    }
}
