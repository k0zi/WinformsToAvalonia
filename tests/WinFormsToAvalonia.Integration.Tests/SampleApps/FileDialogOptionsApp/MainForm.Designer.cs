namespace FileDialogOptionsApp;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;
    private Button openButton;
    private Button saveButton;
    private Button folderButton;
    private Label statusLabel;
    private OpenFileDialog openFileDialog1;
    private SaveFileDialog saveFileDialog1;
    private FolderBrowserDialog folderBrowserDialog1;

    private void InitializeComponent()
    {
        this.openButton = new Button();
        this.saveButton = new Button();
        this.folderButton = new Button();
        this.statusLabel = new Label();
        this.openFileDialog1 = new OpenFileDialog();
        this.saveFileDialog1 = new SaveFileDialog();
        this.folderBrowserDialog1 = new FolderBrowserDialog();
        this.SuspendLayout();
        //
        // openButton
        //
        this.openButton.Location = new Point(12, 12);
        this.openButton.Name = "openButton";
        this.openButton.Size = new Size(90, 23);
        this.openButton.Text = "Open...";
        this.openButton.Click += new EventHandler(this.openButton_Click);
        //
        // saveButton
        //
        this.saveButton.Location = new Point(108, 12);
        this.saveButton.Name = "saveButton";
        this.saveButton.Size = new Size(90, 23);
        this.saveButton.Text = "Save...";
        this.saveButton.Click += new EventHandler(this.saveButton_Click);
        //
        // folderButton
        //
        this.folderButton.Location = new Point(204, 12);
        this.folderButton.Name = "folderButton";
        this.folderButton.Size = new Size(90, 23);
        this.folderButton.Text = "Folder...";
        this.folderButton.Click += new EventHandler(this.folderButton_Click);
        //
        // statusLabel
        //
        this.statusLabel.Location = new Point(12, 44);
        this.statusLabel.Name = "statusLabel";
        this.statusLabel.Size = new Size(282, 20);
        this.statusLabel.Text = "Ready";
        //
        // openFileDialog1
        //
        this.openFileDialog1.FileName = "today.log";
        this.openFileDialog1.Filter = "Log files|*.log;*.txt|All files|*.*";
        this.openFileDialog1.InitialDirectory = "C:\\Logs";
        this.openFileDialog1.Multiselect = true;
        this.openFileDialog1.Title = "Open \"log\"";
        //
        // saveFileDialog1
        //
        this.saveFileDialog1.DefaultExt = "csv";
        this.saveFileDialog1.Filter = "Text|*.txt|CSV|*.csv";
        this.saveFileDialog1.FilterIndex = 2;
        this.saveFileDialog1.OverwritePrompt = false;
        this.saveFileDialog1.Title = "Export";
        //
        // folderBrowserDialog1
        //
        this.folderBrowserDialog1.Description = "Pick the export folder";
        this.folderBrowserDialog1.SelectedPath = "/tmp";
        //
        // MainForm
        //
        this.ClientSize = new Size(306, 76);
        this.Controls.Add(this.openButton);
        this.Controls.Add(this.saveButton);
        this.Controls.Add(this.folderButton);
        this.Controls.Add(this.statusLabel);
        this.Name = "MainForm";
        this.Text = "File dialog options";
        this.ResumeLayout(false);
    }
}
