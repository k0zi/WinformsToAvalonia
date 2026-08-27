using System;
using System.Windows.Forms;

namespace Widgets
{
    public partial class SharedPanel : UserControl
    {
        public SharedPanel()
        {
            InitializeComponent();
        }

        private void refreshButton_Click(object sender, EventArgs e)
        {
            this.captionLabel.Text = "refreshed";
        }
    }
}
