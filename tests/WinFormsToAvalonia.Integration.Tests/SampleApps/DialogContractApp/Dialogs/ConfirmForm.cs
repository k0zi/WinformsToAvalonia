using System;
using System.Windows.Forms;

namespace DialogContractApp.Dialogs
{
    public partial class ConfirmForm : Form
    {
        public ConfirmForm()
        {
            InitializeComponent();
        }

        // The hand-written half of the dialog-result contract: two statements in WinForms, one
        // act - and one Close(true) in Avalonia, because a bare Close() after it would overwrite
        // the result with default(bool).
        private void okButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        // The assignment on its own already closes a modal form in WinForms.
        private void cancelButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        // Work after the assignment: WinForms keeps running the handler, Avalonia's Close is the
        // end of it - so this one is deliberately left for a human.
        private void applyButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.resultLabel.Text = "applied";
        }
    }
}
