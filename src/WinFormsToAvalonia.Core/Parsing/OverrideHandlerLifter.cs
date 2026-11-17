using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WinFormsToAvalonia.Core.Mapping;
using WinFormsToAvalonia.Core.Model;

namespace WinFormsToAvalonia.Core.Parsing;

/// <summary>
/// Turns a Form's <c>protected override void OnLoad(EventArgs e) { base.OnLoad(e); … }</c> into the
/// <c>Load</c> handler it is equivalent to, so it goes through exactly the same planning,
/// translation and reporting as a designer-wired one.
/// </summary>
/// <remarks>
/// <para>
/// The generated View derives from an Avalonia <c>Window</c>, which has no <c>OnLoad</c> to
/// override - so the override used to land among the helpers, as a comment, and its body never ran.
/// An <c>OnX</c> override and an <c>X</c> handler differ in one respect only: where the override's
/// own code runs relative to <c>base.OnX(e)</c>, which is what raises the event to every other
/// subscriber. That is the one degree of freedom this closes:
/// </para>
/// <list type="bullet">
/// <item>the body calls <c>base.OnX(e)</c> exactly once, as its first or last statement, with the
/// override's own parameter - or not at all, when nothing else subscribes the event (then there is
/// nobody for the missing call to have notified);</item>
/// <item>when the designer also wires a handler to the same event, the call must come first: the
/// lifted handler is subscribed after the designer's and so runs after it, which is exactly what
/// "raise the event, then do my part" meant;</item>
/// <item>the body names no other <c>base.</c> member and no <c>sender</c>, which the lifted method
/// gains as a parameter.</item>
/// </list>
/// <para>
/// Only events that map onto the generated root are lifted (plus <c>Paint</c>, which gets its own
/// drawing surface from <see cref="PaintLayerInserter"/>). An override of anything else stays a
/// helper, as it always was.
/// </para>
/// </remarks>
public static class OverrideHandlerLifter
{
    /// <summary>The result: the rewritten source and, per lifted handler, the override it came from.</summary>
    public sealed record Result(string Source, IReadOnlyDictionary<string, string> LiftedFrom);

    /// <summary>
    /// Rewrites every liftable override in <paramref name="source"/>'s declaration of
    /// <paramref name="className"/> into a handler method and adds the matching Form-level binding to
    /// <paramref name="formModel"/>. Null when there was nothing to lift.
    /// </summary>
    public static Result? Lift(
        string? source, string className, FormModel formModel, IReadOnlySet<string> takenNames, EventMappingRegistry eventMappings)
    {
        if (source is null)
        {
            return null;
        }

        var root = CSharpSyntaxTree.ParseText(source).GetRoot();
        if (root.DescendantNodes().OfType<ClassDeclarationSyntax>()
                .FirstOrDefault(c => c.Identifier.ValueText == className) is not { } declaration)
        {
            return null;
        }

        // The designer's handler names too: a binding may name a method the class inherits.
        var taken = new HashSet<string>(takenNames, StringComparer.Ordinal);
        taken.UnionWith(formModel.FormEvents.Select(b => b.HandlerMethodName).OfType<string>());
        taken.UnionWith(formModel.Controls.Values.SelectMany(c => c.Events).Select(b => b.HandlerMethodName).OfType<string>());
        var replacements = new Dictionary<MethodDeclarationSyntax, MethodDeclarationSyntax>();
        var liftedFrom = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var method in declaration.Members.OfType<MethodDeclarationSyntax>())
        {
            if (!TryGetEventName(method, eventMappings, out var eventName, out var parameter)
                || !TryStripBaseCall(method, parameter, formModel.FormEvents.Any(b => b.EventName == eventName), out var body))
            {
                continue;
            }

            var name = $"{className}_{eventName}";
            for (var attempt = 1; !taken.Add(name); attempt++)
            {
                name = $"{className}Override{(attempt == 1 ? "" : attempt.ToString(System.Globalization.CultureInfo.InvariantCulture))}_{eventName}";
            }

            var asyncModifier = method.Modifiers.Any(SyntaxKind.AsyncKeyword) ? "async " : "";
            var handler = (MethodDeclarationSyntax)SyntaxFactory.ParseMemberDeclaration(
                $"private {asyncModifier}void {name}(object? sender, {parameter.Type} {parameter.Identifier.ValueText})\n{{\n}}")!;

            replacements[method] = handler
                .WithBody(body)
                .WithLeadingTrivia(method.GetLeadingTrivia())
                .WithTrailingTrivia(method.GetTrailingTrivia());

            formModel.FormEvents.Add(new EventHandlerBinding(eventName, name, null));
            liftedFrom[name] = method.Identifier.ValueText;
        }

