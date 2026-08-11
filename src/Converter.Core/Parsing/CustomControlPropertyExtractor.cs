using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Converter.Core.Parsing;

/// <summary>
/// A custom control's own public property that is safe to auto-wire as a real Avalonia
/// bindable property (see CodeBehindGenerator's StyledProperty emission).
/// </summary>
public record CustomControlProperty(string Name, string TypeName);

/// <summary>
/// A custom control's own public property found but not auto-wired, and why.
/// </summary>
public record SkippedCustomControlProperty(string Name, string Reason);

public record CustomControlPropertyExtractionResult(
    IReadOnlyList<CustomControlProperty> Bindable,
    IReadOnlyList<SkippedCustomControlProperty> Skipped)
{
    public static readonly CustomControlPropertyExtractionResult Empty = new([], []);
}

/// <summary>
/// Extracts a custom WinForms control's own public properties from its sibling non-designer
/// `.cs` file (a class's Designer.cs never declares hand-authored properties - only
/// InitializeComponent). Narrow, best-effort Roslyn pass in the same spirit as
/// EventHandlerBodyParser/CodeBehindMemberExtractor: a non-designer file is arbitrary,
/// unconstrained user code, so a missing/unparseable file simply yields nothing, never a hard
/// failure. Deliberately conservative about what it treats as safe to auto-wire as a real
/// Avalonia StyledProperty - only a plain public auto-property (`{ get; set; }`, no accessor
/// body) of a simple, well-understood type. A property with any custom getter/setter logic
/// (e.g. delegating to a child control) or an unsupported type is reported as "skipped" instead,
/// for ConversionOrchestrator to surface as a manual step rather than silently dropping it.
/// </summary>
public static class CustomControlPropertyExtractor
{
    private static readonly HashSet<string> SupportedTypeNames =
    [
        "string", "System.String",
        "int", "System.Int32",
        "bool", "System.Boolean",
        "double", "System.Double",
        "float", "System.Single",
        "long", "System.Int64",
        "decimal", "System.Decimal",
        "DateTime", "System.DateTime"
    ];

    public static async Task<CustomControlPropertyExtractionResult> ExtractAsync(
        string codeBehindFilePath, string className)
    {
        try
        {
            var sourceCode = await File.ReadAllTextAsync(codeBehindFilePath);
            var root = CSharpSyntaxTree.ParseText(sourceCode).GetRoot();

            var classDeclaration = root.DescendantNodes().OfType<ClassDeclarationSyntax>()
                .FirstOrDefault(c => c.Identifier.Text == className);
            if (classDeclaration == null)
            {
                return CustomControlPropertyExtractionResult.Empty;
            }

            var bindable = new List<CustomControlProperty>();
            var skipped = new List<SkippedCustomControlProperty>();

            foreach (var property in classDeclaration.Members.OfType<PropertyDeclarationSyntax>())
            {
                if (!property.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)) ||
                    property.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)))
                {
                    continue;
                }

                var name = property.Identifier.Text;
                var typeName = property.Type.ToString();

                if (!IsPlainAutoProperty(property))
                {
                    skipped.Add(new SkippedCustomControlProperty(name, "has custom getter/setter logic"));
                    continue;
                }

                if (!SupportedTypeNames.Contains(typeName))
                {
                    skipped.Add(new SkippedCustomControlProperty(
                        name, $"type '{typeName}' is not supported for auto-binding"));
                    continue;
                }

                bindable.Add(new CustomControlProperty(name, typeName));
            }

            return new CustomControlPropertyExtractionResult(bindable, skipped);
        }
        catch
        {
            // Best-effort: an unparseable/unreadable sibling file means no properties found,
            // not a failed conversion.
            return CustomControlPropertyExtractionResult.Empty;
        }
    }

    /// <summary>
    /// A plain "{ get; set; }" auto-property: both accessors present, neither with a body -
    /// excludes both a computed/custom-logic property and a getter-only ("{ get; }") or
    /// init-only ("{ get; init; }") one, none of which are safe to back with a two-way
    /// StyledProperty the same way.
    /// </summary>
    private static bool IsPlainAutoProperty(PropertyDeclarationSyntax property)
    {
        if (property.AccessorList == null || property.AccessorList.Accessors.Count != 2)
        {
            return false;
        }

        return property.AccessorList.Accessors.All(a => a.Body == null && a.ExpressionBody == null) &&
            property.AccessorList.Accessors.Any(a => a.Kind() == SyntaxKind.GetAccessorDeclaration) &&
            property.AccessorList.Accessors.Any(a => a.Kind() == SyntaxKind.SetAccessorDeclaration);
    }
}
