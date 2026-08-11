using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Converter.Core.Parsing;

/// <summary>
/// Detects references to WinForms/System.Drawing types that have no Avalonia equivalent at
/// all (as opposed to GDI+ drawing types like Graphics/Bitmap/Font/Pen/Brush, which
/// GdiDrawingTranspiler can translate) - shared between SupportFileScanner (deciding whether a
/// whole file is safe to copy) and the migrated-override manual-step check in
/// ConversionOrchestrator (deciding whether a migrated business-logic method needs a "verify
/// this compiles" flag). A single source of truth for "this references something with no
/// automatic path", so both call sites report the same set of names for the same reasons.
///
/// Syntax-only, no semantic model (consistent with the rest of this codebase): matches simple
/// identifier names anywhere in the tree, not just in type-reference positions, so a local
/// variable or method happening to share a name with a blocklisted type (e.g. a domain method
/// literally called "Form") would false-positive. Accepted trade-off - the failure direction is
/// "flag something that was actually fine" (safe, just overly cautious), never "silently miss
/// something that breaks the build" (the actual bug this exists to prevent).
/// </summary>
public static class WinFormsTypeUsageDetector
{
    private static readonly HashSet<string> NoEquivalentTypeNames = new(StringComparer.Ordinal)
    {
        // Window/dialog construction - Avalonia's Window model is fundamentally different
        // (AXAML-defined classes, async ShowDialog<TResult>), not a type-for-type swap.
        "Form", "Label", "TextBox", "Button", "CheckBox", "RadioButton", "ComboBox", "ListBox",
        "Panel", "GroupBox", "DialogResult", "MessageBox", "MessageBoxButtons", "MessageBoxIcon",

        // TreeView - Avalonia's TreeView is hierarchical-data-bound, not populated by
        // imperatively constructing node objects.
        "TreeNode", "TreeView", "TreeViewEventArgs", "TreeNodeMouseClickEventArgs",

        // DataGridView - Avalonia's DataGrid is bound to a data source, not populated via
        // row/column/cell objects constructed in code.
        "DataGridView", "DataGridViewRow", "DataGridViewColumn", "DataGridViewCell",
        "DataGridViewCellFormattingEventArgs", "DataGridViewCellEventArgs",

        // ListView
        "ListView", "ListViewItem", "ListViewItemEventArgs",

        // Misc WinForms-only event args / components with no Avalonia counterpart.
        "PaintEventArgs", "FormClosingEventArgs", "FormClosedEventArgs",
        "LinkLabelLinkClickedEventArgs", "PrintPageEventArgs", "IWin32Window",
        "ErrorProvider", "BindingSource", "ToolStripMenuItem", "ContextMenuStrip", "NotifyIcon",
    };

    /// <summary>
    /// Returns the distinct blocklisted type names found anywhere in <paramref name="root"/>,
    /// in first-encountered order. Empty when none are referenced.
    /// </summary>
    public static IReadOnlyList<string> FindReferencedTypeNames(SyntaxNode root)
    {
        var found = new List<string>();

        foreach (var name in root.DescendantNodes().OfType<SimpleNameSyntax>())
        {
            var identifier = name.Identifier.Text;
            if (NoEquivalentTypeNames.Contains(identifier) && !found.Contains(identifier))
            {
                found.Add(identifier);
            }
        }

        return found;
    }
}
