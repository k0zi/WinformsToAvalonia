using System.Text;
using Converter.Generator.Mapping;
using Converter.Plugin.Abstractions;
using Converter.Mappings.BuiltIn;

namespace Converter.Generator.Axaml;

/// <summary>
/// Generates Avalonia AXAML markup from control trees.
/// </summary>
public class AxamlGenerator
{
    /// <summary>
    /// Generate AXAML for a control tree. <paramref name="overrides"/> - resolved once per
    /// form by MappingResolver before generation starts - is threaded through every
    /// recursive helper as an explicit parameter, never stored as an instance field, so
    /// concurrent Generate() calls on a shared AxamlGenerator instance (this class holds no
    /// mutable state) never touch shared state; each call carries its own overrides down its
    /// own call stack.
    /// </summary>
    public string Generate(
        ControlNode root, LayoutAnalysisResult layoutInfo, string namespaceName, string className,
        PluginMappingOverrides? overrides = null)
    {
        overrides ??= PluginMappingOverrides.Empty;
        var sb = new StringBuilder();

        // Write AXAML header
        sb.AppendLine($"<Window xmlns=\"https://github.com/avaloniaui\"");
        sb.AppendLine("        xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"");
        sb.AppendLine($"        xmlns:vm=\"using:{namespaceName}.ViewModels\"");
        sb.AppendLine($"        x:Class=\"{namespaceName}.Views.{className}\"");
        sb.AppendLine($"        x:DataType=\"vm:{className}ViewModel\"");

        // Add window properties
        WriteControlProperties(sb, root, indent: "        ", namespaceName, overrides);
        WriteEventAttributes(sb, root, indent: "        ", overrides);

        sb.AppendLine("        >");

        // Write design-time DataContext
        sb.AppendLine("    <Design.DataContext>");
        sb.AppendLine($"        <vm:{className}ViewModel/>");
        sb.AppendLine("    </Design.DataContext>");
        sb.AppendLine();

        // Write content based on layout type
        WriteLayoutContainer(sb, root, layoutInfo, "    ", namespaceName, overrides);

        sb.AppendLine("</Window>");

        return sb.ToString();
    }

    private void WriteLayoutContainer(StringBuilder sb, ControlNode control, LayoutAnalysisResult layoutInfo, string indent, string namespaceName, PluginMappingOverrides overrides)
    {
        switch (layoutInfo.LayoutType)
        {
            case LayoutType.Grid:
                WriteGridLayout(sb, control, layoutInfo, indent, namespaceName, overrides);
                break;
            case LayoutType.StackPanel:
                WriteStackPanelLayout(sb, control, layoutInfo, indent, namespaceName, overrides);
                break;
            case LayoutType.DockPanel:
                WriteDockPanelLayout(sb, control, layoutInfo, indent, namespaceName, overrides);
                break;
            case LayoutType.Canvas:
            default:
                WriteCanvasLayout(sb, control, layoutInfo, indent, namespaceName, overrides);
                break;
        }
    }

    private void WriteGridLayout(StringBuilder sb, ControlNode control, LayoutAnalysisResult layoutInfo, string indent, string namespaceName, PluginMappingOverrides overrides)
    {
        sb.AppendLine($"{indent}<Grid>");

        // Extract grid metadata
        if (layoutInfo.Metadata.TryGetValue("Rows", out var rowsObj) && rowsObj is int rows &&
            layoutInfo.Metadata.TryGetValue("Columns", out var colsObj) && colsObj is int cols)
        {
            // Write row definitions
            sb.AppendLine($"{indent}    <Grid.RowDefinitions>");
            for (int i = 0; i < rows; i++)
            {
                sb.AppendLine($"{indent}        <RowDefinition Height=\"Auto\"/>");
            }
            sb.AppendLine($"{indent}    </Grid.RowDefinitions>");

            // Write column definitions
            sb.AppendLine($"{indent}    <Grid.ColumnDefinitions>");
            for (int i = 0; i < cols; i++)
            {
                sb.AppendLine($"{indent}        <ColumnDefinition Width=\"Auto\"/>");
            }
            sb.AppendLine($"{indent}    </Grid.ColumnDefinitions>");
            sb.AppendLine();
        }

        // Write child controls
        WriteChildren(sb, control, layoutInfo, indent + "    ", namespaceName, overrides);

        sb.AppendLine($"{indent}</Grid>");
    }

    private void WriteStackPanelLayout(StringBuilder sb, ControlNode control, LayoutAnalysisResult layoutInfo, string indent, string namespaceName, PluginMappingOverrides overrides)
    {
        var orientation = layoutInfo.Metadata.TryGetValue("Orientation", out var orientObj) &&
                         orientObj?.ToString() == "Horizontal" ? "Horizontal" : "Vertical";

        sb.AppendLine($"{indent}<StackPanel Orientation=\"{orientation}\">");
        WriteChildren(sb, control, layoutInfo, indent + "    ", namespaceName, overrides);
        sb.AppendLine($"{indent}</StackPanel>");
    }

    private void WriteDockPanelLayout(StringBuilder sb, ControlNode control, LayoutAnalysisResult layoutInfo, string indent, string namespaceName, PluginMappingOverrides overrides)
    {
        sb.AppendLine($"{indent}<DockPanel>");
        WriteChildren(sb, control, layoutInfo, indent + "    ", namespaceName, overrides);
        sb.AppendLine($"{indent}</DockPanel>");
    }

    private void WriteCanvasLayout(StringBuilder sb, ControlNode control, LayoutAnalysisResult layoutInfo, string indent, string namespaceName, PluginMappingOverrides overrides)
    {
        sb.AppendLine($"{indent}<Canvas>");
        WriteChildren(sb, control, layoutInfo, indent + "    ", namespaceName, overrides);
        sb.AppendLine($"{indent}</Canvas>");
    }

    private void WriteChildren(StringBuilder sb, ControlNode control, LayoutAnalysisResult layoutInfo, string indent, string namespaceName, PluginMappingOverrides overrides)
    {
        foreach (var child in control.Children)
        {
            WriteControl(sb, child, layoutInfo, indent, namespaceName, overrides);
        }
    }

    private void WriteControl(StringBuilder sb, ControlNode control, LayoutAnalysisResult layoutInfo, string indent, string namespaceName, PluginMappingOverrides overrides)
    {
        if (overrides.ControlMappings.TryGetValue(control, out var pluginMapping))
        {
            WritePluginMappedControl(sb, control, pluginMapping, layoutInfo, indent, namespaceName, overrides);
            return;
        }

        var mapping = ControlMappingRegistry.GetMapping(control.ControlType);
        if (mapping == null)
        {
            WriteUnmappedControl(sb, control, layoutInfo, indent, namespaceName, overrides);
            return;
        }

        var avaloniaType = mapping.AvaloniaType;
        sb.Append($"{indent}<{avaloniaType}");

        // Write Name
        sb.Append($" Name=\"{control.Name}\"");

        // Write properties
        WriteControlProperties(sb, control, indent, namespaceName, overrides);
        WriteEventAttributes(sb, control, indent, overrides);

        if (control.Children.Count > 0)
        {
            sb.AppendLine(">");

            // Recursively write children if applicable
            if (layoutInfo.ChildLayouts.TryGetValue(control.Name, out var childLayout))
            {
                WriteLayoutContainer(sb, control, childLayout, indent + "    ", namespaceName, overrides);
            }
            else
            {
                WriteChildren(sb, control, layoutInfo, indent + "    ", namespaceName, overrides);
            }

            sb.AppendLine($"{indent}</{avaloniaType}>");
        }
        else
        {
            sb.AppendLine(" />");
        }
    }

    /// <summary>
    /// Writes a control a plugin IControlMapper claimed. If the plugin supplied raw
    /// CustomAxaml, it's spliced in verbatim (v1 scope - no further property merging).
    /// Otherwise a normal element is emitted from AvaloniaControlType + Properties. Any
    /// plugin-supplied ManualSteps are emitted as inline AXAML comments (not yet plumbed
    /// into the structured ManualStepInfo/migration-guide list - a reasonable follow-up once
    /// this base wiring has proven out).
    /// </summary>
    private void WritePluginMappedControl(StringBuilder sb, ControlNode control, ControlMappingResult pluginMapping, LayoutAnalysisResult layoutInfo, string indent, string namespaceName, PluginMappingOverrides overrides)
    {
        foreach (var manualStep in pluginMapping.ManualSteps)
        {
            sb.AppendLine($"{indent}<!-- Plugin manual step ({control.Name}): {manualStep} -->");
        }

        if (pluginMapping.CustomAxaml != null)
        {
            sb.AppendLine(pluginMapping.CustomAxaml.TrimEnd());
            return;
        }

        var avaloniaType = pluginMapping.AvaloniaControlType;
        sb.Append($"{indent}<{avaloniaType}");
        sb.Append($" Name=\"{control.Name}\"");

        foreach (var (propName, propValue) in pluginMapping.Properties)
        {
            AppendAttribute(sb, indent, propName, propValue?.ToString());
        }

        if (control.Children.Count > 0)
        {
            sb.AppendLine(">");

            if (layoutInfo.ChildLayouts.TryGetValue(control.Name, out var childLayout))
            {
                WriteLayoutContainer(sb, control, childLayout, indent + "    ", namespaceName, overrides);
            }
            else
            {
                WriteChildren(sb, control, layoutInfo, indent + "    ", namespaceName, overrides);
            }

            sb.AppendLine($"{indent}</{avaloniaType}>");
        }
        else
        {
            sb.AppendLine(" />");
        }
    }

    /// <summary>
    /// Writes an unmapped control as a TODO comment, but - unlike dropping the whole
    /// branch - still recurses into its children wrapped in a plain Panel, so mapped
    /// descendants nested inside an unmapped custom/third-party container still render.
    /// </summary>
    private void WriteUnmappedControl(StringBuilder sb, ControlNode control, LayoutAnalysisResult layoutInfo, string indent, string namespaceName, PluginMappingOverrides overrides)
    {
        sb.AppendLine($"{indent}<!-- TODO: Unmapped control: {control.ControlType} ({control.Name}) -->");

        if (control.Children.Count == 0)
        {
            return;
        }

        sb.AppendLine($"{indent}<Panel Name=\"{control.Name}\">");

        if (layoutInfo.ChildLayouts.TryGetValue(control.Name, out var childLayout))
        {
            WriteLayoutContainer(sb, control, childLayout, indent + "    ", namespaceName, overrides);
        }
        else
        {
            WriteChildren(sb, control, layoutInfo, indent + "    ", namespaceName, overrides);
        }

        sb.AppendLine($"{indent}</Panel>");
    }

    private void WriteControlProperties(StringBuilder sb, ControlNode control, string indent, string namespaceName, PluginMappingOverrides overrides)
    {
        foreach (var prop in control.Properties)
        {
            if (overrides.PropertyTranslations.TryGetValue((control, prop.Key), out var pluginTranslation))
            {
                AppendAttribute(sb, indent, pluginTranslation.AvaloniaPropertyName, pluginTranslation.Value?.ToString());
                continue;
            }

            var mapping = PropertyMappingRegistry.GetMapping(prop.Key, control.ControlType);
            if (mapping == null) continue;

            if (mapping.DirectMapping && !mapping.RequiresCustomLogic)
            {
                AppendAttribute(sb, indent, mapping.AvaloniaProperty, prop.Value.Value?.ToString());
                continue;
            }

            var rawValue = prop.Value.Value?.ToString();
            if (string.IsNullOrEmpty(rawValue))
            {
                continue;
            }

            var converted = PropertyValueConverter.Convert(mapping, rawValue);
            if (converted == null)
            {
                continue;
            }

            foreach (var (attributeName, value) in converted)
            {
                // Resource-backed image paths come back from PropertyValueConverter as a
                // bare "Assets/..." relative path (it deliberately has no namespace/
                // orchestration context); qualify it into a full avares:// URI here, where
                // namespaceName is in scope.
                var qualifiedValue = value.StartsWith("Assets/", StringComparison.Ordinal)
                    ? $"avares://{namespaceName}/{value}"
                    : value;

                AppendAttribute(sb, indent, attributeName, qualifiedValue);
            }
        }
    }

    /// <summary>
    /// Wires PreserveEventHandler events (e.g. MouseDown/KeyDown) as AXAML event attributes
    /// pointing at the original handler method name, so the stub CodeBehindGenerator emits
    /// for it is actually reachable rather than dead code sitting unused. Skipped when a
    /// plugin has already claimed the event (mirrors CollectManualSteps' same check) - v1
    /// scope covers only the static EventMappingRegistry path.
    /// </summary>
    private void WriteEventAttributes(StringBuilder sb, ControlNode control, string indent, PluginMappingOverrides overrides)
    {
        foreach (var (eventName, handlerName) in control.EventHandlers)
        {
            if (overrides.EventMappings.ContainsKey((control, eventName)))
            {
                continue;
            }

            var mapping = EventMappingRegistry.GetMapping(eventName);
            if (mapping?.PreserveEventHandler != true)
            {
                continue;
            }

            AppendAttribute(sb, indent, mapping.AvaloniaEvent, handlerName);
        }
    }

    private void AppendAttribute(StringBuilder sb, string indent, string name, string? value)
    {
        sb.AppendLine();
        sb.Append($"{indent}{name}=\"{EscapeXml(value)}\"");
    }

    private string EscapeXml(string? text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");
    }
}
