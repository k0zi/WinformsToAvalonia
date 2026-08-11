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
    private static readonly HashSet<string> EmptyCustomControlClassNames = [];
    private static readonly Dictionary<(string ControlName, string Property), string> EmptyInferredBindings = [];

    /// <summary>
    /// True if any control in the tree is an unmapped custom control this run also converted
    /// (see WriteControl) - decides whether Generate needs to declare the "controls:" XML
    /// namespace on the root element at all.
    /// </summary>
    private static bool ContainsConvertedCustomControlReference(
        ControlNode control, IReadOnlySet<string> convertedCustomControlClassNames)
    {
        if (ControlMappingRegistry.GetMapping(control.ControlType) == null &&
            convertedCustomControlClassNames.Contains(control.ControlType))
        {
            return true;
        }

        return control.Children.Any(child => ContainsConvertedCustomControlReference(child, convertedCustomControlClassNames));
    }

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
        PluginMappingOverrides? overrides = null,
        IReadOnlySet<string>? convertedCustomControlClassNames = null,
        IReadOnlyDictionary<(string ControlName, string Property), string>? inferredBindings = null)
    {
        overrides ??= PluginMappingOverrides.Empty;
        convertedCustomControlClassNames ??= EmptyCustomControlClassNames;
        inferredBindings ??= EmptyInferredBindings;
        var sb = new StringBuilder();

        // The root element mirrors the source WinForms class's real base type via the same
        // registry that maps every other control ("Form" -> "Window", "UserControl" ->
        // "UserControl") - defaulting to "Window" preserves today's behavior for anything
        // unrecognized (e.g. a custom base Form subclass).
        var rootElement = ControlMappingRegistry.GetMapping(root.ControlType)?.AvaloniaType ?? "Window";

        // PropertyMappingRegistry's control-specific overrides (e.g. "Form"/"Text" -> "Title")
        // are keyed by the WinForms type name, not the resolved Avalonia element above - for a
        // form deriving from a custom base class (e.g. "SalesOrderDetailForm : DetailFormBase<T>"),
        // root.ControlType is that custom base class's own name, not literally "Form", so the
        // "Form" overrides would otherwise never fire even though rootElement already correctly
        // resolved to "Window". Re-deriving the effective type from rootElement (rather than
        // root.ControlType directly) keeps the element-tag and property-mapping decisions in
        // agreement - only the Window case needs this: a UserControl root's ControlType is
        // always already the literal string "UserControl" (see ControlMappingRegistry), and no
        // other resolved element type is possible for a root today.
        var effectiveRootControlType = rootElement == "Window" ? "Form" : root.ControlType;

        // A UserControl-rooted form is itself a custom control this run is independently
        // converting into its own reusable View - it belongs in the Controls/ folder alongside
        // its code-behind (see ConversionOrchestrator), not Views/, so its own x:Class must
        // match.
        var isCustomControlRoot = rootElement == "UserControl";
        var ownNamespaceSegment = isCustomControlRoot ? "Controls" : "Views";

        // Write AXAML header
        sb.AppendLine($"<{rootElement} xmlns=\"https://github.com/avaloniaui\"");
        sb.AppendLine("        xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"");
        sb.AppendLine($"        xmlns:vm=\"using:{namespaceName}.ViewModels\"");

        // Only declared when the tree actually references at least one custom control this run
        // also converted (e.g. <controls:CustomerCard/>) - keeps the header clean for the common
        // case (no custom controls at all).
        if (ContainsConvertedCustomControlReference(root, convertedCustomControlClassNames))
        {
            sb.AppendLine($"        xmlns:controls=\"using:{namespaceName}.Controls\"");
        }

        sb.AppendLine($"        x:Class=\"{namespaceName}.{ownNamespaceSegment}.{className}\"");
        sb.AppendLine($"        x:DataType=\"vm:{className}ViewModel\"");

        // Add window/control properties. LayoutType.Custom is used purely as a "not Canvas"
        // sentinel here - the root element has no container of its own, so Canvas.Left/Top must
        // never leak onto it even if a Form.Location property happens to be present.
        WriteControlProperties(sb, root, LayoutType.Custom, indent: "        ", namespaceName, overrides, inferredBindings, effectiveRootControlType);
        WriteEventAttributes(sb, root, indent: "        ", overrides);
        WriteDialogResultCloseAttribute(sb, root, indent: "        ");

        sb.AppendLine("        >");

        // Write design-time DataContext
        sb.AppendLine("    <Design.DataContext>");
        sb.AppendLine($"        <vm:{className}ViewModel/>");
        sb.AppendLine("    </Design.DataContext>");
        sb.AppendLine();

        // Write content based on layout type
        WriteLayoutContainer(sb, root, layoutInfo, "    ", namespaceName, overrides, convertedCustomControlClassNames, inferredBindings);

        sb.AppendLine($"</{rootElement}>");

        return sb.ToString();
    }

    private void WriteLayoutContainer(StringBuilder sb, ControlNode control, LayoutAnalysisResult layoutInfo, string indent, string namespaceName, PluginMappingOverrides overrides, IReadOnlySet<string> convertedCustomControlClassNames, IReadOnlyDictionary<(string ControlName, string Property), string> inferredBindings)
    {
        switch (layoutInfo.LayoutType)
        {
            case LayoutType.Grid:
                WriteGridLayout(sb, control, layoutInfo, indent, namespaceName, overrides, convertedCustomControlClassNames, inferredBindings);
                break;
            case LayoutType.StackPanel:
                WriteStackPanelLayout(sb, control, layoutInfo, indent, namespaceName, overrides, convertedCustomControlClassNames, inferredBindings);
                break;
            case LayoutType.DockPanel:
                WriteDockPanelLayout(sb, control, layoutInfo, indent, namespaceName, overrides, convertedCustomControlClassNames, inferredBindings);
                break;
            case LayoutType.Canvas:
            default:
                WriteCanvasLayout(sb, control, layoutInfo, indent, namespaceName, overrides, convertedCustomControlClassNames, inferredBindings);
                break;
        }
    }

    private void WriteGridLayout(StringBuilder sb, ControlNode control, LayoutAnalysisResult layoutInfo, string indent, string namespaceName, PluginMappingOverrides overrides, IReadOnlySet<string> convertedCustomControlClassNames, IReadOnlyDictionary<(string ControlName, string Property), string> inferredBindings)
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
        WriteChildren(sb, control, layoutInfo, indent + "    ", namespaceName, overrides, convertedCustomControlClassNames, inferredBindings);

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

    private void WriteStackPanelLayout(StringBuilder sb, ControlNode control, LayoutAnalysisResult layoutInfo, string indent, string namespaceName, PluginMappingOverrides overrides, IReadOnlySet<string> convertedCustomControlClassNames, IReadOnlyDictionary<(string ControlName, string Property), string> inferredBindings)
    {
        var orientation = layoutInfo.Metadata.TryGetValue("Orientation", out var orientObj) &&
                         orientObj?.ToString() == "Horizontal" ? "Horizontal" : "Vertical";

        sb.AppendLine($"{indent}<StackPanel Orientation=\"{orientation}\">");
        WriteChildren(sb, control, layoutInfo, indent + "    ", namespaceName, overrides, convertedCustomControlClassNames, inferredBindings);
        sb.AppendLine($"{indent}</StackPanel>");
    }

    private void WriteDockPanelLayout(StringBuilder sb, ControlNode control, LayoutAnalysisResult layoutInfo, string indent, string namespaceName, PluginMappingOverrides overrides, IReadOnlySet<string> convertedCustomControlClassNames, IReadOnlyDictionary<(string ControlName, string Property), string> inferredBindings)
    {
        sb.AppendLine($"{indent}<DockPanel>");
        WriteChildren(sb, control, layoutInfo, indent + "    ", namespaceName, overrides, convertedCustomControlClassNames, inferredBindings);
        sb.AppendLine($"{indent}</DockPanel>");
    }

    private void WriteCanvasLayout(StringBuilder sb, ControlNode control, LayoutAnalysisResult layoutInfo, string indent, string namespaceName, PluginMappingOverrides overrides, IReadOnlySet<string> convertedCustomControlClassNames, IReadOnlyDictionary<(string ControlName, string Property), string> inferredBindings)
    {
        sb.AppendLine($"{indent}<Canvas>");
        WriteChildren(sb, control, layoutInfo, indent + "    ", namespaceName, overrides, convertedCustomControlClassNames, inferredBindings);
        sb.AppendLine($"{indent}</Canvas>");
    }

    private void WriteChildren(StringBuilder sb, ControlNode control, LayoutAnalysisResult layoutInfo, string indent, string namespaceName, PluginMappingOverrides overrides, IReadOnlySet<string> convertedCustomControlClassNames, IReadOnlyDictionary<(string ControlName, string Property), string> inferredBindings)
    {
        foreach (var child in OrderChildrenForStack(control.Children, layoutInfo))
        {
            WriteControl(sb, child, layoutInfo, indent, namespaceName, overrides, convertedCustomControlClassNames, inferredBindings);
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

    private void WriteControl(StringBuilder sb, ControlNode control, LayoutAnalysisResult layoutInfo, string indent, string namespaceName, PluginMappingOverrides overrides, IReadOnlySet<string> convertedCustomControlClassNames, IReadOnlyDictionary<(string ControlName, string Property), string> inferredBindings)
    {
        if (overrides.ControlMappings.TryGetValue(control, out var pluginMapping))
        {
            WritePluginMappedControl(sb, control, pluginMapping, layoutInfo, indent, namespaceName, overrides, convertedCustomControlClassNames, inferredBindings);
            return;
        }

        var mapping = ControlMappingRegistry.GetMapping(control.ControlType);
        string avaloniaType;
        if (mapping != null)
        {
            avaloniaType = mapping.AvaloniaType;
        }
        else if (convertedCustomControlClassNames.Contains(control.ControlType))
        {
            // This run also independently converted control.ControlType's own Designer.cs into
            // its own Controls/{ControlType}.axaml (a UserControl) - reference it directly
            // instead of the dead "<!-- TODO: Unmapped control -->" placeholder. Generate already
            // declared the "controls:" namespace for this file (ContainsConvertedCustomControlReference).
            avaloniaType = $"controls:{control.ControlType}";
        }
        else
        {
            WriteUnmappedControl(sb, control, layoutInfo, indent, namespaceName, overrides, convertedCustomControlClassNames, inferredBindings);
            return;
        }

        if (TryGetBorderWrapThickness(control, avaloniaType, out var borderThickness))
        {
            WriteBorderWrappedControl(sb, control, avaloniaType, borderThickness, layoutInfo, indent, namespaceName, overrides, convertedCustomControlClassNames, inferredBindings);
            return;
        }

        WriteMappedElement(sb, control, avaloniaType, layoutInfo, indent, namespaceName, overrides, convertedCustomControlClassNames, inferredBindings, includeGridPlacement: true);
    }

    /// <summary>
    /// Avalonia targets with no BorderThickness/BorderBrush of their own at all - Image is a
    /// bare Control (no border support whatsoever), and Panel/Grid/WrapPanel/StackPanel are
    /// all bare Panel-derived layout types (same reasoning already established for their
    /// Padding=null overrides in PropertyMappingRegistry). A WinForms control mapping to one of
    /// these that has BorderStyle set needs wrapping in a real Border instead - see
    /// WriteBorderWrappedControl.
    /// </summary>
    private static readonly HashSet<string> BorderIncapableAvaloniaTypes = ["Image", "Panel", "Grid", "WrapPanel", "StackPanel"];

    private static bool TryGetBorderWrapThickness(ControlNode control, string avaloniaType, out string thickness)
    {
        thickness = "";

        if (!BorderIncapableAvaloniaTypes.Contains(avaloniaType) ||
            !control.Properties.TryGetValue("BorderStyle", out var prop))
        {
            return false;
        }

        var rawValue = prop.Value?.ToString();
        if (string.IsNullOrEmpty(rawValue))
        {
            return false;
        }

        // Uses the *common* BorderStyle mapping directly (bypassing the control-specific null
        // override PropertyMappingRegistry has for these exact control types, which exists
        // precisely to keep the normal per-property loop in WriteControlProperties from also,
        // redundantly and invalidly, emitting this on the inner element itself).
        var commonMapping = PropertyMappingRegistry.GetCommonMappings()["BorderStyle"];
        var converted = PropertyValueConverter.Convert(commonMapping, rawValue);
        if (converted == null)
        {
            return false;
        }

        foreach (var (attributeName, value) in converted)
        {
            if (attributeName == "BorderThickness")
            {
                thickness = value;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Wraps a border-incapable element (see BorderIncapableAvaloniaTypes) in a real Avalonia
    /// Border, which does have BorderThickness. Grid placement (Grid.Row/Grid.Column) moves to
    /// the outer Border - it's the element actually occupying the Grid cell now - while Name
    /// and every other property stay on the inner element unchanged, so existing code-behind/
    /// ViewModel references to the control by name keep working without modification. The
    /// Border itself gets no Name - nothing needs to reference the wrapper.
    /// </summary>
    private void WriteBorderWrappedControl(
        StringBuilder sb, ControlNode control, string avaloniaType, string borderThickness, LayoutAnalysisResult layoutInfo,
        string indent, string namespaceName, PluginMappingOverrides overrides,
        IReadOnlySet<string> convertedCustomControlClassNames, IReadOnlyDictionary<(string ControlName, string Property), string> inferredBindings)
    {
        sb.Append($"{indent}<Border");
        WriteGridPlacementAttributes(sb, control, layoutInfo, indent);
        AppendAttribute(sb, indent, "BorderThickness", borderThickness);
        sb.AppendLine(">");

        WriteMappedElement(
            sb, control, avaloniaType, layoutInfo, indent + "    ", namespaceName, overrides,
            convertedCustomControlClassNames, inferredBindings, includeGridPlacement: false);

        sb.AppendLine($"{indent}</Border>");
    }

    private void WriteMappedElement(
        StringBuilder sb, ControlNode control, string avaloniaType, LayoutAnalysisResult layoutInfo, string indent,
        string namespaceName, PluginMappingOverrides overrides, IReadOnlySet<string> convertedCustomControlClassNames,
        IReadOnlyDictionary<(string ControlName, string Property), string> inferredBindings, bool includeGridPlacement)
    {
        sb.Append($"{indent}<{avaloniaType}");

        // Write Name
        sb.Append($" Name=\"{control.Name}\"");

        // Write grid cell placement (heuristic Grid mode only - TableLayoutPanel cell
        // placement flows through WriteControlProperties instead, via the
        // TableLayoutPanel.Column/Row DirectMapping entries). Skipped when this element is
        // wrapped in a Border, which takes the grid cell instead (see WriteBorderWrappedControl).
        if (includeGridPlacement)
        {
            WriteGridPlacementAttributes(sb, control, layoutInfo, indent);
        }

        // Write properties
        WriteControlProperties(sb, control, layoutInfo.LayoutType, indent, namespaceName, overrides, inferredBindings);
        WriteEventAttributes(sb, control, indent, overrides);
        WriteDialogResultCloseAttribute(sb, control, indent);

        if (control.Children.Count > 0)
        {
            sb.AppendLine(">");

            // Recursively write children if applicable
            if (layoutInfo.ChildLayouts.TryGetValue(control.Name, out var childLayout))
            {
                WriteLayoutContainer(sb, control, childLayout, indent + "    ", namespaceName, overrides, convertedCustomControlClassNames, inferredBindings);
            }
            else
            {
                WriteChildren(sb, control, layoutInfo, indent + "    ", namespaceName, overrides, convertedCustomControlClassNames, inferredBindings);
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
    private void WritePluginMappedControl(StringBuilder sb, ControlNode control, ControlMappingResult pluginMapping, LayoutAnalysisResult layoutInfo, string indent, string namespaceName, PluginMappingOverrides overrides, IReadOnlySet<string> convertedCustomControlClassNames, IReadOnlyDictionary<(string ControlName, string Property), string> inferredBindings)
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
                WriteLayoutContainer(sb, control, childLayout, indent + "    ", namespaceName, overrides, convertedCustomControlClassNames, inferredBindings);
            }
            else
            {
                WriteChildren(sb, control, layoutInfo, indent + "    ", namespaceName, overrides, convertedCustomControlClassNames, inferredBindings);
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
    private void WriteUnmappedControl(StringBuilder sb, ControlNode control, LayoutAnalysisResult layoutInfo, string indent, string namespaceName, PluginMappingOverrides overrides, IReadOnlySet<string> convertedCustomControlClassNames, IReadOnlyDictionary<(string ControlName, string Property), string> inferredBindings)
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
            WriteLayoutContainer(sb, control, childLayout, indent + "    ", namespaceName, overrides, convertedCustomControlClassNames, inferredBindings);
        }
        else
        {
            WriteChildren(sb, control, layoutInfo, indent + "    ", namespaceName, overrides, convertedCustomControlClassNames, inferredBindings);
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

    private void WriteControlProperties(StringBuilder sb, ControlNode control, LayoutType containerLayoutType, string indent, string namespaceName, PluginMappingOverrides overrides, IReadOnlyDictionary<(string ControlName, string Property), string> inferredBindings, string? controlTypeOverride = null)
    {
        foreach (var prop in control.Properties)
        {
            if (overrides.PropertyTranslations.TryGetValue((control, prop.Key), out var pluginTranslation))
            {
                AppendPlacementAwareAttribute(sb, indent, pluginTranslation.AvaloniaPropertyName, pluginTranslation.Value?.ToString(), containerLayoutType);
                continue;
            }

            var mapping = PropertyMappingRegistry.GetMapping(prop.Key, controlTypeOverride ?? control.ControlType);
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

        WriteInferredBindingAttributes(sb, control, containerLayoutType, indent, inferredBindings, controlTypeOverride);
    }

    /// <summary>
    /// Emits `{Binding {ObservableProperty}}` for every UsageInferredBindingDetector-derived
    /// property of this control - properties whose value comes from usage across migrated
    /// methods rather than an explicit Designer.cs value or `.DataBindings.Add(...)` call, so
    /// the loop above (which only ever visits control.Properties) never reaches them on its own.
    /// Skipped when the property already has a literal Designer.cs value - that value takes
    /// precedence and was already written above. Also skipped entirely (not emitted under the
    /// raw WinForms property name) when PropertyMappingRegistry has no real mapping for it -
    /// unlike the main per-property loop above, this path used to fall back to the unmapped
    /// name itself (e.g. a WinForms-only "SelectedNode" leaking straight onto a TreeView, which
    /// has no such property), guaranteeing invalid AXAML instead of just omitting the binding.
    /// </summary>
    private void WriteInferredBindingAttributes(StringBuilder sb, ControlNode control, LayoutType containerLayoutType, string indent, IReadOnlyDictionary<(string ControlName, string Property), string> inferredBindings, string? controlTypeOverride = null)
    {
        if (inferredBindings.Count == 0) return;

        foreach (var ((controlName, property), observablePropertyName) in inferredBindings)
        {
            if (controlName != control.Name || control.Properties.ContainsKey(property))
            {
                continue;
            }

            var mapping = PropertyMappingRegistry.GetMapping(property, controlTypeOverride ?? control.ControlType);
            // A binding markup extension can only target a single simple Avalonia property -
            // a RequiresCustomLogic/RequiresConversion mapping's AvaloniaProperty is commonly a
            // comma-separated compound target (e.g. "FontFamily,FontSize,FontWeight"), which
            // would emit as one garbled, invalid attribute name if used here verbatim.
            if (mapping is not { DirectMapping: true, RequiresCustomLogic: false })
            {
                continue;
            }

            AppendPlacementAwareAttribute(sb, indent, mapping.AvaloniaProperty, $"{{Binding {observablePropertyName}}}", containerLayoutType);
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

    /// <summary>
    /// WinForms' fully declarative "OK/Cancel dialog" idiom: a button whose Designer-declared
    /// DialogResult property isn't None auto-closes the containing form with that result on
    /// click, with no click handler needed at all. Wires a synthetic Click attribute pointing
    /// at a stub CodeBehindGenerator emits (see DialogResultButtonHelper, shared by both, so
    /// they can't drift out of sync about which controls qualify) - skipped entirely when the
    /// control already has real Click wiring of its own, which must never be clobbered.
    /// </summary>
    private void WriteDialogResultCloseAttribute(StringBuilder sb, ControlNode control, string indent)
    {
        if (control.EventHandlers.ContainsKey("Click"))
        {
            return;
        }

        if (!DialogResultButtonHelper.TryGetDialogResultValue(control, out _))
        {
            return;
        }

        AppendAttribute(sb, indent, "Click", $"{control.Name}_DialogResultClick");
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
