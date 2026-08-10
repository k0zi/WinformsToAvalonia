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
            sb.AppendLine("    " + IndentContinuationLines(EnsureInternalAccessibility(source)));
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

        sb.AppendLine("}");

        return new EditableClassContent(sb.ToString(), memberNames);
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
    /// Migrated fields/helper methods are "private" (explicit or implicit) in the original
    /// WinForms code-behind class. Re-emitted verbatim as "private" here, CodeBehindGenerator's
    /// "ViewModel.someField" accessor rewrite (a different class, in a different file) would
    /// not compile - private members aren't visible cross-class even through a typed property.
    /// Upgrading to "internal" (same assembly, so still safely reachable) fixes that. Only ever
    /// applied to migrated fields/helper methods - freshly generated [RelayCommand] methods
    /// stay "private" exactly as before, since code-behind never calls them directly (AXAML
    /// binds through the source-generator's ICommand property instead).
    /// </summary>
    private static string EnsureInternalAccessibility(string declarationText)
    {
        if (Regex.IsMatch(declarationText, @"^\s*private\b"))
        {
            return Regex.Replace(declarationText, @"^\s*private\b", "internal");
        }

        if (Regex.IsMatch(declarationText, @"^\s*(public|protected|internal)\b"))
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
}
