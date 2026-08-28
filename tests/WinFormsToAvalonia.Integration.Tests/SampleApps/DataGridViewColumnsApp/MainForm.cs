namespace DataGridViewColumnsApp;

public partial class MainForm : Form
{
    public MainForm()
    {
        InitializeComponent();
    }

    private void fillButton_Click(object sender, EventArgs e)
    {
        // flatListView has no columns, so it becomes a ListBox and a single-column item has an
        // exact counterpart there.
        this.flatListView.Items.Clear();
        this.flatListView.Items.Add(new ListViewItem("readme.txt"));

        // detailsListView is a DataGrid, whose rows are data objects bound through columns -
        // there is no faithful way to turn a ListViewItem into one, so this stays for a human.
        this.detailsListView.Items.Add(new ListViewItem("notes.txt"));
    }
}
