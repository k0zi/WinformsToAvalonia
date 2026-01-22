using System.Text;
using Converter.Plugin.Abstractions;
using Converter.Mappings.BuiltIn;

namespace Converter.Generator.Axaml;

/// <summary>
/// Generates Avalonia AXAML markup from control trees.
/// </summary>
public class AxamlGenerator
{
    /// <summary>
    /// Generate AXAML for a control tree.
    /// </summary>
    public string Generate(ControlNode root, LayoutAnalysisResult layoutInfo, string namespaceName, string className)
    {
        var sb = new StringBuilder();

        // Write AXAML header
        sb.AppendLine($"<Window xmlns=\"https://github.com/avaloniaui\"");
        sb.AppendLine("        xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"");
        sb.AppendLine($"        xmlns:vm=\"using:{namespaceName}.ViewModels\"");
        sb.AppendLine($"        x:Class=\"{namespaceName}.Views.{className}\"");
        sb.AppendLine($"        x:DataType=\"vm:{className}ViewModel\"");
        
        // Add window properties
        WriteControlProperties(sb, root, "        ");
        
        sb.AppendLine("        >");

        // Write design-time DataContext
        sb.AppendLine("    <Design.DataContext>");
        sb.AppendLine($"        <vm:{className}ViewModel/>");
        sb.AppendLine("    </Design.DataContext>");
        sb.AppendLine();

        // Write content based on layout type
        WriteLayoutContainer(sb, root, layoutInfo, "    ");

        sb.AppendLine("</Window>");

        return sb.ToString();
    }

    private void WriteLayoutContainer(StringBuilder sb, ControlNode control, LayoutAnalysisResult layoutInfo, string indent)
    {
        switch (layoutInfo.LayoutType)
        {
            case LayoutType.Grid:
                WriteGridLayout(sb, control, layoutInfo, indent);
                break;
            case LayoutType.StackPanel:
                WriteStackPanelLayout(sb, control, layoutInfo, indent);
                break;
            case LayoutType.DockPanel:
                WriteDockPanelLayout(sb, control, layoutInfo, indent);
                break;
            case LayoutType.Canvas:
            default:
                WriteCanvasLayout(sb, control, layoutInfo, indent);
                break;
        }
    }

    private void WriteGridLayout(StringBuilder sb, ControlNode control, LayoutAnalysisResult layoutInfo, string indent)
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
        WriteChildren(sb, control, layoutInfo, indent + "    ");

        sb.AppendLine($"{indent}</Grid>");
    }

    private void WriteStackPanelLayout(StringBuilder sb, ControlNode control, LayoutAnalysisResult layoutInfo, string indent)
    {
        var orientation = layoutInfo.Metadata.TryGetValue("Orientation", out var orientObj) && 
                         orientObj?.ToString() == "Horizontal" ? "Horizontal" : "Vertical";

        sb.AppendLine($"{indent}<StackPanel Orientation=\"{orientation}\">");
        WriteChildren(sb, control, layoutInfo, indent + "    ");
        sb.AppendLine($"{indent}</StackPanel>");
    }

    private void WriteDockPanelLayout(StringBuilder sb, ControlNode control, LayoutAnalysisResult layoutInfo, string indent)
    {
        sb.AppendLine($"{indent}<DockPanel>");
        WriteChildren(sb, control, layoutInfo, indent + "    ");
        sb.AppendLine($"{indent}</DockPanel>");
    }

    private void WriteCanvasLayout(StringBuilder sb, ControlNode control, LayoutAnalysisResult layoutInfo, string indent)
    {
        sb.AppendLine($"{indent}<Canvas>");
        WriteChildren(sb, control, layoutInfo, indent + "    ");
        sb.AppendLine($"{indent}</Canvas>");
    }

    private void WriteChildren(StringBuilder sb, ControlNode control, LayoutAnalysisResult layoutInfo, string indent)
    {
        foreach (var child in control.Children)
        {
            WriteControl(sb, child, layoutInfo, indent);
        }
    }

    private void WriteControl(StringBuilder sb, ControlNode control, LayoutAnalysisResult layoutInfo, string indent)
    {
        var mapping = ControlMappingRegistry.GetMapping(control.ControlType);
        if (mapping == null)
        {
            sb.AppendLine($"{indent}<!-- TODO: Unmapped control: {control.ControlType} ({control.Name}) -->");
            return;
        }

        var avaloniaType = mapping.AvaloniaType;
        sb.Append($"{indent}<{avaloniaType}");

        // Write Name
        sb.Append($" Name=\"{control.Name}\"");

        // Write properties
        WriteControlProperties(sb, control, indent);

        if (control.Children.Count > 0)
        {
            sb.AppendLine(">");
            
            // Recursively write children if applicable
            if (layoutInfo.ChildLayouts.TryGetValue(control.Name, out var childLayout))
            {
                WriteLayoutContainer(sb, control, childLayout, indent + "    ");
            }
            else
            {
                WriteChildren(sb, control, layoutInfo, indent + "    ");
            }

            sb.AppendLine($"{indent}</{avaloniaType}>");
        }
        else
        {
            sb.AppendLine(" />");
        }
    }

    private void WriteControlProperties(StringBuilder sb, ControlNode control, string indent)
    {
        foreach (var prop in control.Properties)
        {
            var mapping = PropertyMappingRegistry.GetMapping(prop.Key, control.ControlType);
            if (mapping == null) continue;

            if (mapping.DirectMapping && !mapping.RequiresCustomLogic)
            {
                sb.AppendLine();
                sb.Append($"{indent}{mapping.AvaloniaProperty}=\"{EscapeXml(prop.Value.Value?.ToString())}\"");
            }
        }
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
