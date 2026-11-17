using System;
using System.Windows.Forms;
using SemanticApp.Services;
using SemanticLib;
using Newtonsoft.Json;

namespace SemanticApp
{
    public partial class MainForm : Form
    {
        private readonly Calculator calculator = new Calculator();
        private Scores scores;
        private string[] formats = { "Short", "Long" };
        private object ownerTag;

        public MainForm()
        {
            InitializeComponent();
            scores = new Scores();
            ownerTag = this.GetType();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            this.formatBox.Items.Clear();
            this.formatBox.Items.AddRange(formats);
            this.formatBox.SelectedIndex = 0;
        }

        private void addButton_Click(object sender, EventArgs e)
        {
            int total = calculator.Add(2, 3);
            scores.Record(total);
            this.resultLabel.Text = total.ToString();
            this.resultLabel.Text += " " + calculator.Describe(total);
            var format = (string)this.formatBox.Items[this.formatBox.SelectedIndex];
            this.resultLabel.Text += $" ({format}, best {Report(scores)})";
            string json = JsonConvert.SerializeObject(new Snapshot { Total = total });
            scores.Record(json.Length);
            this.Text = this.GetType().Name;
        }

        private void busyButton_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => this.resultLabel.Text = "marshalled"));
            }
            else
            {
                this.BeginInvoke((MethodInvoker)delegate { this.resultLabel.Text = "queued"; });
            }

            this.Cursor = Cursors.Default;
        }

        // One handler on two buttons: the guard names the one that was clicked.
        private void side_Click(object sender, EventArgs e)
        {
            if (sender is not Button side)
                return;

            side.Text = "chosen";
            scores.Record(side.Text.Length);
        }

        // One handler on two labels.
        private void card_Click(object sender, EventArgs e)
        {
            if (sender is Label card)
            {
                card.Text = "open";
            }
        }

        private string Report(Scores source)
        {
            return source.Best().ToString() + "/" + source.Count;
        }
    }
}
