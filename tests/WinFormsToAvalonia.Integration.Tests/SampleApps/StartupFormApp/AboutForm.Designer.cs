namespace StartupFormApp;

partial class AboutForm
{
    private System.ComponentModel.IContainer components = null;
    private Label titleLabel;

    private void InitializeComponent()
    {
        this.titleLabel = new Label();
        this.SuspendLayout();
        //
        // titleLabel
        //
        this.titleLabel.Location = new Point(12, 12);
        this.titleLabel.Name = "titleLabel";
        this.titleLabel.Size = new Size(200, 23);
        this.titleLabel.Text = "AboutForm";
        //
        // AboutForm
        //
        this.AutoScaleDimensions = new SizeF(7F, 15F);
        this.AutoScaleMode = AutoScaleMode.Font;
        this.ClientSize = new Size(320, 120);
        this.Controls.Add(this.titleLabel);
        this.Name = "AboutForm";
        this.Text = "AboutForm";
        this.ResumeLayout(false);
    }
}
