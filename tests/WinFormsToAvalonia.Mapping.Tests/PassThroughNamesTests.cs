using WinFormsToAvalonia.Core.Semantics;
using Xunit;

namespace WinFormsToAvalonia.Mapping.Tests;

/// <summary>
/// Code the semantic model proves UI-neutral is emitted verbatim, with every type outside a small
/// set of namespaces written namespace-qualified. The ones left as simple names have to mean the
/// same thing inside a generated View as they did in the WinForms source - which is a question
/// about Avalonia's API, so it is answered here.
/// </summary>
public class PassThroughNamesTests
{
    /// <summary>
    /// Every namespace a generated View's code-behind can import (<c>ViewCodeBehindEmitter</c> and
    /// the rewriter's <c>RequiredUsings</c>).
    /// </summary>
    private static readonly string[] ViewImportedNamespaces =
    [
        "Avalonia.Controls",
        "Avalonia.Controls.Primitives",
        "Avalonia.Input",
        "Avalonia.Input.Platform",
        "Avalonia.Interactivity",
        "Avalonia.Media",
        "Avalonia.Platform.Storage",
        "Avalonia.Threading",
    ];

    private static IReadOnlySet<string> SimpleNamedBclTypes() =>
        AvaloniaMetadata.RuntimeExportedTypes()
            .Where(t => !t.IsNested && t.Namespace is { } ns && SemanticProjectContext.SimpleNameNamespaces.Contains(ns))
            .Select(SimpleName)
            .ToHashSet(StringComparer.Ordinal);

    /// <summary>A using of each would make the simple name ambiguous - CS0104 in the generated project.</summary>
    [Fact]
    public void NoSimpleNamedBclType_SharesItsNameWithATypeAViewImports()
    {
        var bcl = SimpleNamedBclTypes();
        Assert.NotEmpty(bcl);

        var imported = AvaloniaMetadata.ExportedTypes()
            .Where(t => !t.IsNested && t.Namespace is { } ns && ViewImportedNamespaces.Contains(ns))
            .ToList();
        Assert.Contains(imported, t => t.FullName == "Avalonia.Controls.Button");
        Assert.Contains("Math", bcl);
        Assert.Contains("List", bcl);

        var collisions = imported
            .Select(t => $"{t.Namespace}.{SimpleName(t)}")
            .Where(full => bcl.Contains(full[(full.LastIndexOf('.') + 1)..]))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.True(
            collisions.Count == 0,
            "These Avalonia types share a simple name with a type pass-through code leaves unqualified: "
            + string.Join(", ", collisions));
    }

    /// <summary>
    /// Inside the View class, a member of its base hides a type of the same name - so `File.Exists`
    /// would reach for a property called `File`.
    /// </summary>
    [Theory]
    [InlineData("Window")]
    [InlineData("UserControl")]
    public void NoSimpleNamedBclType_IsHiddenByAMemberOfTheViewBase(string baseTypeName)
    {
        var bcl = SimpleNamedBclTypes();
        var baseType = AvaloniaMetadata.FindElement(baseTypeName);
        Assert.NotNull(baseType);

        var hiding = new List<string>();
        for (var current = baseType; current is not null; current = current.BaseType)
        {
            hiding.AddRange(current
                .GetMembers(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic
                    | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static
                    | System.Reflection.BindingFlags.DeclaredOnly)
                .Where(m => m is not System.Reflection.ConstructorInfo && bcl.Contains(m.Name)
                    && !(m is System.Reflection.MethodBase method && (method.IsPrivate || method.IsAssembly)))
                .Where(m => m is not System.Reflection.FieldInfo { IsPrivate: true } and not System.Reflection.FieldInfo { IsAssembly: true })
                .Select(m => $"{current.Name}.{m.Name}"));
        }

        Assert.True(
            hiding.Count == 0,
            $"These members of {baseTypeName} hide a type pass-through code leaves unqualified: "
            + string.Join(", ", hiding.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)));
    }

    private static string SimpleName(Type type) =>
        type.Name.IndexOf('`') is var tick and >= 0 ? type.Name[..tick] : type.Name;
}
