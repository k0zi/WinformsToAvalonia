using System;
using System.Windows.Forms;

namespace SettingsApp
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            this.urlLabel.Text = Properties.Settings.Default.ServerUrl;
            this.countLabel.Text = $"Launched {Properties.Settings.Default.LaunchCount} times";
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.UserName = this.nameBox.Text;
            Properties.Settings.Default.LaunchCount += 1;
            Properties.Settings.Default.Save();
        }

        private void resetButton_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.Reset();
            this.nameBox.Text = Properties.Settings.Default.UserName;
            Properties.Settings.Default.ServerUrl = "https://other.invalid";
        }
    }
}
