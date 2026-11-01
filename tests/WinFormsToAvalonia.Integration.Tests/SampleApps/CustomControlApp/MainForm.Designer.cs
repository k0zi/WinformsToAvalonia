using CustomControlApp.Controls;

namespace CustomControlApp;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;
    private RoundButton roundButton1;
    private CurrencyTextBox currencyBox1;
    private Label statusLabel;

    private void InitializeComponent()
    {
        this.roundButton1 = new RoundButton();
        this.currencyBox1 = new CurrencyTextBox();
        this.statusLabel = new Label();
        this.SuspendLayout();
        this.roundButton1.CornerRadius = 12;
        this.roundButton1.Location = new Point(12, 12);
        this.roundButton1.Name = "roundButton1";
        this.roundButton1.Size = new Size(90, 30);
        this.roundButton1.Text = "&Round";
        this.roundButton1.Click += new EventHandler(this.roundButton1_Click);
        this.currencyBox1.Location = new Point(108, 15);
        this.currencyBox1.Name = "currencyBox1";
        this.currencyBox1.Size = new Size(120, 23);
        this.currencyBox1.TextAlign = HorizontalAlignment.Right;
        this.currencyBox1.TextChanged += new EventHandler(this.currencyBox1_TextChanged);
        this.statusLabel.Location = new Point(12, 50);
        this.statusLabel.Name = "statusLabel";
        this.statusLabel.Size = new Size(216, 20);
        this.statusLabel.Text = "Ready";
        this.ClientSize = new Size(240, 82);
        this.Controls.Add(this.roundButton1);
        this.Controls.Add(this.currencyBox1);
        this.Controls.Add(this.statusLabel);
        this.Name = "MainForm";
        this.Text = "Custom controls";
        this.ResumeLayout(false);
    }
}
