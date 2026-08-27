using System;
using System.Windows.Forms;

namespace SenderAndDragApp
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        // Wired to exactly one control, so `sender` provably is that control: the local becomes
        // another name for its field and the cast disappears.
        private void renameButton_Click(object sender, EventArgs e)
        {
            var button = (Button)sender;
            button.Text = "Renamed";
            button.Enabled = false;
        }

        // Two controls share this one - how WinForms shares a handler at all. They are the same
        // type, so the cast is exactly as valid as it was: it survives, against the Avalonia
        // element, and the local stands for a control of that one type.
        private void sharedClick(object sender, EventArgs e)
        {
            var button = (Button)sender;
            button.Text = "Shared";
        }

        private void copyButton_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(this.statusLabel.Text);
        }

        // The one thing a translated body may ask a drag payload.
        private void dropPanel_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop)
                ? DragDropEffects.Copy
                : DragDropEffects.None;
        }

        // Reading the payload is a change of shape, not of spelling - Avalonia hands back
        // storage items rather than a string[] - so this one stays for a human.
        private void dropPanel_DragDrop(object sender, DragEventArgs e)
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            this.statusLabel.Text = files.Length + " file(s)";
        }
    }
}
