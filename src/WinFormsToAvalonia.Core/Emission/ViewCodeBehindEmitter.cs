using System.Text;
using WinFormsToAvalonia.Core.Mapping;
using WinFormsToAvalonia.Core.Model;

namespace WinFormsToAvalonia.Core.Emission;

/// <summary>
/// Emits one Form's View code-behind (.axaml.cs): the constructor Avalonia needs, one real
/// method per event handler that stayed event-driven (per <see cref="FormMigrationPlan"/>),
/// the preserved non-handler members, and - unless skipped - the leftover original file as a
/// trailing comment.
/// </summary>
/// <remarks>
/// <para>
/// Every generated handler has the correct Avalonia signature and is subscribed (from AXAML, by
/// AxamlEmitter), but its <em>body</em> is never re-emitted as compiling code: WinForms API calls
/// like <c>treeView1.Nodes.Add(...)</c> or <c>MessageBox.Show(...)</c> would not compile against
/// Avalonia. Instead the original body sits inside the method as a comment, followed by a
/// <see cref="NotImplementedException"/>. The generated project therefore always builds, and the
/// unit of migration is one method a developer can open and rewrite in place - rather than a
/// single file-sized comment block at the bottom of the class.
/// </para>
/// <para>
/// The one real correctness hazard is a literal <c>*/</c> inside the original source, which would
/// close the comment early and break the build; it is neutralized to <c>* /</c> before embedding.
/// </para>
/// </remarks>
public sealed class ViewCodeBehindEmitter
{
    public string EmitViewCodeBehind(
        string rootNamespace,
        string relativeFolder,
        string viewClassName,
        string viewModelClassName,
        FormMigrationPlan plan,
        RawCodeBehind? rawCodeBehind,
        WinFormsArtifactKind artifactKind = WinFormsArtifactKind.Form)
    {
        var ns = NamingConventions.NamespaceOf($"{rootNamespace}.Views", relativeFolder);
        var viewModelNamespace = NamingConventions.NamespaceOf($"{rootNamespace}.ViewModels", relativeFolder);

        var sb = new StringBuilder();
        void Line(string text = "") => sb.Append(text).Append('\n');

        // A namespace can be asked for from several places at once - the fixed set below, an
        // EventArgs type, a translated statement - so they go through one gate. A duplicate
        // using is not fatal, but it is noise in a file a human is expected to read and edit.
        var emittedUsings = new HashSet<string>(StringComparer.Ordinal);
        void Using(string namespaceName)
        {
            if (emittedUsings.Add(namespaceName))
            {
                Line($"using {namespaceName};");
            }
        }

        Using("System");
        if (plan.FileDialogs.Count > 0 || plan.PromotedHelpers.Any(h => h.IsAsync))
        {
            Using("System.Threading.Tasks");
        }

        Using("Avalonia.Controls");
        Using("Avalonia.Input");
        Using("Avalonia.Interactivity");
        if (plan.FileDialogs.Count > 0)
        {
            Using("Avalonia.Platform.Storage");
        }

        if (plan.Timers.Count > 0)
        {
            Using("Avalonia.Threading");
        }

        foreach (var namespaceName in ExtraEventArgsNamespaces(plan))
        {
            Using(namespaceName);
        }

        // The non-visual components emitted as real fields, and the .NET args types their
        // events carry.
        foreach (var namespaceName in ComponentNamespaces(plan))
        {
            Using(namespaceName);
        }

        // Namespaces the translated handler statements need - the desktop lifetime behind
        // Application.Exit(), or another Form's View for a navigation call. Only what this plan
        // actually emitted, and never this View's own namespace, so no View gets a stray using.
        foreach (var namespaceName in plan.RequiredUsings.Where(n => !string.Equals(n, ns, StringComparison.Ordinal)))
        {
            Using(namespaceName);
        }

        if (plan.RequiredFallbackKeys.Count > 0)
        {
            Using($"{rootNamespace}.Controls");
        }

        Using($"{rootNamespace}.Generated");
        Using(viewModelNamespace);
        Line();
        Line($"namespace {ns};");
        Line();
        // Must match the AXAML root element AxamlEmitter chose for the same artifact - a
        // partial class whose base type disagrees with its .axaml root is an AVLN2000 error.
        var baseTypeName = artifactKind == WinFormsArtifactKind.UserControl ? "UserControl" : "Window";

        // Windows-only components are declared and used all over this class - the field, the
        // constructor, and whichever handlers touch them - so the platform analyser is silenced
        // for the file rather than at each site. Scoped to the one file that needs it, never the
        // project, and the conversion report names every component this covers: the generated app
        // compiles everywhere and throws at these points off Windows.
        var windowsOnly = plan.Components.Where(c => c.WindowsOnly).Select(c => c.ClrTypeName).Distinct(StringComparer.Ordinal).ToList();
        if (windowsOnly.Count > 0)
        {
            Line($"// This view uses {string.Join(", ", windowsOnly)}, which .NET marks as Windows-only.");
            Line("// The generated project targets net10.0 so it builds everywhere; these calls throw elsewhere.");
            Line("#pragma warning disable CA1416");
            Line();
        }

        Line($"public partial class {viewClassName} : {baseTypeName}");
        Line("{");

        foreach (var timer in plan.Timers)
        {
            Line($"    private readonly DispatcherTimer {timer.FieldName};");
        }

        foreach (var component in plan.Components)
        {
            Line($"    private readonly {component.ClrTypeName} {component.FieldName} = new();");
        }

        // The Form's own backing fields, carried over so the helpers that maintain them can be
        // real code rather than a comment.
        foreach (var field in plan.PromotedFields)
        {
            var modifiers = field.ModifiersText.Length > 0 ? field.ModifiersText + " " : "";
            var initializer = field.InitializerText is { } value ? $" = {value}" : "";
            Line($"    private {modifiers}{field.TypeText} {field.Name}{initializer};");
        }

        if (plan.Timers.Count > 0 || plan.Components.Count > 0 || plan.PromotedFields.Count > 0)
        {
            Line();
        }

        Line($"    public {viewClassName}()");
        Line("    {");
        Line("        InitializeComponent();");
        Line($"        DataContext = new {viewModelClassName}();");

        foreach (var component in plan.Components)
        {
            if (component.Initializers.Count == 0 && component.Subscriptions.Count == 0)
            {
                continue;
            }

            Line();
            foreach (var initializer in component.Initializers)
            {
                Line($"        {component.FieldName}.{initializer};");
            }

            foreach (var (eventName, handlerMethodName) in component.Subscriptions)
            {
                Line($"        {component.FieldName}.{eventName} += {handlerMethodName};");
            }
        }

        foreach (var timer in plan.Timers)
        {
            Line();
            Line($"        {timer.FieldName} = new DispatcherTimer {{ Interval = TimeSpan.FromMilliseconds({timer.IntervalMilliseconds}) }};");
            Line($"        {timer.FieldName}.Tick += {timer.TickHandlerMethodName};");
            if (timer.StartImmediately)
            {
                Line($"        {timer.FieldName}.Start();");
            }
        }

        if (plan.ConstructorExtraStatements.Count > 0)
        {
            Line();
            Line("        /* ORIGINAL WINFORMS CONSTRUCTOR STATEMENTS - TODO(Winforms2Avalonia): migrate.");
            foreach (var statement in plan.ConstructorExtraStatements)
            {
                Line($"           {EscapeForBlockComment(statement)}");
            }

            Line("        */");
        }

        Line("    }");

        foreach (var handler in plan.CodeBehindHandlers)
        {
            Line();
            EmitHandler(Line, handler);
        }

        foreach (var dialog in plan.FileDialogs)
        {
            Line();
            EmitFileDialogMethod(Line, dialog);
        }

        foreach (var helper in plan.PromotedHelpers)
        {
            Line();
            EmitPromotedHelper(Line, helper);
        }

        if (plan.PreservedMembers.Count > 0)
        {
            Line();
            Line("    /* ORIGINAL WINFORMS MEMBERS - NOT COMPILED, PRESERVED FOR MANUAL MIGRATION");
            foreach (var member in plan.PreservedMembers)
            {
                Line();
                Line(Indent(EscapeForBlockComment(member.SourceText), "    "));
            }

            Line("    */");
        }

        if (rawCodeBehind is not null)
        {
            Line();
            Line("    /* ORIGINAL WINFORMS CODE-BEHIND - NOT COMPILED, PRESERVED FOR REFERENCE");
            Line($"       Original file: {Path.GetFileName(rawCodeBehind.OriginalFilePath)}");
            Line();
            Line(EscapeForBlockComment(rawCodeBehind.FullText));
            Line("    */");
        }

        Line("}");

        if (windowsOnly.Count > 0)
        {
            Line();
            Line("#pragma warning restore CA1416");
        }

        return sb.ToString();
    }

