namespace ComponentFieldApp
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
            this.startButton = new System.Windows.Forms.Button();
            this.launchButton = new System.Windows.Forms.Button();
            this.watchButton = new System.Windows.Forms.Button();
            this.logButton = new System.Windows.Forms.Button();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.statusLabel = new System.Windows.Forms.Label();
            this.backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            this.process1 = new System.Diagnostics.Process();
            this.fileSystemWatcher1 = new System.IO.FileSystemWatcher();
            this.eventLog1 = new System.Diagnostics.EventLog();
            this.unusedWatcher = new System.IO.FileSystemWatcher();
            this.SuspendLayout();
            //
            // startButton
            //
            this.startButton.Location = new System.Drawing.Point(12, 12);
            this.startButton.Name = "startButton";
            this.startButton.Size = new System.Drawing.Size(90, 28);
            this.startButton.TabIndex = 0;
            this.startButton.Text = "Start";
            this.startButton.Click += new System.EventHandler(this.startButton_Click);
            //
            // launchButton
            //
            this.launchButton.Location = new System.Drawing.Point(108, 12);
            this.launchButton.Name = "launchButton";
            this.launchButton.Size = new System.Drawing.Size(90, 28);
            this.launchButton.TabIndex = 1;
            this.launchButton.Text = "Launch";
            this.launchButton.Click += new System.EventHandler(this.launchButton_Click);
            //
            // watchButton
            //
            this.watchButton.Location = new System.Drawing.Point(204, 12);
            this.watchButton.Name = "watchButton";
            this.watchButton.Size = new System.Drawing.Size(90, 28);
            this.watchButton.TabIndex = 2;
            this.watchButton.Text = "Watch";
            this.watchButton.Click += new System.EventHandler(this.watchButton_Click);
            //
            // logButton
            //
            this.logButton.Location = new System.Drawing.Point(300, 12);
            this.logButton.Name = "logButton";
            this.logButton.Size = new System.Drawing.Size(90, 28);
            this.logButton.TabIndex = 3;
            this.logButton.Text = "Log";
            this.logButton.Click += new System.EventHandler(this.logButton_Click);
            //
            // progressBar1
            //
            this.progressBar1.Location = new System.Drawing.Point(12, 48);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(378, 23);
            this.progressBar1.TabIndex = 4;
            //
            // statusLabel
            //
            this.statusLabel.Location = new System.Drawing.Point(12, 80);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(378, 23);
            this.statusLabel.TabIndex = 5;
            this.statusLabel.Text = "Ready";
            //
            // backgroundWorker1
            //
            this.backgroundWorker1.WorkerReportsProgress = true;
            this.backgroundWorker1.WorkerSupportsCancellation = true;
            this.backgroundWorker1.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.backgroundWorker1_ProgressChanged);
            this.backgroundWorker1.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.backgroundWorker1_RunWorkerCompleted);
            //
            // fileSystemWatcher1
            //
            this.fileSystemWatcher1.Filter = "*.txt";
            this.fileSystemWatcher1.EnableRaisingEvents = false;
            this.fileSystemWatcher1.Changed += new System.IO.FileSystemEventHandler(this.fileSystemWatcher1_Changed);
            //
            // eventLog1
            //
            this.eventLog1.Source = "ComponentFieldApp";
            //
            // unusedWatcher - never referenced and never wired, so it gets no field at all.
            //
            this.unusedWatcher.Filter = "*.log";
            //
            // MainForm
            //
            this.ClientSize = new System.Drawing.Size(402, 116);
            this.Controls.Add(this.startButton);
            this.Controls.Add(this.launchButton);
            this.Controls.Add(this.watchButton);
            this.Controls.Add(this.logButton);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.statusLabel);
            this.Name = "MainForm";
            this.Text = "Component fields";
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.Button startButton;
        private System.Windows.Forms.Button launchButton;
        private System.Windows.Forms.Button watchButton;
        private System.Windows.Forms.Button logButton;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Label statusLabel;
        private System.ComponentModel.BackgroundWorker backgroundWorker1;
        private System.Diagnostics.Process process1;
        private System.IO.FileSystemWatcher fileSystemWatcher1;
        private System.Diagnostics.EventLog eventLog1;
        private System.IO.FileSystemWatcher unusedWatcher;
    }
}
