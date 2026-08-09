using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Converter.Core.Parsing;

/// <summary>
/// Finds a WinForms application's actual startup form - the argument to
/// `Application.Run(new SomeForm())`, wherever that call lives (usually a `static void Main`
/// in Program.cs) - by scanning every non-Designer .cs file under the source project. Best-
/// effort and syntax-only, mirroring EventHandlerBodyParser: a project that doesn't match this
/// exact shape (e.g. `Application.Run()` called with a pre-constructed variable instead of an
/// inline `new`, or no discoverable entry point at all) simply yields no match rather than a
/// hard failure - callers are expected to fall back to a sensible default.
/// </summary>
public static class EntryPointResolver
{
    public static string? FindStartupFormName(string sourcePath)
    {
        foreach (var csFile in Directory.EnumerateFiles(sourcePath, "*.cs", SearchOption.AllDirectories))
        {
            if (csFile.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string source;
            try
            {
                source = File.ReadAllText(csFile);
            }
            catch (IOException)
            {
                continue;
            }

            if (!source.Contains("Application.Run", StringComparison.Ordinal))
            {
                continue;
            }

            var root = CSharpSyntaxTree.ParseText(source).GetRoot();
            var formName = root.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Where(IsApplicationRunCall)
                .Select(GetConstructedFormTypeName)
                .FirstOrDefault(name => name != null);

            if (formName != null)
            {
                return formName;
            }
        }

        return null;
    }

    private static bool IsApplicationRunCall(InvocationExpressionSyntax invocation) =>
        invocation.Expression is MemberAccessExpressionSyntax { Name.Identifier.Text: "Run" } memberAccess &&
        memberAccess.Expression.ToString() is "Application" or "System.Windows.Forms.Application";

    private static string? GetConstructedFormTypeName(InvocationExpressionSyntax invocation)
    {
        var firstArg = invocation.ArgumentList.Arguments.FirstOrDefault()?.Expression;
        if (firstArg is not ObjectCreationExpressionSyntax objectCreation)
        {
            return null;
        }

        var typeName = objectCreation.Type.ToString();
        return typeName.Contains('.') ? typeName[(typeName.LastIndexOf('.') + 1)..] : typeName;
    }
}
