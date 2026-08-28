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

        // Null-conditional on something that already translates to a value.
        private void trimButton_Click(object sender, EventArgs e)
        {
            var trimmed = this.nameTextBox.Text?.Trim();
            this.statusLabel.Text = trimmed ?? "empty";
        }

        // The two-button question, whose answer the caller branches on: one awaited call returning
        // a bool, because the dialog on the other end is one the converter ships.
        private void confirmButton_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Discard changes?", "Demo", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                this.nameTextBox.Text = string.Empty;
            }
        }

        // A dialog WinForms has and Avalonia does not: translated inline onto a bundled window,
        // whose result is the colour rather than an object to ask afterwards.
        private void pickColorButton_Click(object sender, EventArgs e)
        {
            if (this.colorDialog1.ShowDialog(this) == DialogResult.OK)
            {
                this.nameTextBox.BackColor = this.colorDialog1.Color;
            }
        }

        // The other dialog idiom: a guard clause instead of a nested branch. Equivalent because
        // the then-branch is an unconditional return, so C# definite assignment keeps the picked
        // value in scope for the rest of the body - and the generated project has to compile with
        // an `is not` pattern doing exactly that, which is why this fixture builds.
        private void guardColorButton_Click(object sender, EventArgs e)
        {
            if (this.colorDialog1.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            this.nameTextBox.BackColor = this.colorDialog1.Color;
            this.statusLabel.Text = "Colour picked";
        }

        // Colours: WinForms writes a Color, Avalonia wants a brush - and which of the two
        // properties exists at all depends on the element the control maps to.
        private void colorButton_Click(object sender, EventArgs e)
        {
            this.statusLabel.ForeColor = Color.Red;
            this.nameTextBox.BackColor = SystemColors.Window;
        }

        // The WinForms validation idiom: the component has no element of its own, so its Avalonia
        // answer is a static call on the bundled fallback.
        private void flagButton_Click(object sender, EventArgs e)
        {
            this.errorProvider1.SetError(this.nameTextBox, "A name is required.");
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

        // Deliberately un-translatable: a helper is emitted as code only when its whole body
        // is, and CharacterCasing is a WinForms-only property with no Avalonia counterpart at all
        // - which is what keeps this a comment, and keeps its caller's prefix stopping here.
        private void PersistToDisk()
        {
            this.nameTextBox.CharacterCasing = CharacterCasing.Upper;
        }
    }
}
