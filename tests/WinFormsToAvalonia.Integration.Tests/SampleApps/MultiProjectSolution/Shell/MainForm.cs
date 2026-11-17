using System;
using System.Windows.Forms;

namespace Shell
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void titleButton_Click(object sender, EventArgs e)
        {
            this.titleLabel.Text = "shell";
        }
    }
}
