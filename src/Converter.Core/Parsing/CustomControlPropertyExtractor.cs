using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Converter.Core.Parsing;

/// <summary>
/// How a custom control's own public property was classified for Avalonia StyledProperty
/// emission (see CodeBehindGenerator). PlainBindable: a true "{ get; set; }" auto-property, or
/// an expression-bodied field passthrough that's functionally identical to one (e.g.
/// "get => _x; set => _x = value;") - CustomControlProperty.BackingFieldName is set only in the
/// latter case. Coerced: the setter's RHS was mechanically translated into an Avalonia "coerce"
/// callback (CoerceMethodName/CoerceMethodBody carry the generated static method).
/// CoercionFallback: the setter *looked* self-contained (single statement, assigns only the
/// field the getter reads, references "value") but its RHS wasn't in the safe-translatable
/// expression subset - becomes a plain, unvalidated StyledProperty instead of being dropped,
/// plus a manual-step note (see ConversionOrchestrator) pointing at the original logic.
/// </summary>
public enum CustomControlPropertyKind
{
    PlainBindable,
    Coerced,
    CoercionFallback
}

/// <summary>
/// A custom control's own public property that is safe to auto-wire as a real Avalonia
/// bindable property (see CodeBehindGenerator's StyledProperty emission).
/// </summary>
public record CustomControlProperty(
    string Name,
    string TypeName,
    CustomControlPropertyKind Kind,
    string? BackingFieldName = null,
    string? CoerceMethodName = null,
    string? CoerceMethodBody = null);

/// <summary>
/// A custom control's own public property whose getter/setter just forward straight through to
/// a single child member (e.g. "get => _textBox.Text; set => _textBox.Text = value;") -
/// re-emitted by CodeBehindGenerator as a plain forwarding CLR property, no
/// StyledProperty/backing field/AvaloniaProperty machinery at all, since the real state already
/// lives on the named child element. "override" is always stripped regardless of WasOverride:
/// Avalonia's UserControl/Control base declares no "Text" member the way WinForms' Control does,
/// so keeping "override" would be a hard CS0115 compile error - dropping it is always safe
/// (worst case an unused-hides-member CS0108 warning, never an error). FallbackExpression
/// carries a "value ?? X" RHS fallback (e.g. "string.Empty") when present, null when the
/// setter's RHS is exactly "value".
/// </summary>
public record DelegatingCustomControlProperty(
    string Name,
    string TypeName,
    string FieldName,
    string MemberName,
    string? FallbackExpression,
    bool WasOverride);

/// <summary>
/// A custom control's own public property found but not auto-wired, and why.
/// </summary>
public record SkippedCustomControlProperty(string Name, string Reason);

public record CustomControlPropertyExtractionResult(
    IReadOnlyList<CustomControlProperty> Bindable,
    IReadOnlyList<DelegatingCustomControlProperty> Delegating,
    IReadOnlyList<SkippedCustomControlProperty> Skipped)
{
    public static readonly CustomControlPropertyExtractionResult Empty = new([], [], []);

    /// <summary>
    /// Every property name this control settably exposes one way or another (Bindable ∪
    /// Delegating) - used by ConversionOrchestrator both for PropertyTranslations (an embedding
    /// site can set any of these as a literal XAML attribute) and for the "Custom Control
    /// Instance" manual step's dropped-properties computation (only Skipped is actually lost).
    /// </summary>
    public IEnumerable<string> AllSettableNames() =>
        Bindable.Select(p => p.Name).Concat(Delegating.Select(p => p.Name));
}

