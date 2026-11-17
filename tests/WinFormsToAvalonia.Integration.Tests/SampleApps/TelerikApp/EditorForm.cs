using System;
using Telerik.WinControls.UI;

namespace TelerikApp
{
    public partial class EditorForm : RadRibbonForm
    {
        public EditorForm()
        {
            InitializeComponent();
        }

        private void copyButton_Click(object sender, EventArgs e)
        {
            notesBox.Text = cityBox.Text;
        }
    }
}
