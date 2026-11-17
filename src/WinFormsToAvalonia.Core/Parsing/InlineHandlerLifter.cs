using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WinFormsToAvalonia.Core.Model;

namespace WinFormsToAvalonia.Core.Parsing;

/// <summary>
/// Turns the lambdas and anonymous methods a designer file subscribes
/// (<c>this.box.TextChanged += (s, e) => { … };</c>) into named handler methods, so they go
/// through exactly the same analysis, translation and reporting as a hand-written one.
/// </summary>
/// <remarks>
/// <para>
/// They used to be reported and dropped: "uses an inline lambda handler, which is not migrated
/// automatically" - the body was gone from the generated project entirely, not even kept as a
/// comment. The fix is not a second rewriter for lambdas but making them stop being special: once
/// a lambda is a method on the class, every existing rule applies to it unchanged.
/// </para>
/// <para>
/// A lambda in <c>InitializeComponent</c> could in principle capture one of its locals; designer
/// code has none worth capturing (only <c>resources</c>), and a body that names one simply fails to
/// translate and stays a comment, like any other unknown name.
/// </para>
/// </remarks>
public static class InlineHandlerLifter
{
    /// <summary>
    /// Names every inline handler in <paramref name="formModel"/> - rewriting its binding to point at
    /// that name - and returns the method declarations to add to the class.
    /// </summary>
    /// <param name="takenNames">Members the class already declares; a generated name avoids them.</param>
    public static IReadOnlyList<string> Lift(FormModel formModel, IReadOnlySet<string> takenNames)
    {
        var taken = new HashSet<string>(takenNames, StringComparer.Ordinal);
        var methods = new List<string>();

        string? LiftOne(string ownerName, EventHandlerBinding binding)
        {
            if (binding.HandlerMethodName is not null
                || binding.InlineHandlerBody is not { } source
                || SyntaxFactory.ParseExpression(source) is not AnonymousFunctionExpressionSyntax function)
            {
                return null;
            }

            // The designer's own convention first. On a collision the marker goes on the owner half,
            // not after the event: command names are derived by dropping the `_Event` suffix, so
            // `goButtonInline_Click` becomes `GoButtonInline` where `goButton_Click2` would have
            // become `GoButton_Click2`.
            var name = $"{ownerName}_{binding.EventName}";
            for (var attempt = 1; !taken.Add(name); attempt++)
            {
                name = $"{ownerName}Inline{(attempt == 1 ? "" : attempt.ToString(System.Globalization.CultureInfo.InvariantCulture))}_{binding.EventName}";
            }

            methods.Add(ToMethod(function, name));
            return name;
        }

        for (var i = 0; i < formModel.FormEvents.Count; i++)
        {
            if (LiftOne(formModel.ClassName, formModel.FormEvents[i]) is { } name)
            {
                formModel.FormEvents[i] = formModel.FormEvents[i] with { HandlerMethodName = name };
            }
        }

        foreach (var control in formModel.Controls.Values.OrderBy(c => c.FieldName, StringComparer.Ordinal))
        {
            for (var i = 0; i < control.Events.Count; i++)
            {
                if (LiftOne(control.FieldName, control.Events[i]) is { } name)
                {
                    control.Events[i] = control.Events[i] with { HandlerMethodName = name };
                }
            }
        }

        return methods;
    }

    /// <summary>
    /// <paramref name="source"/> with <paramref name="methods"/> added to its declaration of
    /// <paramref name="className"/> - or, when it has none (a Form with no hand-written half), a
    /// partial class holding just them.
    /// </summary>
    public static string InjectInto(string? source, string className, string? @namespace, IReadOnlyList<string> methods)
    {
        var members = methods
            .Select(m => SyntaxFactory.ParseMemberDeclaration(m))
            .OfType<MemberDeclarationSyntax>()
            .ToArray();

        if (source is not null)
        {
            var root = CSharpSyntaxTree.ParseText(source).GetRoot();
            if (root.DescendantNodes().OfType<ClassDeclarationSyntax>()
                    .FirstOrDefault(c => c.Identifier.ValueText == className) is { } declaration)
            {
                return root.ReplaceNode(declaration, declaration.AddMembers(members)).ToFullString();
            }
        }

        var body = string.Join("\n", members.Select(m => m.NormalizeWhitespace().ToFullString()));
        var partialClass = $"partial class {className}\n{{\n{body}\n}}";
        return @namespace is null ? partialClass : $"namespace {@namespace}\n{{\n{partialClass}\n}}";
    }

    /// <summary>The handler method an anonymous function stands for.</summary>
    private static string ToMethod(AnonymousFunctionExpressionSyntax function, string name)
    {
        var parameters = function switch
        {
            ParenthesizedLambdaExpressionSyntax lambda => lambda.ParameterList.Parameters.ToList(),
            SimpleLambdaExpressionSyntax simple => [simple.Parameter],
            AnonymousMethodExpressionSyntax { ParameterList: { } list } => list.Parameters.ToList(),
            _ => [],
        };

        // `(_, _) =>` and `delegate { }` name nothing, and two `_` parameters are not legal on a
        // method - so a discard, a missing name or a missing parameter becomes the conventional one.
        string[] defaults = ["sender", "e"];
        string[] defaultTypes = ["object?", "EventArgs"];
        var signature = string.Join(", ", Enumerable.Range(0, 2).Select(i =>
        {
            var parameter = i < parameters.Count ? parameters[i] : null;
            var type = parameter?.Type?.ToString() ?? defaultTypes[i];
            var parameterName = parameter?.Identifier.ValueText is { Length: > 0 } written && written != "_" ? written : defaults[i];
            return $"{type} {parameterName}";
        }));

        var body = function.Block is { } block
            ? block.ToFullString()
            : $"{{\n    {function.ExpressionBody}; \n}}";

        var modifiers = function.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword) ? "private async void" : "private void";
        return $"{modifiers} {name}({signature})\n{body}";
    }
}
