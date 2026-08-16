using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WinFormsToAvalonia.Core.Parsing;

internal static class RoslynTypeNameHelper
{
    /// <summary>Returns the simple (unqualified, non-generic-argument) name of a type reference, e.g. "Button" for both `Button` and `System.Windows.Forms.Button`.</summary>
    public static string GetSimpleTypeName(TypeSyntax type) => type switch
    {
        QualifiedNameSyntax qualified => GetSimpleTypeName(qualified.Right),
        AliasQualifiedNameSyntax aliasQualified => GetSimpleTypeName(aliasQualified.Name),
        GenericNameSyntax generic => generic.Identifier.ValueText,
        IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
        _ => type.ToString(),
    };
}
