using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WinFormsToAvalonia.Core.Mapping;

/// <summary>
/// The types a carried-over helper, field, property or local may be declared with: ones that are
/// the same type in the generated project as in the WinForms one, and need no import there.
/// </summary>
/// <remarks>
/// <para>
/// Keyword types only, until now - on the grounds that a named type could be a WinForms type whose
/// Avalonia counterpart is a different type entirely, and syntax alone cannot tell. That is true of
/// a name nobody has checked, and not of these: <c>System</c>'s own value types, which every
/// generated View already imports. A helper taking a <c>DateTime</c> or returning a
/// <c>TimeSpan</c> used to stay a comment for no reason the converter could have stated.
/// </para>
/// <para>
/// The nullable and array forms of any of them qualify too - including of the keyword types, which
/// makes <c>string?</c> one: a project with nullable enabled writes it constantly, and it parses
/// as a nullable type rather than a keyword one, so it had been refused along with everything else.
/// </para>
/// </remarks>
public static class PlainDotNetTypeCatalog
{
    /// <summary>The named types accepted, all in <c>System</c>.</summary>
    public static IReadOnlySet<string> NamedTypes { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "DateTime", "DateTimeOffset", "TimeSpan", "DateOnly", "TimeOnly", "Guid", "Uri", "Version",
    };

    public static bool IsPlain(TypeSyntax type) => type switch
    {
        PredefinedTypeSyntax => true,
        IdentifierNameSyntax { Identifier.ValueText: var name } => NamedTypes.Contains(name),
        QualifiedNameSyntax { Left: IdentifierNameSyntax { Identifier.ValueText: "System" }, Right.Identifier.ValueText: var name }
            => NamedTypes.Contains(name),
        NullableTypeSyntax nullable => IsPlain(nullable.ElementType),
        ArrayTypeSyntax array => IsPlain(array.ElementType),
        _ => false,
    };

    /// <remarks>
    /// <c>void</c> first: it is only legal as a return type, so parsing it on its own reports a
    /// diagnostic - and a helper returning nothing is the commonest kind there is.
    /// </remarks>
    public static bool IsPlain(string typeText)
    {
        if (typeText.Trim() == "void")
        {
            return true;
        }

        var type = SyntaxFactory.ParseTypeName(typeText);
        return !type.ContainsDiagnostics && IsPlain(type);
    }
}
