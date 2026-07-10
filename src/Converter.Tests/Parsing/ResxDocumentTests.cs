using Converter.Core.Parsing;

namespace Converter.Tests.Parsing;

public class ResxDocumentTests
{
    private static async Task<string> WriteResxAsync(string content)
    {
        var dir = Directory.CreateTempSubdirectory("resx-test-").FullName;
        var path = Path.Combine(dir, "Sample.resx");
        await File.WriteAllTextAsync(path, content);
        return path;
    }

    [Fact]
    public async Task LoadAsync_PlainStringEntry_ResolvesStringValue()
    {
        const string resx = """
            <?xml version="1.0" encoding="utf-8"?>
            <root>
                <data name="button1.Text" xml:space="preserve">
                    <value>Click Me</value>
                </data>
            </root>
            """;

        var path = await WriteResxAsync(resx);
        try
        {
            var entries = await ResxDocument.LoadAsync(path);

            var entry = Assert.Contains("button1.Text", entries);
            Assert.Equal("Click Me", entry.StringValue);
            Assert.Null(entry.BinaryValue);
            Assert.Null(entry.ExternalFilePath);
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }

    [Fact]
    public async Task LoadAsync_ResXFileRefEntry_ResolvesExternalFilePath()
    {
        var dir = Directory.CreateTempSubdirectory("resx-test-").FullName;
        var imagePath = Path.Combine(dir, "icon.png");
        await File.WriteAllBytesAsync(imagePath, [0x89, 0x50, 0x4E, 0x47]);

        var resx = $"""
            <?xml version="1.0" encoding="utf-8"?>
            <root>
                <data name="pictureBox1.Image" type="System.Resources.ResXFileRef, System.Windows.Forms">
                    <value>icon.png;System.Drawing.Bitmap, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a</value>
                </data>
            </root>
            """;

        var resxPath = Path.Combine(dir, "Sample.resx");
        await File.WriteAllTextAsync(resxPath, resx);

        try
        {
            var entries = await ResxDocument.LoadAsync(resxPath);

            var entry = Assert.Contains("pictureBox1.Image", entries);
            Assert.NotNull(entry.ExternalFilePath);
            Assert.Equal(imagePath, entry.ExternalFilePath);
            Assert.True(File.Exists(entry.ExternalFilePath));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task LoadAsync_Base64ImageEntry_ResolvesBinaryValue()
    {
        var pngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        var base64 = Convert.ToBase64String(pngBytes);

        var resx = $"""
            <?xml version="1.0" encoding="utf-8"?>
            <root>
                <data name="button1.Image" type="System.Drawing.Bitmap, System.Drawing" mimetype="application/x-microsoft.net.object.bytearray.base64">
                    <value>{base64}</value>
                </data>
            </root>
            """;

        var path = await WriteResxAsync(resx);
        try
        {
            var entries = await ResxDocument.LoadAsync(path);

            var entry = Assert.Contains("button1.Image", entries);
            Assert.False(entry.IsBinaryFormatterEnvelope);
            Assert.NotNull(entry.BinaryValue);
            Assert.Equal(pngBytes, entry.BinaryValue);
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }

    [Fact]
    public async Task LoadAsync_BinaryFormatterEnvelope_IsFlaggedAsUnrecoverable()
    {
        var fakeSerializedBytes = new byte[] { 0x00, 0x01, 0x02, 0x03 };
        var base64 = Convert.ToBase64String(fakeSerializedBytes);

        var resx = $"""
            <?xml version="1.0" encoding="utf-8"?>
            <root>
                <data name="$this.Icon" type="System.Drawing.Icon, System.Drawing" mimetype="application/x-microsoft.net.object.binary.base64">
                    <value>{base64}</value>
                </data>
            </root>
            """;

        var path = await WriteResxAsync(resx);
        try
        {
            var entries = await ResxDocument.LoadAsync(path);

            var entry = Assert.Contains("$this.Icon", entries);
            Assert.True(entry.IsBinaryFormatterEnvelope);
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }
}
