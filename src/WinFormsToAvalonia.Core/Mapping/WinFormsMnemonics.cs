using System.Text;

namespace WinFormsToAvalonia.Core.Mapping;

/// <summary>What a control's <c>Text</c> means when it contains an ampersand.</summary>
public enum MnemonicHandling
{
    /// <summary>Not a caption - the text is data, and an ampersand in it is just an ampersand.</summary>
    None,

    /// <summary>A caption, but the target element does not render access keys: the marker goes away.</summary>
    Strip,

    /// <summary>A caption on a target that does render access keys: the marker becomes Avalonia's.</summary>
    AccessKey,
}

/// <summary>
/// The Avalonia elements whose templates really consume an underscore as an access key.
/// </summary>
/// <remarks>
/// <para>
/// Measured, not assumed. Access keys are a *template* detail - a
/// <c>ContentPresenter</c> with <c>RecognizesAccessKey</c>, which produces an
/// <c>AccessText</c> - so unlike the other tables in this folder it cannot be read out of
/// Avalonia's reference metadata at all. It was established by rendering each element with the
/// text <c>_File</c> on the headless platform and looking for an <c>AccessText</c> in the
/// resulting visual tree: the ones below had one and displayed "File"; TextBlock, Expander and
/// a bare ContentControl had none and displayed "_File" verbatim.
/// </para>
/// <para>
/// Getting this wrong is silent either way: a missing entry leaves a stray underscore in front of
/// every caption, and a wrong one eats the first letter of a label.
/// </para>
/// </remarks>
public static class AvaloniaAccessKeySupport
{
    private static readonly HashSet<string> Elements = new(StringComparer.Ordinal)
    {
        "Button",
        "ToggleButton",
        "CheckBox",
        "RadioButton",
        "HyperlinkButton",
        "SplitButton",
        "ToggleSplitButton",
        "Label",
        "MenuItem",
        "TabItem",
    };

    public static bool Consumes(string avaloniaElementName) => Elements.Contains(avaloniaElementName);

    /// <summary>Exposed so the mapping tables can be checked against it.</summary>
    public static IReadOnlySet<string> All => Elements;
}

/// <summary>
/// Which WinForms controls treat an ampersand in <c>Text</c> as a mnemonic marker, and what the
/// conversion should do with it.
/// </summary>
/// <remarks>
/// <para>
/// Two facts at once, which is why it is one table rather than two. Whether the marker is there
/// at all is a fact about <em>WinForms</em>: a Button's Text is a caption and <c>&amp;File</c>
/// underlines the F, while a TextBox's Text is the user's data and an ampersand in it must
/// survive untouched. Whether it can be carried across is a fact about the <em>target</em>, from
/// <see cref="AvaloniaAccessKeySupport"/> - a Label becomes a TextBlock, which renders an
/// underscore literally, so the marker has to go rather than move.
/// </para>
/// <para>
/// The two halves are checked against each other in WinFormsToAvalonia.Mapping.Tests: every
/// entry here is held up against the element its mapper actually emits.
/// </para>
/// </remarks>
public static class WinFormsMnemonicCatalog
{
    private static readonly IReadOnlyDictionary<string, MnemonicHandling> Handling =
        new Dictionary<string, MnemonicHandling>(StringComparer.Ordinal)
        {
            ["Button"] = MnemonicHandling.AccessKey,
            ["CheckBox"] = MnemonicHandling.AccessKey,
            ["RadioButton"] = MnemonicHandling.AccessKey,
            ["LinkLabel"] = MnemonicHandling.AccessKey,
            ["TabPage"] = MnemonicHandling.AccessKey,
            ["ToolStripMenuItem"] = MnemonicHandling.AccessKey,
            ["ToolStripButton"] = MnemonicHandling.AccessKey,
            ["ToolStripDropDownButton"] = MnemonicHandling.AccessKey,
            ["ToolStripSplitButton"] = MnemonicHandling.AccessKey,

            // Captions whose target renders text verbatim. A Label is the common one, and the
            // one that made the sample's menu read "&File": the marker is real, there is just
            // nowhere for it to go.
            ["Label"] = MnemonicHandling.Strip,
            ["ToolStripLabel"] = MnemonicHandling.Strip,
            ["ToolStripStatusLabel"] = MnemonicHandling.Strip,

            // A GroupBox's caption is a mnemonic in WinForms, but GroupBoxFallback is a Canvas
            // with a Header property nothing draws an AccessText for.
            ["GroupBox"] = MnemonicHandling.Strip,
        };

    public static MnemonicHandling For(string winFormsTypeName) =>
        Handling.TryGetValue(winFormsTypeName, out var handling) ? handling : MnemonicHandling.None;

    /// <summary>Exposed so the table can be checked against the mappers it describes.</summary>
    public static IReadOnlyDictionary<string, MnemonicHandling> All => Handling;
}

/// <summary>
/// Rewrites a WinForms caption's ampersand markers into what the Avalonia target expects.
/// </summary>
/// <remarks>
/// WinForms and Avalonia both use one character as the marker and a doubled one as the literal -
/// <c>&amp;</c> and <c>&amp;&amp;</c> against <c>_</c> and <c>__</c> - so this is a
/// transliteration, and it has to go in both directions at once: the marker becomes an
/// underscore, and any underscore already in the text has to be doubled so it does not become a
/// marker of its own.
/// </remarks>
public static class WinFormsMnemonics
{
    public static string Convert(string text, MnemonicHandling handling)
    {
        if (handling == MnemonicHandling.None || (!text.Contains('&') && !text.Contains('_')))
        {
            return text;
        }

        var toAccessKey = handling == MnemonicHandling.AccessKey;
        var result = new StringBuilder(text.Length);

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];

            if (c == '_' && toAccessKey)
            {
                // Already an underscore, and about to land in a control that reads underscores.
                result.Append("__");
                continue;
            }

            if (c != '&')
            {
                result.Append(c);
                continue;
            }

            if (i + 1 < text.Length && text[i + 1] == '&')
            {
                // "&&" is WinForms' escape for a literal ampersand, which Avalonia writes plainly.
                result.Append('&');
                i++;
                continue;
            }

            // A trailing '&' marks nothing in WinForms either, and is dropped with the rest.
            if (i + 1 < text.Length && toAccessKey)
            {
                result.Append('_');
            }
        }

        return result.ToString();
    }
}
