using WinFormsToAvalonia.Core.Model;

namespace WinFormsToAvalonia.Core.Mapping.Vendors;

/// <summary>The conversions a <see cref="VendorProperty"/> needs where a vendor spells a value differently.</summary>
internal static class VendorValues
{
    /// <summary>An enum value as one member, renamed through <paramref name="members"/>; anything else declines.</summary>
    public static Func<PropertyValue, PropertyValue?> Enum(params (string From, string To)[] members) =>
        value => value is PropertyValue.EnumMembers { MemberNames: [var only] }
                 && members.FirstOrDefault(m => m.From == only) is { To: { } to }
            ? new PropertyValue.EnumMembers([to])
            : null;

    /// <summary>An enum value that stands for a bool: Telerik's <c>ToggleState.On</c> is <c>Checked = true</c>.</summary>
    public static Func<PropertyValue, PropertyValue?> EnumAsBool(string trueMember, string falseMember) =>
        value => value switch
        {
            PropertyValue.EnumMembers { MemberNames: [var only] } when only == trueMember => new PropertyValue.Literal(true),
            PropertyValue.EnumMembers { MemberNames: [var only] } when only == falseMember => new PropertyValue.Literal(false),
            _ => null,
        };

    /// <summary>A bool that stands for one of two enum members.</summary>
    public static Func<PropertyValue, PropertyValue?> BoolAsEnum(string whenTrue, string whenFalse) =>
        value => value is PropertyValue.Literal { Value: bool flag }
            ? new PropertyValue.EnumMembers([flag ? whenTrue : whenFalse])
            : null;

    /// <summary>
    /// An <c>EditValue</c> - typed <c>object</c> on every DevExpress editor - only when the designer
    /// wrote a literal of the type the WinForms property has.
    /// </summary>
    public static Func<PropertyValue, PropertyValue?> LiteralOf<T>() =>
        value => value is PropertyValue.Literal { Value: T } ? value : null;

    /// <summary>A number literal of any numeric type, as an editor's <c>EditValue</c> holds one.</summary>
    public static PropertyValue? Number(PropertyValue value) =>
        value is PropertyValue.Literal { Value: int or long or decimal or double or float } ? value : null;
}
