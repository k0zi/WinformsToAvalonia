namespace PrintingApp
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.printDocument1 = new System.Drawing.Printing.PrintDocument();
            this.printDialog1 = new PrintDialog();
            this.printPreviewDialog1 = new PrintPreviewDialog();
            this.pageSetupDialog1 = new PageSetupDialog();
            this.printButton = new Button();
            this.previewButton = new Button();
            this.pageSetupButton = new Button();
            this.SuspendLayout();
            //
            // printDocument1
            //
            this.printDocument1.DocumentName = "Sample report";
            this.printDocument1.PrintPage += new System.Drawing.Printing.PrintPageEventHandler(this.printDocument1_PrintPage);
            //
            // printDialog1
            //
            this.printDialog1.Document = this.printDocument1;
            //
            // printPreviewDialog1
            //
            this.printPreviewDialog1.Document = this.printDocument1;
            //
            // pageSetupDialog1
            //
            this.pageSetupDialog1.Document = this.printDocument1;
            //
            // printButton
            //
            this.printButton.Location = new System.Drawing.Point(12, 12);
            this.printButton.Name = "printButton";
            this.printButton.Size = new System.Drawing.Size(100, 28);
            this.printButton.Text = "Print";
            this.printButton.Click += new System.EventHandler(this.printButton_Click);
            //
            // previewButton
            //
            this.previewButton.Location = new System.Drawing.Point(12, 48);
            this.previewButton.Name = "previewButton";
            this.previewButton.Size = new System.Drawing.Size(100, 28);
            this.previewButton.Text = "Preview";
            this.previewButton.Click += new System.EventHandler(this.previewButton_Click);
            //
            // pageSetupButton
            //
            this.pageSetupButton.Location = new System.Drawing.Point(12, 84);
            this.pageSetupButton.Name = "pageSetupButton";
            this.pageSetupButton.Size = new System.Drawing.Size(100, 28);
            this.pageSetupButton.Text = "Page setup";
            this.pageSetupButton.Click += new System.EventHandler(this.pageSetupButton_Click);
            //
            // MainForm
            //
            this.ClientSize = new System.Drawing.Size(240, 130);
            this.Controls.Add(this.printButton);
            this.Controls.Add(this.previewButton);
            this.Controls.Add(this.pageSetupButton);
            this.Name = "MainForm";
            this.Text = "Printing";
            this.ResumeLayout(false);
        }

        private System.Drawing.Printing.PrintDocument printDocument1;
        private PrintDialog printDialog1;
        private PrintPreviewDialog printPreviewDialog1;
        private PageSetupDialog pageSetupDialog1;
        private Button printButton;
        private Button previewButton;
        private Button pageSetupButton;
    }
}
