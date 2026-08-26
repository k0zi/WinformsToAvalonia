using System.Xml.Linq;

namespace WinFormsToAvalonia.Core.Parsing;

/// <summary>
/// One <c>&lt;data&gt;</c> entry of a .resx file. <see cref="Name"/> is the designer's
/// <c>"&lt;fieldName&gt;.&lt;PropertyName&gt;"</c> key (<c>"$this.Text"</c> for the form itself);
/// <see cref="TypeName"/> is the assembly-qualified CLR type the designer serialized, or null
/// for a plain string.
/// </summary>
public sealed record ResxEntry(string Name, string? TypeName, string? MimeType, string Value)
{
    /// <summary>The field this entry configures - "button1" for "button1.Text".</summary>
    public string OwnerName => Name[..LastDotIndex];

    /// <summary>The property this entry sets - "Text" for "button1.Text".</summary>
    public string PropertyName => Name[(LastDotIndex + 1)..];

    /// <summary>The CLR type's simple name - "Point" for "System.Drawing.Point, System.Drawing".</summary>
    public string? TypeSimpleName
    {
        get
        {
            if (TypeName is null)
            {
                return null;
            }

            var withoutAssembly = TypeName.Split(',')[0].Trim();
            var lastDot = withoutAssembly.LastIndexOf('.');
            return lastDot < 0 ? withoutAssembly : withoutAssembly[(lastDot + 1)..];
        }
    }

    /// <summary>True for a base64 payload (an image, an icon, a serialized object) rather than text.</summary>
    public bool IsBinary => MimeType is not null && MimeType.Contains("base64", StringComparison.OrdinalIgnoreCase);

    private int LastDotIndex => Name.LastIndexOf('.');
}

/// <summary>The <c>&lt;data&gt;</c> entries of one .resx, indexed by the field they belong to.</summary>
public sealed class ResxDocument
{
    /// <summary>The designer's key for the Form/UserControl itself, as opposed to one of its fields.</summary>
    public const string FormOwnerKey = "$this";

    private readonly ILookup<string, ResxEntry> _byOwner;

    public ResxDocument(string filePath, IEnumerable<ResxEntry> entries)
    {
        FilePath = filePath;
        _byOwner = entries.ToLookup(e => e.OwnerName, StringComparer.Ordinal);
    }

    public static ResxDocument Empty { get; } = new("", []);

    public string FilePath { get; }

    /// <summary>Every entry belonging to one designer field, in the order the file declared them.</summary>
    public IEnumerable<ResxEntry> EntriesFor(string ownerName) => _byOwner[ownerName];
}

/// <summary>
/// Reads the <c>&lt;data&gt;</c> entries of a WinForms .resx.
/// </summary>
/// <remarks>
/// A .resx is plain XML, so this is an <see cref="XDocument"/> read rather than a
/// <c>System.Resources</c> one: the real reader would deserialize the payloads with
/// BinaryFormatter, which modern .NET refuses to run - and this converter only ever needs the
/// declared type name and the raw text/base64, never a live object.
///
/// <c>&lt;metadata&gt;</c> (designer tray positions) and <c>&lt;resheader&gt;</c> are skipped:
/// they describe the *designer surface*, not the form. So are the <c>&gt;&gt;</c>-prefixed
/// entries (<c>&gt;&gt;button1.Name</c>, <c>&gt;&gt;button1.Type</c>, ...), which are the
/// designer's own bookkeeping about the field rather than a property to set on it.
/// </remarks>
public sealed class ResxReader
{
    public ResxDocument Read(string? filePath)
    {
        if (filePath is null || !File.Exists(filePath))
        {
            return ResxDocument.Empty;
        }

        XDocument document;
        try
        {
            document = XDocument.Load(filePath);
        }
        catch (System.Xml.XmlException)
        {
            // A malformed .resx must degrade to "no resources", never take the conversion down.
            return ResxDocument.Empty;
        }

        var entries = new List<ResxEntry>();

        foreach (var data in document.Root?.Elements("data") ?? [])
        {
            var name = data.Attribute("name")?.Value;
            if (name is null || name.StartsWith(">>", StringComparison.Ordinal) || !name.Contains('.'))
            {
                continue;
            }

            entries.Add(new ResxEntry(
                name,
                data.Attribute("type")?.Value,
                data.Attribute("mimetype")?.Value,
                data.Element("value")?.Value ?? ""));
        }

        return new ResxDocument(filePath, entries);
    }
}
