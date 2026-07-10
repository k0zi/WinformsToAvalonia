using System.Xml.Linq;

namespace Converter.Core.Parsing;

/// <summary>
/// A single `&lt;data&gt;` entry from a .resx file. Exactly one of StringValue/BinaryValue/
/// ExternalFilePath is meaningfully populated, depending on how the entry was serialized.
/// </summary>
public sealed class ResxEntry
{
    public required string Name { get; init; }

    /// <summary>Plain string/text resource value (the common case: labels, titles, etc.).</summary>
    public string? StringValue { get; init; }

    /// <summary>Decoded base64 bytes, for entries embedding binary data directly.</summary>
    public byte[]? BinaryValue { get; init; }

    /// <summary>
    /// Resolved absolute path to an externally-referenced file, for `ResXFileRef` entries
    /// (WinForms designer's "Select Existing File..." image picker generates these).
    /// </summary>
    public string? ExternalFilePath { get; init; }

    /// <summary>Raw `type` attribute, e.g. "System.Drawing.Bitmap, System.Drawing".</summary>
    public string? TypeName { get; init; }

    /// <summary>Raw `mimetype` attribute.</summary>
    public string? MimeType { get; init; }

    /// <summary>
    /// True for legacy BinaryFormatter-serialized payloads (mimetype
    /// "application/x-microsoft.net.object.binary.base64") - these cannot be safely
    /// deserialized (BinaryFormatter is deprecated/removed for security reasons) or
    /// meaningfully interpreted as raw bytes, so callers should treat them as unrecoverable
    /// rather than attempt to extract an asset from BinaryValue.
    /// </summary>
    public bool IsBinaryFormatterEnvelope { get; init; }
}

/// <summary>
/// Hand-rolled .resx parser using System.Xml.Linq - .resx is plain XML
/// (&lt;root&gt;&lt;data name="..."&gt;&lt;value&gt;...&lt;/value&gt;&lt;/data&gt;...), so this
/// avoids pulling in System.Windows.Forms's Windows-only ResXResourceReader or the
/// System.Resources.Extensions package, keeping this CLI's cross-platform story intact.
/// </summary>
public static class ResxDocument
{
    private const string BinaryFormatterMimeType = "application/x-microsoft.net.object.binary.base64";
    private const string ResXFileRefTypeName = "System.Resources.ResXFileRef";

    public static async Task<IReadOnlyDictionary<string, ResxEntry>> LoadAsync(string resxPath)
    {
        var entries = new Dictionary<string, ResxEntry>(StringComparer.Ordinal);

        var xml = await File.ReadAllTextAsync(resxPath);
        var document = XDocument.Parse(xml);
        var root = document.Root;
        if (root == null)
        {
            return entries;
        }

        var resxDirectory = Path.GetDirectoryName(resxPath) ?? string.Empty;

        foreach (var dataElement in root.Elements("data"))
        {
            var name = (string?)dataElement.Attribute("name");
            if (string.IsNullOrEmpty(name))
            {
                continue;
            }

            var typeName = (string?)dataElement.Attribute("type");
            var mimeType = (string?)dataElement.Attribute("mimetype");
            var rawValue = dataElement.Element("value")?.Value?.Trim() ?? string.Empty;

            entries[name] = ParseEntry(name, typeName, mimeType, rawValue, resxDirectory);
        }

        return entries;
    }

    private static ResxEntry ParseEntry(string name, string? typeName, string? mimeType, string rawValue, string resxDirectory)
    {
        // ResXFileRef: value is "relativePath;AssemblyQualifiedTypeName[;encoding]".
        if (typeName != null && typeName.Contains(ResXFileRefTypeName, StringComparison.Ordinal))
        {
            var parts = rawValue.Split(';');
            var relativePath = parts.Length > 0 ? parts[0] : null;
            var externalPath = !string.IsNullOrEmpty(relativePath)
                ? Path.GetFullPath(Path.Combine(resxDirectory, relativePath))
                : null;

            return new ResxEntry
            {
                Name = name,
                ExternalFilePath = externalPath,
                TypeName = typeName,
                MimeType = mimeType
            };
        }

        var isBinaryFormatterEnvelope = mimeType != null &&
            mimeType.Equals(BinaryFormatterMimeType, StringComparison.OrdinalIgnoreCase);

        // No type attribute (or an explicit System.String) with no mimetype is a plain
        // string/text resource - the overwhelming common case (labels, titles, tooltips).
        if (typeName == null && mimeType == null)
        {
            return new ResxEntry { Name = name, StringValue = rawValue, TypeName = typeName, MimeType = mimeType };
        }

        // Anything else with a type/mimetype is base64-encoded binary data - either a
        // directly-embedded image (the common modern case) or a legacy BinaryFormatter
        // envelope (flagged, not decoded further - see IsBinaryFormatterEnvelope docs).
        byte[]? binaryValue = null;
        try
        {
            var compact = rawValue.Replace("\r", string.Empty).Replace("\n", string.Empty).Replace(" ", string.Empty);
            if (compact.Length > 0)
            {
                binaryValue = Convert.FromBase64String(compact);
            }
        }
        catch (FormatException)
        {
            // Not valid base64 - fall through with binaryValue left null; the entry is then
            // effectively unrecoverable, same as an empty/unrecognized payload.
        }

        return new ResxEntry
        {
            Name = name,
            BinaryValue = binaryValue,
            TypeName = typeName,
            MimeType = mimeType,
            IsBinaryFormatterEnvelope = isBinaryFormatterEnvelope
        };
    }
}
