using System;
using System.Windows.Forms;

namespace EdgeCasesApp
{
    // Text the generated project has to carry as data, not as syntax: designer strings that look
    // like XAML markup extensions, C# escapes or XML comment terminators, plus the code-behind
    // shapes that used to crash the planner or strand a carried field.
    public partial class MainForm : Form
    {
        // Assigned only in the part of MainForm_Load that cannot be translated. Carrying it would
        // leave it null in the generated View while scoreButton_Click reads it.
        private Game _game;

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            this.trayIcon.ShowBalloonTip(1000);
            _game = new Game();
        }

        private void scoreButton_Click(object sender, EventArgs e)
        {
            this.Text = "Score " + _game.Score;
        }

        private void statusButton_Click(object sender, EventArgs e)
        {
            UpdateStatus("Go");
        }

        // Two overloads: everything downstream knows a helper by name, and promoting both crashed
        // the whole conversion.
        private void UpdateStatus(string text)
        {
            this.notesLabel.Text = text;
        }

        private void UpdateStatus()
        {
            this.notesLabel.Text = "Ready";
        }

        // Shares its name with SettingsForm.Row, with different members - Models/ can hold one.
        private class Row
        {
            public int Count { get; set; }
        }
    }
}
