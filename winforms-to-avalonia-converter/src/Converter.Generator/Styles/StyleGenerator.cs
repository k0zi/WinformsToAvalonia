using System.Text;
using Converter.Plugin.Abstractions;

namespace Converter.Generator.Styles;

/// <summary>
/// Generates Avalonia styles from common property patterns.
/// </summary>
public class StyleGenerator
{
    /// <summary>
    /// Extract and generate styles from control tree.
    /// </summary>
    public string GenerateStyles(ControlNode root, int minimumOccurrence = 3)
    {
        var propertyGroups = AnalyzePropertyPatterns(root, minimumOccurrence);
        
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
        
        foreach (var prop in pattern.Properties)
        {
            sb.AppendLine($"        <Setter Property=\"{prop.Key}\" Value=\"{prop.Value}\" />");
        }
        
        sb.AppendLine("    </Style>");
        sb.AppendLine();
    }

    private List<PropertyPattern> AnalyzePropertyPatterns(ControlNode root, int minimumOccurrence)
    {
        var allControls = GetAllControls(root);
        var patterns = new List<PropertyPattern>();

        // Group controls by type
        var controlsByType = allControls.GroupBy(c => c.ControlType);

        foreach (var typeGroup in controlsByType)
        {
            if (typeGroup.Count() < minimumOccurrence)
                continue;

            // Find common properties
            var commonProperties = FindCommonProperties(typeGroup.ToList());

            if (commonProperties.Count > 0)
            {
                patterns.Add(new PropertyPattern
                {
                    Name = $"{typeGroup.Key}CommonStyle",
                    ControlType = typeGroup.Key,
                    ControlCount = typeGroup.Count(),
                    Properties = commonProperties
                });
            }
        }

        return patterns;
    }

    private Dictionary<string, string> FindCommonProperties(List<ControlNode> controls)
    {
        var commonProps = new Dictionary<string, string>();
        
        if (controls.Count == 0)
            return commonProps;

        var propertiesToCheck = new[] { "FontFamily", "FontSize", "FontWeight", "Background", "Foreground" };

        foreach (var propName in propertiesToCheck)
        {
            var firstControl = controls[0];
            if (!firstControl.Properties.ContainsKey(propName))
                continue;

            var value = firstControl.Properties[propName].Value?.ToString();
            if (string.IsNullOrEmpty(value))
                continue;

            // Check if all controls have the same value
            if (controls.All(c => c.Properties.ContainsKey(propName) && 
                                 c.Properties[propName].Value?.ToString() == value))
            {
                commonProps[propName] = value;
            }
        }

        return commonProps;
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
        public required Dictionary<string, string> Properties { get; init; }
    }
}
