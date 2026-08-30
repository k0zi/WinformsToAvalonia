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
            this.trayMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.openMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.optionsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.settingsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.traySeparator = new System.Windows.Forms.ToolStripSeparator();
            this.exitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
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
            this.notifyIcon1.ContextMenuStrip = this.trayMenu;
            this.notifyIcon1.Click += new System.EventHandler(this.notifyIcon1_Click);
            this.notifyIcon1.DoubleClick += new System.EventHandler(this.notifyIcon1_DoubleClick);
            //
            // notifyIcon2 - no icon the conversion can resolve, so no TrayIcon is emitted for it.
            //
            this.notifyIcon2.Text = "No icon";
            this.notifyIcon2.Click += new System.EventHandler(this.notifyIcon2_Click);
            //
            // trayMenu - a NotifyIcon's context menu becomes Avalonia's TrayIcon.Menu, which is a
            // native menu: a nested item, a separator and a disabled item are all it can carry.
            //
            this.trayMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openMenuItem,
            this.optionsMenuItem,
            this.traySeparator,
            this.exitMenuItem});
            this.trayMenu.Name = "trayMenu";
            //
            // openMenuItem
            //
            this.openMenuItem.Name = "openMenuItem";
            this.openMenuItem.Text = "&Open";
            this.openMenuItem.Click += new System.EventHandler(this.openMenuItem_Click);
            //
            // optionsMenuItem
            //
            this.optionsMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.settingsMenuItem});
            this.optionsMenuItem.Name = "optionsMenuItem";
            this.optionsMenuItem.Text = "Options";
            //
            // settingsMenuItem
            //
            this.settingsMenuItem.Name = "settingsMenuItem";
            this.settingsMenuItem.Text = "Settings...";
            this.settingsMenuItem.Enabled = false;
            //
            // traySeparator
            //
            this.traySeparator.Name = "traySeparator";
            //
            // exitMenuItem
            //
            this.exitMenuItem.Name = "exitMenuItem";
            this.exitMenuItem.Text = "Exit";
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
        private System.Windows.Forms.ContextMenuStrip trayMenu;
        private System.Windows.Forms.ToolStripMenuItem openMenuItem;
        private System.Windows.Forms.ToolStripMenuItem optionsMenuItem;
        private System.Windows.Forms.ToolStripMenuItem settingsMenuItem;
        private System.Windows.Forms.ToolStripSeparator traySeparator;
        private System.Windows.Forms.ToolStripMenuItem exitMenuItem;
        private System.Windows.Forms.NotifyIcon notifyIcon2;
        private System.Windows.Forms.Button hideButton;
        private System.Windows.Forms.Button otherButton;
    }
}
