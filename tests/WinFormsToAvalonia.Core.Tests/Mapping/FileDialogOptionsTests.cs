using WinFormsToAvalonia.Core.Mapping;
using WinFormsToAvalonia.Core.Model;
using Xunit;

namespace WinFormsToAvalonia.Core.Tests.Mapping;

public class FileDialogOptionsTests
{
    private static ControlModel Dialog(string type, params (string Name, object Value)[] properties)
    {
        var dialog = new ControlModel { FieldName = "dialog1", ClrTypeName = type };
        foreach (var (name, value) in properties)
        {
            dialog.Properties[name] = new PropertyValue.Literal(value);
        }

        return dialog;
    }

    private static FileDialogOptionsResult Build(ControlModel dialog)
    {
        Assert.True(FileDialogCatalog.TryGet(dialog.ClrTypeName, out var kind));
        return FileDialogOptions.Build(dialog, kind, "StorageProvider");
    }

    [Fact]
    public void Build_NothingSet_IsTheDefaultOptions()
    {
        Assert.Equal("new FilePickerOpenOptions()", Build(Dialog("OpenFileDialog")).Expression);
    }

    [Fact]
    public void Build_OpenDialog_CarriesItsDesignerSettings()
    {
        var result = Build(Dialog("OpenFileDialog",
            ("Title", "Open log"),
            ("Filter", "Logs|*.log;*.txt|All files|*.*"),
            ("FileName", "today.log"),
            ("InitialDirectory", @"C:\Logs"),
            ("Multiselect", true)));

        Assert.Equal(
            "new FilePickerOpenOptions { Title = \"Open log\", SuggestedFileName = \"today.log\", "
            + "SuggestedStartLocation = await StorageProvider.TryGetFolderFromPathAsync(\"C:\\\\Logs\"), "
            + "FileTypeFilter = [new FilePickerFileType(\"Logs\") { Patterns = [\"*.log\", \"*.txt\"] }, "
            + "new FilePickerFileType(\"All files\") { Patterns = [\"*.*\"] }], AllowMultiple = true }",
            result.Expression);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Build_SaveDialog_UsesChoicesAndTheSaveOnlyMembers()
    {
        var result = Build(Dialog("SaveFileDialog",
            ("Filter", "CSV|*.csv"), ("DefaultExt", ".csv"), ("OverwritePrompt", false)));

        Assert.Equal(
            "new FilePickerSaveOptions { FileTypeChoices = [new FilePickerFileType(\"CSV\") { Patterns = [\"*.csv\"] }], "
            + "DefaultExtension = \"csv\", ShowOverwritePrompt = false }",
            result.Expression);
    }

    [Fact]
    public void Build_FolderDialog_IsTitledByItsDescriptionAndStartsAtItsSelectedPath()
    {
        Assert.Equal(
            "new FolderPickerOpenOptions { Title = \"Pick\", SuggestedStartLocation = await StorageProvider.TryGetFolderFromPathAsync(\"/tmp\") }",
            Build(Dialog("FolderBrowserDialog", ("Description", "Pick"), ("SelectedPath", "/tmp"))).Expression);
    }

    [Fact]
    public void Build_MalformedFilter_IsReportedNotGuessed()
    {
        var result = Build(Dialog("OpenFileDialog", ("Filter", "Text files *.txt")));

        Assert.Equal("new FilePickerOpenOptions()", result.Expression);
        Assert.Contains(result.Warnings, w => w.Contains("not name|patterns pairs"));
    }

    [Fact]
    public void Build_NonDefaultFilterIndex_IsReported()
    {
        var result = Build(Dialog("OpenFileDialog", ("Filter", "A|*.a|B|*.b"), ("FilterIndex", 2)));

        Assert.Contains("FileTypeFilter", result.Expression);
        Assert.Contains(result.Warnings, w => w.Contains("preselects filter 2"));
    }

    [Theory]
    [InlineData("A|*.a", true)]
    [InlineData("A|*.a; *.b |B|*.c", true)]
    [InlineData("A|*.a|B", false)]
    [InlineData("|*.a", false)]
    [InlineData("A|", false)]
    public void TryParseFilter_AcceptsOnlyNamePatternPairs(string filter, bool expected)
    {
        Assert.Equal(expected, FileDialogOptions.TryParseFilter(filter, out _));
    }
}
