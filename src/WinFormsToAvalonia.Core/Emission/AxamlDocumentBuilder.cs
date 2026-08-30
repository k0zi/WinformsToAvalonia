using System.Text;

namespace WinFormsToAvalonia.Core.Emission;

/// <summary>
/// Minimal indentation-aware XML text writer for AXAML generation. Deliberately not
/// XDocument/XmlWriter: XAML attached-property attribute names like "Canvas.Left" and
/// namespace-prefixed names like "x:Name" / "w2a:LayoutHint.Anchor" are literal XAML syntax
/// text, not something XDocument's namespace-aware attribute API models cleanly without
/// fighting prefix/URI resolution. This writer treats element/attribute names as plain
/// strings while still safely XML-escaping every attribute value, and produces
/// deterministic, fixed-indent output suitable for golden-file snapshot tests.
/// </summary>
public sealed class AxamlDocumentBuilder
{
    private readonly StringBuilder _text = new();
    private readonly Stack<string> _openElements = new();
    private int _indentLevel;
    private bool _lastOpenTagUnclosed;

    public void OpenElement(string name)
    {
        FinishPendingOpenTag();
        WriteIndent();
        _text.Append('<').Append(name);
        _openElements.Push(name);
        _lastOpenTagUnclosed = true;
        _indentLevel++;
    }

    public void Attribute(string name, string value)
    {
        _text.Append(' ').Append(name).Append("=\"").Append(Escape(value)).Append('"');
    }

    public void CloseElement()
    {
        _indentLevel--;

        if (_lastOpenTagUnclosed)
        {
            _text.Append(" />\n");
            _lastOpenTagUnclosed = false;
            _openElements.Pop();
            return;
        }

        WriteIndent();
        _text.Append("</").Append(_openElements.Pop()).Append(">\n");
    }

    public void Comment(string text)
    {
        FinishPendingOpenTag();
        WriteIndent();
        _text.Append("<!-- ").Append(text.Replace("--", "- -")).Append(" -->\n");
    }

    public override string ToString()
    {
        FinishPendingOpenTag();
        return _text.ToString();
    }

    private void FinishPendingOpenTag()
    {
        if (_lastOpenTagUnclosed)
        {
            _text.Append(">\n");
            _lastOpenTagUnclosed = false;
        }
    }

    private void WriteIndent() => _text.Append(' ', _indentLevel * 4);

    /// <summary>
    /// XML-escapes an attribute value. Internal because App.axaml is assembled as text by
    /// <c>AvaloniaProjectScaffolder</c> rather than through this builder, and the two must not
    /// disagree about what needs escaping.
    /// </summary>
    internal static string Escape(string value) => value
        .Replace("&", "&amp;")
        .Replace("<", "&lt;")
        .Replace(">", "&gt;")
        .Replace("\"", "&quot;");
}
