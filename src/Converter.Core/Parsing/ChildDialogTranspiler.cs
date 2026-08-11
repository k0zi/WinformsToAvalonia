using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Converter.Core.Parsing;

public record ChildDialogTranspileResult(string TransformedBody, bool AddedAwait);

/// <summary>
/// Rewrites the standard WinForms "show a child form modally, check DialogResult" idiom:
/// <code>
/// using var form = new SalesOrderDetailForm();
/// if (form.ShowDialog(this) == DialogResult.OK) { ... }
/// </code>
/// into the Avalonia-idiomatic equivalent, reusing the generated
/// {namespace}.Common.Dialogs.ShowChildAsync&lt;TView, TViewModel&gt;() helper
/// (ProjectFileGenerator.GenerateDialogsHelper):
/// <code>
/// var formResult = await SampleApp.Common.Dialogs.ShowChildAsync&lt;SampleApp.Views.SalesOrderDetailForm, SampleApp.ViewModels.SalesOrderDetailFormViewModel&gt;();
/// if (formResult == SampleApp.Common.DialogResult.OK) { ... }
/// </code>
///
/// Unlike MessageBoxTranspiler (a single-expression rewrite), this recognizes a *pair* of
/// consecutive statements - a local declaration immediately followed by the matching if - so
/// it operates at CSharpSyntaxRewriter.VisitBlock granularity instead of VisitInvocationExpression.
///
/// Deliberately narrow, for two independent safety reasons:
/// 1. Only the parameterless constructor case (`new {FormType}()`) is rewritten - threading a
///    constructor argument (e.g. an entity for an "edit" flow) into a generically-generated
///    ViewModel without knowing its load contract would be an unsafe guess; that shape is left
///    untouched (today's manual step, unchanged).
/// 2. Only fires when `(FormType, DialogResultValue)` is present in
///    <paramref name="formsWithDialogResultButton"/> - i.e. only when the target form is
///    *known* (from its own Designer.cs) to have a button that can actually close it with that
///    result. WinForms code whose target form sets DialogResult imperatively in hand-written
///    code (not via a Designer-declared button property) has no such entry and is correctly
///    left alone - rewriting the caller without a way for the callee to ever produce a result
///    would trade an honest compile error for a dialog that silently never closes.
/// </summary>
public static class ChildDialogTranspiler
{
    public static ChildDialogTranspileResult Transpile(
        string body, string namespaceName,
        IReadOnlySet<(string FormName, string DialogResultValue)> formsWithDialogResultButton)
    {
        try
        {
            var wrapper = $"class __Wrapper {{ void __M() {body} }}";
            var root = CSharpSyntaxTree.ParseText(wrapper).GetRoot();

            var rewriter = new Rewriter(namespaceName, formsWithDialogResultButton);
            var rewrittenRoot = rewriter.Visit(root);

            var method = rewrittenRoot.DescendantNodes().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            if (method?.Body == null)
            {
                return new ChildDialogTranspileResult(body, false);
            }

            return new ChildDialogTranspileResult(method.Body.ToString(), rewriter.AddedAwait);
        }
        catch
        {
            return new ChildDialogTranspileResult(body, false);
        }
    }

