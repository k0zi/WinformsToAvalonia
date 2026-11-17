using System;
using System.Windows.Forms;

namespace CorpusShapes
{
    public partial class SharedForm : Form
    {
        private void goButton_Click(object sender, EventArgs e)
        {
            this.Text = "shared";
        }

        public SharedForm()
        {
            InitializeComponent();
        }
    }
}
