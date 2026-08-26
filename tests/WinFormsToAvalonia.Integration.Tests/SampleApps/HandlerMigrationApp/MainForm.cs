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

        // Control flow: the condition and both branches translate, so the whole `if` does.
        private void validateButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(this.nameTextBox.Text))
            {
                this.statusLabel.Text = "Name is required";
                this.nameTextBox.Focus();
            }
            else
            {
                this.statusLabel.Text = "Looks good";
            }
        }

        // A loop over a translated value, with a local and a compound assignment in the header.
        private void countButton_Click(object sender, EventArgs e)
        {
            var vowels = 0;
            foreach (var letter in this.nameTextBox.Text)
            {
                if (letter == 'a' || letter == 'e')
                {
                    vowels++;
                }
            }

            this.counterLabel.Text = vowels.ToString();
        }

        // The pointer position: WinForms' e.X/e.Y are relative to the raising control, which is
        // exactly what Avalonia's GetPosition takes.
        private void canvas_MouseDown(object sender, MouseEventArgs e)
        {
            this.statusLabel.Text = e.X + "," + e.Y;
        }

        // No exact answer on the Avalonia side, so it stays for a human: a shared handler has no
        // single raising control to measure the pointer against.
        private void sharedMouseDown(object sender, MouseEventArgs e)
        {
            this.statusLabel.Text = e.X.ToString();
        }

        private void PersistToDisk()
        {
        }
    }
}
