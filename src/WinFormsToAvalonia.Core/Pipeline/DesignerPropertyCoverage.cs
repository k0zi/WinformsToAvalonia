using WinFormsToAvalonia.Core.Mapping;
using WinFormsToAvalonia.Core.Model;

namespace WinFormsToAvalonia.Core.Pipeline;

/// <summary>
/// Names the designer properties nothing in the conversion translated, once an artifact has been
/// emitted - the property-level counterpart of what <c>EventCoverageTests</c> guarantees for events.
/// </summary>
/// <remarks>
/// Built on <see cref="PropertyBag"/>'s record of what was looked up, minus
/// <see cref="DesignerPropertyCatalog"/>'s properties that have nothing to carry. One warning per
/// artifact, grouped by type and property, because the reader needs to know <em>which</em> setting
/// was lost and where - a line per control would bury it the way thirty "stays in code-behind" lines
/// buried everything else.
/// </remarks>
public static class DesignerPropertyCoverage
{
    /// <param name="hasMapping">
    /// Whether a control's type is one the registry knows. One that is not is already reported as a
    /// whole ("no mapping registered ... not emitted"), so listing each of its properties as well
    /// would say the same thing once per setting.
    /// </param>
    public static string? Report(
        string artifactName,
        WinFormsArtifactKind kind,
        FormModel formModel,
        Func<ControlModel, bool> hasMapping)
    {
        var lost = new SortedDictionary<string, SortedSet<string>>(StringComparer.Ordinal);

        // `typeName` is what the catalog is keyed on - the WinForms type; `label` what the reader sees,
        // which for the project's own subclass of a control is the name the designer wrote.
        void Collect(string typeName, string? fieldName, PropertyBag properties, string? label = null)
        {
            foreach (var property in properties.UnreadKeys)
            {
                if (DesignerPropertyCatalog.HasNothingToCarry(typeName, property, properties.Peek(property)))
                {
                    continue;
                }

                var key = $"{label ?? typeName}.{property}";
                if (!lost.TryGetValue(key, out var fields))
                {
                    fields = new SortedSet<string>(StringComparer.Ordinal);
                    lost[key] = fields;
                }

                if (fieldName is not null)
                {
                    fields.Add(fieldName);
                }
            }
        }

        Collect(kind == WinFormsArtifactKind.UserControl ? "UserControl" : "Form", null, formModel.FormProperties);
        foreach (var control in formModel.Controls.Values.Where(hasMapping))
        {
            Collect(control.ClrTypeName, control.FieldName, control.Properties, control.DeclaredClrTypeName);
        }

        if (lost.Count == 0)
        {
            return null;
        }

        var items = lost.Select(e => e.Value.Count == 0 ? e.Key : $"{e.Key} ({string.Join(", ", e.Value)})");
        return $"'{artifactName}' sets designer properties nothing in this conversion translates, so they are not "
            + $"carried over: {string.Join("; ", items)}.";
    }
}
