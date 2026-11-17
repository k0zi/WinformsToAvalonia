namespace BaseFormApp.BaseForms;

/// <summary>
/// The shared base every form in this app derives from - the shape that used to make the
/// converter classify the derived form as Other and skip it entirely.
/// </summary>
public class AppFormBase : Form
{
    protected void ApplyHouseStyle()
    {
        this.StartPosition = FormStartPosition.CenterScreen;
    }
}
