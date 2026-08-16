using System.Text;

namespace WinFormsToAvalonia.Core.Emission;

public static class NamingConventions
{
    /// <summary>
    /// Derives a valid C# project/root-namespace identifier from the output directory's leaf name.
    /// </summary>
    public static string DeriveProjectName(string outputDirectory)
    {
        var leaf = Path.GetFileName(outputDirectory.TrimEnd('/', '\\'));
        if (string.IsNullOrEmpty(leaf))
        {
            leaf = "GeneratedAvaloniaApp";
        }

        var builder = new StringBuilder(leaf.Length);
        foreach (var c in leaf)
        {
            if (char.IsLetterOrDigit(c) || c == '_')
            {
                builder.Append(c);
            }
            else
            {
                builder.Append('_');
            }
        }

        if (builder.Length == 0 || char.IsDigit(builder[0]))
        {
            builder.Insert(0, '_');
        }

        return builder.ToString();
    }

    /// <summary>"UserForm" -> "UserView", "MainWindow" -> "MainWindowView", "Form1" -> "Form1View".</summary>
    public static string DeriveViewName(string formClassName) => StripFormSuffix(formClassName) + "View";

    /// <summary>"UserForm" -> "UserViewModel", "MainWindow" -> "MainWindowViewModel".</summary>
    public static string DeriveViewModelName(string formClassName) => StripFormSuffix(formClassName) + "ViewModel";

    /// <summary>
    /// "button1_Click" -> "Button1", "OnSave" -> "OnSave". CommunityToolkit turns the result
    /// into the ICommand property name by appending "Command".
    /// </summary>
    public static string DeriveCommandName(string handlerMethodName, string eventName)
    {
        var suffix = $"_{eventName}";
        var basis = handlerMethodName.EndsWith(suffix, StringComparison.Ordinal)
            ? handlerMethodName[..^suffix.Length]
            : handlerMethodName;

        return basis.Length == 0 ? $"On{eventName}" : Capitalize(basis);
    }

    /// <summary>"Demo.Views" + "Admin/Users" -> "Demo.Views.Admin.Users".</summary>
    public static string NamespaceOf(string rootNamespace, string relativeFolder) =>
        string.IsNullOrEmpty(relativeFolder) ? rootNamespace : $"{rootNamespace}.{relativeFolder.Replace('/', '.')}";

    public static string Capitalize(string value) =>
        value.Length == 0 ? value : char.ToUpperInvariant(value[0]) + value[1..];

    private static string StripFormSuffix(string name)
    {
        if (name.Length > 4 && name.EndsWith("Form", StringComparison.Ordinal))
        {
            return name[..^4];
        }

        if (name.Length > 3 && name.EndsWith("Frm", StringComparison.Ordinal))
        {
            return name[..^3];
        }

        return name;
    }
}
