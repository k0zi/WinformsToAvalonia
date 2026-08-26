using Microsoft.CodeAnalysis.CSharp.Syntax;
using WinFormsToAvalonia.Core.Model;

namespace WinFormsToAvalonia.Core.Parsing;

/// <summary>
/// Resolves the finite vocabulary of expressions that designer-generated code actually
/// emits for property values: literals, `new Point/Size/Padding/Font(...)`,
/// `Color.FromArgb(...)`/named colors, and single or OR'd enum members (including the
/// `((EnumType)(a | b))` cast-wrapped form the designer emits for flag combinations).
/// Designer output is extremely regular, so this finite grammar covers it without needing
/// general C# expression evaluation; anything outside it becomes
/// <see cref="PropertyValue.Unresolved"/> with the raw expression text preserved.
/// </summary>
public static class ExpressionEvaluator
{
    public static PropertyValue Evaluate(ExpressionSyntax expression)
    {
        expression = Unwrap(expression);

        if (expression is LiteralExpressionSyntax literal)
        {
            return new PropertyValue.Literal(literal.Token.Value);
        }

        if (TryGetNegatedNumericLiteral(expression, out var negatedValue))
        {
            return new PropertyValue.Literal(negatedValue);
        }

        if (TryEvaluateColor(expression, out var color))
        {
            return color;
        }

        // `((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")))` - how a
        // *non*-localizable form pulls an image, icon or other blob out of its .resx. Unwrap()
        // has already stripped the cast and parentheses by this point.
        if (expression is InvocationExpressionSyntax
            {
                Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "GetObject" or "GetString" },
                ArgumentList.Arguments: [{ Expression: LiteralExpressionSyntax { Token.Value: string resourceKey } }],
            })
        {
            return new PropertyValue.ResourceReference(resourceKey);
        }

        if (expression is ObjectCreationExpressionSyntax creation)
        {
            var typeName = RoslynTypeNameHelper.GetSimpleTypeName(creation.Type);

            if (typeName == "Point" && TryGetTwoIntArgs(creation.ArgumentList, out var px, out var py))
            {
                return new PropertyValue.PointValue(px, py);
            }

            if (typeName == "Size" && TryGetTwoIntArgs(creation.ArgumentList, out var sw, out var sh))
            {
                return new PropertyValue.SizeValue(sw, sh);
            }

            if (typeName == "Padding" && TryEvaluatePadding(creation.ArgumentList, out var padding))
            {
                return padding;
            }

            if (typeName == "Font" && TryEvaluateFont(creation.ArgumentList, out var font))
            {
                return font;
            }

            // `new Icon("app.ico")` / `new System.Drawing.Icon("app.ico")` - the only Icon
            // construction shape with a literal path; resx/dynamic shapes (the common case
            // for NotifyIcon.Icon in real Designer.cs) stay Unresolved.
            if (typeName == "Icon"
                && creation.ArgumentList is { Arguments.Count: 1 } iconArgs
                && iconArgs.Arguments[0].Expression is LiteralExpressionSyntax { Token.Value: string iconPath })
            {
                return new PropertyValue.Literal(iconPath);
            }
        }

        if (expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Empty" } emptyAccess)
        {
            var emptyQualifier = GetQualifierSimpleName(emptyAccess.Expression);
            if (emptyQualifier == "Point")
            {
                return new PropertyValue.PointValue(0, 0);
            }

            if (emptyQualifier == "Size")
            {
                return new PropertyValue.SizeValue(0, 0);
            }
        }

        if (expression is MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax, Name.Identifier.ValueText: var refFieldName })
        {
            return new PropertyValue.ControlReference(refFieldName);
        }

        if (CollectEnumMembers(expression, out var members))
        {
            return new PropertyValue.EnumMembers(members);
        }

