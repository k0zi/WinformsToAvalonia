namespace WinFormsToAvalonia.Core.Mapping;

/// <param name="PickerMethodName">The <c>TopLevel.StorageProvider</c> method that replaces ShowDialog.</param>
/// <param name="OptionsTypeName">The options object that method takes.</param>
/// <param name="SelectionPattern">
/// The pattern that both tests for a selection and binds it, as a format string where <c>{0}</c>
/// is the variable name. A list-returning picker needs a list pattern; the save picker returns a
/// single nullable file.
/// </param>
/// <param name="SelectionSuffix">Appended to the dialog's field name to name that variable.</param>
/// <param name="PathMemberName">The WinForms property that held the chosen path.</param>
public sealed record FileDialogKind(
    string PickerMethodName,
    string OptionsTypeName,
    string SelectionPattern,
    string SelectionSuffix,
    string PathMemberName);

/// <summary>
/// The WinForms file/folder dialogs and their <c>TopLevel.StorageProvider</c> replacements.
/// </summary>
/// <remarks>
/// Only these three have an Avalonia equivalent at all. <c>ColorDialog</c>, <c>FontDialog</c> and
/// the print dialogs have none, so they stay guidance-only - see docs/known-limitations.md.
/// </remarks>
public static class FileDialogCatalog
{
    private static readonly IReadOnlyDictionary<string, FileDialogKind> ByTypeName =
        new Dictionary<string, FileDialogKind>(StringComparer.Ordinal)
        {
            ["OpenFileDialog"] = new(
                "OpenFilePickerAsync", "FilePickerOpenOptions", "[var {0}, ..]", "File", "FileName"),
            ["SaveFileDialog"] = new(
                "SaveFilePickerAsync", "FilePickerSaveOptions", "{{ }} {0}", "File", "FileName"),
            ["FolderBrowserDialog"] = new(
                "OpenFolderPickerAsync", "FolderPickerOpenOptions", "[var {0}, ..]", "Folder", "SelectedPath"),
        };

    public static bool TryGet(string winFormsTypeName, out FileDialogKind kind) =>
        ByTypeName.TryGetValue(winFormsTypeName, out kind!);

    public static IEnumerable<string> TypeNames => ByTypeName.Keys;
}
