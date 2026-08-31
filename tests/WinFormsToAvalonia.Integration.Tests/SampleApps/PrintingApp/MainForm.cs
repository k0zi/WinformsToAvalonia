using System.Drawing;
using System.Drawing.Printing;

namespace PrintingApp;

public partial class MainForm : Form
{
    public MainForm()
    {
        InitializeComponent();
    }

    // Drawing code, which is the half that translates - the page is really drawn.
    private void printDocument1_PrintPage(object sender, PrintPageEventArgs e)
    {
        e.Graphics!.DrawRectangle(Pens.Black, 0, 0, 100, 40);
        e.Graphics!.DrawString("printed", this.Font, Brushes.Black, e.MarginBounds);
        e.HasMorePages = false;
    }

    // The one shape a PrintDialog appears in: the dialog guards the print.
    private void printButton_Click(object sender, EventArgs e)
    {
        if (this.printDialog1.ShowDialog(this) == DialogResult.OK)
        {
            this.printDocument1.Print();
        }
    }

    private void previewButton_Click(object sender, EventArgs e)
    {
        this.printPreviewDialog1.ShowDialog(this);
    }

    private void pageSetupButton_Click(object sender, EventArgs e)
    {
        this.pageSetupDialog1.ShowDialog(this);
    }
}
