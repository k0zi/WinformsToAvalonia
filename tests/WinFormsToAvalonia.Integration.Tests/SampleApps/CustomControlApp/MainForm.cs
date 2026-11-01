namespace CustomControlApp;

public partial class MainForm : Form
{
    public MainForm()
    {
        InitializeComponent();
    }

    private void roundButton1_Click(object? sender, EventArgs e)
    {
        this.statusLabel.Text = "Round click";
    }

    private void currencyBox1_TextChanged(object? sender, EventArgs e)
    {
        this.statusLabel.Text = this.currencyBox1.Text;
    }
}