/// <summary>
/// Extracts a custom WinForms control's own public properties from its sibling non-designer
/// `.cs` file (a class's Designer.cs never declares hand-authored properties - only
/// InitializeComponent). Narrow, best-effort Roslyn pass in the same spirit as
/// EventHandlerBodyParser/CodeBehindMemberExtractor: a non-designer file is arbitrary,
/// unconstrained user code, so a missing/unparseable file simply yields nothing, never a hard
/// failure.
///
/// Every public, non-static property is classified into exactly one of four buckets, checked in
/// this order:
/// 1. A plain auto-property ("{ get; set; }") or a trivial expression-bodied field passthrough
///    ("get => _x; set => _x = value;") - PlainBindable if the type is supported, else Skipped.
/// 2. A property that just forwards to a single child member ("get => _textBox.Text; set =>
///    _textBox.Text = value;", optionally "value ?? &lt;simple fallback&gt;" on the setter) -
///    Delegating. No type restriction - forwarding needs no StyledProperty&lt;T&gt;.
/// 3. A property whose setter is *exactly one statement* assigning the field the getter reads,
///    from an expression referencing "value" - Coerced if that expression is in a safe,
///    mechanically-translatable subset (value/sibling-property reads/literals/Math.Clamp-Min-
///    Max-Abs/comparisons/arithmetic/ternaries), else CoercionFallback (bindable, but no
///    validation - the original logic needs manual porting).
/// 4. Everything else - Skipped("has custom getter/setter logic"). Deliberately conservative:
///    any second statement, a guard clause, a method call, an event raise, or a write to a
///    *different* field/property disqualifies bucket 3 entirely (never softened into
///    CoercionFallback) - a coerce callback is a pure value-to-value function and cannot
///    represent that behavior; silently reproducing only the first statement of a multi-
///    statement setter would silently drop real behavior (e.g. a "Minimum" setter that also
///    re-clamps "Value"), which is worse than today's honest "not converted, wire manually."
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
        "DateTime", "System.DateTime",
        "object", "object?", "System.Object", "System.Object?"
    ];

    private static readonly HashSet<SyntaxKind> SafeBinaryKinds =
    [
        SyntaxKind.LessThanExpression, SyntaxKind.GreaterThanExpression,
        SyntaxKind.LessThanOrEqualExpression, SyntaxKind.GreaterThanOrEqualExpression,
        SyntaxKind.EqualsExpression, SyntaxKind.NotEqualsExpression,
        SyntaxKind.AddExpression, SyntaxKind.SubtractExpression,
        SyntaxKind.MultiplyExpression, SyntaxKind.DivideExpression
    ];

    private static readonly HashSet<string> SafeMathMethods = ["Clamp", "Min", "Max", "Abs"];

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
            var delegating = new List<DelegatingCustomControlProperty>();
            var skipped = new List<SkippedCustomControlProperty>();

            void AddBindableOrTypeSkip(string name, string typeName, string? backingFieldName)
            {
                if (!SupportedTypeNames.Contains(typeName))
                {
                    skipped.Add(new SkippedCustomControlProperty(
                        name, $"type '{typeName}' is not supported for auto-binding"));
                    return;
                }

                bindable.Add(new CustomControlProperty(
                    name, typeName, CustomControlPropertyKind.PlainBindable, backingFieldName));
            }

            foreach (var property in classDeclaration.Members.OfType<PropertyDeclarationSyntax>())
            {
                if (!property.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)) ||
                    property.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)))
                {
                    continue;
                }

                var name = property.Identifier.Text;
                var typeName = property.Type.ToString();

                if (IsPlainAutoProperty(property))
                {
                    AddBindableOrTypeSkip(name, typeName, backingFieldName: null);
                    continue;
                }

                if (TryGetTrivialFieldPassthrough(property, classDeclaration, out var passthroughField))
                {
                    AddBindableOrTypeSkip(name, typeName, passthroughField);
                    continue;
                }

                if (TryGetDelegatingShape(property, out var fieldName, out var memberName, out var fallback))
                {
                    delegating.Add(new DelegatingCustomControlProperty(
                        name, typeName, fieldName!, memberName!, fallback,
                        WasOverride: property.Modifiers.Any(m => m.IsKind(SyntaxKind.OverrideKeyword))));
                    continue;
                }

                if (TryGetCoercionShapedSetter(property, classDeclaration, out var backingField, out var rhsExpression))
                {
                    if (!SupportedTypeNames.Contains(typeName))
                    {
                        skipped.Add(new SkippedCustomControlProperty(
                            name, $"type '{typeName}' is not supported for auto-binding"));
                        continue;
                    }

                    var siblingProperties = new HashSet<string>();
                    if (IsSafeCoercionExpression(rhsExpression!, classDeclaration, siblingProperties))
                    {
                        var coerceMethodName = $"Coerce{name}";
                        var coerceMethodBody = CoerceExpressionTranslator.Translate(
                            rhsExpression!, siblingProperties, typeName, coerceMethodName, className);
                        bindable.Add(new CustomControlProperty(
                            name, typeName, CustomControlPropertyKind.Coerced,
                            backingField, coerceMethodName, coerceMethodBody));
                    }
                    else
                    {
                        bindable.Add(new CustomControlProperty(
                            name, typeName, CustomControlPropertyKind.CoercionFallback, backingField));
                    }

                    continue;
                }

                skipped.Add(new SkippedCustomControlProperty(name, "has custom getter/setter logic"));
            }

            return new CustomControlPropertyExtractionResult(bindable, delegating, skipped);
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

    /// <summary>
    /// "get => _x; set => _x = value;" - functionally identical to a plain auto-property (no
    /// validation, no side effects), just written with expression-bodied accessors instead
    /// (e.g. NumericStepperControl.Increment). Both accessors must be expression-bodied
    /// (deliberately narrower than TryGetCoercionShapedSetter's block-bodied allowance - this
    /// shape only needs to match the common real-world case).
    /// </summary>
    private static bool TryGetTrivialFieldPassthrough(
        PropertyDeclarationSyntax property, ClassDeclarationSyntax classDeclaration, out string? fieldName)
    {
        fieldName = null;

        if (property.AccessorList == null || property.AccessorList.Accessors.Count != 2)
        {
            return false;
        }

        var getter = property.AccessorList.Accessors.FirstOrDefault(a => a.Kind() == SyntaxKind.GetAccessorDeclaration);
        var setter = property.AccessorList.Accessors.FirstOrDefault(a => a.Kind() == SyntaxKind.SetAccessorDeclaration);
        if (getter?.ExpressionBody == null || setter?.ExpressionBody == null)
        {
            return false;
        }

        if (getter.ExpressionBody.Expression is not IdentifierNameSyntax getterField)
        {
            return false;
        }

        if (setter.ExpressionBody.Expression is not AssignmentExpressionSyntax
            {
                RawKind: (int)SyntaxKind.SimpleAssignmentExpression,
                Left: IdentifierNameSyntax setterField,
                Right: IdentifierNameSyntax { Identifier.Text: "value" }
            })
        {
            return false;
        }

        if (getterField.Identifier.Text != setterField.Identifier.Text ||
            !IsPrivateInstanceField(classDeclaration, getterField.Identifier.Text))
        {
            return false;
        }

        fieldName = getterField.Identifier.Text;
        return true;
    }

    /// <summary>
    /// "get => _textBox.Text; set => _textBox.Text = value;" (optionally "value ?? &lt;simple
    /// fallback&gt;" on the setter, e.g. AutocompleteSearchBox.Text's "value ?? string.Empty") -
    /// a single-level member access on a private field, no "this.", no chaining. Attributes on
    /// the property (e.g. "[AllowNull]", present on the real Text property) never block a
    /// match - only the accessor bodies are inspected.
    /// </summary>
    private static bool TryGetDelegatingShape(
        PropertyDeclarationSyntax property, out string? fieldName, out string? memberName, out string? fallbackExpression)
    {
        fieldName = null;
        memberName = null;
        fallbackExpression = null;

        if (property.AccessorList == null || property.AccessorList.Accessors.Count != 2)
        {
            return false;
        }

        var getter = property.AccessorList.Accessors.FirstOrDefault(a => a.Kind() == SyntaxKind.GetAccessorDeclaration);
        var setter = property.AccessorList.Accessors.FirstOrDefault(a => a.Kind() == SyntaxKind.SetAccessorDeclaration);
        if (getter == null || setter == null)
        {
            return false;
        }

        if (GetSingleExpression(getter) is not MemberAccessExpressionSyntax
            {
                Expression: IdentifierNameSyntax getField,
                Name: IdentifierNameSyntax getMember
            })
        {
            return false;
        }

        if (GetSingleExpression(setter) is not AssignmentExpressionSyntax
            {
                RawKind: (int)SyntaxKind.SimpleAssignmentExpression,
                Left: MemberAccessExpressionSyntax
                {
                    Expression: IdentifierNameSyntax setField,
                    Name: IdentifierNameSyntax setMember
                }
            } assignment)
        {
            return false;
        }

        if (getField.Identifier.Text != setField.Identifier.Text ||
            getMember.Identifier.Text != setMember.Identifier.Text)
        {
            return false;
        }

        if (assignment.Right is IdentifierNameSyntax { Identifier.Text: "value" })
        {
            fallbackExpression = null;
        }
        else if (assignment.Right is BinaryExpressionSyntax { RawKind: (int)SyntaxKind.CoalesceExpression } coalesce &&
            coalesce.Left is IdentifierNameSyntax { Identifier.Text: "value" } &&
            IsSimpleFallbackExpression(coalesce.Right))
        {
            fallbackExpression = coalesce.Right.ToString();
        }
        else
        {
            return false;
        }

        fieldName = getField.Identifier.Text;
        memberName = getMember.Identifier.Text;
        return true;
    }

    private static bool IsSimpleFallbackExpression(ExpressionSyntax expression) =>
        expression is LiteralExpressionSyntax ||
        // A predefined-type member access (e.g. "string.Empty") parses its left side as
        // PredefinedTypeSyntax, not IdentifierNameSyntax - a plain user-defined static ("Foo.Bar")
        // parses as IdentifierNameSyntax instead, so both are accepted here.
        expression is MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax or PredefinedTypeSyntax };

    /// <summary>
    /// A setter that is *exactly one statement*, assigning the same field the getter reads,
    /// from an expression that references "value" - the shape checked before the RHS is walked
    /// for translatable safety (see IsSafeCoercionExpression). Any second statement disqualifies
    /// entirely (see class doc comment for why this is never softened).
    /// </summary>
    private static bool TryGetCoercionShapedSetter(
        PropertyDeclarationSyntax property, ClassDeclarationSyntax classDeclaration,
        out string? fieldName, out ExpressionSyntax? rhsExpression)
    {
        fieldName = null;
        rhsExpression = null;

        if (property.AccessorList == null || property.AccessorList.Accessors.Count != 2)
        {
            return false;
        }

        var getter = property.AccessorList.Accessors.FirstOrDefault(a => a.Kind() == SyntaxKind.GetAccessorDeclaration);
        var setter = property.AccessorList.Accessors.FirstOrDefault(a => a.Kind() == SyntaxKind.SetAccessorDeclaration);
        if (getter?.ExpressionBody?.Expression is not IdentifierNameSyntax getterField || setter == null)
        {
            return false;
        }

        if (!IsPrivateInstanceField(classDeclaration, getterField.Identifier.Text))
        {
            return false;
        }

        if (GetSingleExpression(setter) is not AssignmentExpressionSyntax
            {
                RawKind: (int)SyntaxKind.SimpleAssignmentExpression,
                Left: IdentifierNameSyntax setterField
            } assignment)
        {
            return false;
        }

        if (setterField.Identifier.Text != getterField.Identifier.Text)
        {
            return false;
        }

        if (!assignment.Right.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>()
                .Any(n => n.Identifier.Text == "value"))
        {
            return false;
        }

        fieldName = getterField.Identifier.Text;
        rhsExpression = assignment.Right;
        return true;
    }

    /// <summary>
    /// The mechanically-translatable expression subset for a coerce callback: "value" itself, a
    /// sibling *public instance property* read (never a private field - deliberately: rewards
    /// the "clean" pattern of a setter reading Minimum/Maximum as properties, and is what keeps
    /// a field-referencing variant, e.g. the real NumericStepperControl.Value, correctly out of
    /// this bucket), literals, Math.Clamp/Min/Max/Abs calls, comparison/arithmetic binary
    /// expressions, and ternaries - each recursively. Every matched sibling-property identifier
    /// is recorded into <paramref name="siblingProperties"/> so the caller can rewrite exactly
    /// those (and only those) into "((Owner)sender).Name" - see CoerceExpressionTranslator.
    /// </summary>
    private static bool IsSafeCoercionExpression(
        ExpressionSyntax expression, ClassDeclarationSyntax classDeclaration, HashSet<string> siblingProperties)
    {
        switch (expression)
        {
            case ParenthesizedExpressionSyntax parenthesized:
                return IsSafeCoercionExpression(parenthesized.Expression, classDeclaration, siblingProperties);

            case IdentifierNameSyntax { Identifier.Text: "value" }:
                return true;

            case IdentifierNameSyntax identifier:
                if (!IsPublicInstanceProperty(classDeclaration, identifier.Identifier.Text))
                {
                    return false;
                }
                siblingProperties.Add(identifier.Identifier.Text);
                return true;

            case LiteralExpressionSyntax:
                return true;

            case BinaryExpressionSyntax binary when SafeBinaryKinds.Contains(binary.Kind()):
                return IsSafeCoercionExpression(binary.Left, classDeclaration, siblingProperties) &&
                    IsSafeCoercionExpression(binary.Right, classDeclaration, siblingProperties);

            case ConditionalExpressionSyntax conditional:
                return IsSafeCoercionExpression(conditional.Condition, classDeclaration, siblingProperties) &&
                    IsSafeCoercionExpression(conditional.WhenTrue, classDeclaration, siblingProperties) &&
                    IsSafeCoercionExpression(conditional.WhenFalse, classDeclaration, siblingProperties);

            case InvocationExpressionSyntax
                {
                    Expression: MemberAccessExpressionSyntax
                    {
                        Expression: IdentifierNameSyntax { Identifier.Text: "Math" },
                        Name.Identifier.Text: var methodName
                    }
                } invocation when SafeMathMethods.Contains(methodName):
                return invocation.ArgumentList.Arguments.All(
                    a => IsSafeCoercionExpression(a.Expression, classDeclaration, siblingProperties));

            default:
                return false;
        }
    }

    private static bool IsPublicInstanceProperty(ClassDeclarationSyntax classDeclaration, string propertyName) =>
        classDeclaration.Members.OfType<PropertyDeclarationSyntax>().Any(p =>
            p.Identifier.Text == propertyName &&
            p.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)) &&
            !p.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)));

    private static bool IsPrivateInstanceField(ClassDeclarationSyntax classDeclaration, string fieldName) =>
        classDeclaration.Members.OfType<FieldDeclarationSyntax>().Any(f =>
            !f.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword) || m.IsKind(SyntaxKind.PublicKeyword) ||
                m.IsKind(SyntaxKind.InternalKeyword) || m.IsKind(SyntaxKind.ProtectedKeyword)) &&
            f.Declaration.Variables.Any(v => v.Identifier.Text == fieldName));

    /// <summary>
    /// The single expression an accessor's body reduces to, whether expression-bodied
    /// ("=> expr;") or a one-statement block ("{ return expr; }" for a getter, "{ expr; }" for
    /// a setter) - any other shape (0 or 2+ statements, a block with anything but a single
    /// return/expression statement) yields null, which every caller treats as "does not match."
    /// </summary>
    private static ExpressionSyntax? GetSingleExpression(AccessorDeclarationSyntax accessor)
    {
        if (accessor.ExpressionBody != null)
        {
            return accessor.ExpressionBody.Expression;
        }

        if (accessor.Body is { Statements.Count: 1 } body)
        {
            return body.Statements[0] switch
            {
                ReturnStatementSyntax { Expression: { } expr } => expr,
                ExpressionStatementSyntax { Expression: { } expr } => expr,
                _ => null
            };
        }

        return null;
    }
}

