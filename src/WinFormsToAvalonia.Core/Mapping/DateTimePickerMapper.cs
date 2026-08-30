using WinFormsToAvalonia.Core.Model;

namespace WinFormsToAvalonia.Core.Mapping;

/// <summary>
/// WinForms' DateTimePicker is a date picker or a clock depending on one designer property, so -
/// like <see cref="ListViewMapper"/> - it needs a per-instance decision rather than a fixed
/// <see cref="SimplePropertyMapper"/> entry: <c>Format = DateTimePickerFormat.Time</c> is
/// Avalonia's <c>TimePicker</c>, everything else its <c>CalendarDatePicker</c>.
/// </summary>
/// <remarks>
/// <para>
/// The two Avalonia controls share no value property - <c>SelectedTime</c> is a
/// <c>TimeSpan?</c> and <c>SelectedDate</c> a <c>DateTimeOffset?</c> - so picking the wrong one
/// does not merely look wrong, it discards the half of the value the form was about. A
/// <c>Format=Time</c> picker mapped to a CalendarDatePicker, which is what this replaces, showed
/// the user a calendar where the designer had asked for a clock.
/// </para>
/// <para>
/// <c>Custom</c> stays on the date picker and is reported: <c>CustomFormat</c> is a WinForms
/// format string with no Avalonia counterpart, and guessing which half of it matters would be
/// inventing an answer.
/// </para>
/// </remarks>
public sealed class DateTimePickerMapper : IControlMapper
{
    public string WinFormsTypeName => "DateTimePicker";

    public MappedControl Map(ControlModel control)
    {
        var format = control.Properties.TryGetValue("Format", out var value)
            && value is PropertyValue.EnumMembers { MemberNames: [var member] }
                ? member
                : "Long";

        if (string.Equals(format, "Time", StringComparison.Ordinal))
        {
            return new MappedControl(
                control.ClrTypeName, MappingStatus.Direct, "TimePicker",
                new Dictionary<string, string>(StringComparer.Ordinal), null, [],
                // BindablePropertyCatalog answers for "DateTimePicker" as a whole, and its answer
                // for Value is CalendarDatePicker's SelectedDate. A TimePicker has SelectedTime
                // and no SelectedDate at all, so binding one here would be a CS1061 in the
                // generated project rather than anything this build could see.
                UnreachableBindableMembers: ["SelectedDate"]);
        }

        var warnings = string.Equals(format, "Custom", StringComparison.Ordinal)
            ?
            [
                $"'{control.FieldName}' is a DateTimePicker with Format=Custom; it maps to a "
                + "CalendarDatePicker and the CustomFormat string has no Avalonia counterpart - set "
                + "CustomDateFormatString on the generated element by hand if you need it.",
            ]
            : (IReadOnlyList<string>)[];

        return new MappedControl(
            control.ClrTypeName, MappingStatus.Direct, "CalendarDatePicker",
            new Dictionary<string, string>(StringComparer.Ordinal), null, warnings);
    }
}
