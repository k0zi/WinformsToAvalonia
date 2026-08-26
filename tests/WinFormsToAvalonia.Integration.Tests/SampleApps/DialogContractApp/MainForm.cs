using System;
using System.Windows.Forms;
using DialogContractApp.Dialogs;

namespace DialogContractApp
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        // A window property on the form itself, including an enum WinForms and Avalonia spell
        // differently.
        private void maximizeButton_Click(object sender, EventArgs e)
        {
            this.Text = "Maximized";
            this.WindowState = FormWindowState.Maximized;
        }

        // Reading the title back is a string read, so it is null-guarded like every other one.
        private void titleButton_Click(object sender, EventArgs e)
        {
            this.statusLabel.Text = this.Text;
        }

        // A window property reached through the local that holds the dialog, then the dialog's
        // own bool result driving a branch.
        private void configureButton_Click(object sender, EventArgs e)
        {
            var dialog = new ConfirmForm();
            dialog.Text = "Confirm the change";
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                this.statusLabel.Text = "Applied";
            }
            else
            {
                this.statusLabel.Text = "Discarded";
            }
        }

        // The Timer this conversion turns into a DispatcherTimer field is something a body may
        // name - which was not true before, on a field the conversion's own output declares.
        private void toggleClockButton_Click(object sender, EventArgs e)
        {
            this.clockTimer.Enabled = !this.clockTimer.Enabled;
            this.clockTimer.Interval = 500;
        }

        private void clockTimer_Tick(object sender, EventArgs e)
        {
            this.statusLabel.Text = DateTime.Now.ToLongTimeString();
        }

        // Stopping the timer outright, the other verb a DispatcherTimer keeps.
        private void stopClockButton_Click(object sender, EventArgs e)
        {
            this.clockTimer.Stop();
        }
    }
}
