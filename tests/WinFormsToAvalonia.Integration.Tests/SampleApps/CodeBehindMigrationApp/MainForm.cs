using System;
using System.Drawing;
using System.Windows.Forms;

namespace CodeBehindMigrationApp
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        // Promotable: touches only bindable value properties, ignores sender and EventArgs.
        private void greetButton_Click(object sender, EventArgs e)
        {
            greetingLabel.Text = "Hello, " + nameTextBox.Text;
        }

        // Not promotable: drives the Form itself.
        private void clearButton_Click(object sender, EventArgs e)
        {
            nameTextBox.Text = string.Empty;
            Close();
        }

        // Not promotable: needs the pointer position from the EventArgs.
        private void canvasPanel_MouseDown(object sender, MouseEventArgs e)
        {
            var panel = (Panel)sender;
            panel.Text = e.X + "," + e.Y;
        }

        // Avalonia has no Paint event - drawing is a Render(DrawingContext) override - so this
        // childless Panel becomes the bundled paint surface, which turns that override back
        // into the event, and the constructor subscribes it.
        private void canvasPanel_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.DrawRectangle(Pens.Black, 0, 0, 10, 10);

            // Text as well as geometry: a FormattedText is built and measured during the render
            // pass, so a bad typeface or em size throws there rather than at compile time.
            e.Graphics.DrawString("drawn", this.Font, Brushes.Black, 2, 2);

            // The bounded form, which is where MaxTextWidth/MaxTextHeight and an alignment
            // actually mean something - and where the layout really runs.
            e.Graphics.DrawString(
                "wrapped",
                this.Font,
                Brushes.Black,
                new RectangleF(2, 20, 60, 30),
                new StringFormat { Alignment = StringAlignment.Center });
        }

        // Not promotable (it closes the Form), so these reads touch the real Avalonia members
        // rather than going through a binding - which is where the two sides' types have to
        // actually agree. `Checked` is a bool in WinForms and a bool? in Avalonia; a Button's
        // `Text` becomes `Content`, an object?. Reading either one straight through does not
        // compile in the generated project.
        private void readBackButton_Click(object sender, EventArgs e)
        {
            if (agreeCheckBox.Checked)
            {
                greetingLabel.Text = readBackButton.Text;
            }

            // A bool on this side, a two-valued enum on the other - the value is rewritten, not
            // just the property name.
            nameTextBox.WordWrap = agreeCheckBox.Checked;

            Close();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            greetingLabel.Text = string.Empty;
        }
    }
}
