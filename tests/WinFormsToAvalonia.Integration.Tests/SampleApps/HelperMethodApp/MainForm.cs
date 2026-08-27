using System;
using System.Windows.Forms;

namespace HelperMethodApp
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void startButton_Click(object sender, EventArgs e)
        {
            SetBusy(true);
            this.statusLabel.Text = Describe(1);
        }

        // Calls a helper that itself turns async, so this one has to await it - and therefore
        // becomes async too.
        private void warnButton_Click(object sender, EventArgs e)
        {
            WarnAndReset();
        }

        // A helper the conversion cannot finish, so its caller cannot either.
        private void saveButton_Click(object sender, EventArgs e)
        {
            PersistToDisk();
            this.statusLabel.Text = "Saved";
        }

        // Everything this pair touches is bindable, so the handler and its helper move to the
        // ViewModel together - the relaxed promotion condition 5.
        private void tagButton_Click(object sender, EventArgs e)
        {
            Announce("done");
        }

        // ---- helpers -------------------------------------------------------------------

        private void Announce(string what)
        {
            this.tagLabel.Text = what;
        }


        // The classic pair: a helper maintaining a private flag. Without the field carried over,
        // the helper could not translate - and neither could any handler that calls it.
        private bool isBusy;

        private void SetBusy(bool busy)
        {
            this.isBusy = busy;
            this.startButton.Enabled = !busy;
            this.statusLabel.Text = busy ? "Working" : "Ready";
        }

        // Returns a value, used as an expression at the call site.
        private string Describe(int count)
        {
            if (count == 1)
            {
                return "1 item";
            }

            return count + " items";
        }

        // Turns async because of the message box, which makes every caller await it.
        private void WarnAndReset()
        {
            MessageBox.Show("Resetting", "Helper demo");
            SetBusy(false);
        }

        // Reaches for a WinForms API with no counterpart, so the whole helper stays a comment.
        private void PersistToDisk()
        {
            this.statusLabel.Text = "Persisting";
            this.itemsTreeView.Nodes.Clear();
        }
    }
}
