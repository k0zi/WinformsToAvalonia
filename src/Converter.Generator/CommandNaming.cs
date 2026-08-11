namespace Converter.Generator;

/// <summary>
/// Shared naming for generated [RelayCommand] members, so the two places that must agree on a
/// command's name can never drift apart: the ViewModel generator (emits the private method that
/// CommunityToolkit.Mvvm's source generator turns into the public command property) and the AXAML
/// generator (emits Command="{Binding &lt;property&gt;}" against that source-generated property).
/// CommunityToolkit.Mvvm derives the property name from the method name, PascalCasing the first
/// letter and appending "Command": "loginButtonClick" -> "LoginButtonClickCommand".
/// </summary>
public static class CommandNaming
{
    /// <summary>
    /// The private method name the [RelayCommand] stub gets, derived from the original WinForms
    /// handler method name ("loginButton_Click" -> "loginButtonClick"). This is the exact
    /// transformation ViewModelGenerator has always applied inline.
    /// </summary>
    public static string MethodName(string winFormsHandlerName) =>
        winFormsHandlerName.Replace("_", "");

    /// <summary>
    /// The public command property name the source generator will produce for
    /// <see cref="MethodName"/>, and therefore the name the AXAML Command="{Binding ...}" must
    /// use. "loginButton_Click" -> "loginButtonClick" -> "LoginButtonClickCommand".
    /// </summary>
    public static string CommandPropertyName(string winFormsHandlerName) =>
        ToPascalCase(MethodName(winFormsHandlerName)) + "Command";

    private static string ToPascalCase(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        return char.ToUpperInvariant(text[0]) + text.Substring(1);
    }
}
