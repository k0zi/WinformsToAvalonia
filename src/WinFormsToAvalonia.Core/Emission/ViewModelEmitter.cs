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

            // Without this a derived CanExecute guard would only ever be evaluated once, and the
            // button would keep whatever enabled state it started with.
            foreach (var commandPropertyName in property.NotifiesCommands)
            {
                Line($"    [NotifyCanExecuteChangedFor(nameof({commandPropertyName}))]");
            }

            Line($"    public partial {property.ClrTypeName} {property.ViewModelPropertyName} {{ get; set; }}{property.DefaultValueSuffix}");
        }

        foreach (var command in commands)
        {
            if (!isFirstMember)
            {
                Line();
            }

            isFirstMember = false;
            var rewrite = command.Rewrite;

            Line(command.CanExecuteExpression is null
                ? "    [RelayCommand]"
                : $"    [RelayCommand(CanExecute = nameof({command.CanExecuteMethodName}))]");
            Line($"    private void {command.CommandMethodName}()");
            Line("    {");

            foreach (var statement in rewrite?.MigratedStatements ?? [])
            {
                Line($"        {statement}");
            }

            var remaining = rewrite?.RemainingBody ?? command.OriginalBody;
            if (remaining.Length > 0)
            {
                if (rewrite?.MigratedStatementCount > 0)
                {
                    Line();
                }

                var originalSignature = command.IsAsync ? $"async '{command.OriginalMethodName}'" : $"'{command.OriginalMethodName}'";
                var what = rewrite?.MigratedStatementCount > 0 ? "REMAINING WINFORMS BODY" : "ORIGINAL WINFORMS BODY";
                Line($"        /* {what} of {originalSignature} - TODO(Winforms2Avalonia): rewrite it against this ViewModel's properties.");
                Line(Indent(EscapeForBlockComment(remaining), "        "));
                Line("        */");
                Line($"        MigrationTodo.NotMigrated(nameof({command.CommandMethodName}), \"{command.OriginalMethodName}\");");
            }
            else if (rewrite?.MigratedStatementCount is null or 0)
            {
                Line($"        MigrationTodo.NotMigrated(nameof({command.CommandMethodName}), \"{command.OriginalMethodName}\");");
            }

            Line("    }");

            if (command.CanExecuteExpression is { } guard)
            {
                Line();
                Line($"    /// <summary>Derived from the WinForms handler that kept '{command.ControlFieldName}.Enabled' in sync.</summary>");
                Line($"    private bool {command.CanExecuteMethodName}() => {guard};");
            }
        }

        Line("}");

        return sb.ToString();
    }

    private static string Indent(string text, string indent) =>
        string.Join("\n", text.Replace("\r\n", "\n").Split('\n').Select(l => l.Length == 0 ? l : indent + l));

    private static string EscapeForBlockComment(string text) => text.Replace("*/", "* /");
}