/// <summary>
/// Translates an already-validated-safe coerce expression (see
/// CustomControlPropertyExtractor.IsSafeCoercionExpression) into the full C# source of an
/// Avalonia StyledProperty "coerce" callback method. Unlike EventHandlerBodyParser's
/// ExtractBodyText (which re-parses arbitrary, unproven user code and falls back to a comment
/// block on failure), this method's input was already syntactically proven safe at extraction
/// time - it is pure text reconstruction, not re-validation, so it needs no try/catch fallback.
/// </summary>
public static class CoerceExpressionTranslator
{
    public static string Translate(
        ExpressionSyntax expression, IReadOnlySet<string> siblingProperties,
        string typeName, string coerceMethodName, string className)
    {
        var text = expression.ToString();

        // Plain word-boundary rewrite, not a Roslyn-token-aware one - the same accepted
        // limitation (could also match inside an unrelated identifier sharing the name) already
        // documented for CodeBehindGenerator's own migratedNames rewrite.
        foreach (var propertyName in siblingProperties)
        {
            text = System.Text.RegularExpressions.Regex.Replace(
                text, $@"\b{System.Text.RegularExpressions.Regex.Escape(propertyName)}\b",
                $"(({className})sender).{propertyName}");
        }

        return $"private static {typeName} {coerceMethodName}(Avalonia.AvaloniaObject sender, {typeName} value) => {text};";
    }
}
