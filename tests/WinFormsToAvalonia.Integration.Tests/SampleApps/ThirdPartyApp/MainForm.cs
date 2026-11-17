using System;
using System.Windows.Forms;

namespace ThirdPartyApp
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void nameEdit_EditValueChanged(object sender, EventArgs e)
        {
            this.statusLabel.Text = this.nameEdit.Text;
        }
    }
}
