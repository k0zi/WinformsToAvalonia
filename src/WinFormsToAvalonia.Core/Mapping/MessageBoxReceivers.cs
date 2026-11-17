using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WinFormsToAvalonia.Core.Mapping;

/// <summary>
/// The classes whose static <c>Show</c> is WinForms' <c>MessageBox.Show</c>: WinForms' own, and the
/// two vendor message boxes, whose <c>Show(text)</c>, <c>Show(text, caption)</c> and
/// <c>Show(text, caption, MessageBoxButtons)</c> overloads mean the same and return the same
/// <c>DialogResult</c>.
/// </summary>
/// <remarks>
/// One predicate for every place that recognises a message box, so the analysis that keeps such a
/// handler off the ViewModel and the rewriter that translates it cannot disagree. The vendor
/// overloads that do not match WinForms' take a first argument of a vendor or WinForms type
/// (<c>UserLookAndFeel</c>, <c>XtraMessageBoxArgs</c>, an owner), which no translation accepts - so
/// they refuse on their own rather than being misread.
/// </remarks>
public static class MessageBoxReceivers
{
    private static readonly HashSet<string> SimpleNames = new(StringComparer.Ordinal)
    {
        "MessageBox", "XtraMessageBox", "RadMessageBox",
    };

    /// <summary>Written with a namespace - the vendors' only, so a WinForms project converts exactly as before.</summary>
    private static readonly HashSet<string> QualifiedNames = new(StringComparer.Ordinal)
    {
        "DevExpress.XtraEditors.XtraMessageBox", "Telerik.WinControls.RadMessageBox",
    };

    public static bool IsMessageBox(ExpressionSyntax receiver) => receiver switch
    {
        IdentifierNameSyntax { Identifier.ValueText: var name } => SimpleNames.Contains(name),
        MemberAccessExpressionSyntax access => QualifiedNames.Contains(access.ToString().Replace("global::", "", StringComparison.Ordinal)),
        _ => false,
    };
}
