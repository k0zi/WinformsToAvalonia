using Converter.Plugin.Abstractions;

namespace Converter.Generator.Mapping;

/// <summary>
/// WinForms' fully declarative "OK/Cancel dialog" idiom: a button whose DialogResult property
/// is set in the Designer (e.g. "this.okButton.DialogResult =
/// System.Windows.Forms.DialogResult.OK;") auto-closes the containing form with that result
/// when clicked - no click handler needed at all. WinFormsParser already captures this as an
/// ordinary ControlNode.Properties["DialogResult"] entry (no parser changes needed); this just
/// extracts the enum member name (the raw captured value is the fully-qualified expression
/// text as written, e.g. "System.Windows.Forms.DialogResult.OK"). Shared between
/// AxamlGenerator (emits the Click attribute) and CodeBehindGenerator (emits the matching
/// Close(...) stub) so the two can never drift out of sync about which controls qualify.
/// </summary>
public static class DialogResultButtonHelper
{
    public static bool TryGetDialogResultValue(ControlNode control, out string value)
    {
        value = string.Empty;

        if (!control.Properties.TryGetValue("DialogResult", out var property))
        {
            return false;
        }

        var raw = property.Value?.ToString();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var lastDot = raw.LastIndexOf('.');
        var candidate = lastDot >= 0 ? raw[(lastDot + 1)..] : raw;

        if (string.IsNullOrEmpty(candidate) || candidate == "None")
        {
            return false;
        }

        value = candidate;
        return true;
    }
}
