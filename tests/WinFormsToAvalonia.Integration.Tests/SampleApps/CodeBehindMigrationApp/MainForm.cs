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

        // Avalonia has no Paint event: the method is emitted but nothing subscribes it.
        private void canvasPanel_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.DrawRectangle(Pens.Black, 0, 0, 10, 10);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            greetingLabel.Text = string.Empty;
        }
    }
}
