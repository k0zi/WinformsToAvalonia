namespace EdgeCasesApp
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
            this.countLabel = new System.Windows.Forms.Label();
            this.notesLabel = new System.Windows.Forms.Label();
            this.dashLabel = new System.Windows.Forms.Label();
            this.pathsList = new System.Windows.Forms.CheckedListBox();
            this.scoreButton = new System.Windows.Forms.Button();
            this.statusButton = new System.Windows.Forms.Button();
            this.trayIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.backupIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.SuspendLayout();
            //
            // countLabel - a caption that starts with `{` is a markup extension to the XAML compiler
            //
            this.countLabel.Location = new System.Drawing.Point(12, 12);
            this.countLabel.Name = "countLabel";
            this.countLabel.Size = new System.Drawing.Size(200, 20);
            this.countLabel.Text = "{0} files selected";
            //
            // notesLabel - a line break an attribute value would otherwise flatten into a space
            //
            this.notesLabel.Location = new System.Drawing.Point(12, 36);
            this.notesLabel.Name = "notesLabel";
            this.notesLabel.Size = new System.Drawing.Size(200, 40);
            this.notesLabel.Text = "Line one\r\nLine two\tend";
            //
            // dashLabel - not a literal, so it is reported in an AXAML comment, dashes and all
            //
            this.dashLabel.Location = new System.Drawing.Point(12, 80);
            this.dashLabel.Name = "dashLabel";
            this.dashLabel.Size = new System.Drawing.Size(200, 20);
            this.dashLabel.Text = "Status ---- " + System.Environment.MachineName;
            //
            // pathsList - items written into a C# string literal in the ViewModel
            //
            this.pathsList.Items.AddRange(new object[] {
            "C:\\Temp",
            "Say \"hi\"",
            "{braces}"});
            this.pathsList.Location = new System.Drawing.Point(12, 104);
            this.pathsList.Name = "pathsList";
            this.pathsList.Size = new System.Drawing.Size(200, 64);
            //
            // scoreButton
            //
            this.scoreButton.Location = new System.Drawing.Point(12, 176);
            this.scoreButton.Name = "scoreButton";
            this.scoreButton.Size = new System.Drawing.Size(96, 24);
            this.scoreButton.Text = "Score";
            this.scoreButton.Click += new System.EventHandler(this.scoreButton_Click);
            //
            // statusButton
            //
            this.statusButton.Location = new System.Drawing.Point(116, 176);
            this.statusButton.Name = "statusButton";
            this.statusButton.Size = new System.Drawing.Size(96, 24);
            this.statusButton.Text = "Status";
            this.statusButton.Click += new System.EventHandler(this.statusButton_Click);
            //
            // trayIcon - its tooltip lands in App.axaml, which is parsed before any window exists
            //
            this.trayIcon.Icon = new System.Drawing.Icon("app.ico");
            this.trayIcon.Text = "{R&D} <Monitor> \"live\"";
            this.trayIcon.Visible = true;
            //
            // backupIcon - no resolvable icon, so it is written commented out, tooltip included
            //
            this.backupIcon.Icon = System.Drawing.SystemIcons.Application;
            this.backupIcon.Text = "Backup -- idle";
            //
            // MainForm
            //
            this.ClientSize = new System.Drawing.Size(240, 220);
            this.Controls.Add(this.countLabel);
            this.Controls.Add(this.notesLabel);
            this.Controls.Add(this.dashLabel);
            this.Controls.Add(this.pathsList);
            this.Controls.Add(this.scoreButton);
            this.Controls.Add(this.statusButton);
            this.Name = "MainForm";
            this.Text = "{Edge} cases";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.Label countLabel;
        private System.Windows.Forms.Label notesLabel;
        private System.Windows.Forms.Label dashLabel;
        private System.Windows.Forms.CheckedListBox pathsList;
        private System.Windows.Forms.Button scoreButton;
        private System.Windows.Forms.Button statusButton;
        private System.Windows.Forms.NotifyIcon trayIcon;
        private System.Windows.Forms.NotifyIcon backupIcon;
    }
}
