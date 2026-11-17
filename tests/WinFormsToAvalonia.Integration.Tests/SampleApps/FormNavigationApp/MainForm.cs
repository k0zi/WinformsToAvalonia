using System;
using System.Windows.Forms;
using FormNavigationApp.Dialogs;

namespace FormNavigationApp
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        // Modal navigation into a Form that lives in another folder, so the generated View is in
        // another namespace too.
        private void settingsButton_Click(object sender, EventArgs e)
        {
            new SettingsForm().ShowDialog(this);
        }

        // Modeless, and after a translatable property write - both statements come across.
        private void helpButton_Click(object sender, EventArgs e)
        {
            this.statusLabel.Text = "Opening help";
            new SettingsForm().Show();
        }

        // The result drives a branch - which works because the converted dialog closes with a
        // bool, synthesized from its designer-set DialogResult buttons.
        private void confirmButton_Click(object sender, EventArgs e)
        {
            if (new SettingsForm().ShowDialog(this) == DialogResult.OK)
            {
                this.statusLabel.Text = "Confirmed";
            }
            else
            {
                this.statusLabel.Text = "Cancelled";
            }
        }
    }
}
