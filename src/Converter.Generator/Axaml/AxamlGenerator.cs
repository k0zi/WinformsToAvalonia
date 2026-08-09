using System.Linq;
using System.Text;
using Converter.Core.Parsing;
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

        // Add window properties. LayoutType.Custom is used purely as a "not Canvas" sentinel
        // here - the root <Window> has no container of its own, so Canvas.Left/Top must never
        // leak onto it even if a Form.Location property happens to be present.
        WriteControlProperties(sb, root, LayoutType.Custom, indent: "        ", namespaceName, overrides);
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

        var (rows, cols) = ResolveGridDimensions(control);
        if (rows > 0 && cols > 0)
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

        (int Rows, int Columns) ResolveGridDimensions(ControlNode gridControl)
        {
            if (gridControl.ControlType == "TableLayoutPanel")
            {
                // TableLayoutPanel declares its own dimensions (ColumnCount/RowCount, already
                // captured as plain properties) rather than deriving them from the heuristic
                // grid-line detection used for everything else - fall back to the highest
                // captured child cell index when the panel doesn't set them explicitly (WinForms
                // defaults to auto-grow).
                var panelRows = ParseIntProperty(gridControl, "RowCount");
                var panelCols = ParseIntProperty(gridControl, "ColumnCount");

                if (panelRows == 0) panelRows = MaxChildCellIndex(gridControl, "TableLayoutPanel.Row") + 1;
                if (panelCols == 0) panelCols = MaxChildCellIndex(gridControl, "TableLayoutPanel.Column") + 1;

                return (panelRows, panelCols);
            }

            if (layoutInfo.Metadata.TryGetValue("Rows", out var rowsObj) && rowsObj is int r &&
                layoutInfo.Metadata.TryGetValue("Columns", out var colsObj) && colsObj is int c)
            {
                return (r, c);
            }

            return (0, 0);
        }
    }

    private static int ParseIntProperty(ControlNode control, string propertyName) =>
        control.Properties.TryGetValue(propertyName, out var pv) &&
        int.TryParse(pv.Value?.ToString(), out var value)
            ? value
            : 0;

    private static int MaxChildCellIndex(ControlNode control, string propertyKey) =>
        control.Children
            .Where(c => c.Properties.ContainsKey(propertyKey))
            .Select(c => ParseIntProperty(c, propertyKey))
            .DefaultIfEmpty(-1)
            .Max();

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
        foreach (var child in OrderChildrenForStack(control.Children, layoutInfo))
        {
            WriteControl(sb, child, layoutInfo, indent, namespaceName, overrides);
        }
    }

    /// <summary>
    /// For StackPanel containers, reorders children to match the visual top-to-bottom/
    /// left-to-right order LayoutAnalyzer.AnalyzeStackPattern computed (ChildOrder), instead
    /// of raw WinForms Controls.Add(...) declaration order. Children absent from ChildOrder
    /// (e.g. no Location) sort to the end, preserving their original relative order. No-op for
    /// every other layout type.
    /// </summary>
    private static List<ControlNode> OrderChildrenForStack(List<ControlNode> children, LayoutAnalysisResult layoutInfo)
    {
        if (layoutInfo.LayoutType != LayoutType.StackPanel || layoutInfo.ChildOrder.Count == 0)
        {
            return children;
        }

        var rank = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < layoutInfo.ChildOrder.Count; i++)
        {
            rank[layoutInfo.ChildOrder[i]] = i;
        }

        return children
            .OrderBy(c => rank.TryGetValue(c.Name, out var idx) ? idx : int.MaxValue)
            .ToList();
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

        // Write grid cell placement (heuristic Grid mode only - TableLayoutPanel cell
        // placement flows through WriteControlProperties instead, via the
        // TableLayoutPanel.Column/Row DirectMapping entries).
        WriteGridPlacementAttributes(sb, control, layoutInfo, indent);

        // Write properties
        WriteControlProperties(sb, control, layoutInfo.LayoutType, indent, namespaceName, overrides);
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
        WriteGridPlacementAttributes(sb, control, layoutInfo, indent);

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

        sb.Append($"{indent}<Panel Name=\"{control.Name}\"");
        WriteGridPlacementAttributes(sb, control, layoutInfo, indent);
        sb.AppendLine(">");

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

    /// <summary>
    /// Emits Grid.Row/Grid.Column for a child placed in a heuristically-detected Grid
    /// container (LayoutAnalyzer.AnalyzeGridPattern's GridCellAssignments). No-op outside
    /// Grid mode, and for children with no captured Location (the placement source).
    /// TableLayoutPanel's exact cell assignment is unrelated to this - it flows through the
    /// ordinary TableLayoutPanel.Column/Row property mappings in WriteControlProperties
    /// instead, since AnalyzeGridPattern never runs for a TableLayoutPanel container.
    /// </summary>
    private void WriteGridPlacementAttributes(StringBuilder sb, ControlNode control, LayoutAnalysisResult layoutInfo, string indent)
    {
        if (layoutInfo.LayoutType != LayoutType.Grid) return;
        if (!layoutInfo.GridCellAssignments.TryGetValue(control.Name, out var cell)) return;

        AppendAttribute(sb, indent, "Grid.Row", cell.Row.ToString());
        AppendAttribute(sb, indent, "Grid.Column", cell.Column.ToString());
    }

    private void WriteControlProperties(StringBuilder sb, ControlNode control, LayoutType containerLayoutType, string indent, string namespaceName, PluginMappingOverrides overrides)
    {
        foreach (var prop in control.Properties)
        {
            if (overrides.PropertyTranslations.TryGetValue((control, prop.Key), out var pluginTranslation))
            {
                AppendPlacementAwareAttribute(sb, indent, pluginTranslation.AvaloniaPropertyName, pluginTranslation.Value?.ToString(), containerLayoutType);
                continue;
            }

            var mapping = PropertyMappingRegistry.GetMapping(prop.Key, control.ControlType);
            if (mapping == null) continue;

            if (mapping.DirectMapping && !mapping.RequiresCustomLogic)
            {
                AppendPlacementAwareAttribute(sb, indent, mapping.AvaloniaProperty, prop.Value.Value?.ToString(), containerLayoutType);
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

                AppendPlacementAwareAttribute(sb, indent, attributeName, qualifiedValue, containerLayoutType);
            }
        }
    }

    /// <summary>
    /// Suppresses Canvas.Left/Canvas.Top (and any other Canvas.* attribute) unless the
    /// immediate container is actually a Canvas - without this, every control got dead/no-op
    /// Canvas.Left/Top attributes regardless of its real container (Grid/StackPanel/
    /// DockPanel ignore them, since their parent isn't a Canvas), which both cluttered the
    /// generated AXAML and had no bearing on the control's actual rendered position.
    /// </summary>
    private void AppendPlacementAwareAttribute(StringBuilder sb, string indent, string name, string? value, LayoutType containerLayoutType)
    {
        if (name.StartsWith("Canvas.", StringComparison.Ordinal) && containerLayoutType != LayoutType.Canvas)
        {
            return;
        }

        AppendAttribute(sb, indent, name, value);
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

            if (handlerName == WinFormsParser.InlineLambdaHandlerMarker)
            {
                // No stable method name to reference from AXAML; surfaced as a manual step by
                // ConversionOrchestrator.CollectManualSteps instead.
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
