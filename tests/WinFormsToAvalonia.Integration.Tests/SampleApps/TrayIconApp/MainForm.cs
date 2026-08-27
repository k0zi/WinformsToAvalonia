using System;
using System.Windows.Forms;

namespace TrayIconApp
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        // A NotifyIcon has no element of its own - it becomes an app-level TrayIcon in App.axaml -
        // so showing and hiding it, which is what a WinForms app does with one, had nowhere to go.
        private void hideButton_Click(object sender, EventArgs e)
        {
            this.notifyIcon1.Visible = false;
            this.notifyIcon1.Text = "Hidden";
        }

        // The other NotifyIcon has no icon the conversion can resolve, so App.axaml emits nothing
        // live for it - and this must stay a comment rather than name an accessor that is not
        // there.
        private void otherButton_Click(object sender, EventArgs e)
        {
            this.notifyIcon2.Visible = false;
        }
    }
}
