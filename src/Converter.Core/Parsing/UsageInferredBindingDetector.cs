using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Converter.Core.Parsing;

/// <summary>
/// Infers a two-way property binding for a control from *usage* (its property is read/written
/// from at least two distinct migrated members) rather than requiring an explicit
/// `.DataBindings.Add(...)` call - many real WinForms apps never call that API at all (confirmed
/// against a real sample: zero DataBindings.Add calls across every form), so the
/// DataBindings-only binding machinery (ViewModelGenerator.ExtractBoundProperties) has no reach
/// on them, even though a control read in one method and written in another is behaviorally a
/// bound property either way. Deliberately conservative (a 2-member threshold): a control
/// touched from a single method is at least as likely to be a one-off action (e.g. clearing a
/// field) as a persistent property, so promoting it risks generating bindings for things that
/// were never meant to be ViewModel state.
/// </summary>
public static class UsageInferredBindingDetector
{
    /// <summary>
    /// Longest-first so e.g. "MaskedTextBox" matches before the shorter "TextBox" would.
    /// </summary>
    private static readonly string[] ControlSuffixesLongestFirst =
    [
        "MaskedTextBox", "RichTextBox", "DomainUpDown", "DateTimePicker", "NumericUpDown",
        "RadioButton", "PictureBox", "CheckedListBox", "TrackBar", "TextBox", "ComboBox",
        "CheckBox", "ListView", "ListBox", "TreeView", "Label", "Button"
    ];

    /// <summary>
    /// Finds every (controlName, property) pair referenced from at least
    /// <paramref name="minDistinctMembers"/> distinct member bodies. <paramref name="alreadyBound"/>
    /// (the DataBindings-derived lookup) is excluded - nothing to infer where a real binding
    /// already exists. Returns a lookup shaped exactly like
    /// ViewModelGenerator.BuildBoundControlPropertyLookup's own, so callers can merge the two
    /// directly.
    /// </summary>
    public static IReadOnlyDictionary<(string ControlName, string Property), string> DetectInferredBindings(
        IEnumerable<string> memberBodies,
        IReadOnlySet<string> controlNames,
        IReadOnlyDictionary<(string ControlName, string ControlProperty), string> alreadyBound,
        int minDistinctMembers = 2)
    {
        var occurrenceCounts = new Dictionary<(string, string), int>();

        foreach (var memberBody in memberBodies)
        {
            foreach (var pair in FindControlPropertyReferences(memberBody, controlNames))
            {
                if (alreadyBound.ContainsKey(pair))
                {
                    continue;
                }

                occurrenceCounts[pair] = occurrenceCounts.GetValueOrDefault(pair) + 1;
            }
        }

        var inferred = new Dictionary<(string, string), string>();
        foreach (var ((controlName, property), count) in occurrenceCounts)
        {
            if (count >= minDistinctMembers)
            {
                inferred[(controlName, property)] = DerivePropertyName(controlName);
            }
        }

        return inferred;
    }

    /// <summary>
    /// "skuTextBox" -> "Sku" (strips the trailing control-type suffix WinForms designer naming
    /// conventionally uses, then PascalCases what's left) - there's no DataMember to name the
    /// property from here, unlike the DataBindings.Add path.
    /// </summary>
    public static string DerivePropertyName(string controlName)
    {
        foreach (var suffix in ControlSuffixesLongestFirst)
        {
            if (controlName.Length > suffix.Length &&
                controlName.EndsWith(suffix, StringComparison.Ordinal))
            {
                return ToPascalCase(controlName[..^suffix.Length]);
            }
        }

        return ToPascalCase(controlName);
    }

    private static string ToPascalCase(string text) =>
        string.IsNullOrEmpty(text) ? text : char.ToUpperInvariant(text[0]) + text[1..];

    private static HashSet<(string ControlName, string Property)> FindControlPropertyReferences(
        string fullMethodSource, IReadOnlySet<string> controlNames)
    {
        var results = new HashSet<(string, string)>();

        try
        {
            var body = EventHandlerBodyParser.ExtractBodyText(fullMethodSource);
            var wrapper = $"class __Wrapper {{ void __M() {body} }}";
            var root = CSharpSyntaxTree.ParseText(wrapper).GetRoot();

            foreach (var memberAccess in root.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
            {
                if (memberAccess.Expression is not IdentifierNameSyntax identifier)
                {
                    continue;
                }

                var controlName = identifier.Identifier.Text;
                if (!controlNames.Contains(controlName))
                {
                    continue;
                }

                results.Add((controlName, memberAccess.Name.Identifier.Text));
            }
        }
        catch
        {
            // Best-effort: an unparseable body simply contributes no references.
        }

        return results;
    }
}
