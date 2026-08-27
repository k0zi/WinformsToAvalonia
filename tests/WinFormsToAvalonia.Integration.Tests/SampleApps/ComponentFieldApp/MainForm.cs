using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using ComponentFieldApp.Components;

namespace ComponentFieldApp
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        // The component is a real field now, so its members are ordinary .NET.
        private void startButton_Click(object sender, EventArgs e)
        {
            this.backgroundWorker1.RunWorkerAsync();
            this.statusLabel.Text = "Started";
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            this.progressBar1.Value = e.ProgressPercentage;
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.statusLabel.Text = "Done";
        }

        // A nested property path, which works for the same reason: StartInfo is itself an
        // unchanged .NET object.
        private void launchButton_Click(object sender, EventArgs e)
        {
            this.process1.StartInfo.FileName = "dotnet";
            this.process1.StartInfo.Arguments = "--info";
        }

        private void watchButton_Click(object sender, EventArgs e)
        {
            this.fileSystemWatcher1.Path = Path.GetTempPath();
            this.fileSystemWatcher1.EnableRaisingEvents = true;
        }

        private void fileSystemWatcher1_Changed(object sender, FileSystemEventArgs e)
        {
            this.statusLabel.Text = e.Name;
        }

        // A component this project defines: plain .NET, so its source comes across and the field
        // is real - designer value applied, its own event wired.
        private void bumpButton_Click(object sender, EventArgs e)
        {
            this.counterComponent1.Bump();
        }

        private void counterComponent1_Counted(object sender, EventArgs e)
        {
            this.statusLabel.Text = this.counterComponent1.Count.ToString();
        }

        // Windows-only: the field is still declared, the platform analyser silenced for the file,
        // and the conversion reports the constraint.
        private void logButton_Click(object sender, EventArgs e)
        {
            this.eventLog1.WriteEntry("Component field demo");
        }
    }
}
