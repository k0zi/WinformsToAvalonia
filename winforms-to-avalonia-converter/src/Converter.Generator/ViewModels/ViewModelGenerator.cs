using System.Text;
using Converter.Plugin.Abstractions;
using Converter.Mappings.BuiltIn;

namespace Converter.Generator.ViewModels;

/// <summary>
/// Generates ViewModel classes using CommunityToolkit.Mvvm.
/// </summary>
public class ViewModelGenerator
{
    /// <summary>
    /// Generate a ViewModel class (generated partial class .g.cs).
    /// </summary>
    public string GeneratePartialClass(ControlNode root, string namespaceName, string className)
    {
        var sb = new StringBuilder();

        // Using statements
        sb.AppendLine("using CommunityToolkit.Mvvm.ComponentModel;");
        sb.AppendLine("using CommunityToolkit.Mvvm.Input;");
        sb.AppendLine("using System.Collections.ObjectModel;");
        sb.AppendLine();

        // Namespace
        sb.AppendLine($"namespace {namespaceName}.ViewModels;");
        sb.AppendLine();

        // Class declaration
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// ViewModel for {className} (auto-generated).");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public partial class {className}ViewModel : ObservableObject");
        sb.AppendLine("{");

        // Generate properties from data bindings
        var properties = ExtractBoundProperties(root);
        foreach (var prop in properties)
        {
            sb.AppendLine($"    [ObservableProperty]");
            sb.AppendLine($"    private {prop.Type} {prop.FieldName} = {prop.DefaultValue};");
            sb.AppendLine();
        }

        // Generate commands from events
        var commands = ExtractCommands(root);
        foreach (var command in commands)
        {
            sb.AppendLine($"    [RelayCommand]");
            if (command.HasParameter)
            {
                sb.AppendLine($"    private void {command.MethodName}({command.ParameterType} parameter)");
            }
            else
            {
                sb.AppendLine($"    private void {command.MethodName}()");
            }
            sb.AppendLine("    {");
            sb.AppendLine($"        // TODO: Implement {command.OriginalEvent} logic");
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// Generate user-editable partial class (.cs) - only created once.
    /// </summary>
    public string GenerateUserClass(string namespaceName, string className)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"namespace {namespaceName}.ViewModels;");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// ViewModel for {className} (user customizations).");
        sb.AppendLine("/// This file is preserved during reconversion - add your custom code here.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public partial class {className}ViewModel");
        sb.AppendLine("{");
        sb.AppendLine("    // Add your custom properties and methods here");
        sb.AppendLine("    // This file will not be regenerated");
        sb.AppendLine("}");

        return sb.ToString();
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

    private List<CommandInfo> ExtractCommands(ControlNode root)
    {
        var commands = new List<CommandInfo>();
        ExtractCommandsRecursive(root, commands);
        return commands;
    }

    private void ExtractCommandsRecursive(ControlNode control, List<CommandInfo> commands)
    {
        foreach (var eventHandler in control.EventHandlers)
        {
            if (EventMappingRegistry.ShouldConvertToCommand(eventHandler.Key))
            {
                var mapping = EventMappingRegistry.GetMapping(eventHandler.Key);
                var commandName = mapping?.CommandName ?? $"{eventHandler.Key}Command";

                commands.Add(new CommandInfo
                {
                    MethodName = eventHandler.Value.Replace("_", ""),
                    OriginalEvent = eventHandler.Key,
                    CommandName = commandName,
                    HasParameter = RequiresParameter(eventHandler.Key),
                    ParameterType = GetParameterType(eventHandler.Key)
                });
            }
        }

        foreach (var child in control.Children)
        {
            ExtractCommandsRecursive(child, commands);
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
        public required string CommandName { get; init; }
        public bool HasParameter { get; init; }
        public string ParameterType { get; init; } = "object";
    }
}
