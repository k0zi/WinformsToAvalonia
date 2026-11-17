using System;
using System.Windows.Forms;

namespace CorpusShapes.A
{
    public partial class MainForm : Form
    {
        private string pattern;

        private void SetPattern(string value)
        {
            pattern = value;
        }

        private void goButton_Click(object sender, EventArgs e)
        {
            SetPattern("x");
            MessageBox.Show(pattern.Length.ToString());
        }

        public MainForm()
        {
            InitializeComponent();
        }
    }
}
