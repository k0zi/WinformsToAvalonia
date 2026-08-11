using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Converter.Core.Parsing;

public record MessageBoxTranspileResult(string TransformedBody, bool AddedAwait);

/// <summary>
/// Rewrites `System.Windows.Forms.MessageBox.Show(owner, text, caption, buttons, icon)` calls
/// (and standalone `DialogResult`/`MessageBoxButtons`/`MessageBoxIcon` references elsewhere in
/// the same body, e.g. `if (confirm != DialogResult.Yes)`) into calls against the small
/// Avalonia-compatible dialog helper `ProjectFileGenerator` generates into every output project
/// (`{namespace}.Common.Dialogs`/`MessageBoxButtons`/`MessageBoxIcon`/`DialogResult`) - the same
/// "recognize a WinForms API shape, transpile it" approach `GdiDrawingTranspiler` already uses
/// for GDI+ drawing calls. Only the 5-argument `MessageBox.Show(owner, text, caption, buttons,
/// icon)` overload is recognized (confirmed to be the only shape real-world code uses); any
/// other overload is left untouched, falling through to the existing "Migrated Logic May Not
/// Compile" flagging.
///
/// The owner argument is dropped, not rewritten: this runs on ViewModel-bound bodies too (the
/// common case - WinForms's synchronous "this" doesn't mean anything from a ViewModel, which
/// must not reach into the View), so `Dialogs.ShowAsync` resolves an appropriate parent window
/// internally instead of requiring the caller to supply one. Avalonia has no synchronous modal
/// API, so the rewrite target is `await Dialogs.ShowAsync(...)` - `AddedAwait` tells the caller
/// to force the enclosing method `async` if it isn't already (safe in this codebase:
/// ViewModelGenerator/CodeBehindGenerator already support async RelayCommand/handler methods).
/// </summary>
public static class MessageBoxTranspiler
{
    private static readonly HashSet<string> RewrittenEnumNames = new(StringComparer.Ordinal)
    {
        "MessageBoxButtons", "MessageBoxIcon", "DialogResult"
    };

    public static MessageBoxTranspileResult Transpile(string body, string namespaceName)
    {
        try
        {
            var wrapper = $"class __Wrapper {{ void __M() {body} }}";
            var root = CSharpSyntaxTree.ParseText(wrapper).GetRoot();

            var rewriter = new Rewriter(namespaceName);
            var rewrittenRoot = rewriter.Visit(root);

            var method = rewrittenRoot.DescendantNodes().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            if (method?.Body == null)
            {
                return new MessageBoxTranspileResult(body, false);
            }

            return new MessageBoxTranspileResult(method.Body.ToString(), rewriter.AddedAwait);
        }
        catch
        {
            // Best-effort: an unparseable/unexpected shape leaves the body untouched, not a
            // failed conversion - the existing WinFormsTypeUsageDetector-based manual step still
            // catches whatever this didn't handle.
            return new MessageBoxTranspileResult(body, false);
        }
    }

