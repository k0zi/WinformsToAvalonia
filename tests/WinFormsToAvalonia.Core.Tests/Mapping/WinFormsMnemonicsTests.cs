using WinFormsToAvalonia.Core.Mapping;

namespace WinFormsToAvalonia.Core.Tests.Mapping;

/// <summary>
/// The ampersand-to-underscore transliteration, in both directions at once.
/// </summary>
public class WinFormsMnemonicsTests
{
    [Theory]
    [InlineData("&File", "_File")]
    [InlineData("Save &As...", "Save _As...")]
    // "&&" is WinForms' escape for a literal ampersand; Avalonia writes one plainly.
    [InlineData("Buttons && Text", "Buttons & Text")]
    [InlineData("&Save && Close", "_Save & Close")]
    // An underscore already in the text would become a marker of its own, so it is doubled.
    [InlineData("Open_file", "Open__file")]
    [InlineData("no markers here", "no markers here")]
    // A trailing ampersand marks nothing in WinForms either.
    [InlineData("Trailing&", "Trailing")]
    public void AccessKey_TransliteratesBothMarkers(string winFormsText, string expected) =>
        Assert.Equal(expected, WinFormsMnemonics.Convert(winFormsText, MnemonicHandling.AccessKey));

    [Theory]
    [InlineData("&File", "File")]
    [InlineData("Buttons && Text", "Buttons & Text")]
    // Nothing reads underscores on this target, so one in the text stays exactly as it is.
    [InlineData("Open_file", "Open_file")]
    [InlineData("Trailing&", "Trailing")]
    public void Strip_RemovesTheMarkerAndKeepsTheRest(string winFormsText, string expected) =>
        Assert.Equal(expected, WinFormsMnemonics.Convert(winFormsText, MnemonicHandling.Strip));

    /// <summary>
    /// The case that makes this a table rather than a blanket rule: a TextBox's Text is the
    /// user's data, and "Smith &amp; Sons" has to survive the conversion character for character.
    /// </summary>
    [Theory]
    [InlineData("Smith & Sons")]
    [InlineData("a && b")]
    [InlineData("under_score")]
    public void None_LeavesTheTextAlone(string text) =>
        Assert.Equal(text, WinFormsMnemonics.Convert(text, MnemonicHandling.None));

    [Theory]
    [InlineData("TextBox", MnemonicHandling.None)]
    [InlineData("ToolStripTextBox", MnemonicHandling.None)]
    [InlineData("ComboBox", MnemonicHandling.None)]
    [InlineData("Button", MnemonicHandling.AccessKey)]
    [InlineData("ToolStripMenuItem", MnemonicHandling.AccessKey)]
    [InlineData("TabPage", MnemonicHandling.AccessKey)]
    [InlineData("Label", MnemonicHandling.Strip)]
    [InlineData("ToolStripStatusLabel", MnemonicHandling.Strip)]
    public void Catalog_ClassifiesTheControlsThatCarryCaptions(string winFormsTypeName, MnemonicHandling expected) =>
        Assert.Equal(expected, WinFormsMnemonicCatalog.For(winFormsTypeName));
}
