using System.Text;
using System.Text.RegularExpressions;
using Converter.Core.Parsing;
using Converter.Generator.Mapping;
using Converter.Plugin.Abstractions;
using Converter.Mappings.BuiltIn;

namespace Converter.Generator.ViewModels;

/// <summary>
/// Flat manifest of every member name BuildEditableClass seeded into the hand-editable
/// ViewModel file this run, alongside its full source. Used by the caller both to decide
/// whether to write the file at all (only on first creation - never touched again after) and,
/// on later runs where the file already exists, to detect drift (a name discovered this run
/// that isn't present in the existing hand-edited file yet) without recomputing anything twice.
/// </summary>
public record EditableClassContent(string Source, IReadOnlyList<string> MemberNames);

/// <summary>
/// Generates ViewModel classes using CommunityToolkit.Mvvm.
/// </summary>
public class ViewModelGenerator
{
    /// <summary>
    /// Generate the auto-regenerated partial class (.g.cs) - properties only, mechanically
    /// derived from ControlNode.DataBindings, safe to regenerate every run since none of it is
    /// migrated user logic. Returns string.Empty when there are zero bound properties; callers
    /// should skip writing (or delete a stale) .g.cs in that case rather than emit an empty
    /// shell. Declares no base type - ObservableObject is declared once, on the always-present
    /// hand-editable partial (see BuildEditableClass), since this file may or may not exist.
    /// </summary>
    public string GeneratePartialClass(ControlNode root, string namespaceName, string className)
    {
        var properties = ExtractBoundProperties(root);
        if (properties.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();

        sb.AppendLine("using CommunityToolkit.Mvvm.ComponentModel;");
        sb.AppendLine("using System.Collections.ObjectModel;");
        sb.AppendLine();

        sb.AppendLine($"namespace {namespaceName}.ViewModels;");
        sb.AppendLine();

        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// ViewModel for {className} (auto-generated - observable properties only).");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public partial class {className}ViewModel");
        sb.AppendLine("{");

        foreach (var prop in properties)
        {
            sb.AppendLine($"    [ObservableProperty]");
            sb.AppendLine($"    private {prop.Type} {prop.FieldName} = {prop.DefaultValue};");
            sb.AppendLine();
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// Builds the hand-editable partial class (className + ViewModelSuffix + ".cs", no ".g."
    /// infix) - written only once by the caller (skipped entirely on subsequent runs if it
    /// already exists on disk), so user edits survive reconversion. Declares
    /// ": ObservableObject" (this file always exists, unlike the conditional .g.cs, so the
    /// base type lives here). Seeds it with everything CodeBehindMemberExtractor/
    /// EventHandlerBodyParser found in the sibling WinForms code-behind: migrated fields and
    /// helper methods (accessibility upgraded to "internal" so CodeBehindGenerator's
    /// ViewModel-accessor rewrite can reach them from a different class/file), and
    /// [RelayCommand] methods for ConvertToCommand-bucket events with their real bodies as
    /// live code (falling back to a TODO when no original body was found).
    /// </summary>
    public EditableClassContent BuildEditableClass(
        ControlNode root, string namespaceName, string className,
        PluginMappingOverrides? overrides = null,
        IReadOnlyDictionary<string, string>? handlerBodies = null,
        CodeBehindMembers? codeBehindMembers = null)
    {
        overrides ??= PluginMappingOverrides.Empty;
        codeBehindMembers ??= CodeBehindMembers.Empty;
        var boundControlProperties = BuildBoundControlPropertyLookup(root);

        var memberNames = new List<string>();
        var sb = new StringBuilder();

        foreach (var usingNamespace in BuildUsingDirectives(codeBehindMembers))
        {
            sb.AppendLine($"using {usingNamespace};");
        }
        sb.AppendLine();

        sb.AppendLine($"namespace {namespaceName}.ViewModels;");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// ViewModel for {className} (user customizations).");
        sb.AppendLine("/// This file is preserved during reconversion - add your custom code here.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public partial class {className}ViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject");
        sb.AppendLine("{");

        foreach (var field in codeBehindMembers.Fields)
        {
            sb.AppendLine("    " + IndentContinuationLines(EnsureInternalAccessibility(field.DeclarationText)));
            sb.AppendLine();
            memberNames.AddRange(field.Names);
        }

        foreach (var (name, source) in codeBehindMembers.HelperMethods)
        {
            // A migrated helper method (e.g. LoadFromEntity/ValidateInput/SaveToEntity) is
            // exactly as likely as a RelayCommand body to read/write another control directly
            // (e.g. "skuTextBox.Text") - the same rewrite that already fixes that for
            // RelayCommand/property-changed-hook bodies below applies here too, so the
            // ViewModel doesn't end up referencing View-only controls.
            var rewritten = RewriteBoundControlReferences(source, boundControlProperties);
            sb.AppendLine("    " + IndentContinuationLines(EnsureInternalAccessibility(rewritten)));
            sb.AppendLine();
            memberNames.Add(name);
        }

        var commands = ExtractCommands(root, overrides);
        foreach (var command in commands)
        {
            string? originalSource = null;
            var hasOriginalSource = handlerBodies != null &&
                handlerBodies.TryGetValue(command.OriginalHandlerMethodName, out originalSource);
            // The original handler is commonly "async void" (WinForms event handlers are
            // fire-and-forget by nature); the body carries "await" regardless of whether we
            // preserve that modifier, so dropping it would emit code that doesn't compile.
            var asyncModifier = hasOriginalSource && EventHandlerBodyParser.IsAsyncMethodSignature(originalSource!) ? "async " : "";

            sb.AppendLine($"    [CommunityToolkit.Mvvm.Input.RelayCommand]");
            if (command.HasParameter)
            {
                sb.AppendLine($"    private {asyncModifier}void {command.MethodName}({command.ParameterType} parameter)");
            }
            else
            {
                sb.AppendLine($"    private {asyncModifier}void {command.MethodName}()");
            }

            if (hasOriginalSource)
            {
                var body = EventHandlerBodyParser.ExtractBodyText(originalSource!);
                body = RewriteBoundControlReferences(body, boundControlProperties);
                sb.AppendLine("    " + IndentContinuationLines(body));
            }
            else
            {
                sb.AppendLine("    {");
                sb.AppendLine($"        // TODO: Implement {command.OriginalEvent} logic");
                sb.AppendLine("    }");
            }

            sb.AppendLine();
            memberNames.Add(command.MethodName);
        }

        foreach (var hook in ExtractPropertyChangedHooks(root))
        {
            string? originalSource = null;
            var hasOriginalSource = handlerBodies != null &&
                handlerBodies.TryGetValue(hook.OriginalHandlerMethodName, out originalSource);
            if (!hasOriginalSource)
            {
                // No body to migrate - fall through to the "RequiresCustomLogic" manual step
                // ConversionOrchestrator.CollectManualStepsRecursive emits for this case instead
                // of emitting an empty/TODO hook here.
                continue;
            }

            // CommunityToolkit.Mvvm's source generator always declares this hook's partial
            // signature as plain "partial void" (never async) - an "async" implementation is
            // legal C# (partial method halves only need matching name/params/return type, not
            // matching async-ness) but muddies a feature that's already best-effort, so this
            // intentionally does not add an async modifier even if the original handler had one.
            sb.AppendLine($"    partial void On{hook.BoundPropertyName}Changed({hook.ParameterType} value)");
            var body = EventHandlerBodyParser.ExtractBodyText(originalSource!);
            body = RewriteBoundControlReferences(body, boundControlProperties);
            sb.AppendLine("    " + IndentContinuationLines(body));
            sb.AppendLine();
            memberNames.Add($"On{hook.BoundPropertyName}Changed");
        }

        sb.AppendLine("}");

        return new EditableClassContent(sb.ToString(), memberNames);
    }

    /// <summary>
    /// Maps every "controlName.ControlSideProperty" pair backed by a DataBindings.Add(...) entry
    /// anywhere under <paramref name="root"/> to the [ObservableProperty] this generator emits
    /// for it (see ExtractBoundProperties/GeneratePartialClass). A migrated event-handler body
    /// spliced into this ViewModel as live code (BuildEditableClass, below) commonly reads/writes
    /// another control directly - e.g. "textBox1.Text" - which does not compile here (the
    /// ViewModel has no "textBox1" field; that's a View concern). When the control's property is
    /// already bound, this lookup lets RewriteBoundControlReferences replace that reference with
    /// the ViewModel's own property instead, keeping the ViewModel from reaching into the View.
    /// Keyed by (control.Name, binding.PropertyName); value is the PascalCase form of
    /// binding.DataMember, matching CommunityToolkit.Mvvm's [ObservableProperty] source-generator
    /// naming convention (same casing ExtractBoundProperties' FieldName/ToCamelCase already
    /// assumes, just capitalized back to the generated public property's name).
    /// </summary>
    public IReadOnlyDictionary<(string ControlName, string ControlProperty), string> BuildBoundControlPropertyLookup(ControlNode root)
    {
        var lookup = new Dictionary<(string, string), string>();
        BuildBoundControlPropertyLookupRecursive(root, lookup);
        return lookup;
    }

    private void BuildBoundControlPropertyLookupRecursive(
        ControlNode control, Dictionary<(string, string), string> lookup)
    {
        foreach (var binding in control.DataBindings)
        {
            if (string.IsNullOrEmpty(binding.DataMember))
            {
                continue;
            }

            lookup[(control.Name, binding.PropertyName)] = ToPascalCase(binding.DataMember);
        }

        foreach (var child in control.Children)
        {
            BuildBoundControlPropertyLookupRecursive(child, lookup);
        }
    }

    /// <summary>
    /// Rewrites "controlName.ControlProperty" references in a migrated body (already spliced in
    /// as live code) into the corresponding [ObservableProperty]'s name, for every pair
    /// BuildBoundControlPropertyLookup found. A per-pair word-boundary regex, mirroring
    /// CodeBehindGenerator's identical treatment of its own migratedNames rewrite - same accepted
    /// limitation (not Roslyn-token-aware, so it can also match inside a string/comment literal
    /// or a shadowed local sharing the same name) rather than a different approach for the same
    /// class of problem. References to unbound controls are left untouched here - flagged
    /// instead as a manual step by ConversionOrchestrator using the same lookup.
    /// </summary>
    private static string RewriteBoundControlReferences(
        string body, IReadOnlyDictionary<(string ControlName, string ControlProperty), string> boundControlProperties)
    {
        foreach (var ((controlName, controlProperty), observableProperty) in boundControlProperties)
        {
            body = Regex.Replace(
                body,
                $@"\b{Regex.Escape(controlName)}\.{Regex.Escape(controlProperty)}\b",
                observableProperty);
        }

        return body;
    }

    private List<PropertyInfo> ExtractBoundProperties(ControlNode root)
    {
        var properties = new List<PropertyInfo>();
        ExtractPropertiesRecursive(root, properties);
        return properties.DistinctBy(p => p.Name).ToList();
    }

    private void ExtractPropertiesRecursive(ControlNode control, List<PropertyInfo> properties)
    {
        // Extract from data bindings
        foreach (var binding in control.DataBindings)
        {
            var propName = binding.DataMember;
            if (string.IsNullOrEmpty(propName)) continue;

            properties.Add(new PropertyInfo
            {
                Name = propName,
                FieldName = ToCamelCase(propName),
                Type = InferPropertyType(binding.PropertyName),
                DefaultValue = GetDefaultValue(InferPropertyType(binding.PropertyName))
            });
        }

        // Recursively process children
        foreach (var child in control.Children)
        {
            ExtractPropertiesRecursive(child, properties);
        }
    }

    private List<CommandInfo> ExtractCommands(ControlNode root, PluginMappingOverrides overrides)
    {
        var commands = new List<CommandInfo>();
        ExtractCommandsRecursive(root, commands, overrides);
        return commands;
    }

    private void ExtractCommandsRecursive(ControlNode control, List<CommandInfo> commands, PluginMappingOverrides overrides)
    {
        foreach (var eventHandler in control.EventHandlers)
        {
            if (eventHandler.Value == WinFormsParser.InlineLambdaHandlerMarker)
            {
                // No stable method name to generate a [RelayCommand] for; surfaced as a manual
                // step by ConversionOrchestrator.CollectManualSteps instead.
                continue;
            }

            if (overrides.EventMappings.TryGetValue((control, eventHandler.Key), out var pluginMapping))
            {
                if (pluginMapping.ConvertToCommand)
                {
                    commands.Add(new CommandInfo
                    {
                        MethodName = eventHandler.Value.Replace("_", ""),
                        OriginalEvent = eventHandler.Key,
                        OriginalHandlerMethodName = eventHandler.Value,
                        CommandName = pluginMapping.CommandName ?? $"{eventHandler.Key}Command",
                        HasParameter = RequiresParameter(eventHandler.Key),
                        ParameterType = GetParameterType(eventHandler.Key)
                    });
                }

                continue;
            }

            if (EventMappingRegistry.ShouldConvertToCommand(eventHandler.Key))
            {
                var mapping = EventMappingRegistry.GetMapping(eventHandler.Key);
                var commandName = mapping?.CommandName ?? $"{eventHandler.Key}Command";

                commands.Add(new CommandInfo
                {
                    MethodName = eventHandler.Value.Replace("_", ""),
                    OriginalEvent = eventHandler.Key,
                    OriginalHandlerMethodName = eventHandler.Value,
                    CommandName = commandName,
                    HasParameter = RequiresParameter(eventHandler.Key),
                    ParameterType = GetParameterType(eventHandler.Key)
                });
            }
        }

        foreach (var child in control.Children)
        {
            ExtractCommandsRecursive(child, commands, overrides);
        }
    }

    /// <summary>
    /// Finds TextChanged/ValueChanged/CheckedChanged handlers whose control has a matching
    /// DataBindings entry (see EventMappingRegistry.FindBoundPropertyName) - these are
    /// automatable as a CommunityToolkit property-changed hook instead of staying a manual
    /// step, since the property is already an [ObservableProperty] in the .g.cs partial.
    /// </summary>
    private List<PropertyChangedHookInfo> ExtractPropertyChangedHooks(ControlNode root)
    {
        var hooks = new List<PropertyChangedHookInfo>();
        ExtractPropertyChangedHooksRecursive(root, hooks);
        return hooks.DistinctBy(h => h.BoundPropertyName).ToList();
    }

    private void ExtractPropertyChangedHooksRecursive(ControlNode control, List<PropertyChangedHookInfo> hooks)
    {
        foreach (var (eventName, handlerName) in control.EventHandlers)
        {
            if (handlerName == WinFormsParser.InlineLambdaHandlerMarker)
            {
                continue;
            }

            var boundPropertyName = EventMappingRegistry.FindBoundPropertyName(control, eventName);
            if (boundPropertyName == null)
            {
                continue;
            }

            var winFormsPropertyName = eventName switch
            {
                "TextChanged" => "Text",
                "ValueChanged" => "Value",
                "CheckedChanged" => "Checked",
                _ => "Text"
            };

            hooks.Add(new PropertyChangedHookInfo
            {
                OriginalHandlerMethodName = handlerName,
                BoundPropertyName = boundPropertyName,
                ParameterType = InferPropertyType(winFormsPropertyName)
            });
        }

        foreach (var child in control.Children)
        {
            ExtractPropertyChangedHooksRecursive(child, hooks);
        }
    }

    private string InferPropertyType(string propertyName)
    {
        return propertyName switch
        {
            "Text" or "Name" or "Title" => "string",
            "Checked" or "Visible" or "Enabled" => "bool",
            "Value" or "SelectedIndex" => "int",
            "Items" or "DataSource" => "ObservableCollection<object>",
            _ => "string"
        };
    }

    private string GetDefaultValue(string type)
    {
        return type switch
        {
            "string" => "string.Empty",
            "bool" => "false",
            "int" => "0",
            "ObservableCollection<object>" => "new()",
            _ => "default!"
        };
    }

    private bool RequiresParameter(string eventName)
    {
        return eventName is "CellClick" or "NodeClick" or "SelectedIndexChanged";
    }

    private string GetParameterType(string eventName)
    {
        return "object";
    }

    private string ToCamelCase(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        return char.ToLowerInvariant(text[0]) + text.Substring(1);
    }

    private static string ToPascalCase(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        return char.ToUpperInvariant(text[0]) + text.Substring(1);
    }

    /// <summary>
    /// Namespaces every "ImplicitUsings"-enabled non-web .NET SDK project gets for free
    /// (System, System.Collections.Generic, ...). The generated .csproj does not enable that
    /// SDK feature, and migrated code commonly relies on it (e.g. List&lt;T&gt;, Task,
    /// Environment.NewLine) without any explicit "using" in the original WinForms file to copy
    /// forward - unlike a genuinely custom namespace, there's nothing to extract, so this list
    /// is emitted unconditionally instead.
    /// </summary>
    private static readonly string[] BaselineImplicitUsings =
    [
        "System",
        "System.Collections.Generic",
        "System.IO",
        "System.Linq",
        "System.Threading",
        "System.Threading.Tasks",
    ];

    /// <summary>
    /// Namespace prefixes that are pointless to carry over from the code-behind file's own
    /// "using" directives: WinForms/GDI+ types they exist for (System.Windows.Forms,
    /// System.Drawing.*) have no Avalonia equivalent and no reference in the generated project,
    /// so keeping the "using" just trades one CS0246 (unresolved type) for another (unresolved
    /// namespace) - the underlying manual-porting need is already surfaced separately via the
    /// "Preserved Event Handlers"/"Skipped Override Methods" manual steps.
    /// </summary>
    private static readonly string[] UnusableUsingPrefixes = ["System.Windows.Forms", "System.Drawing"];

    /// <summary>
    /// Combines BaselineImplicitUsings with whatever the sibling code-behind file itself
    /// imported (typically the app's own domain-model namespaces, e.g.
    /// "WarehouseApp.Data.Models") - filtered through UnusableUsingPrefixes and deduplicated,
    /// baseline first, then in the order the original file declared them.
    /// </summary>
    private static IEnumerable<string> BuildUsingDirectives(CodeBehindMembers codeBehindMembers)
    {
        var seen = new HashSet<string>(BaselineImplicitUsings);
        foreach (var ns in BaselineImplicitUsings)
        {
            yield return ns;
        }

        foreach (var ns in codeBehindMembers.UsingDirectives)
        {
            if (UnusableUsingPrefixes.Any(prefix => ns == prefix || ns.StartsWith(prefix + ".", StringComparison.Ordinal)))
            {
                continue;
            }

            if (seen.Add(ns))
            {
                yield return ns;
            }
        }
    }

    /// <summary>
    /// Migrated fields/helper methods are "private" or "protected" (explicit or implicit) in
    /// the original WinForms code-behind class (a migrated business-logic override - see
    /// CodeBehindMemberExtractor.KnownWinFormsOverrideMethodNames - is commonly "protected
    /// override"). Re-emitted verbatim here, CodeBehindGenerator's "ViewModel.someField"
    /// accessor rewrite (a different class, in a different file) would not compile - neither
    /// "private" nor "protected" members are visible cross-class through a typed property
    /// (protected only reaches derived classes, which this isn't). Upgrading to "internal"
    /// (same assembly, so still safely reachable) fixes that. Only ever applied to migrated
    /// fields/helper methods - freshly generated [RelayCommand] methods stay "private" exactly
    /// as before, since code-behind never calls them directly (AXAML binds through the
    /// source-generator's ICommand property instead).
    /// </summary>
    private static string EnsureInternalAccessibility(string declarationText)
    {
        if (Regex.IsMatch(declarationText, @"^\s*(private|protected)\b"))
        {
            return Regex.Replace(declarationText, @"^\s*(private|protected)\b", "internal");
        }

        if (Regex.IsMatch(declarationText, @"^\s*(public|internal)\b"))
        {
            return declarationText;
        }

        return "internal " + declarationText;
    }

    /// <summary>
    /// The generator writes source line-by-line via StringBuilder.AppendLine, each new entry
    /// starting flush at column 0; multi-line text (a field declaration spanning lines, a
    /// method body) needs every line after the first indented to match, or the emitted file
    /// looks malformed (still compiles - C# doesn't care about whitespace - but is unreadable).
    /// </summary>
    private static string IndentContinuationLines(string text) => text.Replace("\n", "\n    ");

    private record PropertyInfo
    {
        public required string Name { get; init; }
        public required string FieldName { get; init; }
        public required string Type { get; init; }
        public required string DefaultValue { get; init; }
    }

    private record CommandInfo
    {
        public required string MethodName { get; init; }
        public required string OriginalEvent { get; init; }
        public required string OriginalHandlerMethodName { get; init; }
        public required string CommandName { get; init; }
        public bool HasParameter { get; init; }
        public string ParameterType { get; init; } = "object";
    }

    private record PropertyChangedHookInfo
    {
        public required string OriginalHandlerMethodName { get; init; }
        public required string BoundPropertyName { get; init; }
        public required string ParameterType { get; init; }
    }
}