        if (replacements.Count == 0)
        {
            return null;
        }

        return new Result(root.ReplaceNodes(replacements.Keys, (original, _) => replacements[original]).ToFullString(), liftedFrom);
    }

    /// <summary>
    /// <c>protected override void OnX(XEventArgs e)</c> with a block body, where <c>X</c> is an event
    /// the generated root can raise.
    /// </summary>
    private static bool TryGetEventName(
        MethodDeclarationSyntax method, EventMappingRegistry eventMappings, out string eventName, out ParameterSyntax parameter)
    {
        eventName = "";
        parameter = null!;

        if (!method.Modifiers.Any(SyntaxKind.OverrideKeyword)
            || method.ReturnType is not PredefinedTypeSyntax { Keyword.RawKind: (int)SyntaxKind.VoidKeyword }
            || method.TypeParameterList is not null
            || method.Body is null
            || method.ParameterList.Parameters is not [{ Type: not null, Modifiers.Count: 0, Default: null } only]
            || method.Identifier.ValueText is not { Length: > 2 } methodName
            || !methodName.StartsWith("On", StringComparison.Ordinal)
            || !char.IsUpper(methodName[2]))
        {
            return false;
        }

        eventName = methodName[2..];
        parameter = only;
        return eventName == "Paint" || eventMappings.ResolveFormEvent(eventName).AvaloniaEventName is not null;
    }

    /// <summary>The body without its <c>base.OnX(e);</c>, when that call sits where it can be dropped.</summary>
    private static bool TryStripBaseCall(MethodDeclarationSyntax method, ParameterSyntax parameter, bool hasOtherHandlers, out BlockSyntax body)
    {
        body = method.Body!;
        var parameterName = parameter.Identifier.ValueText;
        var statements = body.Statements;

        bool IsBaseCall(StatementSyntax statement) =>
            statement is ExpressionStatementSyntax
            {
                Expression: InvocationExpressionSyntax
                {
                    Expression: MemberAccessExpressionSyntax { Expression: BaseExpressionSyntax } access,
                    ArgumentList.Arguments: [{ RefKindKeyword.RawKind: 0, Expression: IdentifierNameSyntax argument }],
                },
            }
            && access.Name.Identifier.ValueText == method.Identifier.ValueText
            && argument.Identifier.ValueText == parameterName;

        var baseCalls = statements.Where(IsBaseCall).ToList();
        var otherBaseUses = body.DescendantNodes().OfType<BaseExpressionSyntax>().Count() - baseCalls.Count;
        // Any token, not just a name use: a local or lambda parameter called `sender` would collide too.
        var namesSender = body.DescendantTokens().Any(t => t.IsKind(SyntaxKind.IdentifierToken) && t.ValueText == "sender");

        if (otherBaseUses != 0 || namesSender || baseCalls.Count > 1)
        {
            return false;
        }

        if (baseCalls is [var call])
        {
            var isFirst = statements[0] == call;
            if (!isFirst && (hasOtherHandlers || statements[^1] != call))
            {
                return false;
            }

            body = body.RemoveNode(call, SyntaxRemoveOptions.KeepExteriorTrivia)!;
            return true;
        }

        // No base call at all: the override swallowed the event. Equivalent only when no one else
        // was listening for it.
        return !hasOtherHandlers;
    }
}
