using System;
using System.Windows.Forms;

namespace EdgeCasesApp.Dialogs
{
    // Lives in a folder whose name is not an identifier: the generated namespace has to be one.
    public partial class SettingsForm : Form
    {
        public SettingsForm()
        {
            InitializeComponent();
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private class Row
        {
            public string Path { get; set; }
        }
    }
}
