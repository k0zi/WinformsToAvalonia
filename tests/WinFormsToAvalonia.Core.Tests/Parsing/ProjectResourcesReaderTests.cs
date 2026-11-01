using WinFormsToAvalonia.Core.Emission;
using WinFormsToAvalonia.Core.Parsing;
using Xunit;

namespace WinFormsToAvalonia.Core.Tests.Parsing;

public sealed class ProjectResourcesReaderTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "w2a-projres-" + Guid.NewGuid());

    public ProjectResourcesReaderTests()
    {
        Directory.CreateDirectory(Path.Combine(_root, "Properties"));
        Directory.CreateDirectory(Path.Combine(_root, "Resources"));
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private string WriteResx(string data)
    {
        var path = Path.Combine(_root, "Properties", "Resources.resx");
        File.WriteAllText(path, $"<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<root>\n{data}\n</root>");
        return path;
    }

    private const string FileRefType = "System.Resources.ResXFileRef, System.Windows.Forms";

    [Fact]
    public void Read_FileReference_ResolvesAgainstTheResxFolder()
    {
        File.WriteAllBytes(Path.Combine(_root, "Resources", "logo.png"), [1, 2, 3]);
        var resources = ProjectResourcesReader.Read(WriteResx(
            $"<data name=\"logo\" type=\"{FileRefType}\"><value>..\\Resources\\logo.png;System.Drawing.Bitmap, System.Drawing</value></data>"));

        var entry = resources.Entries["logo"];
        Assert.Equal(ProjectResourceKind.File, entry.Kind);
        Assert.Equal("Bitmap", entry.TypeSimpleName);
        Assert.Equal(Path.Combine(_root, "Resources", "logo.png"), entry.FilePath);
        Assert.True(entry.IsImage);
    }

    [Fact]
    public void Read_MissingFileReference_KeepsTheEntryWithoutAPath()
    {
        var resources = ProjectResourcesReader.Read(WriteResx(
            $"<data name=\"gone\" type=\"{FileRefType}\"><value>..\\Resources\\gone.png;System.Drawing.Bitmap, System.Drawing</value></data>"));

        Assert.Null(resources.Entries["gone"].FilePath);
    }

    [Fact]
    public void Read_TextFileReference_IsAString()
    {
        File.WriteAllText(Path.Combine(_root, "Resources", "notes.txt"), "line one");
        var resources = ProjectResourcesReader.Read(WriteResx(
            $"<data name=\"notes\" type=\"{FileRefType}\"><value>..\\Resources\\notes.txt;System.String, mscorlib;Windows-1252</value></data>"));

        Assert.Equal(new ProjectResourceEntry("notes", ProjectResourceKind.Text, "String", Text: "line one"), resources.Entries["notes"]);
    }

    [Fact]
    public void Read_InlineString_AndBareNamesAreKept()
    {
        var resources = ProjectResourcesReader.Read(WriteResx(
            "<data name=\"Greeting\" xml:space=\"preserve\"><value>Hello</value></data>"));

        Assert.Equal(new ProjectResourceEntry("Greeting", ProjectResourceKind.Text, "String", Text: "Hello"), resources.Entries["Greeting"]);
    }

    [Fact]
    public void TryGet_FindsAnEntryByItsGeneratedMemberName()
    {
        var resources = ProjectResourcesReader.Read(WriteResx(
            "<data name=\"my-logo\" xml:space=\"preserve\"><value>x</value></data>"));

        Assert.True(resources.TryGet("my_logo", out var entry));
        Assert.Equal("my-logo", entry.Name);
    }

    [Theory]
    [InlineData("logo", "logo")]
    [InlineData("my-logo", "my_logo")]
    [InlineData("1st", "_1st")]
    [InlineData("class", "_class")]
    public void ToIdentifier_MatchesTheResourceGenerator(string name, string expected) =>
        Assert.Equal(expected, ProjectResourcesReader.ToIdentifier(name));

    [Theory]
    [InlineData("Properties/Resources.resx", true)]
    [InlineData("Properties/Resources.hu.resx", false)]
    [InlineData("Resources.resx", false)]
    [InlineData("Forms/MainForm.resx", false)]
    public void IsProjectResourceFile_IsPropertiesResourcesOnly(string relative, bool expected) =>
        Assert.Equal(expected, ProjectResourcesReader.IsProjectResourceFile(Path.Combine(_root, relative)));

    [Fact]
    public void Emit_WritesStringsAndImages_AndNamesWhatItSkipped()
    {
        File.WriteAllBytes(Path.Combine(_root, "Resources", "logo.png"), [1, 2, 3]);
        var resources = ProjectResourcesReader.Read(WriteResx(
            $"<data name=\"logo\" type=\"{FileRefType}\"><value>..\\Resources\\logo.png;System.Drawing.Bitmap, System.Drawing</value></data>\n"
            + "<data name=\"Greeting\" xml:space=\"preserve\"><value>Hello \"you\"</value></data>\n"
            + "<data name=\"Margin\" type=\"System.Drawing.Size, System.Drawing\"><value>4, 4</value></data>"));

        var carried = ProjectResourcesEmitter.Emit(resources, "Out");

        Assert.NotNull(carried.File);
        Assert.Equal("Properties/Resources.cs", carried.File.RelativePath);
        Assert.Contains("namespace Out.Properties;", carried.File.Text);
        Assert.Contains("public static string Greeting => \"Hello \\\"you\\\"\";", carried.File.Text);
        Assert.Contains("public static Bitmap logo => new(AssetLoader.Open(new Uri(\"avares://Out/Assets/Resources/logo.png\")));", carried.File.Text);
        Assert.DoesNotContain("Margin", carried.File.Text);
        Assert.Equal(["Greeting"], carried.Strings.Names);
        Assert.Equal("Out.Properties.Resources", carried.Strings.ClassExpression);
        Assert.Equal("Assets/Resources/logo.png", carried.AssetPaths["logo"]);
        Assert.Equal([1, 2, 3], carried.Assets["Assets/Resources/logo.png"]);
        Assert.Contains(carried.Warnings, w => w.Contains("'Margin'"));
    }

    [Fact]
    public void Emit_NoResourceFile_WritesNothing()
    {
        var carried = ProjectResourcesEmitter.Emit(ProjectResources.None, "Out");

        Assert.Null(carried.File);
        Assert.Empty(carried.Warnings);
    }
}
