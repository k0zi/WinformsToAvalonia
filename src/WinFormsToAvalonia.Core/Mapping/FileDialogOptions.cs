using Microsoft.CodeAnalysis.CSharp;
using WinFormsToAvalonia.Core.Model;

namespace WinFormsToAvalonia.Core.Mapping;

/// <param name="Expression">The <c>new …Options { … }</c> expression the picker call takes.</param>
/// <param name="Warnings">Designer settings the options could not express.</param>
public sealed record FileDialogOptionsResult(string Expression, IReadOnlyList<string> Warnings);

/// <summary>
/// A file dialog's designer settings, as the options object its <c>StorageProvider</c> picker takes.
/// </summary>
/// <remarks>
/// <para>
/// The picker used to be opened with default options, on the grounds that parsing a WinForms
/// filter string and getting it subtly wrong is worse than leaving it out. The grammar is not
/// subtle, though - <c>"Text files|*.txt;*.log|All files|*.*"</c> is name/patterns pairs separated
/// by <c>|</c>, patterns by <c>;</c> - and leaving it out meant every converted Open dialog offered
/// every file and every Save dialog no type at all. A string that does not have that shape is
/// still refused, and reported.
/// </para>
/// <para>
/// One builder for all three call sites (the two inline shapes in <c>HandlerBodyRewriter</c> and the
/// generated helper method), so the same dialog opens the same way wherever it is called from.
/// Only designer literals, as everywhere else; and the property names and types were read off
/// Avalonia's reference assemblies (<c>WinFormsToAvalonia.Mapping.Tests</c> keeps checking them).
/// </para>
/// </remarks>
public static class FileDialogOptions
{
    /// <summary>The options members this builder can set, per options type - what Mapping.Tests checks.</summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> EmittedMembers { get; } =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            ["FilePickerOpenOptions"] = ["Title", "SuggestedFileName", "SuggestedStartLocation", "FileTypeFilter", "AllowMultiple"],
            ["FilePickerSaveOptions"] = ["Title", "SuggestedFileName", "SuggestedStartLocation", "FileTypeChoices", "DefaultExtension", "ShowOverwritePrompt"],
            ["FolderPickerOpenOptions"] = ["Title", "SuggestedStartLocation"],
        };

    public static FileDialogOptionsResult Build(ControlModel dialog, FileDialogKind kind, string storageProviderExpression)
    {
        var members = new List<string>();
        var warnings = new List<string>();
        var properties = dialog.Properties;
        var isFolder = kind.OptionsTypeName == "FolderPickerOpenOptions";
        var isSave = kind.OptionsTypeName == "FilePickerSaveOptions";

        string? Text(string name) =>
            properties.TryGetValue(name, out var value) && value is PropertyValue.Literal { Value: string s } ? s : null;

        bool? Flag(string name) =>
            properties.TryGetValue(name, out var value) && value is PropertyValue.Literal { Value: bool b } ? b : null;

        static string Quote(string text) => SymbolDisplay.FormatLiteral(text, quote: true);

        // A folder dialog has no title of its own in WinForms - its Description is the prompt it
        // shows - so that is what titles the folder picker.
        if ((isFolder ? Text("Description") : Text("Title")) is { Length: > 0 } title)
        {
            members.Add($"Title = {Quote(title)}");
        }

        if (!isFolder && Text("FileName") is { Length: > 0 } fileName)
        {
            members.Add($"SuggestedFileName = {Quote(fileName)}");
        }

        // An awaited folder lookup inside the initializer: every call site is already async, and a
        // path that does not exist on this machine simply yields null, which is the option's default.
        var startFolder = Text("InitialDirectory") is { Length: > 0 } initial ? initial
            : isFolder && Text("SelectedPath") is { Length: > 0 } selected ? selected
            : null;
        if (startFolder is not null)
        {
            members.Add($"SuggestedStartLocation = await {storageProviderExpression}.TryGetFolderFromPathAsync({Quote(startFolder)})");
        }

        if (!isFolder && Text("Filter") is { Length: > 0 } filter)
        {
            if (TryParseFilter(filter, out var types))
            {
                var list = string.Join(", ", types.Select(t =>
                    $"new FilePickerFileType({Quote(t.Name)}) {{ Patterns = [{string.Join(", ", t.Patterns.Select(Quote))}] }}"));
                members.Add($"{(isSave ? "FileTypeChoices" : "FileTypeFilter")} = [{list}]");

                // FilterIndex picks the type selected first. Avalonia takes that as a type
                // *instance* (SuggestedFileType), which an inline initializer cannot point back at -
                // so only the default, the first, comes across exactly.
                if (properties.TryGetValue("FilterIndex", out var index)
                    && index is PropertyValue.Literal { Value: int filterIndex }
                    && filterIndex != 1)
                {
                    warnings.Add(
                        $"'{dialog.FieldName}' ({dialog.ClrTypeName}) preselects filter {filterIndex}; the picker opens with "
                        + "the first file type selected - move that type to the front of the Filter if it matters.");
                }
            }
            else
            {
                warnings.Add(
                    $"'{dialog.FieldName}' ({dialog.ClrTypeName}) has a Filter that is not name|patterns pairs "
                    + $"({Quote(filter)}), so the picker opens without file types rather than with a guess at them.");
            }
        }

        if (isSave && Text("DefaultExt") is { Length: > 0 } extension)
        {
            members.Add($"DefaultExtension = {Quote(extension.TrimStart('.'))}");
        }

        if (kind.OptionsTypeName == "FilePickerOpenOptions" && Flag("Multiselect") == true)
        {
            members.Add("AllowMultiple = true");
        }

        if (isSave && Flag("OverwritePrompt") == false)
        {
            members.Add("ShowOverwritePrompt = false");
        }

        var expression = members.Count == 0
            ? $"new {kind.OptionsTypeName}()"
            : $"new {kind.OptionsTypeName} {{ {string.Join(", ", members)} }}";
        return new FileDialogOptionsResult(expression, warnings);
    }

    /// <summary>
    /// <c>"Name|*.a;*.b|Name2|*.c"</c> into its pairs. Refuses an odd number of parts, an empty name
    /// or an empty pattern list - each is a filter WinForms itself throws on.
    /// </summary>
    public static bool TryParseFilter(string filter, out IReadOnlyList<(string Name, IReadOnlyList<string> Patterns)> types)
    {
        types = [];
        var parts = filter.Split('|');
        if (parts.Length % 2 != 0)
        {
            return false;
        }

        var parsed = new List<(string, IReadOnlyList<string>)>();
        for (var i = 0; i < parts.Length; i += 2)
        {
            var name = parts[i].Trim();
            var patterns = parts[i + 1].Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (name.Length == 0 || patterns.Length == 0)
            {
                return false;
            }

            parsed.Add((name, patterns));
        }

        types = parsed;
        return true;
    }
}
