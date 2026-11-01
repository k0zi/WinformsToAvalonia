using VisualInheritanceApp.Dialogs;

namespace VisualInheritanceApp;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new SettingsForm());
    }
}