    /// <summary>
    /// Same rewrite as <see cref="Transpile"/>, for a *full* method-source string (signature +
    /// body, e.g. a migrated helper method from CodeBehindMemberExtractor) rather than just a
    /// body block - the wrapping strategy differs (a full method declaration is itself a valid
    /// class member; a bare "{ ... }" block is not), so this can't share Transpile's "void
    /// __M() {body}" wrapper. Returns the full rewritten method source (signature included,
    /// unchanged except for whatever EnsureAsyncModifier-style caller adds separately).
    /// </summary>
    public static MessageBoxTranspileResult TranspileMethod(string fullMethodSource, string namespaceName)
    {
        try
        {
            var wrapper = $"class __Wrapper {{ {fullMethodSource} }}";
            var root = CSharpSyntaxTree.ParseText(wrapper).GetRoot();

            // Unlike Transpile's freshly-constructed "void"/"async void" signature (always safe
            // to mark async), this method's return type is migrated verbatim and could be
            // anything (e.g. "bool ValidateInput()") - "async" is only legal on void/Task/
            // Task<T> (or an already-async method, any return type). Skip the rewrite entirely
            // rather than emit "async bool" (found via a real build against WarehouseApp).
            var originalMethod = root.DescendantNodes().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            if (originalMethod == null || !CanSafelyBecomeAsync(originalMethod))
            {
                return new MessageBoxTranspileResult(fullMethodSource, false);
            }

            var rewriter = new Rewriter(namespaceName);
            var rewrittenRoot = rewriter.Visit(root);

            var method = rewrittenRoot.DescendantNodes().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            if (method == null)
            {
                return new MessageBoxTranspileResult(fullMethodSource, false);
            }

            return new MessageBoxTranspileResult(method.ToFullString().Trim(), rewriter.AddedAwait);
        }
        catch
        {
            return new MessageBoxTranspileResult(fullMethodSource, false);
        }
    }

    private static bool CanSafelyBecomeAsync(MethodDeclarationSyntax method)
    {
        if (method.Modifiers.Any(m => m.IsKind(SyntaxKind.AsyncKeyword)))
        {
            return true;
        }

        var returnTypeName = method.ReturnType.ToString();
        return returnTypeName == "void" || returnTypeName == "Task" ||
            returnTypeName.StartsWith("Task<", StringComparison.Ordinal);
    }

    private sealed class Rewriter(string namespaceName) : CSharpSyntaxRewriter
    {
        private readonly string _commonNamespace = $"{namespaceName}.Common";

        public bool AddedAwait { get; private set; }

        public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (node.Expression is MemberAccessExpressionSyntax { Name.Identifier.Text: "Show" } memberAccess &&
                GetSimpleTargetName(memberAccess.Expression) == "MessageBox" &&
                node.ArgumentList.Arguments.Count == 5)
            {
                // (owner, text, caption, buttons, icon) - owner (args[0]) is intentionally
                // dropped; the rest are re-visited (so their own MessageBoxButtons/
                // MessageBoxIcon references get the same namespace-qualifying rewrite below).
                var originalArgs = node.ArgumentList.Arguments;
                var keptArgs = SyntaxFactory.SeparatedList(new[]
                {
                    (ArgumentSyntax)Visit(originalArgs[1])!,
                    (ArgumentSyntax)Visit(originalArgs[2])!,
                    (ArgumentSyntax)Visit(originalArgs[3])!,
                    (ArgumentSyntax)Visit(originalArgs[4])!,
                });

                var newInvocation = SyntaxFactory.InvocationExpression(
                    SyntaxFactory.ParseExpression($"{_commonNamespace}.Dialogs.ShowAsync"),
                    SyntaxFactory.ArgumentList(keptArgs));

                AddedAwait = true;
                var awaitKeyword = SyntaxFactory.Token(SyntaxKind.AwaitKeyword).WithTrailingTrivia(SyntaxFactory.Space);
                return SyntaxFactory.AwaitExpression(awaitKeyword, newInvocation).WithTriviaFrom(node);
            }

            return base.VisitInvocationExpression(node);
        }

        public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            // Standalone references outside a MessageBox.Show(...) call - e.g.
            // "if (confirm != DialogResult.Yes)" - need the same qualifying rewrite so they
            // resolve against the generated enum instead of the (never copied) WinForms one.
            if (GetSimpleTargetName(node.Expression) is { } targetName && RewrittenEnumNames.Contains(targetName))
            {
                return node.WithExpression(SyntaxFactory.ParseExpression($"{_commonNamespace}.{targetName}"))
                    .WithTriviaFrom(node);
            }

            return base.VisitMemberAccessExpression(node);
        }

        private static string? GetSimpleTargetName(ExpressionSyntax expression) => expression switch
        {
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            _ => null
        };
    }
}
