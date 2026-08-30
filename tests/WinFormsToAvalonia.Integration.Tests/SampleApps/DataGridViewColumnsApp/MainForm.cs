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

        // detailsListView is a DataGrid, whose rows are data objects bound through columns - so
        // this one goes to the ViewModel collection instead, as the string[] of sub-item texts a
        // ListViewItem already is. One column here, so the one-string form is the whole row.
        this.detailsListView.Items.Add(new ListViewItem("notes.txt"));
    }
}
