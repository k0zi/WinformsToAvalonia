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
        var collections = plan.DataSourceBindings;
        var commands = plan.ViewModelCommands;

        var sb = new StringBuilder();
        void Line(string text = "") => sb.Append(text).Append('\n');

        Line("using System;");
        if (collections.Count > 0)
        {
            Line("using System.Collections.ObjectModel;");
        }

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

        // A get-only ObservableCollection rather than an [ObservableProperty]: the reference never
        // changes, and what ItemsSource actually listens to is INotifyCollectionChanged, which the
        // collection raises itself. `object` because the row type is usually a private nested class
        // in the WinForms form that cannot be carried over - and does not need to be, since the
        // generated columns bind with {ReflectionBinding}.
        foreach (var collection in collections)
        {
            if (!isFirstMember)
            {
                Line();
            }

            isFirstMember = false;
            Line($"    /// <summary>Bound to {collection.ControlFieldName}.ItemsSource in the view, replacing");
            Line($"    /// the WinForms BindingSource '{collection.SourceFieldName}'. Generated empty: the wiring is");
            Line("    /// here, the rows are not - populate it where the WinForms code set DataSource.</summary>");
            Line($"    public ObservableCollection<object> {collection.ViewModelPropertyName} {{ get; }} = [];");
        }

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
                // Indent every line, not just the first: a translated `if` spans several.
                Line(Indent(statement, "        "));
            }

            var remaining = command.RemainingBody;
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

        foreach (var helper in plan.ViewModelHelpers)
        {
            if (!isFirstMember)
            {
                Line();
            }

            isFirstMember = false;
            EmitViewModelHelper(Line, helper);
        }

        Line("}");

        return sb.ToString();
    }

    /// <summary>
    /// A code-behind helper that moved here with the command that calls it.
    /// </summary>
    /// <remarks>
    /// No <c>MigrationTodo</c> and no preserved comment: a helper is emitted at all only when its
    /// whole body translated, so there is nothing left to report. The View keeps its own copy for
    /// the handlers that stayed event-driven - two translations of one method, because the two
    /// sides address the same control differently (a field here, a bound property there).
    /// </remarks>
    private static void EmitViewModelHelper(Action<string> line, PromotedHelperPlan helper)
    {
        var asyncModifier = helper.IsAsync ? "async " : "";
        var returnType = helper.IsAsync ? "Task" : helper.ReturnTypeText;

        line($"    private {asyncModifier}{returnType} {helper.Name}{helper.ParameterListText}");
        line("    {");

        foreach (var statement in helper.Rewrite.MigratedStatements)
        {
            line(Indent(statement, "        "));
        }

        line("    }");
    }

    private static string Indent(string text, string indent) =>
        string.Join("\n", text.Replace("\r\n", "\n").Split('\n').Select(l => l.Length == 0 ? l : indent + l));

    private static string EscapeForBlockComment(string text) => text.Replace("*/", "* /");
}
