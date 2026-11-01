using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WinFormsToAvalonia.Core.Model;

namespace WinFormsToAvalonia.Core.Parsing;

/// <summary>
/// The answer to "which Form does this application start with", read from the
/// <c>Application.Run(...)</c> call that says so - and why, when it cannot be read.
/// </summary>
/// <param name="Form">
/// The startup Form, or null when no <c>Application.Run</c> named a converted one. The caller
/// then falls back to the first Form, as the conversion always used to.
/// </param>
/// <param name="Warning">
/// Set whenever the choice is a guess: an <c>Application.Run</c> that was found but could not be
/// resolved, two that disagree, or none at all in a project with more than one Form. Null when
/// the answer is certain, or when there is only one Form to choose from.
/// </param>
public sealed record StartupFormResolution(DesignerFilePairing? Form, string? Warning);

/// <summary>
/// Finds the application's startup Form the way WinForms itself does: by the argument of
/// <c>Application.Run</c>. Without this the main window was simply the first Form in discovery
/// order - which for an app with an <c>AboutForm</c> beside its <c>MainForm</c>, or a base form
/// beside the derived one, is the wrong window, built and launched without a word.
/// </summary>
/// <remarks>
/// Syntax only, like the rest of the parser. The two shapes a real <c>Program.cs</c> uses are
/// read - <c>Application.Run(new MainForm())</c> and a local holding the form
/// (<c>var form = new MainForm(); Application.Run(form);</c>). Anything else, an
/// <c>ApplicationContext</c> above all, is reported rather than guessed at: the context decides
/// at run time which form it shows, which no reading of the call site can recover.
/// </remarks>
public sealed class StartupFormLocator
{
    public StartupFormResolution Locate(WinFormsProjectModel project, IReadOnlyList<DesignerFilePairing> forms) =>
        Locate(
            project.CompileFiles
                .Order(StringComparer.Ordinal)
                .Where(File.Exists)
                .Select(path => (path, File.ReadAllText(path))),
            forms);

    /// <param name="sources">Every compiled source file, as (path, text), in a stable order.</param>
    /// <param name="forms">The Forms this run converts - the only valid answers.</param>
    public StartupFormResolution Locate(
        IEnumerable<(string Path, string Text)> sources,
        IReadOnlyList<DesignerFilePairing> forms)
    {
        if (forms.Count == 0)
        {
            return new StartupFormResolution(null, null);
        }

        var calls = new List<RunCall>();
        foreach (var (path, text) in sources)
        {
            // Cheap pre-filter: most files in a project never mention it, and parsing them all a
            // second time (the locator already did once) is the only real cost here.
            if (!text.Contains("Application", StringComparison.Ordinal) || !text.Contains("Run", StringComparison.Ordinal))
            {
                continue;
            }

            var root = CSharpSyntaxTree.ParseText(text, path: path).GetRoot();
            foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                if (IsApplicationRun(invocation))
                {
                    calls.Add(new RunCall(Path.GetFileName(path), invocation, Resolve(invocation, forms)));
                }
            }
        }

        var fallbackName = forms[0].ClassName;

        if (calls.Count == 0)
        {
            return new StartupFormResolution(
                null,
                forms.Count > 1
                    ? $"Startup form: no 'Application.Run(...)' call was found, so '{fallbackName}' - the first Form "
                        + "discovered - was made the main window. If the application starts with a different Form, "
                        + "change the window App.axaml.cs creates."
                    : null);
        }

        var resolved = calls.Where(c => c.Form is not null).ToList();
        var distinct = resolved.Select(c => c.Form!).Distinct().ToList();

        if (distinct.Count == 0)
        {
            var reasons = string.Join("; ", calls.Select(c => $"{c.Describe()} - {c.Reason}"));
            return new StartupFormResolution(
                null,
                $"Startup form: {reasons}. '{fallbackName}' - the first Form discovered - was made the main window "
                + "instead; change the window App.axaml.cs creates if that is wrong.");
        }

        var chosen = resolved[0];
        if (distinct.Count > 1)
        {
            var all = string.Join(", ", resolved.Select(c => $"'{c.Form!.ClassName}' ({c.FileName})"));
            return new StartupFormResolution(
                chosen.Form,
                $"Startup form: several 'Application.Run(...)' calls name different Forms ({all}). The first, "
                + $"'{chosen.Form!.ClassName}', was made the main window; the choice between them is made at run time "
                + "in the original, so check App.axaml.cs.");
        }

