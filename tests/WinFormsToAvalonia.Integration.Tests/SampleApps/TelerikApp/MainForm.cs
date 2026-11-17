using System;
using Telerik.WinControls;
using Telerik.WinControls.UI;

namespace TelerikApp
{
    public partial class MainForm : RadForm
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void openMenuItem_Click(object sender, EventArgs e)
        {
            nameBox.Text = "Opened";
        }

        private void newButton_Click(object sender, EventArgs e)
        {
            statusLabel.Text = "New";
        }

        private void nameBox_TextChanged(object sender, EventArgs e)
        {
            progress.Value1 = nameBox.Text.Length;
        }

        private void agreeCheck_ToggleStateChanged(object sender, StateChangedEventArgs args)
        {
            okButton.Enabled = agreeCheck.IsChecked;
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            RadMessageBox.Show("Thanks, " + nameBox.Text, "Done");
        }

        // The corpus' BoundGridReorderRows shape: a nested type that names nothing WinForms but derives
        // from a Telerik type - which the generated project has no more of than WinForms.
        private class ReorderBehavior : GridDataRowBehavior
        {
        }
    }
}
