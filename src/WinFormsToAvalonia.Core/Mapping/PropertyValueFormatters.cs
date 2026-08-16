using System.Globalization;
using WinFormsToAvalonia.Core.Model;

namespace WinFormsToAvalonia.Core.Mapping;

/// <summary>Small, composable value -> XAML-attribute-string transforms used by <see cref="SimplePropertyMapper"/> property mapping specs.</summary>
public static class PropertyValueFormatters
{
    public static string? AsText(PropertyValue value) => value switch
    {
        PropertyValue.Literal { Value: string s } => s,
        PropertyValue.Literal { Value: { } v } => Convert.ToString(v, CultureInfo.InvariantCulture),
        _ => null,
    };

    public static string? AsBool(PropertyValue value) =>
        value is PropertyValue.Literal { Value: bool b } ? (b ? "True" : "False") : null;

    public static string? AsNumber(PropertyValue value) => value switch
    {
        PropertyValue.Literal { Value: string } => null,
        PropertyValue.Literal { Value: { } v } => Convert.ToString(v, CultureInfo.InvariantCulture),
        _ => null,
    };
}
