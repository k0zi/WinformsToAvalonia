namespace VisualInheritanceApp;

partial class SettingsForm
{
    private TextBox nameTextBox;
    private Button resetButton;

    private void InitializeComponent()
    {
        this.nameTextBox = new TextBox();
        this.resetButton = new Button();
        this.SuspendLayout();
        //
        // headerLabel - inherited, re-captioned here
        //
        this.headerLabel.Text = "Settings";
        //
        // okButton - inherited, re-captioned here; its Click is still the base's handler
        //
        this.okButton.Text = "Save";
        //
        // nameTextBox
        //
        this.nameTextBox.Location = new Point(12, 40);
        this.nameTextBox.Name = "nameTextBox";
        this.nameTextBox.Size = new Size(260, 23);
        this.nameTextBox.TabIndex = 0;
        //
        // resetButton
        //
        this.resetButton.Location = new Point(12, 120);
        this.resetButton.Name = "resetButton";
        this.resetButton.Size = new Size(75, 23);
        this.resetButton.TabIndex = 1;
        this.resetButton.Text = "Reset";
        this.resetButton.Click += new EventHandler(this.resetButton_Click);
        //
        // SettingsForm
        //
        this.ClientSize = new Size(284, 155);
        this.Controls.Add(this.nameTextBox);
        this.Controls.Add(this.resetButton);
        this.Name = "SettingsForm";
        this.Text = "Settings";
        this.ResumeLayout(false);
    }
}
