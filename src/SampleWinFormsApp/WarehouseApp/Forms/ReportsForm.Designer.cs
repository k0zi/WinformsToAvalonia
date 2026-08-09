using System.Drawing.Printing;
using WarehouseApp.Controls;

namespace WarehouseApp.Forms;

partial class ReportsForm
{
    private System.ComponentModel.IContainer components = null!;
    private TabControl mainTabControl = null!;
    private TabPage reportsTabPage = null!;
    private TabPage auditLogTabPage = null!;

    private ChartControl salesChart = null!;
    private MonthCalendar referenceMonthCalendar = null!;
    private Label panLabel = null!;
    private HScrollBar panScrollBar = null!;
    private Label zoomLabel = null!;
    private VScrollBar zoomScrollBar = null!;
    private Button exportCsvButton = null!;
    private Button chooseFontButton = null!;
    private Button printPreviewButton = null!;
    private RadioButton barChartRadioButton = null!;
    private RadioButton lineChartRadioButton = null!;

    private ListView auditListView = null!;
    private ColumnHeader timestampColumnHeader = null!;
    private ColumnHeader entityColumnHeader = null!;
    private ColumnHeader actionColumnHeader = null!;
    private ColumnHeader userColumnHeader = null!;
    private ColumnHeader detailsColumnHeader = null!;

    private SaveFileDialog saveFileDialog = null!;
    private FontDialog fontDialog = null!;
    private PrintDocument printDocument = null!;
    private PrintPreviewDialog printPreviewDialog = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.components?.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        this.components = new System.ComponentModel.Container();
        this.SuspendLayout();

        this.salesChart = new WarehouseApp.Controls.ChartControl();
        this.salesChart.Location = new System.Drawing.Point(12, 12);
        this.salesChart.Size = new System.Drawing.Size(560, 260);
        this.salesChart.Title = "Sales Orders by Month";
        this.salesChart.Type = ChartType.Bar;

        this.barChartRadioButton = new System.Windows.Forms.RadioButton();
        this.barChartRadioButton.Text = "Bar";
        this.barChartRadioButton.Location = new System.Drawing.Point(12, 280);
        this.barChartRadioButton.AutoSize = true;
        this.barChartRadioButton.Checked = true;
        this.lineChartRadioButton = new System.Windows.Forms.RadioButton();
        this.lineChartRadioButton.Text = "Line";
        this.lineChartRadioButton.Location = new System.Drawing.Point(80, 280);
        this.lineChartRadioButton.AutoSize = true;
        this.barChartRadioButton.CheckedChanged += (_, _) => { if (this.barChartRadioButton.Checked) { this.salesChart.Type = ChartType.Bar; this.salesChart.Invalidate(); } };
        this.lineChartRadioButton.CheckedChanged += (_, _) => { if (this.lineChartRadioButton.Checked) { this.salesChart.Type = ChartType.Line; this.salesChart.Invalidate(); } };

        this.panLabel = new System.Windows.Forms.Label();
        this.panLabel.Text = "Pan (months back):";
        this.panLabel.Location = new System.Drawing.Point(12, 312);
        this.panLabel.AutoSize = true;
        this.panScrollBar = new System.Windows.Forms.HScrollBar();
        this.panScrollBar.Location = new System.Drawing.Point(12, 330);
        this.panScrollBar.Width = 300;
        this.panScrollBar.Minimum = 0;
        this.panScrollBar.Maximum = 30;
        this.panScrollBar.SmallChange = 1;
        this.panScrollBar.LargeChange = 3;
        this.panScrollBar.ValueChanged += async (_, _) => await this.RefreshChartAsync();

        this.zoomLabel = new System.Windows.Forms.Label();
        this.zoomLabel.Text = "Zoom";
        this.zoomLabel.Location = new System.Drawing.Point(590, 12);
        this.zoomLabel.AutoSize = true;
        this.zoomScrollBar = new System.Windows.Forms.VScrollBar();
        this.zoomScrollBar.Location = new System.Drawing.Point(590, 30);
        this.zoomScrollBar.Height = 240;
        this.zoomScrollBar.Minimum = 3;
        this.zoomScrollBar.Maximum = 15;
        this.zoomScrollBar.Value = 6;
        this.zoomScrollBar.SmallChange = 1;
        this.zoomScrollBar.LargeChange = 2;
        this.zoomScrollBar.ValueChanged += async (_, _) => await this.RefreshChartAsync();

        this.referenceMonthCalendar = new System.Windows.Forms.MonthCalendar();
        this.referenceMonthCalendar.Location = new System.Drawing.Point(12, 366);
        this.referenceMonthCalendar.MaxSelectionCount = 1;
        this.referenceMonthCalendar.DateChanged += async (_, _) => await this.RefreshChartAsync();

