namespace VisualStyleApp
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.headerLabel = new Label();
            this.underlinedLabel = new Label();
            this.accentButton = new Button();
            this.tintedPanel = new Panel();
            this.styledTextBox = new TextBox();
            this.styledCheckBox = new CheckBox();
            this.styledGroupBox = new GroupBox();
            this.logoPictureBox = new PictureBox();
            this.styledTabControl = new TabControl();
            this.firstTabPage = new TabPage();
            this.tintedPanel.SuspendLayout();
            this.styledGroupBox.SuspendLayout();
            this.styledTabControl.SuspendLayout();
            this.SuspendLayout();
            //
            // headerLabel - a TextBlock: the full styling surface, including bold italic.
            //
            this.headerLabel.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic);
            this.headerLabel.ForeColor = System.Drawing.Color.FromArgb(30, 144, 255);
            this.headerLabel.Location = new System.Drawing.Point(12, 9);
            this.headerLabel.Name = "headerLabel";
            this.headerLabel.Size = new System.Drawing.Size(300, 30);
            this.headerLabel.TabIndex = 0;
            this.headerLabel.Text = "Visual style demo";
            //
            // underlinedLabel - TextDecorations, which only text-hosting elements understand.
            //
            this.underlinedLabel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Underline);
            this.underlinedLabel.ForeColor = System.Drawing.SystemColors.GrayText;
            this.underlinedLabel.Location = new System.Drawing.Point(12, 45);
            this.underlinedLabel.Name = "underlinedLabel";
            this.underlinedLabel.Size = new System.Drawing.Size(200, 20);
            this.underlinedLabel.TabIndex = 1;
            this.underlinedLabel.Text = "Underlined caption";
            //
            // accentButton - a TemplatedControl: background, foreground, font and padding.
            //
            this.accentButton.BackColor = System.Drawing.Color.FromArgb(0, 120, 215);
            this.accentButton.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.accentButton.ForeColor = System.Drawing.Color.White;
            this.accentButton.Location = new System.Drawing.Point(12, 75);
            this.accentButton.Name = "accentButton";
            this.accentButton.Padding = new System.Windows.Forms.Padding(6, 2, 6, 2);
            this.accentButton.Size = new System.Drawing.Size(120, 32);
            this.accentButton.TabIndex = 2;
            this.accentButton.Text = "Accent";
            //
            // tintedPanel - a Canvas: Background only, ForeColor/Font must be dropped.
            //
            this.tintedPanel.BackColor = System.Drawing.Color.LightYellow;
            this.tintedPanel.Controls.Add(this.styledTextBox);
            this.tintedPanel.Font = new System.Drawing.Font("Consolas", 11F);
            this.tintedPanel.ForeColor = System.Drawing.Color.DarkRed;
            this.tintedPanel.Location = new System.Drawing.Point(12, 115);
            this.tintedPanel.Name = "tintedPanel";
            this.tintedPanel.Size = new System.Drawing.Size(300, 60);
            this.tintedPanel.TabIndex = 3;
            //
            // styledTextBox
            //
            this.styledTextBox.BackColor = System.Drawing.SystemColors.Info;
            this.styledTextBox.Font = new System.Drawing.Font("Consolas", 11F);
            this.styledTextBox.Location = new System.Drawing.Point(10, 15);
            this.styledTextBox.Name = "styledTextBox";
            this.styledTextBox.Size = new System.Drawing.Size(280, 25);
            this.styledTextBox.TabIndex = 0;
            this.styledTextBox.Text = "monospaced";
            //
            // styledCheckBox
            //
            this.styledCheckBox.Checked = true;
            this.styledCheckBox.ForeColor = System.Drawing.SystemColors.ControlText;
            this.styledCheckBox.Location = new System.Drawing.Point(12, 185);
            this.styledCheckBox.Name = "styledCheckBox";
            this.styledCheckBox.Size = new System.Drawing.Size(150, 24);
            this.styledCheckBox.TabIndex = 4;
            this.styledCheckBox.Text = "Enabled";
            //
            // styledGroupBox - a bundled fallback control: styling must be skipped entirely,
            // since the template does not necessarily expose those properties.
            //
            this.styledGroupBox.BackColor = System.Drawing.Color.WhiteSmoke;
            this.styledGroupBox.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.styledGroupBox.Location = new System.Drawing.Point(12, 215);
            this.styledGroupBox.Name = "styledGroupBox";
            this.styledGroupBox.Size = new System.Drawing.Size(300, 70);
            this.styledGroupBox.TabIndex = 5;
            this.styledGroupBox.Text = "Grouped";
            //
            // logoPictureBox - an Image: no styling surface at all.
            //
            this.logoPictureBox.BackColor = System.Drawing.Color.Black;
            this.logoPictureBox.Image = ((System.Drawing.Image)(resources.GetObject("logoPictureBox.Image")));
            this.logoPictureBox.Location = new System.Drawing.Point(330, 9);
            this.logoPictureBox.Name = "logoPictureBox";
            this.logoPictureBox.Size = new System.Drawing.Size(64, 64);
            this.logoPictureBox.TabIndex = 6;
            //
            // styledTabControl
            //
            this.styledTabControl.Controls.Add(this.firstTabPage);
            this.styledTabControl.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.styledTabControl.Location = new System.Drawing.Point(330, 85);
            this.styledTabControl.Name = "styledTabControl";
            this.styledTabControl.Size = new System.Drawing.Size(200, 120);
            this.styledTabControl.TabIndex = 7;
            //
            // firstTabPage
            //
            this.firstTabPage.BackColor = System.Drawing.Color.AliceBlue;
            this.firstTabPage.Location = new System.Drawing.Point(4, 24);
            this.firstTabPage.Name = "firstTabPage";
            this.firstTabPage.Size = new System.Drawing.Size(192, 92);
            this.firstTabPage.TabIndex = 0;
            this.firstTabPage.Text = "First";
            //
            // MainForm - form-level Font inherits down to every child that never overrode it.
            //
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(560, 300);
            this.Controls.Add(this.headerLabel);
            this.Controls.Add(this.underlinedLabel);
            this.Controls.Add(this.accentButton);
            this.Controls.Add(this.tintedPanel);
            this.Controls.Add(this.styledCheckBox);
            this.Controls.Add(this.styledGroupBox);
            this.Controls.Add(this.logoPictureBox);
            this.Controls.Add(this.styledTabControl);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.ForeColor = System.Drawing.SystemColors.ControlText;
            this.Name = "MainForm";
            this.Text = "Visual Style Demo";
            this.tintedPanel.ResumeLayout(false);
            this.styledGroupBox.ResumeLayout(false);
            this.styledTabControl.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        private Label headerLabel;
        private Label underlinedLabel;
        private Button accentButton;
        private Panel tintedPanel;
        private TextBox styledTextBox;
        private CheckBox styledCheckBox;
        private GroupBox styledGroupBox;
        private PictureBox logoPictureBox;
        private TabControl styledTabControl;
        private TabPage firstTabPage;
    }
}
