using System;
using System.Drawing;
using System.Windows.Forms;

namespace RuntimeControlsApp
{
    public partial class MainForm : Form
    {
        // Controls this Form creates itself rather than through the designer: one created in Load
        // and added to the status strip, one created by its field initializer, and buttons made
        // per click.
        private ToolStripStatusLabel statusText;
        private Label counterLabel = new Label();
        private int count;

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            statusText = new ToolStripStatusLabel();
            statusText.Text = "Ready";
            ToolStripItem[] items = { statusText };
            this.statusStrip1.Items.AddRange(items);

            counterLabel.Location = new Point(10, 10);
            counterLabel.Size = new Size(120, 20);
            counterLabel.Text = "none yet";
            hostPanel.Controls.Add(counterLabel);
        }

        private void addButton_Click(object sender, EventArgs e)
        {
            count++;
            var button = new Button { Text = "Added", Enabled = false };
            button.Location = new Point(10, 10 + 30 * count);
            button.Size = new Size(100, 25);
            hostPanel.Controls.Add(button);
            statusText.Text = "Added a button";
            counterLabel.Text = "one or more";
        }
    }
}