        this.exportCsvButton = new System.Windows.Forms.Button();
        this.exportCsvButton.Text = "Export CSV...";
        this.exportCsvButton.Location = new System.Drawing.Point(230, 366);
        this.exportCsvButton.Size = new System.Drawing.Size(110, 28);
        this.exportCsvButton.Click += this.exportCsvButton_Click;
        this.chooseFontButton = new System.Windows.Forms.Button();
        this.chooseFontButton.Text = "Chart Title Font...";
        this.chooseFontButton.Location = new System.Drawing.Point(230, 400);
        this.chooseFontButton.Size = new System.Drawing.Size(110, 28);
        this.chooseFontButton.Click += this.chooseFontButton_Click;
        this.printPreviewButton = new System.Windows.Forms.Button();
        this.printPreviewButton.Text = "Print Preview...";
        this.printPreviewButton.Location = new System.Drawing.Point(230, 434);
        this.printPreviewButton.Size = new System.Drawing.Size(110, 28);
        this.printPreviewButton.Click += this.printPreviewButton_Click;

        this.reportsTabPage = new System.Windows.Forms.TabPage("Reports");
        this.reportsTabPage.Controls.Add(this.salesChart);
        this.reportsTabPage.Controls.Add(this.barChartRadioButton);
        this.reportsTabPage.Controls.Add(this.lineChartRadioButton);
        this.reportsTabPage.Controls.Add(this.panLabel);
        this.reportsTabPage.Controls.Add(this.panScrollBar);
        this.reportsTabPage.Controls.Add(this.zoomLabel);
        this.reportsTabPage.Controls.Add(this.zoomScrollBar);
        this.reportsTabPage.Controls.Add(this.referenceMonthCalendar);
        this.reportsTabPage.Controls.Add(this.exportCsvButton);
        this.reportsTabPage.Controls.Add(this.chooseFontButton);
        this.reportsTabPage.Controls.Add(this.printPreviewButton);

        this.timestampColumnHeader = new System.Windows.Forms.ColumnHeader();
        this.timestampColumnHeader.Text = "Timestamp";
        this.timestampColumnHeader.Width = 140;
        this.entityColumnHeader = new System.Windows.Forms.ColumnHeader();
        this.entityColumnHeader.Text = "Entity";
        this.entityColumnHeader.Width = 100;
        this.actionColumnHeader = new System.Windows.Forms.ColumnHeader();
        this.actionColumnHeader.Text = "Action";
        this.actionColumnHeader.Width = 80;
        this.userColumnHeader = new System.Windows.Forms.ColumnHeader();
        this.userColumnHeader.Text = "User";
        this.userColumnHeader.Width = 100;
        this.detailsColumnHeader = new System.Windows.Forms.ColumnHeader();
        this.detailsColumnHeader.Text = "Details";
        this.detailsColumnHeader.Width = 260;
        this.auditListView = new System.Windows.Forms.ListView();
        this.auditListView.Dock = System.Windows.Forms.DockStyle.Fill;
        this.auditListView.View = System.Windows.Forms.View.Details;
        this.auditListView.FullRowSelect = true;
        this.auditListView.GridLines = true;
        this.auditListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] { this.timestampColumnHeader, this.entityColumnHeader, this.actionColumnHeader, this.userColumnHeader, this.detailsColumnHeader });

        this.auditLogTabPage = new System.Windows.Forms.TabPage("Audit Log");
        this.auditLogTabPage.Controls.Add(this.auditListView);

        this.mainTabControl = new System.Windows.Forms.TabControl();
        this.mainTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
        this.mainTabControl.TabPages.AddRange(new System.Windows.Forms.TabPage[] { this.reportsTabPage, this.auditLogTabPage });

        this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
        this.saveFileDialog.Filter = "CSV Files|*.csv|All Files|*.*";
        this.saveFileDialog.FileName = "sales-report.csv";
        this.fontDialog = new System.Windows.Forms.FontDialog();
        this.printDocument = new System.Drawing.Printing.PrintDocument();
        this.printDocument.PrintPage += this.PrintDocument_PrintPage;
        this.printPreviewDialog = new System.Windows.Forms.PrintPreviewDialog();
        this.printPreviewDialog.Document = this.printDocument;
        this.printPreviewDialog.Width = 700;
        this.printPreviewDialog.Height = 600;

        this.ClientSize = new System.Drawing.Size(780, 520);
        this.Controls.Add(this.mainTabControl);
        this.Text = "Reports — WarehouseApp";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;

        this.ResumeLayout(false);
        this.PerformLayout();
    }
}