        return new StartupFormResolution(chosen.Form, null);
    }

    private static bool IsApplicationRun(InvocationExpressionSyntax invocation) =>
        invocation.Expression is MemberAccessExpressionSyntax
        {
            Name.Identifier.ValueText: "Run",
            Expression: var receiver,
        }
        && RightmostName(receiver) == "Application";

    private static (DesignerFilePairing? Form, string? Reason) Resolve(
        InvocationExpressionSyntax invocation,
        IReadOnlyList<DesignerFilePairing> forms)
    {
        if (invocation.ArgumentList.Arguments.Count != 1)
        {
            return (null, "it is not given a Form");
        }

        var argument = invocation.ArgumentList.Arguments[0].Expression;
        var typeName = argument switch
        {
            ObjectCreationExpressionSyntax creation => creation.Type,
            IdentifierNameSyntax local => LocalFormType(invocation, local.Identifier.ValueText),
            _ => null,
        };

        if (typeName is null)
        {
            return (null, "its argument does not construct a Form this conversion could identify");
        }

        if (RightmostName(typeName) == "ApplicationContext")
        {
            return (null, "it runs an ApplicationContext, which picks its Form at run time");
        }

        return ResolveTypeName(typeName, invocation, forms);
    }

    /// <summary>
    /// The type a local passed to <c>Application.Run</c> holds, from what the same method
    /// <c>new</c>s into it. That beats the declared type, which is usually just <c>Form</c>. Every
    /// value it is given must be a <c>new</c> of one and the same type, or there is no single
    /// answer; only a local that is never constructed here falls back to its declared type.
    /// </summary>
    private static TypeSyntax? LocalFormType(SyntaxNode callSite, string localName)
    {
        var body = callSite.Ancestors().FirstOrDefault(a =>
            a is BaseMethodDeclarationSyntax or LocalFunctionStatementSyntax or CompilationUnitSyntax);
        if (body is null)
        {
            return null;
        }

        var candidates = new List<TypeSyntax>();
        TypeSyntax? declaredType = null;
        foreach (var declarator in body.DescendantNodes().OfType<VariableDeclaratorSyntax>())
        {
            if (declarator.Identifier.ValueText != localName
                || declarator.Parent is not VariableDeclarationSyntax declaration)
            {
                continue;
            }

            if (!declaration.Type.IsVar)
            {
                declaredType = declaration.Type;
            }

            switch (declarator.Initializer?.Value)
            {
                case null:
                    break;
                case ObjectCreationExpressionSyntax creation:
                    candidates.Add(creation.Type);
                    break;
                default:
                    return null;
            }
        }

        foreach (var assignment in body.DescendantNodes().OfType<AssignmentExpressionSyntax>())
        {
            if (assignment.Left is IdentifierNameSyntax { Identifier.ValueText: var name } && name == localName)
            {
                if (assignment.Right is not ObjectCreationExpressionSyntax creation)
                {
                    return null;
                }

                candidates.Add(creation.Type);
            }
        }

        if (candidates.Count == 0)
        {
            return declaredType;
        }

        var names = candidates.Select(t => t.ToString()).Distinct(StringComparer.Ordinal).ToList();
        return names.Count == 1 ? candidates[0] : null;
    }

    private static (DesignerFilePairing? Form, string? Reason) ResolveTypeName(
        TypeSyntax typeName,
        SyntaxNode callSite,
        IReadOnlyList<DesignerFilePairing> forms)
    {
        var written = typeName.ToString().Replace("global::", "", StringComparison.Ordinal);
        var simpleName = RightmostName(typeName);

        var candidates = forms.Where(f => f.ClassName == simpleName).ToList();
        if (written.Contains('.', StringComparison.Ordinal))
        {
            candidates = candidates
                .Where(f => f.FullyQualifiedName == written || f.FullyQualifiedName.EndsWith("." + written, StringComparison.Ordinal))
                .ToList();
        }

        if (candidates.Count > 1)
        {
            // Two Forms of one simple name: the one in the calling file's own namespace is what
            // an unqualified name binds to first in C#.
            var callerNamespace = callSite.Ancestors().OfType<BaseNamespaceDeclarationSyntax>()
                .Select(n => n.Name.ToString())
                .FirstOrDefault();
            var sameNamespace = candidates.Where(f => f.Namespace == callerNamespace).ToList();
            if (sameNamespace.Count == 1)
            {
                return (sameNamespace[0], null);
            }

            return (null, $"'{written}' matches more than one Form");
        }

        return candidates.Count == 1
            ? (candidates[0], null)
            : (null, $"'{written}' is not a Form this conversion converts");
    }

    private static string? RightmostName(SyntaxNode node) => node switch
    {
        IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
        GenericNameSyntax generic => generic.Identifier.ValueText,
        QualifiedNameSyntax qualified => RightmostName(qualified.Right),
        AliasQualifiedNameSyntax alias => RightmostName(alias.Name),
        MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText,
        _ => null,
    };

    private sealed record RunCall(
        string FileName,
        InvocationExpressionSyntax Invocation,
        (DesignerFilePairing? Form, string? Reason) Resolution)
    {
        public DesignerFilePairing? Form => Resolution.Form;

        public string? Reason => Resolution.Reason;

        public string Describe() => $"'{Invocation}' in {FileName}";
    }
}
