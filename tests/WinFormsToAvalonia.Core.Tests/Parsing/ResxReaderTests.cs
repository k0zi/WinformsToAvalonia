using WinFormsToAvalonia.Core.Parsing;
using WinFormsToAvalonia.Core.Tests.TestSupport;
using Xunit;

namespace WinFormsToAvalonia.Core.Tests.Parsing;

public class ResxReaderTests
{
    private const string ResxBody = """
        <?xml version="1.0" encoding="utf-8"?>
        <root>
          <resheader name="resmimetype">
            <value>text/microsoft-resx</value>
          </resheader>
          <metadata name="button1.TrayLocation" type="System.Drawing.Point, System.Drawing">
            <value>17, 17</value>
          </metadata>
          <data name="button1.Text" xml:space="preserve">
            <value>OK</value>
          </data>
          <data name="button1.Location" type="System.Drawing.Point, System.Drawing">
            <value>12, 12</value>
          </data>
          <data name="&gt;&gt;button1.Name" xml:space="preserve">
            <value>button1</value>
          </data>
          <data name="pictureBox1.Image" type="System.Drawing.Bitmap, System.Drawing" mimetype="application/x-microsoft.net.object.bytearray.base64">
            <value>AAEC</value>
          </data>
          <data name="$this.Text" xml:space="preserve">
            <value>My Form</value>
          </data>
        </root>
        """;

    [Fact]
    public void Read_ReturnsDataEntriesGroupedByOwner()
    {
        using var fixture = TempProjectFixture.Create();
        fixture.WriteFile("MainForm.resx", ResxBody);

        var document = new ResxReader().Read(fixture.PathTo("MainForm.resx"));

        var button = document.EntriesFor("button1").ToList();
        Assert.Equal(["Text", "Location"], button.Select(e => e.PropertyName));
        Assert.Equal("OK", button[0].Value);
        Assert.Null(button[0].TypeName);
        Assert.Equal("Point", button[1].TypeSimpleName);
    }

    /// <summary>`metadata` is the designer surface (tray positions), not a property of the form.</summary>
    [Fact]
    public void Read_SkipsMetadataAndResheaderElements()
    {
        using var fixture = TempProjectFixture.Create();
        fixture.WriteFile("MainForm.resx", ResxBody);

        var document = new ResxReader().Read(fixture.PathTo("MainForm.resx"));

        Assert.DoesNotContain(document.EntriesFor("button1"), e => e.PropertyName == "TrayLocation");
    }

    /// <summary>`&gt;&gt;`-prefixed entries are the designer's bookkeeping about the field, not properties to set.</summary>
    [Fact]
    public void Read_SkipsDesignerBookkeepingEntries()
    {
        using var fixture = TempProjectFixture.Create();
        fixture.WriteFile("MainForm.resx", ResxBody);

        var document = new ResxReader().Read(fixture.PathTo("MainForm.resx"));

        Assert.DoesNotContain(document.EntriesFor(">>button1"), _ => true);
        Assert.DoesNotContain(document.EntriesFor("button1"), e => e.Name.StartsWith(">>", StringComparison.Ordinal));
    }

    [Fact]
    public void Read_RecognizesTheFormsOwnEntriesUnderTheDollarThisKey()
    {
        using var fixture = TempProjectFixture.Create();
        fixture.WriteFile("MainForm.resx", ResxBody);

        var document = new ResxReader().Read(fixture.PathTo("MainForm.resx"));

        var formEntry = Assert.Single(document.EntriesFor(ResxDocument.FormOwnerKey));
        Assert.Equal("Text", formEntry.PropertyName);
        Assert.Equal("My Form", formEntry.Value);
    }

    [Fact]
    public void Read_FlagsBase64PayloadsAsBinary()
    {
        using var fixture = TempProjectFixture.Create();
        fixture.WriteFile("MainForm.resx", ResxBody);

        var document = new ResxReader().Read(fixture.PathTo("MainForm.resx"));

        var image = Assert.Single(document.EntriesFor("pictureBox1"));
        Assert.True(image.IsBinary);
        Assert.Equal("Bitmap", image.TypeSimpleName);
    }

    [Fact]
    public void Read_MissingFile_ReturnsEmptyDocument()
    {
        var document = new ResxReader().Read("/no/such/file.resx");

        Assert.Same(ResxDocument.Empty, document);
    }

    /// <summary>A broken resource file must degrade to "no resources", never fail the conversion.</summary>
    [Fact]
    public void Read_MalformedXml_ReturnsEmptyDocumentInsteadOfThrowing()
    {
        using var fixture = TempProjectFixture.Create();
        fixture.WriteFile("Broken.resx", "<root><data name=\"a.b\"><value>oops");

        var document = new ResxReader().Read(fixture.PathTo("Broken.resx"));

        Assert.Same(ResxDocument.Empty, document);
    }
}
