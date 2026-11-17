using System;
using System.Windows.Forms;

namespace CorpusShapes
{
    public partial class LinkedForm : Form
    {
        private void goButton_Click(object sender, EventArgs e)
        {
            this.Text = "linked";
        }

        public LinkedForm()
        {
            InitializeComponent();
        }
    }
}
