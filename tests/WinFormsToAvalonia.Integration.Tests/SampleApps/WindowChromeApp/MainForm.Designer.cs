namespace WindowChromeApp;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;
    private Button optionsButton;

    private void InitializeComponent()
    {
        this.optionsButton = new Button();
        this.SuspendLayout();
        //
        // optionsButton
        //
        this.optionsButton.Location = new Point(12, 12);
        this.optionsButton.Name = "optionsButton";
        this.optionsButton.Size = new Size(100, 23);
        this.optionsButton.TabIndex = 0;
        this.optionsButton.Text = "Options...";
        this.optionsButton.Click += new EventHandler(this.optionsButton_Click);
        //
        // MainForm - centred, with a floor below which it may not shrink.
        //
        this.AutoScaleDimensions = new SizeF(7F, 15F);
        this.AutoScaleMode = AutoScaleMode.Font;
        this.ClientSize = new Size(400, 300);
        this.Controls.Add(this.optionsButton);
        this.MinimumSize = new Size(300, 200);
        this.Name = "MainForm";
        this.StartPosition = FormStartPosition.CenterScreen;
        this.Text = "Window chrome";
        this.ResumeLayout(false);
    }
}
