using System;
using System.Windows.Forms;
using DevExpress.XtraBars;
using DevExpress.XtraEditors;

namespace DevExpressApp
{
    public partial class MainForm : XtraForm
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void openItem_ItemClick(object sender, ItemClickEventArgs e)
        {
            nameEdit.Text = "Opened";
        }

        private void exitItem_ItemClick(object sender, ItemClickEventArgs e)
        {
            statusItem.Caption = "Bye";
        }

        private void nameEdit_EditValueChanged(object sender, EventArgs e)
        {
            progress.Position = nameEdit.Text.Length;
        }

        private void activeCheck_CheckedChanged(object sender, EventArgs e)
        {
            ageSpin.Enabled = activeCheck.Checked;
        }

        private void darkToggle_Toggled(object sender, EventArgs e)
        {
            nameLabel.Text = darkToggle.IsOn ? "Dark" : "Light";
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            if (XtraMessageBox.Show("Save the changes?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                nameEdit.Text = nameEdit.Text.Trim();
            }
        }

        private void peopleView_FocusedRowChanged(object sender, DevExpress.XtraGrid.Views.Base.FocusedRowChangedEventArgs e)
        {
            nameEdit.Text = peopleView.GetFocusedRowCellDisplayText(colName);
        }
    }
}
