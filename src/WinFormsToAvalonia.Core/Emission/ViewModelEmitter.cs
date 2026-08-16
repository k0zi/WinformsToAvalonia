using System.Text;
using WinFormsToAvalonia.Core.Model;

namespace WinFormsToAvalonia.Core.Emission;

/// <summary>
/// Emits one Form's ViewModel from the <see cref="FormMigrationPlan"/>: an [ObservableProperty]
/// for every property the plan bound in the AXAML, and a [RelayCommand] for every handler the
/// plan promoted out of code-behind. Uses CommunityToolkit.Mvvm 8.4's field-less
/// partial-property [ObservableProperty] syntax.
/// </summary>
/// <remarks>
/// <para>
/// Nothing here is generated speculatively. Earlier revisions emitted an [ObservableProperty]
/// for every TextBox/CheckBox/NumericUpDown/ProgressBar and a [RelayCommand] for every Click
/// handler, none of which the AXAML ever referenced - dead members that also mis-promoted
/// handlers (a Click handler casting `sender`, for instance) that can only work in code-behind.
/// The plan now decides, and it only ever plans a ViewModel property together with the
/// {Binding} that feeds it.
/// </para>
/// <para>
/// A promoted command's original WinForms body is preserved inside the command as a comment
/// rather than re-emitted as code, exactly as ViewCodeBehindEmitter does for the handlers that
/// stayed event-driven - so the generated project always compiles.
/// </para>
/// </remarks>
public sealed class ViewModelEmitter
{
    public string EmitViewModel(FormMigrationPlan plan, string rootNamespace, string relativeFolder, string viewModelClassName)
    {
        var ns = NamingConventions.NamespaceOf($"{rootNamespace}.ViewModels", relativeFolder);
        var properties = plan.BoundProperties;
        var commands = plan.ViewModelCommands;

        var sb = new StringBuilder();
        void Line(string text = "") => sb.Append(text).Append('\n');

        Line("using System;");
        if (properties.Count > 0)
        {
            Line("using CommunityToolkit.Mvvm.ComponentModel;");
        }

        if (commands.Count > 0)
        {
            Line("using CommunityToolkit.Mvvm.Input;");
            Line($"using {rootNamespace}.Generated;");
        }

        Line();
        Line($"namespace {ns};");
        Line();
        Line($"public sealed partial class {viewModelClassName} : ViewModelBase");
        Line("{");

        var isFirstMember = true;

        foreach (var property in properties)
        {
            if (!isFirstMember)
            {
                Line();
            }

            isFirstMember = false;
            Line($"    /// <summary>Bound to {property.ControlFieldName}.{property.AvaloniaPropertyName} in the view.</summary>");
            Line("    [ObservableProperty]");
            Line($"    public partial {property.ClrTypeName} {property.ViewModelPropertyName} {{ get; set; }}{property.DefaultValueSuffix}");
        }

        foreach (var command in commands)
        {
            if (!isFirstMember)
            {
                Line();
            }

            isFirstMember = false;
            Line("    [RelayCommand]");
            Line($"    private void {command.CommandMethodName}()");
            Line("    {");

            if (command.OriginalBody.Length > 0)
            {
                var originalSignature = command.IsAsync ? $"async '{command.OriginalMethodName}'" : $"'{command.OriginalMethodName}'";
                Line($"        /* ORIGINAL WINFORMS BODY of {originalSignature} - TODO(Winforms2Avalonia): rewrite it against this ViewModel's properties.");
                Line(Indent(EscapeForBlockComment(command.OriginalBody), "        "));
                Line("        */");
            }

            Line($"        MigrationTodo.NotMigrated(nameof({command.CommandMethodName}), \"{command.OriginalMethodName}\");");
            Line("    }");
        }

        Line("}");

        return sb.ToString();
    }

    private static string Indent(string text, string indent) =>
        string.Join("\n", text.Replace("\r\n", "\n").Split('\n').Select(l => l.Length == 0 ? l : indent + l));

    private static string EscapeForBlockComment(string text) => text.Replace("*/", "* /");
}