    /// <summary>
    /// The generated method is deliberately never declared `async`, even when the original was:
    /// its body is a single non-awaiting call, and an `async void` method with no `await`
    /// compiles with a CS1998 warning. The original modifier is recorded in the TODO comment
    /// instead, so a developer restoring the body knows to add `async` back.
    /// </summary>
    /// <remarks>
    /// The body reports through <c>MigrationTodo.NotMigrated</c> rather than throwing. Avalonia
    /// invokes these from the framework - including during XAML initialization, where a
    /// TabControl selecting its first tab raises SelectionChanged and a Window raises Loaded -
    /// so a throwing stub killed the generated app before its first window appeared. See
    /// AvaloniaProjectScaffolder.BuildMigrationTodo, including the switch that restores
    /// throwing.
    /// </remarks>
    private static void EmitHandler(Action<string> line, CodeBehindHandlerPlan handler)
    {
        var rewrite = handler.Rewrite;
        var asyncModifier = rewrite?.RequiresAsync == true ? "async " : "";

        line($"    private {asyncModifier}void {handler.MethodName}(object? sender, {handler.EventArgsTypeName} e)");
        line("    {");

        foreach (var statement in rewrite?.MigratedStatements ?? [])
        {
            // Indent every line, not just the first: a translated `if` spans several.
            line(Indent(statement, "        "));
        }

        var remaining = rewrite?.RemainingBody ?? handler.OriginalBody;
        if (remaining.Length > 0)
        {
            if (rewrite?.MigratedStatementCount > 0)
            {
                line("");
            }

            var originalSignature = handler.IsAsync ? $"async '{handler.OriginalMethodName}'" : $"'{handler.OriginalMethodName}'";
            var what = rewrite?.MigratedStatementCount > 0 ? "REMAINING WINFORMS BODY" : "ORIGINAL WINFORMS BODY";
            line($"        /* {what} of {originalSignature} - TODO(Winforms2Avalonia): migrate it into this method.");
            line(Indent(EscapeForBlockComment(remaining), "        "));
            line("        */");
            line($"        MigrationTodo.NotMigrated(nameof({handler.MethodName}), \"{handler.OriginalMethodName}\");");
        }
        else if (rewrite?.MigratedStatementCount is null or 0)
        {
            // An empty original body: nothing to migrate, but the subscription still needs a method.
            line($"        MigrationTodo.NotMigrated(nameof({handler.MethodName}), \"{handler.OriginalMethodName}\");");
        }

        line("    }");
    }

