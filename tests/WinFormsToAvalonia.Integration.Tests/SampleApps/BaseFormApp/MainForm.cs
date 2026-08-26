using BaseFormApp.BaseForms;

namespace BaseFormApp;

public partial class MainForm : AppFormBase
{
    public MainForm()
    {
        InitializeComponent();
        ApplyHouseStyle();
    }

    private void okButton_Click(object sender, EventArgs e)
    {
        this.statusLabel.Text = "Clicked";
    }
}
