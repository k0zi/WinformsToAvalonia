using DevExpress.XtraBars;
using DevExpress.XtraBars.Ribbon;
using DevExpress.XtraEditors;

namespace DevExpressApp
{
    public partial class DetailsForm : RibbonForm
    {
        public DetailsForm()
        {
            InitializeComponent();
        }

        private void saveItem_ItemClick(object sender, ItemClickEventArgs e)
        {
            notesMemo.Text = "Saved";
        }

        private void printItem_ItemClick(object sender, ItemClickEventArgs e)
        {
            XtraMessageBox.Show("Printing is not available.", "Print");
        }
    }
}
