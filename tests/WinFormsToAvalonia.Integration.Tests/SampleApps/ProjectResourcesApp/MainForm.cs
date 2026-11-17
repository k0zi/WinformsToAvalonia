using System;
using System.Windows.Forms;

namespace ProjectResourcesApp
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void greetButton_Click(object sender, EventArgs e)
        {
            this.statusLabel.Text = Properties.Resources.Greeting;
            this.nameBox.Text = global::ProjectResourcesApp.Properties.Resources.Farewell + "!";
        }

        private void logoButton_Click(object sender, EventArgs e)
        {
            this.statusLabel.Text = "logo";
            this.pictureBox1.Image = Properties.Resources.logo;
        }
    }
}
