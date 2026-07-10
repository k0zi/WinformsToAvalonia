using System.Text;
using Converter.Generator.Axaml;
using Converter.Generator.Mapping;
using Converter.Mappings.BuiltIn;
using Converter.Plugin.Abstractions;

namespace Converter.Generator.Styles;

/// <summary>
/// Generates Avalonia styles from common property patterns.
/// </summary>
public class StyleGenerator
{
    /// <summary>
    /// WinForms-side property names to look for common values across a control group.
    /// ControlNode.Properties is keyed by the original WinForms property name (e.g.
    /// "BackColor", "Font"), not the Avalonia one, so matching must happen on that side and
    /// then be converted via PropertyMappingRegistry/PropertyValueConverter - the same
    /// pipeline AxamlGenerator uses - to produce valid Avalonia setter values.
    /// </summary>
    private static readonly string[] StyleWorthyProperties = ["Font", "BackColor", "ForeColor"];

    /// <summary>
    /// Extract and generate styles from control tree. <paramref name="overrides"/> - resolved
    /// once per form by MappingResolver before generation starts - lets a plugin
    /// IPropertyTranslator override a style setter's value; default null (normalized to
    /// Empty) preserves this method's behavior for every existing caller.
    /// </summary>
    public string GenerateStyles(ControlNode root, int minimumOccurrence = 3, PluginMappingOverrides? overrides = null)
    {
        overrides ??= PluginMappingOverrides.Empty;
        var propertyGroups = AnalyzePropertyPatterns(root, minimumOccurrence, overrides);

        if (propertyGroups.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();

        sb.AppendLine("<Styles xmlns=\"https://github.com/avaloniaui\"");
        sb.AppendLine("        xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">");
        sb.AppendLine();

        foreach (var group in propertyGroups)
        {
            GenerateStyle(sb, group);
        }

        sb.AppendLine("</Styles>");

        return sb.ToString();
    }

    private void GenerateStyle(StringBuilder sb, PropertyPattern pattern)
    {
        sb.AppendLine($"    <!-- Style: {pattern.Name} (used by {pattern.ControlCount} controls) -->");
        sb.AppendLine($"    <Style Selector=\"{pattern.ControlType}\">");

        foreach (var setter in pattern.Setters)
        {
            sb.AppendLine($"        <Setter Property=\"{setter.Key}\" Value=\"{setter.Value}\" />");
        }

        sb.AppendLine("    </Style>");
        sb.AppendLine();
    }

    private List<PropertyPattern> AnalyzePropertyPatterns(ControlNode root, int minimumOccurrence, PluginMappingOverrides overrides)
    {
        var allControls = GetAllControls(root);
        var patterns = new List<PropertyPattern>();

        // Group controls by type
        var controlsByType = allControls.GroupBy(c => c.ControlType);

        foreach (var typeGroup in controlsByType)
        {
            if (typeGroup.Count() < minimumOccurrence)
                continue;

            var controls = typeGroup.ToList();
            var setters = FindCommonPropertySetters(controls, typeGroup.Key, overrides);

            if (setters.Count > 0)
            {
                patterns.Add(new PropertyPattern
                {
                    Name = $"{typeGroup.Key}CommonStyle",
                    ControlType = typeGroup.Key,
                    ControlCount = controls.Count,
                    Setters = setters
                });
            }
        }

        return patterns;
    }

    /// <summary>
    /// Finds WinForms properties shared verbatim (identical raw captured value) across
    /// every control in the group, and converts each to its Avalonia attribute form.
    /// </summary>
    private Dictionary<string, string> FindCommonPropertySetters(List<ControlNode> controls, string controlType, PluginMappingOverrides overrides)
    {
        var setters = new Dictionary<string, string>();

        if (controls.Count == 0)
        {
            return setters;
        }

        var firstControl = controls[0];

        foreach (var propName in StyleWorthyProperties)
        {
            if (!firstControl.Properties.TryGetValue(propName, out var firstValue))
                continue;

            var rawValue = firstValue.Value?.ToString();
            if (string.IsNullOrEmpty(rawValue))
                continue;

            var allShareValue = controls.All(c =>
                c.Properties.TryGetValue(propName, out var v) && v.Value?.ToString() == rawValue);

            if (!allShareValue)
                continue;

            if (overrides.PropertyTranslations.TryGetValue((firstControl, propName), out var pluginTranslation))
            {
                var pluginValue = pluginTranslation.Value?.ToString();
                if (!string.IsNullOrEmpty(pluginValue))
                {
                    setters[pluginTranslation.AvaloniaPropertyName] = pluginValue;
                }
                continue;
            }

            var mapping = PropertyMappingRegistry.GetMapping(propName, controlType);
            if (mapping == null)
                continue;

            var converted = mapping.DirectMapping && !mapping.RequiresCustomLogic
                ? new[] { (AttributeName: mapping.AvaloniaProperty, Value: rawValue) }
                : PropertyValueConverter.Convert(mapping, rawValue);

            if (converted == null)
                continue;

            foreach (var (attributeName, value) in converted)
            {
                setters[attributeName] = value;
            }
        }

        return setters;
    }

    private List<ControlNode> GetAllControls(ControlNode root)
    {
        var result = new List<ControlNode> { root };
        
        foreach (var child in root.Children)
        {
            result.AddRange(GetAllControls(child));
        }
        
        return result;
    }

    private class PropertyPattern
    {
        public required string Name { get; init; }
        public required string ControlType { get; init; }
        public required int ControlCount { get; init; }
        public required Dictionary<string, string> Setters { get; init; }
    }
}