    /// <summary>
    /// Same rewrite, for a full method-source string (signature + body) rather than a bare
    /// body block - mirrors MessageBoxTranspiler.TranspileMethod's own reasoning: a helper
    /// method's original return type is migrated verbatim and might not legally support
    /// "async" (e.g. "bool ValidateInput()"), so the same CanSafelyBecomeAsync gate applies
    /// before any rewrite is attempted.
    /// </summary>
    public static ChildDialogTranspileResult TranspileMethod(
        string fullMethodSource, string namespaceName,
        IReadOnlySet<(string FormName, string DialogResultValue)> formsWithDialogResultButton)
    {
        try
        {
            var wrapper = $"class __Wrapper {{ {fullMethodSource} }}";
            var root = CSharpSyntaxTree.ParseText(wrapper).GetRoot();

            var originalMethod = root.DescendantNodes().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            if (originalMethod == null || !CanSafelyBecomeAsync(originalMethod))
            {
                return new ChildDialogTranspileResult(fullMethodSource, false);
            }

            var rewriter = new Rewriter(namespaceName, formsWithDialogResultButton);
            var rewrittenRoot = rewriter.Visit(root);

            var method = rewrittenRoot.DescendantNodes().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            if (method == null)
            {
                return new ChildDialogTranspileResult(fullMethodSource, false);
            }

            return new ChildDialogTranspileResult(method.ToFullString().Trim(), rewriter.AddedAwait);
        }
        catch
        {
            return new ChildDialogTranspileResult(fullMethodSource, false);
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

    private sealed class Rewriter(
        string namespaceName, IReadOnlySet<(string FormName, string DialogResultValue)> formsWithDialogResultButton)
        : CSharpSyntaxRewriter
    {
        private readonly string _commonNamespace = $"{namespaceName}.Common";

        public bool AddedAwait { get; private set; }

        public override SyntaxNode? VisitBlock(BlockSyntax node)
        {
            var statements = node.Statements;
            var newStatements = new List<StatementSyntax>();

            for (var i = 0; i < statements.Count; i++)
            {
                if (i + 1 < statements.Count &&
                    TryMatch(statements[i], statements[i + 1], out var replacement))
                {
                    newStatements.AddRange(replacement);
                    AddedAwait = true;
                    i++; // The pair was consumed together.
                    continue;
                }

                newStatements.Add((StatementSyntax)Visit(statements[i])!);
            }

            return node.WithStatements(SyntaxFactory.List(newStatements));
        }

        private bool TryMatch(StatementSyntax first, StatementSyntax second, out StatementSyntax[] replacement)
        {
            replacement = [];

            if (first is not LocalDeclarationStatementSyntax { Declaration.Variables.Count: 1 } localDecl)
            {
                return false;
            }

            var variable = localDecl.Declaration.Variables[0];
            if (variable.Initializer?.Value is not ObjectCreationExpressionSyntax objectCreation)
            {
                return false;
            }

            // Parameterless constructor only - see the class-level doc comment for why.
            if (objectCreation.ArgumentList == null || objectCreation.ArgumentList.Arguments.Count != 0)
            {
                return false;
            }

            var formType = GetSimpleTypeName(objectCreation.Type);
            if (formType == null)
            {
                return false;
            }

            if (second is not IfStatementSyntax ifStatement)
            {
                return false;
            }

            if (ifStatement.Condition is not BinaryExpressionSyntax { RawKind: (int)SyntaxKind.EqualsExpression } binary)
            {
                return false;
            }

            if (binary.Left is not InvocationExpressionSyntax
                {
                    Expression: MemberAccessExpressionSyntax { Name.Identifier.Text: "ShowDialog" } showDialogAccess
                })
            {
                return false;
            }

            if (showDialogAccess.Expression is not IdentifierNameSyntax localRef ||
                localRef.Identifier.Text != variable.Identifier.Text)
            {
                return false;
            }

            if (binary.Right is not MemberAccessExpressionSyntax { Name.Identifier.Text: var dialogResultValue } resultAccess ||
                GetSimpleTargetName(resultAccess.Expression) != "DialogResult")
            {
                return false;
            }

            // Only when we *know* the target form can actually close with this result - see
            // the class-level doc comment.
            if (!formsWithDialogResultButton.Contains((formType, dialogResultValue)))
            {
                return false;
            }

            var resultVarName = $"{variable.Identifier.Text}Result";
            var qualifiedView = $"{namespaceName}.Views.{formType}";
            var qualifiedViewModel = $"{namespaceName}.ViewModels.{formType}ViewModel";

            var declaration = SyntaxFactory.ParseStatement(
                $"var {resultVarName} = await {_commonNamespace}.Dialogs.ShowChildAsync<{qualifiedView}, {qualifiedViewModel}>();\n");

            var newCondition = SyntaxFactory.ParseExpression($"{resultVarName} == {_commonNamespace}.DialogResult.{dialogResultValue}");
            var newIf = ifStatement.WithCondition(newCondition);

            replacement = [declaration.WithTriviaFrom(first), newIf.WithTriviaFrom(second)];
            return true;
        }

        private static string? GetSimpleTypeName(TypeSyntax type) => type switch
        {
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            QualifiedNameSyntax qualified => qualified.Right.Identifier.Text,
            _ => null
        };

        private static string? GetSimpleTargetName(ExpressionSyntax expression) => expression switch
        {
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            _ => null
        };
    }
}
