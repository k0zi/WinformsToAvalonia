using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.Controls.Primitives;
using All_In_One_WinForms.Components;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Media;
using System.ServiceProcess;
using All_In_One_WinForms.Views.Forms;
using Avalonia.Input.Platform;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using All_In_One_WinForms.Controls;
using All_In_One_WinForms.Generated;
using All_In_One_WinForms.ViewModels;

namespace All_In_One_WinForms.Views;

// This view uses EventLog, PerformanceCounter, ServiceController, SoundPlayer, which .NET marks as Windows-only.
// The generated project targets net10.0 so it builds everywhere; these calls throw elsewhere.
#pragma warning disable CA1416

public partial class MainView : Window
{
    private readonly DispatcherTimer clockTimer;
    private readonly BackgroundWorker backgroundWorker1 = new();
    private readonly DemoComponent demoComponent1 = new();
    private readonly FileSystemWatcher fileSystemWatcher1 = new();
    private readonly Process process1 = new();
    private readonly SerialPort serialPort1 = new();
    private EventLog? _eventLog1;
    private PerformanceCounter? _performanceCounter1;
    private ServiceController? _serviceController1;
    private SoundPlayer? _soundPlayer1;
    private bool isBusy;
    private bool w2aInitialized;

    public MainView()
    {
        InitializeComponent();
        DataContext = new MainViewModel();

        backgroundWorker1.WorkerReportsProgress = true;
        backgroundWorker1.WorkerSupportsCancellation = true;
        backgroundWorker1.DoWork += backgroundWorker1_DoWork;
        backgroundWorker1.ProgressChanged += backgroundWorker1_ProgressChanged;
        backgroundWorker1.RunWorkerCompleted += backgroundWorker1_RunWorkerCompleted;

        demoComponent1.Caption = "All-In-One";
        demoComponent1.Ticked += demoComponent1_Ticked;

        fileSystemWatcher1.EnableRaisingEvents = false;
        fileSystemWatcher1.Filter = "*.txt";
        fileSystemWatcher1.Changed += fileSystemWatcher1_Changed;

        serialPort1.BaudRate = 115200;
        serialPort1.PortName = "COM1";

        clockTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1000) };
        clockTimer.Tick += clockTimer_Tick;

        w2aInitialized = true;
    }

    private EventLog eventLog1
    {
        get
        {
            if (_eventLog1 is null)
            {
                _eventLog1 = new EventLog
                {
                    Log = "Application",
                    Source = "AllInOneWinForms",
                };
            }

            return _eventLog1;
        }
    }

    private PerformanceCounter performanceCounter1
    {
        get
        {
            if (_performanceCounter1 is null)
            {
                _performanceCounter1 = new PerformanceCounter
                {
                    CategoryName = "Processor",
                    CounterName = "% Processor Time",
                    InstanceName = "_Total",
                };
            }

            return _performanceCounter1;
        }
    }

    private ServiceController serviceController1
    {
        get
        {
            if (_serviceController1 is null)
            {
                _serviceController1 = new ServiceController
                {
                    ServiceName = "Spooler",
                };
            }

            return _serviceController1;
        }
    }

    private SoundPlayer soundPlayer1
    {
        get
        {
            if (_soundPlayer1 is null)
            {
                _soundPlayer1 = new SoundPlayer();
            }

            return _soundPlayer1;
        }
    }

    private void MainForm_Load(object? sender, EventArgs e)
    {
        /* ORIGINAL WINFORMS BODY of 'MainForm_Load' - TODO(Winforms2Avalonia): migrate it into this method.
        this.itemsTreeView.Nodes.Add("Documents");
        this.itemsTreeView.Nodes.Add("Pictures");
        this.itemsListView.Items.Add(new ListViewItem(new[] { "readme.txt", "2 KB" }));
        this.itemsListView.Items.Add(new ListViewItem(new[] { "notes.txt", "11 KB" }));

        this.bindingSource1.DataSource = new BindingList<GalleryRow>
        {
            new GalleryRow { Name = "First", Active = true, Category = "Alpha" },
            new GalleryRow { Name = "Second", Active = false, Category = "Beta" },
        };

        this.propertyGrid1.SelectedObject = this.demoComponent1;
        this.statusLabel.Text = "Loaded";
        */
        MigrationTodo.NotMigrated(nameof(MainForm_Load), "MainForm_Load");
    }

    private void MainForm_FormClosing(object? sender, WindowClosingEventArgs e)
    {
        /* ORIGINAL WINFORMS BODY of 'MainForm_FormClosing' - TODO(Winforms2Avalonia): migrate it into this method.
        if (this.isBusy)
        {
            e.Cancel = MessageBox.Show(
                "A background operation is still running. Close anyway?",
                "All-In-One",
                MessageBoxButtons.YesNo) == DialogResult.No;
        }

        this.notifyIcon1.Visible = false;
        */
        MigrationTodo.NotMigrated(nameof(MainForm_FormClosing), "MainForm_FormClosing");
    }

    private void newMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        titleTextBox.Clear();
        notesRichTextBox.Clear();
        statusLabel.Text = "New document";
    }

    private async void openMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        if (await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()) is [var openFileDialog1File, ..])
        {
            notesRichTextBox.Text = File.ReadAllText(openFileDialog1File.Path.LocalPath);
            statusLabel.Text = openFileDialog1File.Path.LocalPath;
        }
    }

    private void exitMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void wordWrapMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        notesRichTextBox.TextWrapping = (wordWrapMenuItem.IsChecked) ? TextWrapping.Wrap : TextWrapping.NoWrap;
    }

    private async void aboutMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        var dialog = new DialogView();
        await dialog.ShowDialog(this);
    }

    private void tabControl1_SelectedIndexChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!w2aInitialized)
        {
            return;
        }

        statusInfoLabel.Text = ((tabControl1.SelectedItem as TabItem)?.Header as string) ?? string.Empty;
    }

    private void linkLabel1_LinkClicked(object? sender, RoutedEventArgs e)
    {
        /* ORIGINAL WINFORMS BODY of 'linkLabel1_LinkClicked' - TODO(Winforms2Avalonia): migrate it into this method.
        this.linkLabel1.LinkVisited = true;
        this.statusLabel.Text = "Link clicked";
        */
        MigrationTodo.NotMigrated(nameof(linkLabel1_LinkClicked), "linkLabel1_LinkClicked");
    }

    private async void demoButton_Click(object? sender, RoutedEventArgs e)
    {
        await MessageBoxFallback.ShowAsync(this, $"Hello, {(titleTextBox.Text ?? string.Empty)}!", "All-In-One");
    }

    private void sharedButton_Click(object? sender, RoutedEventArgs e)
    {
        var button = (Button)sender!;
        statusLabel.Text = $"{(button.Content as string ?? string.Empty)} pressed";
    }

    private void validateButton_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace((titleTextBox.Text ?? string.Empty)))
        {
            ErrorProviderFallback.SetError(titleTextBox, "A title is required.");
        }
        else
        {
            ErrorProviderFallback.SetError(titleTextBox, string.Empty);
        }
    }

    private void itemsListBox_SelectedIndexChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!w2aInitialized)
        {
            return;
        }

        statusLabel.Text = $"Selected: {itemsListBox.SelectedItem}";
    }

    private void refreshButton_Click(object? sender, RoutedEventArgs e)
    {
        /* ORIGINAL WINFORMS BODY of 'refreshButton_Click' - TODO(Winforms2Avalonia): migrate it into this method.
        this.itemsTreeView.Nodes.Clear();
        var root = this.itemsTreeView.Nodes.Add("Reloaded");
        root.Nodes.Add("Child one");
        root.Nodes.Add("Child two");
        this.itemsTreeView.ExpandAll();
        */
        MigrationTodo.NotMigrated(nameof(refreshButton_Click), "refreshButton_Click");
    }

    private void dataGridView1_CellClick(object? sender, DataGridCellPointerPressedEventArgs e)
    {
        /* ORIGINAL WINFORMS BODY of 'dataGridView1_CellClick' - TODO(Winforms2Avalonia): migrate it into this method.
        this.gridInfoLabel.Text = $"Cell clicked: row {e.RowIndex}, column {e.ColumnIndex}";
        */
        MigrationTodo.NotMigrated(nameof(dataGridView1_CellClick), "dataGridView1_CellClick");
    }

    private void clockToggleButton_Click(object? sender, RoutedEventArgs e)
    {
        clockTimer.IsEnabled = !clockTimer.IsEnabled;
        clockToggleButton.Content = clockTimer.IsEnabled ? "Stop clock" : "Start clock";
    }

    private void clockTimer_Tick(object? sender, EventArgs e)
    {
        clockLabel.Text = DateTime.Now.ToLongTimeString();
        demoComponent1.Tick();
    }

    private void pictureBox1_Click(object? sender, PointerPressedEventArgs e)
    {
        pictureBox1.InvalidateVisual();
    }

    private void pictureBox1_MouseDown(object? sender, PointerPressedEventArgs e)
    {
        statusLabel.Text = $"Mouse down at {e.GetPosition(pictureBox1).X},{e.GetPosition(pictureBox1).Y}";
    }

    private void pictureBox1_Paint(object? sender, EventArgs e)
    {
        /* ORIGINAL WINFORMS BODY of 'pictureBox1_Paint' - TODO(Winforms2Avalonia): migrate it into this method.
        e.Graphics.DrawEllipse(Pens.SteelBlue, 10, 10, 200, 120);
        e.Graphics.DrawString("PictureBox.Paint", this.Font, Brushes.SteelBlue, 20, 60);
        */
        MigrationTodo.NotMigrated(nameof(pictureBox1_Paint), "pictureBox1_Paint");
    }

    private void trackBar1_Scroll(object? sender, RangeBaseValueChangedEventArgs e)
    {
        progressBar1.Value = trackBar1.Value;
        graphicsInfoLabel.Text = $"TrackBar value: {trackBar1.Value}";
    }

    private void hScrollBar1_Scroll(object? sender, ScrollEventArgs e)
    {
        statusLabel.Text = $"Scrolled to {e.NewValue}";
    }

    private async void openFileButton_Click(object? sender, RoutedEventArgs e)
    {
        if (await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()) is [var openFileDialog1File, ..])
        {
            selectedPathLabel.Text = openFileDialog1File.Path.LocalPath;
        }
    }

    private async void saveFileButton_Click(object? sender, RoutedEventArgs e)
    {
        if (await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions()) is { } saveFileDialog1File)
        {
            selectedPathLabel.Text = saveFileDialog1File.Path.LocalPath;
        }
    }

    private async void folderBrowserButton_Click(object? sender, RoutedEventArgs e)
    {
        if (await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()) is [var folderBrowserDialog1Folder, ..])
        {
            selectedPathLabel.Text = folderBrowserDialog1Folder.Path.LocalPath;
        }
    }

    private async void colorButton_Click(object? sender, RoutedEventArgs e)
    {
        if (await ColorDialogFallback.ShowAsync(this) is { } colorDialog1Color)
        {
            plainPanel.Background = new SolidColorBrush(colorDialog1Color);
        }
    }

    private async void fontButton_Click(object? sender, RoutedEventArgs e)
    {
        if (await FontDialogFallback.ShowAsync(this) is { } fontDialog1Font)
        {
            notesRichTextBox.FontFamily = fontDialog1Font.Family;
            notesRichTextBox.FontSize = fontDialog1Font.Size;
            notesRichTextBox.FontWeight = fontDialog1Font.Weight;
            notesRichTextBox.FontStyle = fontDialog1Font.Style;
        }
    }

    private void printButton_Click(object? sender, RoutedEventArgs e)
    {
        /* ORIGINAL WINFORMS BODY of 'printButton_Click' - TODO(Winforms2Avalonia): migrate it into this method.
        if (this.printDialog1.ShowDialog(this) == DialogResult.OK)
        {
            this.printDocument1.Print();
        }
        */
        MigrationTodo.NotMigrated(nameof(printButton_Click), "printButton_Click");
    }

    private void pageSetupButton_Click(object? sender, RoutedEventArgs e)
    {
        /* ORIGINAL WINFORMS BODY of 'pageSetupButton_Click' - TODO(Winforms2Avalonia): migrate it into this method.
        this.pageSetupDialog1.ShowDialog(this);
        */
        MigrationTodo.NotMigrated(nameof(pageSetupButton_Click), "pageSetupButton_Click");
    }

    private void printPreviewButton_Click(object? sender, RoutedEventArgs e)
    {
        /* ORIGINAL WINFORMS BODY of 'printPreviewButton_Click' - TODO(Winforms2Avalonia): migrate it into this method.
        this.printPreviewDialog1.ShowDialog(this);
        */
        MigrationTodo.NotMigrated(nameof(printPreviewButton_Click), "printPreviewButton_Click");
    }

    private void printDocument1_PrintPage(object? sender, EventArgs e)
    {
        /* ORIGINAL WINFORMS BODY of 'printDocument1_PrintPage' - TODO(Winforms2Avalonia): migrate it into this method.
        e.Graphics!.DrawString(
            this.notesRichTextBox.Text,
            this.notesRichTextBox.Font,
            Brushes.Black,
            e.MarginBounds);
        e.HasMorePages = false;
        */
        MigrationTodo.NotMigrated(nameof(printDocument1_PrintPage), "printDocument1_PrintPage");
    }

    private void startWorkerButton_Click(object? sender, RoutedEventArgs e)
    {
        if (backgroundWorker1.IsBusy)
        {
            backgroundWorker1.CancelAsync();
            return;
        }
        SetBusy(true);
        backgroundWorker1.RunWorkerAsync();
    }

    private void watchButton_Click(object? sender, RoutedEventArgs e)
    {
        fileSystemWatcher1.Path = Path.GetTempPath();
        fileSystemWatcher1.EnableRaisingEvents = !fileSystemWatcher1.EnableRaisingEvents;
        watcherLabel.Text = fileSystemWatcher1.EnableRaisingEvents ? $"Watching {fileSystemWatcher1.Path}" : "Watcher idle";
    }

    private void launchProcessButton_Click(object? sender, RoutedEventArgs e)
    {
        process1.StartInfo.FileName = "notepad.exe";
        process1.StartInfo.UseShellExecute = true;
        process1.Start();
        Log("Started notepad.exe");
    }

    private void writeEventLogButton_Click(object? sender, RoutedEventArgs e)
    {
        eventLog1.WriteEntry("All-In-One sample wrote an entry.");
        Log("Event log entry written.");
    }

    private void readCounterButton_Click(object? sender, RoutedEventArgs e)
    {
        Log($"CPU: {performanceCounter1.NextValue():F1}%");
    }

    private void serviceStatusButton_Click(object? sender, RoutedEventArgs e)
    {
        serviceController1.Refresh();
        Log($"{serviceController1.ServiceName}: {serviceController1.Status}");
    }

    private void serialOpenButton_Click(object? sender, RoutedEventArgs e)
    {
        if (serialPort1.IsOpen)
        {
            serialPort1.Close();
            Log($"{serialPort1.PortName} closed.");
            return;
        }
        serialPort1.Open();
        Log($"{serialPort1.PortName} opened at {serialPort1.BaudRate} baud.");
    }

    private void playSoundButton_Click(object? sender, RoutedEventArgs e)
    {
        soundPlayer1.Play();
        Log("Sound played.");
    }

    private void showBalloonButton_Click(object? sender, RoutedEventArgs e)
    {
        /* ORIGINAL WINFORMS BODY of 'showBalloonButton_Click' - TODO(Winforms2Avalonia): migrate it into this method.
        this.notifyIcon1.ShowBalloonTip(3000);
        */
        MigrationTodo.NotMigrated(nameof(showBalloonButton_Click), "showBalloonButton_Click");
    }

    private void contextPanel_DragDrop(object? sender, DragEventArgs e)
    {
        /* ORIGINAL WINFORMS BODY of 'contextPanel_DragDrop' - TODO(Winforms2Avalonia): migrate it into this method.
        var files = (string[])e.Data!.GetData(DataFormats.FileDrop)!;
        this.contextPanelLabel.Text = string.Join(", ", files);
        */
        MigrationTodo.NotMigrated(nameof(contextPanel_DragDrop), "contextPanel_DragDrop");
    }

    private void contextPanel_DragEnter(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.DataTransfer.Contains(DataFormat.File) ? DragDropEffects.Copy : DragDropEffects.None;
    }

    private void backgroundWorker1_DoWork(object? sender, DoWorkEventArgs e)
    {
        for (var i = 0; i <= 100; i += 10)
        {
            if (backgroundWorker1.CancellationPending)
            {
                e.Cancel = true;
                return;
            }
            Thread.Sleep(100);
            backgroundWorker1.ReportProgress(i);
        }
    }

    private void backgroundWorker1_ProgressChanged(object? sender, ProgressChangedEventArgs e)
    {
        workerProgressBar.Value = e.ProgressPercentage;
        statusProgressBar.Value = e.ProgressPercentage;
    }

    private void backgroundWorker1_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
    {
        SetBusy(false);
        Log(e.Cancelled ? "Worker cancelled." : "Worker finished.");
    }

    private void fileSystemWatcher1_Changed(object? sender, FileSystemEventArgs e)
    {
        watcherLabel.Text = $"{e.ChangeType}: {e.Name}";
        Log($"{e.ChangeType}: {e.FullPath}");
    }

    private void notifyIcon1_DoubleClick(object? sender, TappedEventArgs e)
    {
        WindowState = WindowState.Normal;
        Activate();
    }

    private async void copyContextMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        await TopLevel.GetTopLevel(this)!.Clipboard!.SetTextAsync((contextPanelLabel.Text ?? string.Empty));
        statusLabel.Text = "Copied";
    }

    private async void openDialogFormButton_Click(object? sender, RoutedEventArgs e)
    {
        var dialog = new DialogView();
        if (await dialog.ShowDialog<bool>(this))
        {
            titleTextBox.Text = dialog.EnteredText;
        }
    }

    private async void aboutButton_Click(object? sender, RoutedEventArgs e)
    {
        var dialog = new DialogView();
        dialog.Title = "About";
        await dialog.ShowDialog(this);
    }

    private void demoComponent1_Ticked(object? sender, EventArgs e)
    {
        advancedInfoLabel.Text = $"DemoComponent ticks: {demoComponent1.TickCount}";
    }

    private void SetBusy(bool busy)
    {
        isBusy = busy;
        startWorkerButton.Content = busy ? "Cancel" : "Run BackgroundWorker";
        statusLabel.Text = busy ? "Working..." : "Ready";
    }

    private void Log(string message)
    {
        componentsLogTextBox.Text += message + Environment.NewLine;
    }

    /* ORIGINAL WINFORMS MEMBERS - NOT COMPILED, PRESERVED FOR MANUAL MIGRATION

    private sealed class GalleryRow
    {
        public string Name { get; set; } = string.Empty;

        public bool Active { get; set; }

        public string Category { get; set; } = string.Empty;
    }
    */

    /* ORIGINAL WINFORMS CODE-BEHIND - NOT COMPILED, PRESERVED FOR REFERENCE
       Original file: MainForm.cs

using System.ComponentModel;
using System.Drawing.Printing;
using AllInOneWinForms.Forms;

namespace AllInOneWinForms;

/// <summary>
/// A gallery of every control and component that ships in the Windows Forms toolbox on
/// modern .NET, laid out over a tabbed main window.
/// <para>
/// Deliberately absent, because they do not exist on modern .NET (they were dropped when
/// Windows Forms moved off .NET Framework): <c>MainMenu</c>, <c>ContextMenu</c>,
/// <c>ToolBar</c>, <c>StatusBar</c> and <c>DataGrid</c> - superseded by <c>MenuStrip</c>,
/// <c>ContextMenuStrip</c>, <c>ToolStrip</c>, <c>StatusStrip</c> and <c>DataGridView</c>,
/// all of which are here. Also absent: <c>MessageQueue</c> (MSMQ is not supported on .NET),
/// <c>DirectoryEntry</c>/<c>DirectorySearcher</c> (System.DirectoryServices, not a Windows
/// Forms type) and <c>Chart</c> (never part of the framework itself).
/// </para>
/// <para>
/// Also not instantiated here, on purpose: abstract or base types that exist only to be
/// derived from (<c>Control</c>, <c>ButtonBase</c>, <c>ScrollableControl</c>,
/// <c>ContainerControl</c>, <c>ListControl</c>, <c>TextBoxBase</c>, <c>WebBrowserBase</c>,
/// <c>ToolStripItem</c>, <c>ToolStripDropDown</c>, <c>DataGridViewColumn</c>,
/// <c>DataGridViewCell</c>), and the <c>DataGridView*Cell</c> types, which are per-cell
/// objects the grid creates from its column types rather than toolbox items a designer ever
/// news up. <c>Form</c> and <c>UserControl</c> appear as the artifacts themselves
/// (<see cref="MainForm"/>, <c>DialogForm</c>, <c>DemoUserControl</c>).
/// </para>
/// </summary>
public partial class MainForm : Form
{
    private bool isBusy;

    public MainForm()
    {
        InitializeComponent();
    }

    // ---------------------------------------------------------------- form lifecycle ----

    private void MainForm_Load(object? sender, EventArgs e)
    {
        this.itemsTreeView.Nodes.Add("Documents");
        this.itemsTreeView.Nodes.Add("Pictures");
        this.itemsListView.Items.Add(new ListViewItem(new[] { "readme.txt", "2 KB" }));
        this.itemsListView.Items.Add(new ListViewItem(new[] { "notes.txt", "11 KB" }));

        this.bindingSource1.DataSource = new BindingList<GalleryRow>
        {
            new GalleryRow { Name = "First", Active = true, Category = "Alpha" },
            new GalleryRow { Name = "Second", Active = false, Category = "Beta" },
        };

        this.propertyGrid1.SelectedObject = this.demoComponent1;
        this.statusLabel.Text = "Loaded";
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (this.isBusy)
        {
            e.Cancel = MessageBox.Show(
                "A background operation is still running. Close anyway?",
                "All-In-One",
                MessageBoxButtons.YesNo) == DialogResult.No;
        }

        this.notifyIcon1.Visible = false;
    }

    // --------------------------------------------------------------------- menu bar ----

    private void newMenuItem_Click(object? sender, EventArgs e)
    {
        this.titleTextBox.Clear();
        this.notesRichTextBox.Clear();
        this.statusLabel.Text = "New document";
    }

    private void openMenuItem_Click(object? sender, EventArgs e)
    {
        if (this.openFileDialog1.ShowDialog(this) == DialogResult.OK)
        {
            this.notesRichTextBox.Text = File.ReadAllText(this.openFileDialog1.FileName);
            this.statusLabel.Text = this.openFileDialog1.FileName;
        }
    }

    private void exitMenuItem_Click(object? sender, EventArgs e)
    {
        Close();
    }

    private void wordWrapMenuItem_Click(object? sender, EventArgs e)
    {
        this.notesRichTextBox.WordWrap = this.wordWrapMenuItem.Checked;
    }

    private void aboutMenuItem_Click(object? sender, EventArgs e)
    {
        using var dialog = new DialogForm();
        dialog.ShowDialog(this);
    }

    private void toolStripNewButton_Click(object? sender, EventArgs e)
    {
        this.toolStripProgressBar1.Value = Math.Min(100, this.toolStripProgressBar1.Value + 10);
        this.statusLabel.Text = "Toolbar: new";
    }

    // ------------------------------------------------------------- tab 1: buttons ------

    /// <summary>
    /// Touches nothing but two-way bindable value properties of directly mapped controls,
    /// uses neither <c>sender</c> nor <c>e</c>, and is wired to a single button - the shape
    /// a converter can lift into a ViewModel command.
    /// </summary>
    private void applyButton_Click(object? sender, EventArgs e)
    {
        this.captionLabel.Text = this.titleTextBox.Text;
        this.enabledCheckBox.Checked = true;
    }

    /// <summary>Same shape as <see cref="applyButton_Click"/>.</summary>
    private void resetButton_Click(object? sender, EventArgs e)
    {
        this.titleTextBox.Text = string.Empty;
        this.enabledCheckBox.Checked = false;
        this.amountUpDown.Value = 0;
        this.itemsComboBox.SelectedIndex = -1;
    }

    private void demoButton_Click(object? sender, EventArgs e)
    {
        MessageBox.Show(this, $"Hello, {this.titleTextBox.Text}!", "All-In-One");
    }

    /// <summary>Wired to two buttons at once, so it needs <c>sender</c> to tell them apart.</summary>
    private void sharedButton_Click(object? sender, EventArgs e)
    {
        var button = (Button)sender!;
        this.statusLabel.Text = $"{button.Text} pressed";
    }

    private void validateButton_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(this.titleTextBox.Text))
        {
            this.errorProvider1.SetError(this.titleTextBox, "A title is required.");
        }
        else
        {
            this.errorProvider1.SetError(this.titleTextBox, string.Empty);
        }
    }

    private void linkLabel1_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
    {
        this.linkLabel1.LinkVisited = true;
        this.statusLabel.Text = "Link clicked";
    }

    // --------------------------------------------------------------- tab 2: lists ------

    private void itemsListBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        this.statusLabel.Text = $"Selected: {this.itemsListBox.SelectedItem}";
    }

    private void refreshButton_Click(object? sender, EventArgs e)
    {
        this.itemsTreeView.Nodes.Clear();
        var root = this.itemsTreeView.Nodes.Add("Reloaded");
        root.Nodes.Add("Child one");
        root.Nodes.Add("Child two");
        this.itemsTreeView.ExpandAll();
    }

    // ---------------------------------------------------------------- tab 4: data ------

    private void dataGridView1_CellClick(object? sender, DataGridViewCellEventArgs e)
    {
        this.gridInfoLabel.Text = $"Cell clicked: row {e.RowIndex}, column {e.ColumnIndex}";
    }

    // ----------------------------------------------------------- tab 5: date & time ----

    private void clockToggleButton_Click(object? sender, EventArgs e)
    {
        this.clockTimer.Enabled = !this.clockTimer.Enabled;
        this.clockToggleButton.Text = this.clockTimer.Enabled ? "Stop clock" : "Start clock";
    }

    private void clockTimer_Tick(object? sender, EventArgs e)
    {
        this.clockLabel.Text = DateTime.Now.ToLongTimeString();
        this.demoComponent1.Tick();
    }

    // ------------------------------------------------------- tab 6: graphics & range ---

    private void pictureBox1_Paint(object? sender, PaintEventArgs e)
    {
        e.Graphics.DrawEllipse(Pens.SteelBlue, 10, 10, 200, 120);
        e.Graphics.DrawString("PictureBox.Paint", this.Font, Brushes.SteelBlue, 20, 60);
    }

    private void pictureBox1_MouseDown(object? sender, MouseEventArgs e)
    {
        this.statusLabel.Text = $"Mouse down at {e.X},{e.Y}";
    }

    private void pictureBox1_Click(object? sender, EventArgs e)
    {
        this.pictureBox1.Invalidate();
    }

    private void trackBar1_Scroll(object? sender, EventArgs e)
    {
        this.progressBar1.Value = this.trackBar1.Value;
        this.graphicsInfoLabel.Text = $"TrackBar value: {this.trackBar1.Value}";
    }

    private void hScrollBar1_Scroll(object? sender, ScrollEventArgs e)
    {
        this.statusLabel.Text = $"Scrolled to {e.NewValue}";
    }

    // ------------------------------------------------------ tab 7: dialogs & printing --

    private void openFileButton_Click(object? sender, EventArgs e)
    {
        if (this.openFileDialog1.ShowDialog(this) == DialogResult.OK)
        {
            this.selectedPathLabel.Text = this.openFileDialog1.FileName;
        }
    }

    private void saveFileButton_Click(object? sender, EventArgs e)
    {
        if (this.saveFileDialog1.ShowDialog(this) == DialogResult.OK)
        {
            this.selectedPathLabel.Text = this.saveFileDialog1.FileName;
        }
    }

    private void folderBrowserButton_Click(object? sender, EventArgs e)
    {
        if (this.folderBrowserDialog1.ShowDialog(this) == DialogResult.OK)
        {
            this.selectedPathLabel.Text = this.folderBrowserDialog1.SelectedPath;
        }
    }

    private void colorButton_Click(object? sender, EventArgs e)
    {
        if (this.colorDialog1.ShowDialog(this) == DialogResult.OK)
        {
            this.plainPanel.BackColor = this.colorDialog1.Color;
        }
    }

    private void fontButton_Click(object? sender, EventArgs e)
    {
        if (this.fontDialog1.ShowDialog(this) == DialogResult.OK)
        {
            this.notesRichTextBox.Font = this.fontDialog1.Font;
        }
    }

    private void printButton_Click(object? sender, EventArgs e)
    {
        if (this.printDialog1.ShowDialog(this) == DialogResult.OK)
        {
            this.printDocument1.Print();
        }
    }

    private void pageSetupButton_Click(object? sender, EventArgs e)
    {
        this.pageSetupDialog1.ShowDialog(this);
    }

    private void printPreviewButton_Click(object? sender, EventArgs e)
    {
        this.printPreviewDialog1.ShowDialog(this);
    }

    private void printDocument1_PrintPage(object? sender, PrintPageEventArgs e)
    {
        e.Graphics!.DrawString(
            this.notesRichTextBox.Text,
            this.notesRichTextBox.Font,
            Brushes.Black,
            e.MarginBounds);
        e.HasMorePages = false;
    }

    // ----------------------------------------------------------- tab 8: components -----

    private void startWorkerButton_Click(object? sender, EventArgs e)
    {
        if (this.backgroundWorker1.IsBusy)
        {
            this.backgroundWorker1.CancelAsync();
            return;
        }

        SetBusy(true);
        this.backgroundWorker1.RunWorkerAsync();
    }

    private void backgroundWorker1_DoWork(object? sender, DoWorkEventArgs e)
    {
        for (var i = 0; i <= 100; i += 10)
        {
            if (this.backgroundWorker1.CancellationPending)
            {
                e.Cancel = true;
                return;
            }

            Thread.Sleep(100);
            this.backgroundWorker1.ReportProgress(i);
        }
    }

    private void backgroundWorker1_ProgressChanged(object? sender, ProgressChangedEventArgs e)
    {
        this.workerProgressBar.Value = e.ProgressPercentage;
        this.statusProgressBar.Value = e.ProgressPercentage;
    }

    private void backgroundWorker1_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
    {
        SetBusy(false);
        Log(e.Cancelled ? "Worker cancelled." : "Worker finished.");
    }

    private void watchButton_Click(object? sender, EventArgs e)
    {
        this.fileSystemWatcher1.Path = Path.GetTempPath();
        this.fileSystemWatcher1.EnableRaisingEvents = !this.fileSystemWatcher1.EnableRaisingEvents;
        this.watcherLabel.Text = this.fileSystemWatcher1.EnableRaisingEvents
            ? $"Watching {this.fileSystemWatcher1.Path}"
            : "Watcher idle";
    }

    private void fileSystemWatcher1_Changed(object? sender, FileSystemEventArgs e)
    {
        this.watcherLabel.Text = $"{e.ChangeType}: {e.Name}";
        Log($"{e.ChangeType}: {e.FullPath}");
    }

    private void launchProcessButton_Click(object? sender, EventArgs e)
    {
        this.process1.StartInfo.FileName = "notepad.exe";
        this.process1.StartInfo.UseShellExecute = true;
        this.process1.Start();
        Log("Started notepad.exe");
    }

    private void writeEventLogButton_Click(object? sender, EventArgs e)
    {
        this.eventLog1.WriteEntry("All-In-One sample wrote an entry.");
        Log("Event log entry written.");
    }

    private void readCounterButton_Click(object? sender, EventArgs e)
    {
        Log($"CPU: {this.performanceCounter1.NextValue():F1}%");
    }

    private void serviceStatusButton_Click(object? sender, EventArgs e)
    {
        this.serviceController1.Refresh();
        Log($"{this.serviceController1.ServiceName}: {this.serviceController1.Status}");
    }

    private void serialOpenButton_Click(object? sender, EventArgs e)
    {
        if (this.serialPort1.IsOpen)
        {
            this.serialPort1.Close();
            Log($"{this.serialPort1.PortName} closed.");
            return;
        }

        this.serialPort1.Open();
        Log($"{this.serialPort1.PortName} opened at {this.serialPort1.BaudRate} baud.");
    }

    private void playSoundButton_Click(object? sender, EventArgs e)
    {
        this.soundPlayer1.Play();
        Log("Sound played.");
    }

    private void showBalloonButton_Click(object? sender, EventArgs e)
    {
        this.notifyIcon1.ShowBalloonTip(3000);
    }

    private void notifyIcon1_DoubleClick(object? sender, EventArgs e)
    {
        WindowState = FormWindowState.Normal;
        Activate();
    }

    private void copyContextMenuItem_Click(object? sender, EventArgs e)
    {
        Clipboard.SetText(this.contextPanelLabel.Text);
        this.statusLabel.Text = "Copied";
    }

    private void contextPanel_DragEnter(object? sender, DragEventArgs e)
    {
        e.Effect = e.Data!.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
    }

    private void contextPanel_DragDrop(object? sender, DragEventArgs e)
    {
        var files = (string[])e.Data!.GetData(DataFormats.FileDrop)!;
        this.contextPanelLabel.Text = string.Join(", ", files);
    }

    // ------------------------------------------------------------- tab 9: advanced -----

    private void openDialogFormButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new DialogForm();
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            this.titleTextBox.Text = dialog.EnteredText;
        }
    }

    private void aboutButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new DialogForm();
        dialog.Text = "About";
        dialog.ShowDialog(this);
    }

    private void demoComponent1_Ticked(object? sender, EventArgs e)
    {
        this.advancedInfoLabel.Text = $"DemoComponent ticks: {this.demoComponent1.TickCount}";
    }

    private void tabControl1_SelectedIndexChanged(object? sender, EventArgs e)
    {
        this.statusInfoLabel.Text = this.tabControl1.SelectedTab?.Text ?? string.Empty;
    }

    // ------------------------------------------------------------------- helpers -------

    private void SetBusy(bool busy)
    {
        this.isBusy = busy;
        this.startWorkerButton.Text = busy ? "Cancel" : "Run BackgroundWorker";
        this.statusLabel.Text = busy ? "Working..." : "Ready";
    }

    private void Log(string message)
    {
        this.componentsLogTextBox.AppendText(message + Environment.NewLine);
    }

    private sealed class GalleryRow
    {
        public string Name { get; set; } = string.Empty;

        public bool Active { get; set; }

        public string Category { get; set; } = string.Empty;
    }
}

    */
}

#pragma warning restore CA1416