        return new PropertyValue.Unresolved(expression.ToString());
    }

    private static ExpressionSyntax Unwrap(ExpressionSyntax expression)
    {
        while (true)
        {
            switch (expression)
            {
                case ParenthesizedExpressionSyntax paren:
                    expression = paren.Expression;
                    continue;
                case CastExpressionSyntax cast:
                    expression = cast.Expression;
                    continue;
                default:
                    return expression;
            }
        }
    }

    private static bool TryEvaluateColor(ExpressionSyntax expression, out PropertyValue.ColorValue color)
    {
        if (expression is InvocationExpressionSyntax
            {
                Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "FromArgb" } target,
            } invocation
            && GetQualifierSimpleName(target.Expression) == "Color"
            && invocation.ArgumentList is not null)
        {
            var args = invocation.ArgumentList.Arguments.Select(a => a.Expression).ToList();

            if (args.Count == 3
                && TryGetByte(args[0], out var r3) && TryGetByte(args[1], out var g3) && TryGetByte(args[2], out var b3))
            {
                color = new PropertyValue.ColorValue(null, 255, r3, g3, b3);
                return true;
            }

            if (args.Count == 4
                && TryGetByte(args[0], out var a4) && TryGetByte(args[1], out var r4)
                && TryGetByte(args[2], out var g4) && TryGetByte(args[3], out var b4))
            {
                color = new PropertyValue.ColorValue(null, a4, r4, g4, b4);
                return true;
            }
        }

        if (expression is MemberAccessExpressionSyntax namedColorAccess)
        {
            var qualifierName = GetQualifierSimpleName(namedColorAccess.Expression);
            if (qualifierName is "Color" or "SystemColors" or "KnownColor")
            {
                color = new PropertyValue.ColorValue(namedColorAccess.Name.Identifier.ValueText, null, null, null, null);
                return true;
            }
        }

        color = null!;
        return false;
    }

    private static string? GetQualifierSimpleName(ExpressionSyntax expression) => expression switch
    {
        IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
        MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText,
        _ => null,
    };

    private static bool TryEvaluatePadding(ArgumentListSyntax? argumentList, out PropertyValue.PaddingValue padding)
    {
        if (argumentList is not null && argumentList.Arguments.Count == 1
            && TryGetInt(argumentList.Arguments[0].Expression, out var uniform))
        {
            padding = new PropertyValue.PaddingValue(uniform, uniform, uniform, uniform);
            return true;
        }

        if (argumentList is not null && argumentList.Arguments.Count == 4
            && TryGetInt(argumentList.Arguments[0].Expression, out var left)
            && TryGetInt(argumentList.Arguments[1].Expression, out var top)
            && TryGetInt(argumentList.Arguments[2].Expression, out var right)
            && TryGetInt(argumentList.Arguments[3].Expression, out var bottom))
        {
            padding = new PropertyValue.PaddingValue(left, top, right, bottom);
            return true;
        }

        padding = null!;
        return false;
    }

    private static bool TryEvaluateFont(ArgumentListSyntax? argumentList, out PropertyValue.FontValue font)
    {
        if (argumentList is not null
            && argumentList.Arguments.Count >= 2
            && argumentList.Arguments[0].Expression is LiteralExpressionSyntax { Token.Value: string familyName }
            && TryGetFloat(argumentList.Arguments[1].Expression, out var size))
        {
            var styleFlags = new List<string>();
            if (argumentList.Arguments.Count >= 3)
            {
                // Best-effort: FontStyle is the 3rd positional arg in the common designer
                // overload. If it's not a plain enum-member expression, just leave the
                // style flags empty rather than failing the whole Font parse.
                CollectEnumMembers(argumentList.Arguments[2].Expression, out var flags);
                styleFlags = flags;
            }

            font = new PropertyValue.FontValue(familyName, size, styleFlags);
            return true;
        }

        font = null!;
        return false;
    }

    private static bool CollectEnumMembers(ExpressionSyntax expression, out List<string> members)
    {
        members = [];
        return CollectEnumMembersInto(expression, members);
    }

    private static bool CollectEnumMembersInto(ExpressionSyntax expression, List<string> members)
    {
        expression = Unwrap(expression);

        if (expression is BinaryExpressionSyntax { OperatorToken.Text: "|" } binary)
        {
            return CollectEnumMembersInto(binary.Left, members) && CollectEnumMembersInto(binary.Right, members);
        }

        if (expression is MemberAccessExpressionSyntax member)
        {
            members.Add(member.Name.Identifier.ValueText);
            return true;
        }

        return false;
    }

    private static bool TryGetTwoIntArgs(ArgumentListSyntax? argumentList, out int a, out int b)
    {
        a = 0;
        b = 0;

        if (argumentList is null || argumentList.Arguments.Count != 2)
        {
            return false;
        }

        return TryGetInt(argumentList.Arguments[0].Expression, out a)
            && TryGetInt(argumentList.Arguments[1].Expression, out b);
    }

    private static bool TryGetInt(ExpressionSyntax expression, out int value)
    {
        if (expression is LiteralExpressionSyntax { Token.Value: int literalInt })
        {
            value = literalInt;
            return true;
        }

        if (TryGetNegatedNumericLiteral(expression, out var negated) && negated is int negatedInt)
        {
            value = negatedInt;
            return true;
        }

        value = 0;
        return false;
    }

    private static bool TryGetByte(ExpressionSyntax expression, out byte value)
    {
        if (TryGetInt(expression, out var i) && i is >= 0 and <= 255)
        {
            value = (byte)i;
            return true;
        }

        value = 0;
        return false;
    }

    private static bool TryGetFloat(ExpressionSyntax expression, out float value)
    {
        if (expression is LiteralExpressionSyntax literal)
        {
            switch (literal.Token.Value)
            {
                case float f:
                    value = f;
                    return true;
                case double d:
                    value = (float)d;
                    return true;
                case int i:
                    value = i;
                    return true;
            }
        }

        if (TryGetNegatedNumericLiteral(expression, out var negated))
        {
            switch (negated)
            {
                case float f:
                    value = f;
                    return true;
                case double d:
                    value = (float)d;
                    return true;
                case int i:
                    value = i;
                    return true;
            }
        }

        value = 0f;
        return false;
    }

    private static bool TryGetNegatedNumericLiteral(ExpressionSyntax expression, out object? value)
    {
        if (expression is PrefixUnaryExpressionSyntax { OperatorToken.Text: "-", Operand: LiteralExpressionSyntax literal })
        {
            value = literal.Token.Value switch
            {
                int i => -i,
                double d => -d,
                float f => -f,
                long l => -l,
                var other => other,
            };
            return true;
        }

        value = null;
        return false;
    }
}
