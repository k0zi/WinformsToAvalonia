namespace AllInOneWinForms
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
            // -- menu / tool / status bars ------------------------------------------------
            this.menuStrip1 = new MenuStrip();
            this.fileMenuItem = new ToolStripMenuItem();
            this.newMenuItem = new ToolStripMenuItem();
            this.openMenuItem = new ToolStripMenuItem();
            this.fileMenuSeparator = new ToolStripSeparator();
            this.exitMenuItem = new ToolStripMenuItem();
            this.editMenuItem = new ToolStripMenuItem();
            this.undoMenuItem = new ToolStripMenuItem();
            this.redoMenuItem = new ToolStripMenuItem();
            this.wordWrapMenuItem = new ToolStripMenuItem();
            this.viewMenuItem = new ToolStripMenuItem();
            this.barsMenuItem = new ToolStripMenuItem();
            this.showToolStripMenuItem = new ToolStripMenuItem();
            this.showStatusStripMenuItem = new ToolStripMenuItem();
            this.helpMenuItem = new ToolStripMenuItem();
            this.aboutMenuItem = new ToolStripMenuItem();
            this.toolStrip1 = new ToolStrip();
            this.toolStripNewButton = new ToolStripButton();
            this.toolStripLabel1 = new ToolStripLabel();
            this.toolStripSeparator1 = new ToolStripSeparator();
            this.toolStripComboBox1 = new ToolStripComboBox();
            this.toolStripTextBox1 = new ToolStripTextBox();
            this.toolStripProgressBar1 = new ToolStripProgressBar();
            this.toolStripDropDownButton1 = new ToolStripDropDownButton();
            this.dropDownItemA = new ToolStripMenuItem();
            this.dropDownItemB = new ToolStripMenuItem();
            this.toolStripSplitButton1 = new ToolStripSplitButton();
            this.splitButtonItemA = new ToolStripMenuItem();
            this.hostedTrackBar = new TrackBar();
            this.toolStripControlHost1 = new ToolStripControlHost(this.hostedTrackBar);
            this.statusStrip1 = new StatusStrip();
            this.statusLabel = new ToolStripStatusLabel();
            this.statusInfoLabel = new ToolStripStatusLabel();
            this.statusProgressBar = new ToolStripProgressBar();
            // -- tab host -----------------------------------------------------------------
            this.tabControl1 = new TabControl();
            this.buttonsTabPage = new TabPage();
            this.listsTabPage = new TabPage();
            this.containersTabPage = new TabPage();
            this.dataTabPage = new TabPage();
            this.dateTabPage = new TabPage();
            this.graphicsTabPage = new TabPage();
            this.dialogsTabPage = new TabPage();
            this.componentsTabPage = new TabPage();
            this.advancedTabPage = new TabPage();
            // -- tab 1: buttons & text ----------------------------------------------------
            this.captionLabel = new Label();
            this.linkLabel1 = new LinkLabel();
            this.titleTextBox = new TextBox();
            this.phoneMaskedTextBox = new MaskedTextBox();
            this.notesRichTextBox = new RichTextBox();
            this.enabledCheckBox = new CheckBox();
            this.optionsGroupBox = new GroupBox();
            this.radioOption1 = new RadioButton();
            this.radioOption2 = new RadioButton();
            this.demoButton = new Button();
            this.applyButton = new Button();
            this.resetButton = new Button();
            this.sharedButtonA = new Button();
            this.sharedButtonB = new Button();
            this.validateButton = new Button();
            // -- tab 2: lists -------------------------------------------------------------
            this.itemsListBox = new ListBox();
            this.checkedListBox1 = new CheckedListBox();
            this.itemsComboBox = new ComboBox();
            this.domainUpDown1 = new DomainUpDown();
            this.amountUpDown = new NumericUpDown();
            this.refreshButton = new Button();
            this.itemsListView = new ListView();
            this.listViewNameColumn = new ColumnHeader();
            this.listViewSizeColumn = new ColumnHeader();
            this.itemsTreeView = new TreeView();
            this.propertyGrid1 = new PropertyGrid();
            // -- tab 3: containers --------------------------------------------------------
            this.plainPanel = new Panel();
            this.panelLabel = new Label();
            this.containerGroupBox = new GroupBox();
            this.groupBoxCheckBox = new CheckBox();
            this.flowLayoutPanel1 = new FlowLayoutPanel();
            this.flowButton1 = new Button();
            this.flowButton2 = new Button();
            this.tableLayoutPanel1 = new TableLayoutPanel();
            this.tableLabel1 = new Label();
            this.tableTextBox1 = new TextBox();
            this.splitContainer1 = new SplitContainer();
            this.splitLeftListBox = new ListBox();
            this.splitRightLabel = new Label();
            this.splitRightButton = new Button();
            this.splitter1 = new Splitter();
            this.innerTabControl = new TabControl();
            this.innerTabPage1 = new TabPage();
            this.innerTabPage2 = new TabPage();
            this.innerLabel = new Label();
            this.toolStripContainer1 = new ToolStripContainer();
            this.containerToolStrip = new ToolStrip();
            this.containerToolStripButton = new ToolStripButton();
            this.contentPanelLabel = new Label();
            this.toolStripPanel1 = new ToolStripPanel();
            this.toolStripContentPanel1 = new ToolStripContentPanel();
            // -- tab 4: data --------------------------------------------------------------
            this.dataGridView1 = new DataGridView();
            this.nameColumn = new DataGridViewTextBoxColumn();
            this.activeColumn = new DataGridViewCheckBoxColumn();
            this.categoryColumn = new DataGridViewComboBoxColumn();
            this.actionColumn = new DataGridViewButtonColumn();
            this.iconColumn = new DataGridViewImageColumn();
            this.linkColumn = new DataGridViewLinkColumn();
            this.bindingSource1 = new BindingSource(this.components);
            this.bindingNavigator1 = new BindingNavigator(this.components);
            this.navigatorMoveFirstButton = new ToolStripButton();
            this.navigatorMoveNextButton = new ToolStripButton();
            this.navigatorSeparator = new ToolStripSeparator();
            this.navigatorPositionLabel = new ToolStripLabel();
            this.gridInfoLabel = new Label();
            // -- tab 5: date & time -------------------------------------------------------
            this.dateTimePicker1 = new DateTimePicker();
            this.monthCalendar1 = new MonthCalendar();
            this.clockLabel = new Label();
            this.clockToggleButton = new Button();
            this.clockTimer = new System.Windows.Forms.Timer(this.components);
            // -- tab 6: graphics & range --------------------------------------------------
            this.pictureBox1 = new PictureBox();
            this.progressBar1 = new ProgressBar();
            this.trackBar1 = new TrackBar();
            this.hScrollBar1 = new HScrollBar();
            this.vScrollBar1 = new VScrollBar();
            this.graphicsInfoLabel = new Label();
            // -- tab 7: dialogs & printing ------------------------------------------------
            this.openFileButton = new Button();
            this.saveFileButton = new Button();
            this.folderBrowserButton = new Button();
            this.colorButton = new Button();
            this.fontButton = new Button();
            this.printButton = new Button();
            this.pageSetupButton = new Button();
            this.printPreviewButton = new Button();
            this.selectedPathLabel = new Label();
            this.printPreviewControl1 = new PrintPreviewControl();
            this.openFileDialog1 = new OpenFileDialog();
            this.saveFileDialog1 = new SaveFileDialog();
            this.folderBrowserDialog1 = new FolderBrowserDialog();
            this.colorDialog1 = new ColorDialog();
            this.fontDialog1 = new FontDialog();
            this.printDialog1 = new PrintDialog();
            this.pageSetupDialog1 = new PageSetupDialog();
            this.printPreviewDialog1 = new PrintPreviewDialog();
            this.printDocument1 = new System.Drawing.Printing.PrintDocument();
            // -- tab 8: components --------------------------------------------------------
            this.startWorkerButton = new Button();
            this.workerProgressBar = new ProgressBar();
            this.watchButton = new Button();
            this.watcherLabel = new Label();
            this.launchProcessButton = new Button();
            this.writeEventLogButton = new Button();
            this.readCounterButton = new Button();
            this.serviceStatusButton = new Button();
            this.serialOpenButton = new Button();
            this.playSoundButton = new Button();
            this.showBalloonButton = new Button();
            this.componentsLogTextBox = new TextBox();
            this.contextPanel = new Panel();
            this.contextPanelLabel = new Label();
            this.backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            this.fileSystemWatcher1 = new System.IO.FileSystemWatcher();
            this.process1 = new System.Diagnostics.Process();
            this.eventLog1 = new System.Diagnostics.EventLog();
            this.performanceCounter1 = new System.Diagnostics.PerformanceCounter();
            this.serviceController1 = new System.ServiceProcess.ServiceController();
            this.serialPort1 = new System.IO.Ports.SerialPort(this.components);
            this.soundPlayer1 = new System.Media.SoundPlayer();
            this.notifyIcon1 = new NotifyIcon(this.components);
            this.helpProvider1 = new HelpProvider();
            this.contextMenuStrip1 = new ContextMenuStrip(this.components);
            this.copyContextMenuItem = new ToolStripMenuItem();
            this.pasteContextMenuItem = new ToolStripMenuItem();
            this.contextMenuSeparator = new ToolStripSeparator();
            this.selectAllContextMenuItem = new ToolStripMenuItem();
            // -- tab 9: advanced ----------------------------------------------------------
            this.webBrowser1 = new WebBrowser();
            this.demoUserControl1 = new AllInOneWinForms.Controls.DemoUserControl();
            this.openDialogFormButton = new Button();
            this.aboutButton = new Button();
            this.advancedInfoLabel = new Label();
            this.demoComponent1 = new AllInOneWinForms.Components.DemoComponent(this.components);
            // -- form-wide extender components --------------------------------------------
            this.imageList1 = new ImageList(this.components);
            this.toolTip1 = new ToolTip(this.components);
            this.errorProvider1 = new ErrorProvider(this.components);
            this.menuStrip1.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.buttonsTabPage.SuspendLayout();
            this.optionsGroupBox.SuspendLayout();
            this.listsTabPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.amountUpDown)).BeginInit();
            this.containersTabPage.SuspendLayout();
            this.plainPanel.SuspendLayout();
            this.containerGroupBox.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.innerTabControl.SuspendLayout();
            this.innerTabPage1.SuspendLayout();
            this.toolStripContainer1.SuspendLayout();
            this.containerToolStrip.SuspendLayout();
            this.dataTabPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.bindingSource1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.bindingNavigator1)).BeginInit();
            this.bindingNavigator1.SuspendLayout();
            this.dateTabPage.SuspendLayout();
            this.graphicsTabPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar1)).BeginInit();
            this.dialogsTabPage.SuspendLayout();
            this.componentsTabPage.SuspendLayout();
            this.contextPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.fileSystemWatcher1)).BeginInit();
            this.contextMenuStrip1.SuspendLayout();
            this.advancedTabPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider1)).BeginInit();
            this.SuspendLayout();
            //
            // newMenuItem
            //
            this.newMenuItem.Name = "newMenuItem";
            this.newMenuItem.ShortcutKeys = ((Keys)((Keys.Control | Keys.N)));
            this.newMenuItem.Text = "&New";
            this.newMenuItem.Click += new EventHandler(this.newMenuItem_Click);
            //
            // openMenuItem
            //
            this.openMenuItem.Name = "openMenuItem";
            this.openMenuItem.Text = "&Open...";
            this.openMenuItem.Click += new EventHandler(this.openMenuItem_Click);
            //
            // fileMenuSeparator
            //
            this.fileMenuSeparator.Name = "fileMenuSeparator";
            //
            // exitMenuItem
            //
            this.exitMenuItem.Name = "exitMenuItem";
            this.exitMenuItem.Text = "E&xit";
            this.exitMenuItem.Click += new EventHandler(this.exitMenuItem_Click);
            //
            // fileMenuItem
            //
            this.fileMenuItem.DropDownItems.AddRange(new ToolStripItem[] {
                this.newMenuItem,
                this.openMenuItem,
                this.fileMenuSeparator,
                this.exitMenuItem});
            this.fileMenuItem.Name = "fileMenuItem";
            this.fileMenuItem.Text = "&File";
            //
            // undoMenuItem
            //
            this.undoMenuItem.Name = "undoMenuItem";
            this.undoMenuItem.Text = "&Undo";
            //
            // redoMenuItem
            //
            this.redoMenuItem.Name = "redoMenuItem";
            this.redoMenuItem.Text = "&Redo";
            //
            // wordWrapMenuItem
            //
            this.wordWrapMenuItem.Checked = true;
            this.wordWrapMenuItem.CheckOnClick = true;
            this.wordWrapMenuItem.Name = "wordWrapMenuItem";
            this.wordWrapMenuItem.Text = "&Word wrap";
            this.wordWrapMenuItem.Click += new EventHandler(this.wordWrapMenuItem_Click);
            //
            // editMenuItem
            //
            this.editMenuItem.DropDownItems.AddRange(new ToolStripItem[] {
                this.undoMenuItem,
                this.redoMenuItem,
                this.wordWrapMenuItem});
            this.editMenuItem.Name = "editMenuItem";
            this.editMenuItem.Text = "&Edit";
            //
            // showToolStripMenuItem
            //
            this.showToolStripMenuItem.Checked = true;
            this.showToolStripMenuItem.CheckOnClick = true;
            this.showToolStripMenuItem.Name = "showToolStripMenuItem";
            this.showToolStripMenuItem.Text = "Show &toolbar";
            //
            // showStatusStripMenuItem
            //
            this.showStatusStripMenuItem.Checked = true;
            this.showStatusStripMenuItem.CheckOnClick = true;
            this.showStatusStripMenuItem.Name = "showStatusStripMenuItem";
            this.showStatusStripMenuItem.Text = "Show &status bar";
            //
            // barsMenuItem
            //
            this.barsMenuItem.DropDownItems.AddRange(new ToolStripItem[] {
                this.showToolStripMenuItem,
                this.showStatusStripMenuItem});
            this.barsMenuItem.Name = "barsMenuItem";
            this.barsMenuItem.Text = "&Bars";
            //
            // viewMenuItem
            //
            this.viewMenuItem.DropDownItems.AddRange(new ToolStripItem[] {
                this.barsMenuItem});
            this.viewMenuItem.Name = "viewMenuItem";
            this.viewMenuItem.Text = "&View";
            //
            // aboutMenuItem
            //
            this.aboutMenuItem.Name = "aboutMenuItem";
            this.aboutMenuItem.Text = "&About...";
            this.aboutMenuItem.Click += new EventHandler(this.aboutMenuItem_Click);
            //
            // helpMenuItem
            //
            this.helpMenuItem.DropDownItems.AddRange(new ToolStripItem[] {
                this.aboutMenuItem});
            this.helpMenuItem.Name = "helpMenuItem";
            this.helpMenuItem.Text = "&Help";
            //
            // menuStrip1
            //
            this.menuStrip1.Items.AddRange(new ToolStripItem[] {
                this.fileMenuItem,
                this.editMenuItem,
                this.viewMenuItem,
                this.helpMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1024, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            //
            // toolStripNewButton
            //
            this.toolStripNewButton.Name = "toolStripNewButton";
            this.toolStripNewButton.Text = "New";
            this.toolStripNewButton.ToolTipText = "Create a new document";
            this.toolStripNewButton.Click += new EventHandler(this.toolStripNewButton_Click);
            //
            // toolStripLabel1
            //
            this.toolStripLabel1.Name = "toolStripLabel1";
            this.toolStripLabel1.Text = "Filter:";
            //
            // toolStripSeparator1
            //
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            //
            // toolStripComboBox1
            //
            this.toolStripComboBox1.Name = "toolStripComboBox1";
            this.toolStripComboBox1.Size = new System.Drawing.Size(140, 25);
            //
            // toolStripTextBox1
            //
            this.toolStripTextBox1.Name = "toolStripTextBox1";
            this.toolStripTextBox1.Size = new System.Drawing.Size(140, 25);
            //
            // toolStripProgressBar1
            //
            this.toolStripProgressBar1.Name = "toolStripProgressBar1";
            this.toolStripProgressBar1.Size = new System.Drawing.Size(100, 22);
            this.toolStripProgressBar1.Value = 35;
            //
            // dropDownItemA
            //
            this.dropDownItemA.Name = "dropDownItemA";
            this.dropDownItemA.Text = "Drop-down item A";
            //
            // dropDownItemB
            //
            this.dropDownItemB.Name = "dropDownItemB";
            this.dropDownItemB.Text = "Drop-down item B";
            //
            // toolStripDropDownButton1
            //
            this.toolStripDropDownButton1.DropDownItems.AddRange(new ToolStripItem[] {
                this.dropDownItemA,
                this.dropDownItemB});
            this.toolStripDropDownButton1.Name = "toolStripDropDownButton1";
            this.toolStripDropDownButton1.Text = "Layout";
            //
            // splitButtonItemA
            //
            this.splitButtonItemA.Name = "splitButtonItemA";
            this.splitButtonItemA.Text = "Split item A";
            //
            // toolStripSplitButton1
            //
            this.toolStripSplitButton1.DropDownItems.AddRange(new ToolStripItem[] {
                this.splitButtonItemA});
            this.toolStripSplitButton1.Name = "toolStripSplitButton1";
            this.toolStripSplitButton1.Text = "Run";
            //
            // hostedTrackBar
            //
            this.hostedTrackBar.AutoSize = false;
            this.hostedTrackBar.Maximum = 100;
            this.hostedTrackBar.Name = "hostedTrackBar";
            this.hostedTrackBar.Size = new System.Drawing.Size(100, 22);
            this.hostedTrackBar.TickStyle = TickStyle.None;
            //
            // toolStripControlHost1
            //
            this.toolStripControlHost1.Name = "toolStripControlHost1";
            this.toolStripControlHost1.Size = new System.Drawing.Size(100, 22);
            //
            // toolStrip1
            //
            this.toolStrip1.Items.AddRange(new ToolStripItem[] {
                this.toolStripNewButton,
                this.toolStripLabel1,
                this.toolStripSeparator1,
                this.toolStripComboBox1,
                this.toolStripTextBox1,
                this.toolStripProgressBar1,
                this.toolStripDropDownButton1,
                this.toolStripSplitButton1,
                this.toolStripControlHost1});
            this.toolStrip1.Location = new System.Drawing.Point(0, 24);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(1024, 25);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            //
            // statusLabel
            //
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Text = "Ready";
            //
            // statusInfoLabel
            //
            this.statusInfoLabel.Name = "statusInfoLabel";
            this.statusInfoLabel.Text = "All-In-One WinForms control gallery";
            //
            // statusProgressBar
            //
            this.statusProgressBar.Name = "statusProgressBar";
            this.statusProgressBar.Size = new System.Drawing.Size(100, 16);
            //
            // statusStrip1
            //
            this.statusStrip1.Items.AddRange(new ToolStripItem[] {
                this.statusLabel,
                this.statusInfoLabel,
                this.statusProgressBar});
            this.statusStrip1.Location = new System.Drawing.Point(0, 698);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(1024, 22);
            this.statusStrip1.TabIndex = 2;
            this.statusStrip1.Text = "statusStrip1";
            //
            // captionLabel
            //
            this.captionLabel.Location = new System.Drawing.Point(12, 12);
            this.captionLabel.Name = "captionLabel";
            this.captionLabel.Size = new System.Drawing.Size(220, 20);
            this.captionLabel.TabIndex = 0;
            this.captionLabel.Text = "Label - plain static text";
            //
            // linkLabel1
            //
            this.linkLabel1.Location = new System.Drawing.Point(12, 38);
            this.linkLabel1.Name = "linkLabel1";
            this.linkLabel1.Size = new System.Drawing.Size(220, 20);
            this.linkLabel1.TabIndex = 1;
            this.linkLabel1.TabStop = true;
            this.linkLabel1.Text = "LinkLabel - click me";
            this.linkLabel1.LinkClicked += new LinkLabelLinkClickedEventHandler(this.linkLabel1_LinkClicked);
            //
            // titleTextBox
            //
            this.titleTextBox.Location = new System.Drawing.Point(12, 64);
            this.titleTextBox.Name = "titleTextBox";
            this.titleTextBox.Size = new System.Drawing.Size(220, 23);
            this.titleTextBox.TabIndex = 2;
            this.titleTextBox.Text = "TextBox";
            //
            // phoneMaskedTextBox
            //
            this.phoneMaskedTextBox.Location = new System.Drawing.Point(12, 93);
            this.phoneMaskedTextBox.Mask = "(999) 000-0000";
            this.phoneMaskedTextBox.Name = "phoneMaskedTextBox";
            this.phoneMaskedTextBox.Size = new System.Drawing.Size(220, 23);
            this.phoneMaskedTextBox.TabIndex = 3;
            //
            // notesRichTextBox
            //
            this.notesRichTextBox.Location = new System.Drawing.Point(12, 122);
            this.notesRichTextBox.Name = "notesRichTextBox";
            this.notesRichTextBox.Size = new System.Drawing.Size(220, 100);
            this.notesRichTextBox.TabIndex = 4;
            this.notesRichTextBox.Text = "RichTextBox - formatted text";
            //
            // enabledCheckBox
            //
            this.enabledCheckBox.Checked = true;
            this.enabledCheckBox.CheckState = CheckState.Checked;
            this.enabledCheckBox.Location = new System.Drawing.Point(250, 12);
            this.enabledCheckBox.Name = "enabledCheckBox";
            this.enabledCheckBox.Size = new System.Drawing.Size(200, 24);
            this.enabledCheckBox.TabIndex = 5;
            this.enabledCheckBox.Text = "CheckBox - enabled";
            //
            // radioOption1
            //
            this.radioOption1.Checked = true;
            this.radioOption1.Location = new System.Drawing.Point(12, 22);
            this.radioOption1.Name = "radioOption1";
            this.radioOption1.Size = new System.Drawing.Size(170, 22);
            this.radioOption1.TabIndex = 0;
            this.radioOption1.TabStop = true;
            this.radioOption1.Text = "First option";
            //
            // radioOption2
            //
            this.radioOption2.Location = new System.Drawing.Point(12, 48);
            this.radioOption2.Name = "radioOption2";
            this.radioOption2.Size = new System.Drawing.Size(170, 22);
            this.radioOption2.TabIndex = 1;
            this.radioOption2.Text = "Second option";
            //
            // optionsGroupBox
            //
            this.optionsGroupBox.Controls.Add(this.radioOption1);
            this.optionsGroupBox.Controls.Add(this.radioOption2);
            this.optionsGroupBox.Location = new System.Drawing.Point(250, 42);
            this.optionsGroupBox.Name = "optionsGroupBox";
            this.optionsGroupBox.Size = new System.Drawing.Size(200, 90);
            this.optionsGroupBox.TabIndex = 6;
            this.optionsGroupBox.TabStop = false;
            this.optionsGroupBox.Text = "GroupBox - options";
            //
            // demoButton
            //
            this.demoButton.Location = new System.Drawing.Point(250, 144);
            this.demoButton.Name = "demoButton";
            this.demoButton.Size = new System.Drawing.Size(100, 28);
            this.demoButton.TabIndex = 7;
            this.demoButton.Text = "Say hello";
            this.demoButton.Click += new EventHandler(this.demoButton_Click);
            //
            // applyButton
            //
            this.applyButton.Location = new System.Drawing.Point(356, 144);
            this.applyButton.Name = "applyButton";
            this.applyButton.Size = new System.Drawing.Size(100, 28);
            this.applyButton.TabIndex = 8;
            this.applyButton.Text = "Apply";
            this.applyButton.Click += new EventHandler(this.applyButton_Click);
            //
            // resetButton
            //
            this.resetButton.Location = new System.Drawing.Point(250, 178);
            this.resetButton.Name = "resetButton";
            this.resetButton.Size = new System.Drawing.Size(100, 28);
            this.resetButton.TabIndex = 9;
            this.resetButton.Text = "Reset";
            this.resetButton.Click += new EventHandler(this.resetButton_Click);
            //
            // sharedButtonA
            //
            this.sharedButtonA.Location = new System.Drawing.Point(356, 178);
            this.sharedButtonA.Name = "sharedButtonA";
            this.sharedButtonA.Size = new System.Drawing.Size(100, 28);
            this.sharedButtonA.TabIndex = 10;
            this.sharedButtonA.Text = "Shared A";
            this.sharedButtonA.Click += new EventHandler(this.sharedButton_Click);
            //
            // sharedButtonB
            //
            this.sharedButtonB.Location = new System.Drawing.Point(250, 212);
            this.sharedButtonB.Name = "sharedButtonB";
            this.sharedButtonB.Size = new System.Drawing.Size(100, 28);
            this.sharedButtonB.TabIndex = 11;
            this.sharedButtonB.Text = "Shared B";
            this.sharedButtonB.Click += new EventHandler(this.sharedButton_Click);
            //
            // validateButton
            //
            this.validateButton.Location = new System.Drawing.Point(356, 212);
            this.validateButton.Name = "validateButton";
            this.validateButton.Size = new System.Drawing.Size(100, 28);
            this.validateButton.TabIndex = 12;
            this.validateButton.Text = "Validate";
            this.validateButton.Click += new EventHandler(this.validateButton_Click);
            //
            // buttonsTabPage
            //
            this.buttonsTabPage.Controls.Add(this.captionLabel);
            this.buttonsTabPage.Controls.Add(this.linkLabel1);
            this.buttonsTabPage.Controls.Add(this.titleTextBox);
            this.buttonsTabPage.Controls.Add(this.phoneMaskedTextBox);
            this.buttonsTabPage.Controls.Add(this.notesRichTextBox);
            this.buttonsTabPage.Controls.Add(this.enabledCheckBox);
            this.buttonsTabPage.Controls.Add(this.optionsGroupBox);
            this.buttonsTabPage.Controls.Add(this.demoButton);
            this.buttonsTabPage.Controls.Add(this.applyButton);
            this.buttonsTabPage.Controls.Add(this.resetButton);
            this.buttonsTabPage.Controls.Add(this.sharedButtonA);
            this.buttonsTabPage.Controls.Add(this.sharedButtonB);
            this.buttonsTabPage.Controls.Add(this.validateButton);
            this.buttonsTabPage.Location = new System.Drawing.Point(4, 24);
            this.buttonsTabPage.Name = "buttonsTabPage";
            this.buttonsTabPage.Size = new System.Drawing.Size(992, 602);
            this.buttonsTabPage.TabIndex = 0;
            this.buttonsTabPage.Text = "Buttons && Text";
            //
            // itemsListBox
            //
            this.itemsListBox.Items.AddRange(new object[] {
                "Alpha",
                "Beta",
                "Gamma"});
            this.itemsListBox.Location = new System.Drawing.Point(12, 12);
            this.itemsListBox.Name = "itemsListBox";
            this.itemsListBox.Size = new System.Drawing.Size(180, 124);
            this.itemsListBox.TabIndex = 0;
            this.itemsListBox.SelectedIndexChanged += new EventHandler(this.itemsListBox_SelectedIndexChanged);
            //
            // checkedListBox1
            //
            this.checkedListBox1.CheckOnClick = true;
            this.checkedListBox1.Items.AddRange(new object[] {
                "Logging",
                "Telemetry",
                "Auto-update"});
            this.checkedListBox1.Location = new System.Drawing.Point(204, 12);
            this.checkedListBox1.Name = "checkedListBox1";
            this.checkedListBox1.Size = new System.Drawing.Size(180, 124);
            this.checkedListBox1.TabIndex = 1;
            //
            // itemsComboBox
            //
            this.itemsComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            this.itemsComboBox.Items.AddRange(new object[] {
                "Small",
                "Medium",
                "Large"});
            this.itemsComboBox.Location = new System.Drawing.Point(396, 12);
            this.itemsComboBox.Name = "itemsComboBox";
            this.itemsComboBox.Size = new System.Drawing.Size(180, 23);
            this.itemsComboBox.TabIndex = 2;
            //
            // domainUpDown1
            //
            this.domainUpDown1.Items.AddRange(new object[] {
                "Monday",
                "Tuesday",
                "Wednesday"});
            this.domainUpDown1.Location = new System.Drawing.Point(396, 41);
            this.domainUpDown1.Name = "domainUpDown1";
            this.domainUpDown1.Size = new System.Drawing.Size(180, 23);
            this.domainUpDown1.TabIndex = 3;
            this.domainUpDown1.Text = "Monday";
            //
            // amountUpDown
            //
            this.amountUpDown.Location = new System.Drawing.Point(396, 70);
            this.amountUpDown.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            this.amountUpDown.Name = "amountUpDown";
            this.amountUpDown.Size = new System.Drawing.Size(180, 23);
            this.amountUpDown.TabIndex = 4;
            //
            // refreshButton
            //
            this.refreshButton.Location = new System.Drawing.Point(396, 99);
            this.refreshButton.Name = "refreshButton";
            this.refreshButton.Size = new System.Drawing.Size(180, 28);
            this.refreshButton.TabIndex = 5;
            this.refreshButton.Text = "Reload tree";
            this.refreshButton.Click += new EventHandler(this.refreshButton_Click);
            //
            // listViewNameColumn
            //
            this.listViewNameColumn.Text = "Name";
            this.listViewNameColumn.Width = 200;
            //
            // listViewSizeColumn
            //
            this.listViewSizeColumn.Text = "Size";
            this.listViewSizeColumn.Width = 100;
            //
            // itemsListView
            //
            this.itemsListView.Columns.AddRange(new ColumnHeader[] {
                this.listViewNameColumn,
                this.listViewSizeColumn});
            this.itemsListView.FullRowSelect = true;
            this.itemsListView.LargeImageList = this.imageList1;
            this.itemsListView.Location = new System.Drawing.Point(12, 142);
            this.itemsListView.Name = "itemsListView";
            this.itemsListView.Size = new System.Drawing.Size(372, 150);
            this.itemsListView.SmallImageList = this.imageList1;
            this.itemsListView.TabIndex = 6;
            this.itemsListView.View = View.Details;
            //
            // itemsTreeView
            //
            this.itemsTreeView.ImageList = this.imageList1;
            this.itemsTreeView.Location = new System.Drawing.Point(396, 142);
            this.itemsTreeView.Name = "itemsTreeView";
            this.itemsTreeView.Size = new System.Drawing.Size(180, 150);
            this.itemsTreeView.TabIndex = 7;
            //
            // propertyGrid1
            //
            this.propertyGrid1.Location = new System.Drawing.Point(588, 12);
            this.propertyGrid1.Name = "propertyGrid1";
            this.propertyGrid1.Size = new System.Drawing.Size(280, 280);
            this.propertyGrid1.TabIndex = 8;
            //
            // listsTabPage
            //
            this.listsTabPage.Controls.Add(this.itemsListBox);
            this.listsTabPage.Controls.Add(this.checkedListBox1);
            this.listsTabPage.Controls.Add(this.itemsComboBox);
            this.listsTabPage.Controls.Add(this.domainUpDown1);
            this.listsTabPage.Controls.Add(this.amountUpDown);
            this.listsTabPage.Controls.Add(this.refreshButton);
            this.listsTabPage.Controls.Add(this.itemsListView);
            this.listsTabPage.Controls.Add(this.itemsTreeView);
            this.listsTabPage.Controls.Add(this.propertyGrid1);
            this.listsTabPage.Location = new System.Drawing.Point(4, 24);
            this.listsTabPage.Name = "listsTabPage";
            this.listsTabPage.Size = new System.Drawing.Size(992, 602);
            this.listsTabPage.TabIndex = 1;
            this.listsTabPage.Text = "Lists";
            //
            // panelLabel
            //
            this.panelLabel.Location = new System.Drawing.Point(8, 8);
            this.panelLabel.Name = "panelLabel";
            this.panelLabel.Size = new System.Drawing.Size(190, 20);
            this.panelLabel.TabIndex = 0;
            this.panelLabel.Text = "Panel - free-form container";
            //
            // plainPanel
            //
            this.plainPanel.BorderStyle = BorderStyle.FixedSingle;
            this.plainPanel.Controls.Add(this.panelLabel);
            this.plainPanel.Location = new System.Drawing.Point(12, 12);
            this.plainPanel.Name = "plainPanel";
            this.plainPanel.Size = new System.Drawing.Size(220, 120);
            this.plainPanel.TabIndex = 0;
            //
            // groupBoxCheckBox
            //
            this.groupBoxCheckBox.Location = new System.Drawing.Point(12, 24);
            this.groupBoxCheckBox.Name = "groupBoxCheckBox";
            this.groupBoxCheckBox.Size = new System.Drawing.Size(190, 24);
            this.groupBoxCheckBox.TabIndex = 0;
            this.groupBoxCheckBox.Text = "Grouped check box";
            //
            // containerGroupBox
            //
            this.containerGroupBox.Controls.Add(this.groupBoxCheckBox);
            this.containerGroupBox.Location = new System.Drawing.Point(244, 12);
            this.containerGroupBox.Name = "containerGroupBox";
            this.containerGroupBox.Size = new System.Drawing.Size(220, 120);
            this.containerGroupBox.TabIndex = 1;
            this.containerGroupBox.TabStop = false;
            this.containerGroupBox.Text = "GroupBox";
            //
            // flowButton1
            //
            this.flowButton1.Location = new System.Drawing.Point(3, 3);
            this.flowButton1.Name = "flowButton1";
            this.flowButton1.Size = new System.Drawing.Size(96, 28);
            this.flowButton1.TabIndex = 0;
            this.flowButton1.Text = "Flow one";
            //
            // flowButton2
            //
            this.flowButton2.Location = new System.Drawing.Point(105, 3);
            this.flowButton2.Name = "flowButton2";
            this.flowButton2.Size = new System.Drawing.Size(96, 28);
            this.flowButton2.TabIndex = 1;
            this.flowButton2.Text = "Flow two";
            //
            // flowLayoutPanel1
            //
            this.flowLayoutPanel1.BorderStyle = BorderStyle.FixedSingle;
            this.flowLayoutPanel1.Controls.Add(this.flowButton1);
            this.flowLayoutPanel1.Controls.Add(this.flowButton2);
            this.flowLayoutPanel1.Location = new System.Drawing.Point(476, 12);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(220, 120);
            this.flowLayoutPanel1.TabIndex = 2;
            //
            // tableLabel1
            //
            this.tableLabel1.Location = new System.Drawing.Point(3, 6);
            this.tableLabel1.Name = "tableLabel1";
            this.tableLabel1.Size = new System.Drawing.Size(100, 20);
            this.tableLabel1.TabIndex = 0;
            this.tableLabel1.Text = "Cell 0,0";
            //
            // tableTextBox1
            //
            this.tableTextBox1.Location = new System.Drawing.Point(109, 3);
            this.tableTextBox1.Name = "tableTextBox1";
            this.tableTextBox1.Size = new System.Drawing.Size(120, 23);
            this.tableTextBox1.TabIndex = 1;
            this.tableTextBox1.Text = "Cell 1,0";
            //
            // tableLayoutPanel1
            //
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.Controls.Add(this.tableLabel1);
            this.tableLayoutPanel1.Controls.Add(this.tableTextBox1);
            this.tableLayoutPanel1.Location = new System.Drawing.Point(708, 12);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.Size = new System.Drawing.Size(240, 120);
            this.tableLayoutPanel1.TabIndex = 3;
            //
            // splitLeftListBox
            //
            this.splitLeftListBox.Location = new System.Drawing.Point(8, 8);
            this.splitLeftListBox.Name = "splitLeftListBox";
            this.splitLeftListBox.Size = new System.Drawing.Size(180, 154);
            this.splitLeftListBox.TabIndex = 0;
            //
            // splitRightLabel
            //
            this.splitRightLabel.Location = new System.Drawing.Point(8, 8);
            this.splitRightLabel.Name = "splitRightLabel";
            this.splitRightLabel.Size = new System.Drawing.Size(190, 20);
            this.splitRightLabel.TabIndex = 0;
            this.splitRightLabel.Text = "SplitContainer.Panel2";
            //
            // splitRightButton
            //
            this.splitRightButton.Location = new System.Drawing.Point(8, 34);
            this.splitRightButton.Name = "splitRightButton";
            this.splitRightButton.Size = new System.Drawing.Size(120, 28);
            this.splitRightButton.TabIndex = 1;
            this.splitRightButton.Text = "Right side";
            //
            // splitContainer1
            //
            this.splitContainer1.Location = new System.Drawing.Point(12, 144);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Panel1.Controls.Add(this.splitLeftListBox);
            this.splitContainer1.Panel2.Controls.Add(this.splitRightLabel);
            this.splitContainer1.Panel2.Controls.Add(this.splitRightButton);
            this.splitContainer1.Size = new System.Drawing.Size(440, 180);
            this.splitContainer1.SplitterDistance = 200;
            this.splitContainer1.TabIndex = 4;
            //
            // splitter1
            //
            this.splitter1.Location = new System.Drawing.Point(460, 144);
            this.splitter1.Name = "splitter1";
            this.splitter1.Size = new System.Drawing.Size(8, 180);
            this.splitter1.TabIndex = 5;
            this.splitter1.TabStop = false;
            //
            // innerLabel
            //
            this.innerLabel.Location = new System.Drawing.Point(8, 8);
            this.innerLabel.Name = "innerLabel";
            this.innerLabel.Size = new System.Drawing.Size(200, 20);
            this.innerLabel.TabIndex = 0;
            this.innerLabel.Text = "Nested tab content";
            //
            // innerTabPage1
            //
            this.innerTabPage1.Controls.Add(this.innerLabel);
            this.innerTabPage1.Location = new System.Drawing.Point(4, 24);
            this.innerTabPage1.Name = "innerTabPage1";
            this.innerTabPage1.Size = new System.Drawing.Size(232, 152);
            this.innerTabPage1.TabIndex = 0;
            this.innerTabPage1.Text = "Inner one";
            //
            // innerTabPage2
            //
            this.innerTabPage2.Location = new System.Drawing.Point(4, 24);
            this.innerTabPage2.Name = "innerTabPage2";
            this.innerTabPage2.Size = new System.Drawing.Size(232, 152);
            this.innerTabPage2.TabIndex = 1;
            this.innerTabPage2.Text = "Inner two";
            //
            // innerTabControl
            //
            this.innerTabControl.Controls.Add(this.innerTabPage1);
            this.innerTabControl.Controls.Add(this.innerTabPage2);
            this.innerTabControl.Location = new System.Drawing.Point(476, 144);
            this.innerTabControl.Name = "innerTabControl";
            this.innerTabControl.SelectedIndex = 0;
            this.innerTabControl.Size = new System.Drawing.Size(240, 180);
            this.innerTabControl.TabIndex = 6;
            //
            // toolStripPanel1
            //
            this.toolStripPanel1.Location = new System.Drawing.Point(724, 144);
            this.toolStripPanel1.Name = "toolStripPanel1";
            this.toolStripPanel1.Size = new System.Drawing.Size(224, 86);
            this.toolStripPanel1.TabIndex = 7;
            //
            // toolStripContentPanel1
            //
            this.toolStripContentPanel1.Location = new System.Drawing.Point(724, 238);
            this.toolStripContentPanel1.Name = "toolStripContentPanel1";
            this.toolStripContentPanel1.Size = new System.Drawing.Size(224, 86);
            this.toolStripContentPanel1.TabIndex = 8;
            //
            // containerToolStripButton
            //
            this.containerToolStripButton.Name = "containerToolStripButton";
            this.containerToolStripButton.Text = "Docked tool";
            //
            // containerToolStrip
            //
            this.containerToolStrip.Items.AddRange(new ToolStripItem[] {
                this.containerToolStripButton});
            this.containerToolStrip.Location = new System.Drawing.Point(0, 0);
            this.containerToolStrip.Name = "containerToolStrip";
            this.containerToolStrip.Size = new System.Drawing.Size(936, 25);
            this.containerToolStrip.TabIndex = 0;
            //
            // contentPanelLabel
            //
            this.contentPanelLabel.Location = new System.Drawing.Point(8, 8);
            this.contentPanelLabel.Name = "contentPanelLabel";
            this.contentPanelLabel.Size = new System.Drawing.Size(400, 20);
            this.contentPanelLabel.TabIndex = 0;
            this.contentPanelLabel.Text = "ToolStripContainer.ContentPanel";
            //
            // toolStripContainer1
            //
            this.toolStripContainer1.ContentPanel.Controls.Add(this.contentPanelLabel);
            this.toolStripContainer1.ContentPanel.Size = new System.Drawing.Size(936, 195);
            this.toolStripContainer1.Location = new System.Drawing.Point(12, 336);
            this.toolStripContainer1.Name = "toolStripContainer1";
            this.toolStripContainer1.Size = new System.Drawing.Size(936, 220);
            this.toolStripContainer1.TabIndex = 9;
            this.toolStripContainer1.TopToolStripPanel.Controls.Add(this.containerToolStrip);
            //
            // containersTabPage
            //
            this.containersTabPage.Controls.Add(this.plainPanel);
            this.containersTabPage.Controls.Add(this.containerGroupBox);
            this.containersTabPage.Controls.Add(this.flowLayoutPanel1);
            this.containersTabPage.Controls.Add(this.tableLayoutPanel1);
            this.containersTabPage.Controls.Add(this.splitContainer1);
            this.containersTabPage.Controls.Add(this.splitter1);
            this.containersTabPage.Controls.Add(this.innerTabControl);
            this.containersTabPage.Controls.Add(this.toolStripPanel1);
            this.containersTabPage.Controls.Add(this.toolStripContentPanel1);
            this.containersTabPage.Controls.Add(this.toolStripContainer1);
            this.containersTabPage.Location = new System.Drawing.Point(4, 24);
            this.containersTabPage.Name = "containersTabPage";
            this.containersTabPage.Size = new System.Drawing.Size(992, 602);
            this.containersTabPage.TabIndex = 2;
            this.containersTabPage.Text = "Containers";
            //
            // nameColumn
            //
            this.nameColumn.DataPropertyName = "Name";
            this.nameColumn.HeaderText = "Name";
            this.nameColumn.Name = "nameColumn";
            //
            // activeColumn
            //
            this.activeColumn.DataPropertyName = "Active";
            this.activeColumn.HeaderText = "Active";
            this.activeColumn.Name = "activeColumn";
            //
            // categoryColumn
            //
            this.categoryColumn.DataPropertyName = "Category";
            this.categoryColumn.HeaderText = "Category";
            this.categoryColumn.Name = "categoryColumn";
            //
            // actionColumn
            //
            this.actionColumn.HeaderText = "Action";
            this.actionColumn.Name = "actionColumn";
            this.actionColumn.Text = "Run";
            //
            // iconColumn
            //
            this.iconColumn.HeaderText = "Icon";
            this.iconColumn.Name = "iconColumn";
            //
            // linkColumn
            //
            this.linkColumn.HeaderText = "Details";
            this.linkColumn.Name = "linkColumn";
            //
            // dataGridView1
            //
            this.dataGridView1.AutoGenerateColumns = false;
            this.dataGridView1.Columns.AddRange(new DataGridViewColumn[] {
                this.nameColumn,
                this.activeColumn,
                this.categoryColumn,
                this.actionColumn,
                this.iconColumn,
                this.linkColumn});
            this.dataGridView1.DataSource = this.bindingSource1;
            this.dataGridView1.Location = new System.Drawing.Point(12, 12);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.Size = new System.Drawing.Size(940, 300);
            this.dataGridView1.TabIndex = 0;
            this.dataGridView1.CellClick += new DataGridViewCellEventHandler(this.dataGridView1_CellClick);
            //
            // navigatorMoveFirstButton
            //
            this.navigatorMoveFirstButton.Name = "navigatorMoveFirstButton";
            this.navigatorMoveFirstButton.Text = "|<";
            //
            // navigatorMoveNextButton
            //
            this.navigatorMoveNextButton.Name = "navigatorMoveNextButton";
            this.navigatorMoveNextButton.Text = ">";
            //
            // navigatorSeparator
            //
            this.navigatorSeparator.Name = "navigatorSeparator";
            //
            // navigatorPositionLabel
            //
            this.navigatorPositionLabel.Name = "navigatorPositionLabel";
            this.navigatorPositionLabel.Text = "of 0";
            //
            // bindingNavigator1
            //
            this.bindingNavigator1.BindingSource = this.bindingSource1;
            this.bindingNavigator1.Items.AddRange(new ToolStripItem[] {
                this.navigatorMoveFirstButton,
                this.navigatorMoveNextButton,
                this.navigatorSeparator,
                this.navigatorPositionLabel});
            this.bindingNavigator1.Location = new System.Drawing.Point(12, 320);
            this.bindingNavigator1.MoveFirstItem = this.navigatorMoveFirstButton;
            this.bindingNavigator1.MoveNextItem = this.navigatorMoveNextButton;
            this.bindingNavigator1.Name = "bindingNavigator1";
            this.bindingNavigator1.Size = new System.Drawing.Size(940, 25);
            this.bindingNavigator1.TabIndex = 1;
            //
            // gridInfoLabel
            //
            this.gridInfoLabel.Location = new System.Drawing.Point(12, 356);
            this.gridInfoLabel.Name = "gridInfoLabel";
            this.gridInfoLabel.Size = new System.Drawing.Size(940, 20);
            this.gridInfoLabel.TabIndex = 2;
            this.gridInfoLabel.Text = "DataGridView bound to a BindingSource, driven by a BindingNavigator.";
            //
            // dataTabPage
            //
            this.dataTabPage.Controls.Add(this.dataGridView1);
            this.dataTabPage.Controls.Add(this.bindingNavigator1);
            this.dataTabPage.Controls.Add(this.gridInfoLabel);
            this.dataTabPage.Location = new System.Drawing.Point(4, 24);
            this.dataTabPage.Name = "dataTabPage";
            this.dataTabPage.Size = new System.Drawing.Size(992, 602);
            this.dataTabPage.TabIndex = 3;
            this.dataTabPage.Text = "Data";
            //
            // dateTimePicker1
            //
            this.dateTimePicker1.Location = new System.Drawing.Point(12, 12);
            this.dateTimePicker1.Name = "dateTimePicker1";
            this.dateTimePicker1.Size = new System.Drawing.Size(240, 23);
            this.dateTimePicker1.TabIndex = 0;
            //
            // monthCalendar1
            //
            this.monthCalendar1.Location = new System.Drawing.Point(12, 44);
            this.monthCalendar1.Name = "monthCalendar1";
            this.monthCalendar1.Size = new System.Drawing.Size(240, 170);
            this.monthCalendar1.TabIndex = 1;
            //
            // clockLabel
            //
            this.clockLabel.Location = new System.Drawing.Point(270, 14);
            this.clockLabel.Name = "clockLabel";
            this.clockLabel.Size = new System.Drawing.Size(240, 20);
            this.clockLabel.TabIndex = 2;
            this.clockLabel.Text = "Clock stopped";
            //
            // clockToggleButton
            //
            this.clockToggleButton.Location = new System.Drawing.Point(270, 44);
            this.clockToggleButton.Name = "clockToggleButton";
            this.clockToggleButton.Size = new System.Drawing.Size(140, 28);
            this.clockToggleButton.TabIndex = 3;
            this.clockToggleButton.Text = "Start clock";
            this.clockToggleButton.Click += new EventHandler(this.clockToggleButton_Click);
            //
            // clockTimer
            //
            this.clockTimer.Interval = 1000;
            this.clockTimer.Tick += new EventHandler(this.clockTimer_Tick);
            //
            // dateTabPage
            //
            this.dateTabPage.Controls.Add(this.dateTimePicker1);
            this.dateTabPage.Controls.Add(this.monthCalendar1);
            this.dateTabPage.Controls.Add(this.clockLabel);
            this.dateTabPage.Controls.Add(this.clockToggleButton);
            this.dateTabPage.Location = new System.Drawing.Point(4, 24);
            this.dateTabPage.Name = "dateTabPage";
            this.dateTabPage.Size = new System.Drawing.Size(992, 602);
            this.dateTabPage.TabIndex = 4;
            this.dateTabPage.Text = "Date && Time";
            //
            // pictureBox1
            //
            this.pictureBox1.BorderStyle = BorderStyle.FixedSingle;
            this.pictureBox1.Location = new System.Drawing.Point(12, 12);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(240, 160);
            this.pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            this.pictureBox1.Click += new EventHandler(this.pictureBox1_Click);
            this.pictureBox1.MouseDown += new MouseEventHandler(this.pictureBox1_MouseDown);
            this.pictureBox1.Paint += new PaintEventHandler(this.pictureBox1_Paint);
            //
            // progressBar1
            //
            this.progressBar1.Location = new System.Drawing.Point(270, 12);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(240, 23);
            this.progressBar1.TabIndex = 1;
            this.progressBar1.Value = 40;
            //
            // trackBar1
            //
            this.trackBar1.Location = new System.Drawing.Point(270, 41);
            this.trackBar1.Maximum = 100;
            this.trackBar1.Name = "trackBar1";
            this.trackBar1.Size = new System.Drawing.Size(240, 45);
            this.trackBar1.TabIndex = 2;
            this.trackBar1.Value = 40;
            this.trackBar1.Scroll += new EventHandler(this.trackBar1_Scroll);
            //
            // hScrollBar1
            //
            this.hScrollBar1.Location = new System.Drawing.Point(270, 92);
            this.hScrollBar1.Name = "hScrollBar1";
            this.hScrollBar1.Size = new System.Drawing.Size(240, 17);
            this.hScrollBar1.TabIndex = 3;
            this.hScrollBar1.Scroll += new ScrollEventHandler(this.hScrollBar1_Scroll);
            //
            // vScrollBar1
            //
            this.vScrollBar1.Location = new System.Drawing.Point(530, 12);
            this.vScrollBar1.Name = "vScrollBar1";
            this.vScrollBar1.Size = new System.Drawing.Size(17, 160);
            this.vScrollBar1.TabIndex = 4;
            //
            // graphicsInfoLabel
            //
            this.graphicsInfoLabel.Location = new System.Drawing.Point(270, 120);
            this.graphicsInfoLabel.Name = "graphicsInfoLabel";
            this.graphicsInfoLabel.Size = new System.Drawing.Size(240, 20);
            this.graphicsInfoLabel.TabIndex = 5;
            this.graphicsInfoLabel.Text = "TrackBar value: 40";
            //
            // graphicsTabPage
            //
            this.graphicsTabPage.Controls.Add(this.pictureBox1);
            this.graphicsTabPage.Controls.Add(this.progressBar1);
            this.graphicsTabPage.Controls.Add(this.trackBar1);
            this.graphicsTabPage.Controls.Add(this.hScrollBar1);
            this.graphicsTabPage.Controls.Add(this.vScrollBar1);
            this.graphicsTabPage.Controls.Add(this.graphicsInfoLabel);
            this.graphicsTabPage.Location = new System.Drawing.Point(4, 24);
            this.graphicsTabPage.Name = "graphicsTabPage";
            this.graphicsTabPage.Size = new System.Drawing.Size(992, 602);
            this.graphicsTabPage.TabIndex = 5;
            this.graphicsTabPage.Text = "Graphics && Range";
            //
            // openFileButton
            //
            this.openFileButton.Location = new System.Drawing.Point(12, 12);
            this.openFileButton.Name = "openFileButton";
            this.openFileButton.Size = new System.Drawing.Size(180, 28);
            this.openFileButton.TabIndex = 0;
            this.openFileButton.Text = "OpenFileDialog...";
            this.openFileButton.Click += new EventHandler(this.openFileButton_Click);
            //
            // saveFileButton
            //
            this.saveFileButton.Location = new System.Drawing.Point(12, 46);
            this.saveFileButton.Name = "saveFileButton";
            this.saveFileButton.Size = new System.Drawing.Size(180, 28);
            this.saveFileButton.TabIndex = 1;
            this.saveFileButton.Text = "SaveFileDialog...";
            this.saveFileButton.Click += new EventHandler(this.saveFileButton_Click);
            //
            // folderBrowserButton
            //
            this.folderBrowserButton.Location = new System.Drawing.Point(12, 80);
            this.folderBrowserButton.Name = "folderBrowserButton";
            this.folderBrowserButton.Size = new System.Drawing.Size(180, 28);
            this.folderBrowserButton.TabIndex = 2;
            this.folderBrowserButton.Text = "FolderBrowserDialog...";
            this.folderBrowserButton.Click += new EventHandler(this.folderBrowserButton_Click);
            //
            // colorButton
            //
            this.colorButton.Location = new System.Drawing.Point(12, 114);
            this.colorButton.Name = "colorButton";
            this.colorButton.Size = new System.Drawing.Size(180, 28);
            this.colorButton.TabIndex = 3;
            this.colorButton.Text = "ColorDialog...";
            this.colorButton.Click += new EventHandler(this.colorButton_Click);
            //
            // fontButton
            //
            this.fontButton.Location = new System.Drawing.Point(12, 148);
            this.fontButton.Name = "fontButton";
            this.fontButton.Size = new System.Drawing.Size(180, 28);
            this.fontButton.TabIndex = 4;
            this.fontButton.Text = "FontDialog...";
            this.fontButton.Click += new EventHandler(this.fontButton_Click);
            //
            // printButton
            //
            this.printButton.Location = new System.Drawing.Point(12, 182);
            this.printButton.Name = "printButton";
            this.printButton.Size = new System.Drawing.Size(180, 28);
            this.printButton.TabIndex = 5;
            this.printButton.Text = "PrintDialog...";
            this.printButton.Click += new EventHandler(this.printButton_Click);
            //
            // pageSetupButton
            //
            this.pageSetupButton.Location = new System.Drawing.Point(12, 216);
            this.pageSetupButton.Name = "pageSetupButton";
            this.pageSetupButton.Size = new System.Drawing.Size(180, 28);
            this.pageSetupButton.TabIndex = 6;
            this.pageSetupButton.Text = "PageSetupDialog...";
            this.pageSetupButton.Click += new EventHandler(this.pageSetupButton_Click);
            //
            // printPreviewButton
            //
            this.printPreviewButton.Location = new System.Drawing.Point(12, 250);
            this.printPreviewButton.Name = "printPreviewButton";
            this.printPreviewButton.Size = new System.Drawing.Size(180, 28);
            this.printPreviewButton.TabIndex = 7;
            this.printPreviewButton.Text = "PrintPreviewDialog...";
            this.printPreviewButton.Click += new EventHandler(this.printPreviewButton_Click);
            //
            // selectedPathLabel
            //
            this.selectedPathLabel.Location = new System.Drawing.Point(12, 288);
            this.selectedPathLabel.Name = "selectedPathLabel";
            this.selectedPathLabel.Size = new System.Drawing.Size(180, 40);
            this.selectedPathLabel.TabIndex = 8;
            this.selectedPathLabel.Text = "No file selected";
            //
            // printPreviewControl1
            //
            this.printPreviewControl1.Document = this.printDocument1;
            this.printPreviewControl1.Location = new System.Drawing.Point(204, 12);
            this.printPreviewControl1.Name = "printPreviewControl1";
            this.printPreviewControl1.Size = new System.Drawing.Size(600, 400);
            this.printPreviewControl1.TabIndex = 9;
            //
            // openFileDialog1
            //
            this.openFileDialog1.Filter = "All files (*.*)|*.*";
            this.openFileDialog1.Title = "Pick a file";
            //
            // saveFileDialog1
            //
            this.saveFileDialog1.Filter = "Text files (*.txt)|*.txt";
            this.saveFileDialog1.Title = "Save as";
            //
            // folderBrowserDialog1
            //
            this.folderBrowserDialog1.Description = "Pick a folder";
            //
            // fontDialog1
            //
            this.fontDialog1.ShowColor = true;
            //
            // printDialog1
            //
            this.printDialog1.Document = this.printDocument1;
            this.printDialog1.UseEXDialog = true;
            //
            // pageSetupDialog1
            //
            this.pageSetupDialog1.Document = this.printDocument1;
            //
            // printPreviewDialog1
            //
            this.printPreviewDialog1.ClientSize = new System.Drawing.Size(400, 300);
            this.printPreviewDialog1.Document = this.printDocument1;
            this.printPreviewDialog1.Name = "printPreviewDialog1";
            this.printPreviewDialog1.Text = "Print preview";
            //
            // printDocument1
            //
            this.printDocument1.DocumentName = "All-In-One sample";
            this.printDocument1.PrintPage += new System.Drawing.Printing.PrintPageEventHandler(this.printDocument1_PrintPage);
            //
            // dialogsTabPage
            //
            this.dialogsTabPage.Controls.Add(this.openFileButton);
            this.dialogsTabPage.Controls.Add(this.saveFileButton);
            this.dialogsTabPage.Controls.Add(this.folderBrowserButton);
            this.dialogsTabPage.Controls.Add(this.colorButton);
            this.dialogsTabPage.Controls.Add(this.fontButton);
            this.dialogsTabPage.Controls.Add(this.printButton);
            this.dialogsTabPage.Controls.Add(this.pageSetupButton);
            this.dialogsTabPage.Controls.Add(this.printPreviewButton);
            this.dialogsTabPage.Controls.Add(this.selectedPathLabel);
            this.dialogsTabPage.Controls.Add(this.printPreviewControl1);
            this.dialogsTabPage.Location = new System.Drawing.Point(4, 24);
            this.dialogsTabPage.Name = "dialogsTabPage";
            this.dialogsTabPage.Size = new System.Drawing.Size(992, 602);
            this.dialogsTabPage.TabIndex = 6;
            this.dialogsTabPage.Text = "Dialogs && Printing";
            //
            // startWorkerButton
            //
            this.startWorkerButton.Location = new System.Drawing.Point(12, 12);
            this.startWorkerButton.Name = "startWorkerButton";
            this.startWorkerButton.Size = new System.Drawing.Size(160, 28);
            this.startWorkerButton.TabIndex = 0;
            this.startWorkerButton.Text = "Run BackgroundWorker";
            this.startWorkerButton.Click += new EventHandler(this.startWorkerButton_Click);
            //
            // workerProgressBar
            //
            this.workerProgressBar.Location = new System.Drawing.Point(178, 15);
            this.workerProgressBar.Name = "workerProgressBar";
            this.workerProgressBar.Size = new System.Drawing.Size(300, 23);
            this.workerProgressBar.TabIndex = 1;
            //
            // watchButton
            //
            this.watchButton.Location = new System.Drawing.Point(12, 46);
            this.watchButton.Name = "watchButton";
            this.watchButton.Size = new System.Drawing.Size(160, 28);
            this.watchButton.TabIndex = 2;
            this.watchButton.Text = "Watch temp folder";
            this.watchButton.Click += new EventHandler(this.watchButton_Click);
            //
            // watcherLabel
            //
            this.watcherLabel.Location = new System.Drawing.Point(496, 18);
            this.watcherLabel.Name = "watcherLabel";
            this.watcherLabel.Size = new System.Drawing.Size(300, 20);
            this.watcherLabel.TabIndex = 3;
            this.watcherLabel.Text = "Watcher idle";
            //
            // launchProcessButton
            //
            this.launchProcessButton.Location = new System.Drawing.Point(12, 80);
            this.launchProcessButton.Name = "launchProcessButton";
            this.launchProcessButton.Size = new System.Drawing.Size(160, 28);
            this.launchProcessButton.TabIndex = 4;
            this.launchProcessButton.Text = "Start Notepad";
            this.launchProcessButton.Click += new EventHandler(this.launchProcessButton_Click);
            //
            // writeEventLogButton
            //
            this.writeEventLogButton.Location = new System.Drawing.Point(12, 114);
            this.writeEventLogButton.Name = "writeEventLogButton";
            this.writeEventLogButton.Size = new System.Drawing.Size(160, 28);
            this.writeEventLogButton.TabIndex = 5;
            this.writeEventLogButton.Text = "Write event log entry";
            this.writeEventLogButton.Click += new EventHandler(this.writeEventLogButton_Click);
            //
            // readCounterButton
            //
            this.readCounterButton.Location = new System.Drawing.Point(12, 148);
            this.readCounterButton.Name = "readCounterButton";
            this.readCounterButton.Size = new System.Drawing.Size(160, 28);
            this.readCounterButton.TabIndex = 6;
            this.readCounterButton.Text = "Read CPU counter";
            this.readCounterButton.Click += new EventHandler(this.readCounterButton_Click);
            //
            // serviceStatusButton
            //
            this.serviceStatusButton.Location = new System.Drawing.Point(12, 182);
            this.serviceStatusButton.Name = "serviceStatusButton";
            this.serviceStatusButton.Size = new System.Drawing.Size(160, 28);
            this.serviceStatusButton.TabIndex = 7;
            this.serviceStatusButton.Text = "Query spooler service";
            this.serviceStatusButton.Click += new EventHandler(this.serviceStatusButton_Click);
            //
            // serialOpenButton
            //
            this.serialOpenButton.Location = new System.Drawing.Point(12, 216);
            this.serialOpenButton.Name = "serialOpenButton";
            this.serialOpenButton.Size = new System.Drawing.Size(160, 28);
            this.serialOpenButton.TabIndex = 8;
            this.serialOpenButton.Text = "Open COM1";
            this.serialOpenButton.Click += new EventHandler(this.serialOpenButton_Click);
            //
            // playSoundButton
            //
            this.playSoundButton.Location = new System.Drawing.Point(12, 250);
            this.playSoundButton.Name = "playSoundButton";
            this.playSoundButton.Size = new System.Drawing.Size(160, 28);
            this.playSoundButton.TabIndex = 9;
            this.playSoundButton.Text = "Play system sound";
            this.playSoundButton.Click += new EventHandler(this.playSoundButton_Click);
            //
            // showBalloonButton
            //
            this.showBalloonButton.Location = new System.Drawing.Point(12, 284);
            this.showBalloonButton.Name = "showBalloonButton";
            this.showBalloonButton.Size = new System.Drawing.Size(160, 28);
            this.showBalloonButton.TabIndex = 10;
            this.showBalloonButton.Text = "Show tray balloon";
            this.showBalloonButton.Click += new EventHandler(this.showBalloonButton_Click);
            //
            // componentsLogTextBox
            //
            this.componentsLogTextBox.Location = new System.Drawing.Point(178, 46);
            this.componentsLogTextBox.Multiline = true;
            this.componentsLogTextBox.Name = "componentsLogTextBox";
            this.componentsLogTextBox.ReadOnly = true;
            this.componentsLogTextBox.ScrollBars = ScrollBars.Vertical;
            this.componentsLogTextBox.Size = new System.Drawing.Size(618, 266);
            this.componentsLogTextBox.TabIndex = 11;
            //
            // contextPanelLabel
            //
            this.contextPanelLabel.Location = new System.Drawing.Point(8, 8);
            this.contextPanelLabel.Name = "contextPanelLabel";
            this.contextPanelLabel.Size = new System.Drawing.Size(600, 20);
            this.contextPanelLabel.TabIndex = 0;
            this.contextPanelLabel.Text = "Right-click for a ContextMenuStrip, or drop a file here.";
            //
            // contextPanel
            //
            this.contextPanel.AllowDrop = true;
            this.contextPanel.BorderStyle = BorderStyle.FixedSingle;
            this.contextPanel.ContextMenuStrip = this.contextMenuStrip1;
            this.contextPanel.Controls.Add(this.contextPanelLabel);
            this.contextPanel.Location = new System.Drawing.Point(12, 320);
            this.contextPanel.Name = "contextPanel";
            this.contextPanel.Size = new System.Drawing.Size(784, 60);
            this.contextPanel.TabIndex = 12;
            this.contextPanel.DragDrop += new DragEventHandler(this.contextPanel_DragDrop);
            this.contextPanel.DragEnter += new DragEventHandler(this.contextPanel_DragEnter);
            //
            // backgroundWorker1
            //
            this.backgroundWorker1.WorkerReportsProgress = true;
            this.backgroundWorker1.WorkerSupportsCancellation = true;
            this.backgroundWorker1.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorker1_DoWork);
            this.backgroundWorker1.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.backgroundWorker1_ProgressChanged);
            this.backgroundWorker1.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.backgroundWorker1_RunWorkerCompleted);
            //
            // fileSystemWatcher1
            //
            this.fileSystemWatcher1.EnableRaisingEvents = false;
            this.fileSystemWatcher1.Filter = "*.txt";
            this.fileSystemWatcher1.SynchronizingObject = this;
            this.fileSystemWatcher1.Changed += new System.IO.FileSystemEventHandler(this.fileSystemWatcher1_Changed);
            //
            // eventLog1
            //
            this.eventLog1.Log = "Application";
            this.eventLog1.Source = "AllInOneWinForms";
            //
            // performanceCounter1
            //
            this.performanceCounter1.CategoryName = "Processor";
            this.performanceCounter1.CounterName = "% Processor Time";
            this.performanceCounter1.InstanceName = "_Total";
            //
            // serviceController1
            //
            this.serviceController1.ServiceName = "Spooler";
            //
            // serialPort1
            //
            this.serialPort1.BaudRate = 115200;
            this.serialPort1.PortName = "COM1";
            //
            // notifyIcon1
            //
            this.notifyIcon1.BalloonTipText = "All-In-One WinForms sample is running.";
            this.notifyIcon1.BalloonTipTitle = "All-In-One";
            this.notifyIcon1.Text = "All-In-One WinForms";
            this.notifyIcon1.Visible = true;
            this.notifyIcon1.DoubleClick += new EventHandler(this.notifyIcon1_DoubleClick);
            //
            // copyContextMenuItem
            //
            this.copyContextMenuItem.Name = "copyContextMenuItem";
            this.copyContextMenuItem.Text = "&Copy";
            this.copyContextMenuItem.Click += new EventHandler(this.copyContextMenuItem_Click);
            //
            // pasteContextMenuItem
            //
            this.pasteContextMenuItem.Name = "pasteContextMenuItem";
            this.pasteContextMenuItem.Text = "&Paste";
            //
            // contextMenuSeparator
            //
            this.contextMenuSeparator.Name = "contextMenuSeparator";
            //
            // selectAllContextMenuItem
            //
            this.selectAllContextMenuItem.Name = "selectAllContextMenuItem";
            this.selectAllContextMenuItem.Text = "Select &all";
            //
            // contextMenuStrip1
            //
            this.contextMenuStrip1.Items.AddRange(new ToolStripItem[] {
                this.copyContextMenuItem,
                this.pasteContextMenuItem,
                this.contextMenuSeparator,
                this.selectAllContextMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(140, 76);
            //
            // componentsTabPage
            //
            this.componentsTabPage.Controls.Add(this.startWorkerButton);
            this.componentsTabPage.Controls.Add(this.workerProgressBar);
            this.componentsTabPage.Controls.Add(this.watchButton);
            this.componentsTabPage.Controls.Add(this.watcherLabel);
            this.componentsTabPage.Controls.Add(this.launchProcessButton);
            this.componentsTabPage.Controls.Add(this.writeEventLogButton);
            this.componentsTabPage.Controls.Add(this.readCounterButton);
            this.componentsTabPage.Controls.Add(this.serviceStatusButton);
            this.componentsTabPage.Controls.Add(this.serialOpenButton);
            this.componentsTabPage.Controls.Add(this.playSoundButton);
            this.componentsTabPage.Controls.Add(this.showBalloonButton);
            this.componentsTabPage.Controls.Add(this.componentsLogTextBox);
            this.componentsTabPage.Controls.Add(this.contextPanel);
            this.componentsTabPage.Location = new System.Drawing.Point(4, 24);
            this.componentsTabPage.Name = "componentsTabPage";
            this.componentsTabPage.Size = new System.Drawing.Size(992, 602);
            this.componentsTabPage.TabIndex = 7;
            this.componentsTabPage.Text = "Components";
            //
            // webBrowser1
            //
            this.webBrowser1.Location = new System.Drawing.Point(12, 12);
            this.webBrowser1.Name = "webBrowser1";
            this.webBrowser1.Size = new System.Drawing.Size(600, 320);
            this.webBrowser1.TabIndex = 0;
            //
            // demoUserControl1
            //
            this.demoUserControl1.Location = new System.Drawing.Point(624, 12);
            this.demoUserControl1.Name = "demoUserControl1";
            this.demoUserControl1.Size = new System.Drawing.Size(220, 70);
            this.demoUserControl1.TabIndex = 1;
            //
            // openDialogFormButton
            //
            this.openDialogFormButton.Location = new System.Drawing.Point(624, 92);
            this.openDialogFormButton.Name = "openDialogFormButton";
            this.openDialogFormButton.Size = new System.Drawing.Size(220, 28);
            this.openDialogFormButton.TabIndex = 2;
            this.openDialogFormButton.Text = "Open dialog form...";
            this.openDialogFormButton.Click += new EventHandler(this.openDialogFormButton_Click);
            //
            // aboutButton
            //
            this.aboutButton.Location = new System.Drawing.Point(624, 126);
            this.aboutButton.Name = "aboutButton";
            this.aboutButton.Size = new System.Drawing.Size(220, 28);
            this.aboutButton.TabIndex = 3;
            this.aboutButton.Text = "About";
            this.aboutButton.Click += new EventHandler(this.aboutButton_Click);
            //
            // advancedInfoLabel
            //
            this.advancedInfoLabel.Location = new System.Drawing.Point(624, 160);
            this.advancedInfoLabel.Name = "advancedInfoLabel";
            this.advancedInfoLabel.Size = new System.Drawing.Size(220, 40);
            this.advancedInfoLabel.TabIndex = 4;
            this.advancedInfoLabel.Text = "DemoComponent ticks: 0";
            //
            // demoComponent1
            //
            this.demoComponent1.Caption = "All-In-One";
            this.demoComponent1.Ticked += new EventHandler(this.demoComponent1_Ticked);
            //
            // advancedTabPage
            //
            this.advancedTabPage.Controls.Add(this.webBrowser1);
            this.advancedTabPage.Controls.Add(this.demoUserControl1);
            this.advancedTabPage.Controls.Add(this.openDialogFormButton);
            this.advancedTabPage.Controls.Add(this.aboutButton);
            this.advancedTabPage.Controls.Add(this.advancedInfoLabel);
            this.advancedTabPage.Location = new System.Drawing.Point(4, 24);
            this.advancedTabPage.Name = "advancedTabPage";
            this.advancedTabPage.Size = new System.Drawing.Size(992, 602);
            this.advancedTabPage.TabIndex = 8;
            this.advancedTabPage.Text = "Advanced";
            //
            // tabControl1
            //
            this.tabControl1.Controls.Add(this.buttonsTabPage);
            this.tabControl1.Controls.Add(this.listsTabPage);
            this.tabControl1.Controls.Add(this.containersTabPage);
            this.tabControl1.Controls.Add(this.dataTabPage);
            this.tabControl1.Controls.Add(this.dateTabPage);
            this.tabControl1.Controls.Add(this.graphicsTabPage);
            this.tabControl1.Controls.Add(this.dialogsTabPage);
            this.tabControl1.Controls.Add(this.componentsTabPage);
            this.tabControl1.Controls.Add(this.advancedTabPage);
            this.tabControl1.Location = new System.Drawing.Point(12, 58);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(1000, 630);
            this.tabControl1.TabIndex = 3;
            this.tabControl1.SelectedIndexChanged += new EventHandler(this.tabControl1_SelectedIndexChanged);
            //
            // imageList1
            //
            this.imageList1.ColorDepth = ColorDepth.Depth32Bit;
            this.imageList1.ImageSize = new System.Drawing.Size(16, 16);
            this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
            //
            // toolTip1
            //
            this.toolTip1.AutoPopDelay = 5000;
            this.toolTip1.InitialDelay = 500;
            this.toolTip1.ReshowDelay = 100;
            this.toolTip1.SetToolTip(this.titleTextBox, "The document title");
            this.toolTip1.SetToolTip(this.demoButton, "Shows a message box");
            this.toolTip1.SetToolTip(this.dataGridView1, "Bound to bindingSource1");
            //
            // errorProvider1
            //
            this.errorProvider1.BlinkStyle = ErrorBlinkStyle.NeverBlink;
            this.errorProvider1.ContainerControl = this;
            //
            // helpProvider1
            //
            this.helpProvider1.HelpNamespace = "https://learn.microsoft.com/dotnet/desktop/winforms/";
            this.helpProvider1.SetHelpString(this.notesRichTextBox, "Free-form notes about the current document.");
            this.helpProvider1.SetShowHelp(this.notesRichTextBox, true);
            //
            // MainForm
            //
            this.ClientSize = new System.Drawing.Size(1024, 720);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "MainForm";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "All-In-One WinForms control gallery";
            this.Load += new EventHandler(this.MainForm_Load);
            this.FormClosing += new FormClosingEventHandler(this.MainForm_FormClosing);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.toolStrip1.ResumeLayout(false);
            this.statusStrip1.ResumeLayout(false);
            this.tabControl1.ResumeLayout(false);
            this.buttonsTabPage.ResumeLayout(false);
            this.buttonsTabPage.PerformLayout();
            this.optionsGroupBox.ResumeLayout(false);
            this.listsTabPage.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.amountUpDown)).EndInit();
            this.containersTabPage.ResumeLayout(false);
            this.plainPanel.ResumeLayout(false);
            this.containerGroupBox.ResumeLayout(false);
            this.flowLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.innerTabControl.ResumeLayout(false);
            this.innerTabPage1.ResumeLayout(false);
            this.containerToolStrip.ResumeLayout(false);
            this.toolStripContainer1.ResumeLayout(false);
            this.dataTabPage.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.bindingSource1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.bindingNavigator1)).EndInit();
            this.bindingNavigator1.ResumeLayout(false);
            this.dateTabPage.ResumeLayout(false);
            this.graphicsTabPage.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar1)).EndInit();
            this.dialogsTabPage.ResumeLayout(false);
            this.componentsTabPage.ResumeLayout(false);
            this.componentsTabPage.PerformLayout();
            this.contextPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.fileSystemWatcher1)).EndInit();
            this.contextMenuStrip1.ResumeLayout(false);
            this.advancedTabPage.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private MenuStrip menuStrip1;
        private ToolStripMenuItem fileMenuItem;
        private ToolStripMenuItem newMenuItem;
        private ToolStripMenuItem openMenuItem;
        private ToolStripSeparator fileMenuSeparator;
        private ToolStripMenuItem exitMenuItem;
        private ToolStripMenuItem editMenuItem;
        private ToolStripMenuItem undoMenuItem;
        private ToolStripMenuItem redoMenuItem;
        private ToolStripMenuItem wordWrapMenuItem;
        private ToolStripMenuItem viewMenuItem;
        private ToolStripMenuItem barsMenuItem;
        private ToolStripMenuItem showToolStripMenuItem;
        private ToolStripMenuItem showStatusStripMenuItem;
        private ToolStripMenuItem helpMenuItem;
        private ToolStripMenuItem aboutMenuItem;
        private ToolStrip toolStrip1;
        private ToolStripButton toolStripNewButton;
        private ToolStripLabel toolStripLabel1;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripComboBox toolStripComboBox1;
        private ToolStripTextBox toolStripTextBox1;
        private ToolStripProgressBar toolStripProgressBar1;
        private ToolStripDropDownButton toolStripDropDownButton1;
        private ToolStripMenuItem dropDownItemA;
        private ToolStripMenuItem dropDownItemB;
        private ToolStripSplitButton toolStripSplitButton1;
        private ToolStripMenuItem splitButtonItemA;
        private TrackBar hostedTrackBar;
        private ToolStripControlHost toolStripControlHost1;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel statusLabel;
        private ToolStripStatusLabel statusInfoLabel;
        private ToolStripProgressBar statusProgressBar;
        private TabControl tabControl1;
        private TabPage buttonsTabPage;
        private TabPage listsTabPage;
        private TabPage containersTabPage;
        private TabPage dataTabPage;
        private TabPage dateTabPage;
        private TabPage graphicsTabPage;
        private TabPage dialogsTabPage;
        private TabPage componentsTabPage;
        private TabPage advancedTabPage;
        private Label captionLabel;
        private LinkLabel linkLabel1;
        private TextBox titleTextBox;
        private MaskedTextBox phoneMaskedTextBox;
        private RichTextBox notesRichTextBox;
        private CheckBox enabledCheckBox;
        private GroupBox optionsGroupBox;
        private RadioButton radioOption1;
        private RadioButton radioOption2;
        private Button demoButton;
        private Button applyButton;
        private Button resetButton;
        private Button sharedButtonA;
        private Button sharedButtonB;
        private Button validateButton;
        private ListBox itemsListBox;
        private CheckedListBox checkedListBox1;
        private ComboBox itemsComboBox;
        private DomainUpDown domainUpDown1;
        private NumericUpDown amountUpDown;
        private Button refreshButton;
        private ListView itemsListView;
        private ColumnHeader listViewNameColumn;
        private ColumnHeader listViewSizeColumn;
        private TreeView itemsTreeView;
        private PropertyGrid propertyGrid1;
        private Panel plainPanel;
        private Label panelLabel;
        private GroupBox containerGroupBox;
        private CheckBox groupBoxCheckBox;
        private FlowLayoutPanel flowLayoutPanel1;
        private Button flowButton1;
        private Button flowButton2;
        private TableLayoutPanel tableLayoutPanel1;
        private Label tableLabel1;
        private TextBox tableTextBox1;
        private SplitContainer splitContainer1;
        private ListBox splitLeftListBox;
        private Label splitRightLabel;
        private Button splitRightButton;
        private Splitter splitter1;
        private TabControl innerTabControl;
        private TabPage innerTabPage1;
        private TabPage innerTabPage2;
        private Label innerLabel;
        private ToolStripContainer toolStripContainer1;
        private ToolStrip containerToolStrip;
        private ToolStripButton containerToolStripButton;
        private Label contentPanelLabel;
        private ToolStripPanel toolStripPanel1;
        private ToolStripContentPanel toolStripContentPanel1;
        private DataGridView dataGridView1;
        private DataGridViewTextBoxColumn nameColumn;
        private DataGridViewCheckBoxColumn activeColumn;
        private DataGridViewComboBoxColumn categoryColumn;
        private DataGridViewButtonColumn actionColumn;
        private DataGridViewImageColumn iconColumn;
        private DataGridViewLinkColumn linkColumn;
        private BindingSource bindingSource1;
        private BindingNavigator bindingNavigator1;
        private ToolStripButton navigatorMoveFirstButton;
        private ToolStripButton navigatorMoveNextButton;
        private ToolStripSeparator navigatorSeparator;
        private ToolStripLabel navigatorPositionLabel;
        private Label gridInfoLabel;
        private DateTimePicker dateTimePicker1;
        private MonthCalendar monthCalendar1;
        private Label clockLabel;
        private Button clockToggleButton;
        private System.Windows.Forms.Timer clockTimer;
        private PictureBox pictureBox1;
        private ProgressBar progressBar1;
        private TrackBar trackBar1;
        private HScrollBar hScrollBar1;
        private VScrollBar vScrollBar1;
        private Label graphicsInfoLabel;
        private Button openFileButton;
        private Button saveFileButton;
        private Button folderBrowserButton;
        private Button colorButton;
        private Button fontButton;
        private Button printButton;
        private Button pageSetupButton;
        private Button printPreviewButton;
        private Label selectedPathLabel;
        private PrintPreviewControl printPreviewControl1;
        private OpenFileDialog openFileDialog1;
        private SaveFileDialog saveFileDialog1;
        private FolderBrowserDialog folderBrowserDialog1;
        private ColorDialog colorDialog1;
        private FontDialog fontDialog1;
        private PrintDialog printDialog1;
        private PageSetupDialog pageSetupDialog1;
        private PrintPreviewDialog printPreviewDialog1;
        private System.Drawing.Printing.PrintDocument printDocument1;
        private Button startWorkerButton;
        private ProgressBar workerProgressBar;
        private Button watchButton;
        private Label watcherLabel;
        private Button launchProcessButton;
        private Button writeEventLogButton;
        private Button readCounterButton;
        private Button serviceStatusButton;
        private Button serialOpenButton;
        private Button playSoundButton;
        private Button showBalloonButton;
        private TextBox componentsLogTextBox;
        private Panel contextPanel;
        private Label contextPanelLabel;
        private System.ComponentModel.BackgroundWorker backgroundWorker1;
        private System.IO.FileSystemWatcher fileSystemWatcher1;
        private System.Diagnostics.Process process1;
        private System.Diagnostics.EventLog eventLog1;
        private System.Diagnostics.PerformanceCounter performanceCounter1;
        private System.ServiceProcess.ServiceController serviceController1;
        private System.IO.Ports.SerialPort serialPort1;
        private System.Media.SoundPlayer soundPlayer1;
        private NotifyIcon notifyIcon1;
        private HelpProvider helpProvider1;
        private ContextMenuStrip contextMenuStrip1;
        private ToolStripMenuItem copyContextMenuItem;
        private ToolStripMenuItem pasteContextMenuItem;
        private ToolStripSeparator contextMenuSeparator;
        private ToolStripMenuItem selectAllContextMenuItem;
        private WebBrowser webBrowser1;
        private AllInOneWinForms.Controls.DemoUserControl demoUserControl1;
        private Button openDialogFormButton;
        private Button aboutButton;
        private Label advancedInfoLabel;
        private AllInOneWinForms.Components.DemoComponent demoComponent1;
        private ImageList imageList1;
        private ToolTip toolTip1;
        private ErrorProvider errorProvider1;
    }
}