    /// <summary>
    /// The Avalonia replacement for a WinForms file dialog's <c>ShowDialog()</c>. It lives on the
    /// View because <c>StorageProvider</c> hangs off the TopLevel - and the View is the TopLevel,
    /// so `this` is all it needs. The designer never records which button opened which dialog
    /// (handler bodies aren't part of InitializeComponent), so this is emitted as a callable
    /// method rather than wired to any particular control.
    /// </summary>
    private static void EmitFileDialogMethod(Action<string> line, FileDialogPlan dialog)
    {
        line($"    /// <summary>Avalonia replacement for the WinForms '{dialog.FieldName}.ShowDialog()' call. Call this from the handler that used to open it.</summary>");
        line($"    private async Task {dialog.MethodName}()");
        line("    {");
        line($"        var result = await StorageProvider.{dialog.PickerMethodName}(new {dialog.OptionsTypeName}());");
        line($"        throw new NotImplementedException(\"TODO(Winforms2Avalonia): migrate the logic that used to run after '{dialog.FieldName}.ShowDialog()' here, using 'result'.\");");
        line("    }");
    }

    /// <summary>
    /// Avalonia EventArgs types that live outside the four namespaces every generated View
    /// already imports (Avalonia.Controls/Input/Interactivity and System). Only the ones a
    /// handler in this plan actually uses are imported, so no View gets an unused using.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> EventArgsNamespaces =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["RangeBaseValueChangedEventArgs"] = "Avalonia.Controls.Primitives",
            ["ScrollEventArgs"] = "Avalonia.Controls.Primitives",
        };

    /// <summary>
    /// The namespaces a plan's component fields need: the component types themselves, plus the
    /// .NET args types their events carry - which are not Avalonia types, so nothing else brings
    /// them in.
    /// </summary>
    private static IEnumerable<string> ComponentNamespaces(FormMigrationPlan plan) =>
        plan.Components
            .SelectMany(c => new[] { c.Namespace }.Concat(
                c.Subscriptions
                    .Select(s => ComponentFieldCatalog.TryGetEvent(c.ClrTypeName, s.EventName, out var e) ? e.ArgsNamespace : null)
                    .OfType<string>()))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(n => n, StringComparer.Ordinal);

    private static IEnumerable<string> ExtraEventArgsNamespaces(FormMigrationPlan plan) =>
        plan.CodeBehindHandlers
            .Select(h => h.EventArgsTypeName)
            .Where(EventArgsNamespaces.ContainsKey)
            .Select(t => EventArgsNamespaces[t])
            .Distinct(StringComparer.Ordinal)
            .OrderBy(n => n, StringComparer.Ordinal);

    /// <summary>
    /// A helper method whose whole body translated. Unlike a handler this carries no
    /// <c>MigrationTodo</c> and no comment: a helper is emitted as code only when there is
    /// nothing left over, so there is nothing to report.
    /// </summary>
    private static void EmitPromotedHelper(Action<string> line, PromotedHelperPlan helper)
    {
        var asyncModifier = helper.IsAsync ? "async " : "";

        // A helper that turned async returns `Task`, never `async void`: its callers await it,
        // and `void` is not awaitable. The planner only promotes void helpers into this shape.
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
