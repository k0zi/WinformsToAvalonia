using System;
using System.Windows.Forms;

namespace HandlerMigrationApp
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        // Fully translatable: bindable writes plus the Window's own Close().
        private void okButton_Click(object sender, EventArgs e)
        {
            this.statusLabel.Text = "Accepted";
            this.nameTextBox.Text = string.Empty;
            this.Close();
        }

        // A dialog keeps the handler in code-behind, and makes the generated method async.
        private void aboutButton_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Handler migration demo", "About");
        }

        // Focus plus a computed value that only uses plain .NET.
        private void resetButton_Click(object sender, EventArgs e)
        {
            this.counterLabel.Text = (int.Parse(this.counterLabel.Text) + 1).ToString();
            this.nameTextBox.Focus();
        }

        // The prefix rule: statement one translates, the unknown call stops the rest.
        private void saveButton_Click(object sender, EventArgs e)
        {
            this.statusLabel.Text = "Saving";
            PersistToDisk();
            this.statusLabel.Text = "Saved";
        }

        // Not translatable at all - needs the pointer position.
        private void canvas_MouseDown(object sender, MouseEventArgs e)
        {
            this.statusLabel.Text = e.X + "," + e.Y;
        }

        private void PersistToDisk()
        {
        }
    }
}
