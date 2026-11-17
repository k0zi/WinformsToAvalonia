namespace LegacyFrameworkApp
{
    // A plain non-WinForms class. Used to verify DesignerFileLocator does not misclassify
    // ordinary classes as Forms/UserControls/Components.
    internal static class Helpers
    {
        public static string Greet(string name) => $"Hello, {name}!";
    }
}
