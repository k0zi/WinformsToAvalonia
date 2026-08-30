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

        // WinForms' NotifyIcon.Click and Avalonia's TrayIcon.Clicked are close enough to carry
        // over - and this used to be emitted as a fully translated method that nothing anywhere
        // subscribed, with no warning to say so.
        private void notifyIcon1_Click(object sender, EventArgs e)
        {
            this.notifyIcon1.Text = "Clicked";
        }

        // A tray menu item's Click. Avalonia's NativeMenuItem raises Click as an event, which XAML
        // cannot point at a method - so the item is emitted and this is reported as unwired.
        private void openMenuItem_Click(object sender, EventArgs e)
        {
            this.notifyIcon1.Text = "Opened";
        }

        // Avalonia's TrayIcon has no double-click, and a single click is not one - so this stays
        // a method nobody subscribes, and the conversion has to say so.
        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            this.notifyIcon1.Text = "Double-clicked";
        }

        // The same Click that works above, on the icon that did not resolve: there is no accessor
        // to subscribe against, so naming one would not compile.
        private void notifyIcon2_Click(object sender, EventArgs e)
        {
            this.notifyIcon2.Text = "Clicked";
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
