using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
        return ToIdentifier(string.IsNullOrEmpty(leaf) ? "GeneratedAvaloniaApp" : leaf);
    }

    /// <summary>
    /// Any text as a C# identifier: every character that cannot appear in one becomes <c>_</c>,
    /// and a leading digit or a reserved keyword gets a <c>_</c> in front.
    /// </summary>
    /// <remarks>
    /// A directory called <c>out</c>, <c>class</c> or <c>new</c> sanitizes to a reserved keyword,
    /// which is not a legal namespace - and every generated file opens with it, so the whole
    /// project fails to compile. Only <em>reserved</em> words need this: a contextual keyword like
    /// <c>var</c> or <c>record</c> is a perfectly good identifier.
    /// </remarks>
    public static string ToIdentifier(string text)
    {
        var builder = new StringBuilder(text.Length + 1);
        foreach (var c in text)
        {
            builder.Append(char.IsLetterOrDigit(c) || c == '_' ? c : '_');
        }

        if (builder.Length == 0 || char.IsDigit(builder[0]))
        {
            builder.Insert(0, '_');
        }

        if (SyntaxFacts.GetKeywordKind(builder.ToString()) != SyntaxKind.None)
        {
            builder.Insert(0, '_');
        }

        return builder.ToString();
    }

    /// <summary>"UserForm" -> "UserView", "MainWindow" -> "MainWindowView", "Form1" -> "Form1View".</summary>
    public static string DeriveViewName(string formClassName) => StripFormSuffix(formClassName) + "View";

    /// <summary>"UserForm" -> "UserViewModel", "MainWindow" -> "MainWindowViewModel".</summary>
    /// <summary>
    /// The wrapper <c>Window</c> generated beside a UserControl-rooted main View: `MainForm` ->
    /// `MainWindow`. <paramref name="taken"/> guards the one collision this can produce - a Form
    /// literally named `MainWindow`, whose View is already `MainWindowView`.
    /// </summary>
    public static string DeriveWindowName(string formClassName, IReadOnlySet<string>? taken = null)
    {
        var name = StripFormSuffix(formClassName) + "Window";
        return taken?.Contains(name) == true ? name + "Shell" : name;
    }

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
    /// <remarks>
    /// Each folder name becomes an identifier first: a Form in <c>User Controls/</c> or
    /// <c>2024/</c> would otherwise open its file with a namespace that does not parse. Only the
    /// namespace changes - the file still goes to the folder it came from.
    /// </remarks>
    public static string NamespaceOf(string rootNamespace, string relativeFolder) =>
        string.IsNullOrEmpty(relativeFolder)
            ? rootNamespace
            : $"{rootNamespace}.{string.Join('.', relativeFolder.Split('/').Select(ToIdentifier))}";

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
