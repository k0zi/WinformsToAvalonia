namespace StatementsApp;

public partial class MainForm : Form
{
    public MainForm()
    {
        InitializeComponent();
    }

    // try/catch with a typed, named exception, and a finally.
    private void parseButton_Click(object? sender, EventArgs e)
    {
        try
        {
            var amount = int.Parse(this.amountBox.Text);
            this.resultLabel.Text = $"Doubled: {amount * 2}";
        }
        catch (FormatException ex) when (ex.Message != "")
        {
            this.resultLabel.Text = "Not a number: " + ex.Message;
        }
        finally
        {
            this.parseButton.Enabled = true;
        }
    }

    // An IO exception caught unqualified - the generated View has to import System.IO for it.
    private void loadButton_Click(object? sender, EventArgs e)
    {
        try
        {
            this.amountBox.Text = File.ReadAllText("amount.txt");
        }
        catch (IOException)
        {
            this.resultLabel.Text = "Could not read amount.txt";
        }
    }

    // switch with literal labels, break, and a default.
    private void modeCombo_SelectedIndexChanged(object? sender, EventArgs e)
    {
        switch (this.modeCombo.SelectedIndex)
        {
            case 0:
                this.resultLabel.Text = "Simple";
                break;
            case 1:
            case 2:
                this.resultLabel.Text = "Advanced";
                break;
            default:
                this.resultLabel.Text = "";
                break;
        }
    }

    // A helper over System types that are not keywords, and a field of one - both used to keep
    // the helper (and so its caller) a comment.
    private DateTime started;

    private string Describe(DateTime since, TimeSpan? extra)
    {
        return since.ToString("d") + " / " + extra;
    }

    private void timeButton_Click(object? sender, EventArgs e)
    {
        string? note = null;
        this.started = DateTime.Now;
        this.resultLabel.Text = Describe(this.started, null) + note;
    }

    // The three-way question, kept in a local, compared and switched on - and asked inline.
    private void saveButton_Click(object? sender, EventArgs e)
    {
        var answer = MessageBox.Show("Save changes?", "Statements", MessageBoxButtons.YesNoCancel);
        if (answer == DialogResult.Cancel)
        {
            return;
        }

        switch (answer)
        {
            case DialogResult.Yes:
                this.resultLabel.Text = "Saved";
                break;
            case DialogResult.No:
                this.resultLabel.Text = "Discarded";
                break;
        }

        if (MessageBox.Show("Again?", "Statements", MessageBoxButtons.YesNoCancel) != DialogResult.No)
        {
            this.resultLabel.Text = "Again";
        }
    }

    // do/while with a local, and continue inside a loop.
    private void countButton_Click(object? sender, EventArgs e)
    {
        var count = 0;
        do
        {
            count++;
            if (count % 2 == 0)
            {
                continue;
            }

            this.resultLabel.Text = count.ToString();
        }
        while (count < 5);
    }
}
