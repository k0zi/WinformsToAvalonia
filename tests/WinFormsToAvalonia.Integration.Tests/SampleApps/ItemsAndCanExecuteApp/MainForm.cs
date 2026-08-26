using System;
using System.Windows.Forms;

namespace ItemsAndCanExecuteApp
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        // Promotable: touches only a bindable property.
        private void submitButton_Click(object sender, EventArgs e)
        {
            this.resultLabel.Text = this.nameTextBox.Text;
        }

        // Its whole job is keeping the button's enabled state in sync - a CanExecute guard.
        private void nameTextBox_TextChanged(object sender, EventArgs e)
        {
            this.submitButton.Enabled = this.nameTextBox.Text.Length > 0;
        }
    }
}
