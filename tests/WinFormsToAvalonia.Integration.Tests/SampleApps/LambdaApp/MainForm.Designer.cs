namespace LambdaApp;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;
    private Button goButton;
    private Button askButton;
    private TextBox nameBox;
    private Label statusLabel;

    private void InitializeComponent()
    {
        this.goButton = new Button();
        this.askButton = new Button();
        this.nameBox = new TextBox();
        this.statusLabel = new Label();
        this.SuspendLayout();
        this.goButton.Location = new Point(12, 12);
        this.goButton.Name = "goButton";
        this.goButton.Size = new Size(75, 23);
        this.goButton.Text = "Go";
        // A block body.
        this.goButton.Click += (s, e) =>
        {
            this.statusLabel.Text = "Going";
            this.goButton.Enabled = false;
        };
        this.askButton.Location = new Point(93, 12);
        this.askButton.Name = "askButton";
        this.askButton.Size = new Size(75, 23);
        this.askButton.Text = "Ask";
        // An async lambda, with discards.
        this.askButton.Click += async (_, _) => MessageBox.Show("Sure?", "Lambda");
        this.nameBox.Location = new Point(174, 12);
        this.nameBox.Name = "nameBox";
        this.nameBox.Size = new Size(100, 23);
        // An expression body.
        this.nameBox.TextChanged += (sender, args) => this.statusLabel.Text = this.nameBox.Text;
        this.statusLabel.Location = new Point(12, 44);
        this.statusLabel.Name = "statusLabel";
        this.statusLabel.Size = new Size(262, 20);
        this.statusLabel.Text = "Ready";
        this.ClientSize = new Size(286, 76);
        this.Controls.Add(this.goButton);
        this.Controls.Add(this.askButton);
        this.Controls.Add(this.nameBox);
        this.Controls.Add(this.statusLabel);
        this.Name = "MainForm";
        this.Text = "Lambdas";
        // An anonymous method on the Form itself.
        this.Load += delegate { this.statusLabel.Text = "Loaded"; };
        this.ResumeLayout(false);
    }
}
