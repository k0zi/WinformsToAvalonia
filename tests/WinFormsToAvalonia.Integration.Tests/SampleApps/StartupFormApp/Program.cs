namespace StartupFormApp;

// The startup Form is named by a local rather than inline, and it is not the Form discovery
// lists first: AboutForm sorts ahead of MainForm, which is exactly what used to become the
// converted app's main window.
internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        var mainForm = new MainForm();
        Application.Run(mainForm);
    }
}
