using System;
using System.Windows.Forms;

namespace CorpusShapes.B
{
    public partial class MainForm : Form
    {
        private void goButton_Click(object sender, EventArgs e)
        {
            this.Text = "B";
        }

        public MainForm()
        {
            InitializeComponent();
        }
    }
}
