using System.Buffers.Binary;
using System.IO.Compression;
using WinFormsToAvalonia.Core.Parsing;
using WinFormsToAvalonia.Core.Tests.TestSupport;

namespace WinFormsToAvalonia.Core.Tests.Parsing;

/// <summary>
/// The ImageList reader, against payloads built by <see cref="ImageListStreamWriter"/>.
/// </summary>
/// <remarks>
/// See that writer for why the input is synthesized rather than committed, and for what this
/// suite therefore does and does not prove.
/// </remarks>
public class ImageListExtractorTests
{
    private static readonly (byte R, byte G, byte B)[] ThreeColours =
    [
        (0xFF, 0x00, 0x00),
        (0x00, 0xFF, 0x00),
        (0x00, 0x00, 0xFF),
    ];

    [Fact]
    public void Extract_RecoversOneImagePerEntry()
    {
        var payload = ImageListStreamWriter.Write(ThreeColours, 16, 16, transparentCorner: false);

        var result = ImageListExtractor.TryExtract(payload);

        Assert.NotNull(result);
        Assert.Equal(3, result.Images.Count);
        Assert.Equal(16, result.ImageWidth);
        Assert.Equal(16, result.ImageHeight);
    }

    /// <summary>
    /// The images have to come out in the order the ImageList held them, or every
    /// <c>ImageIndex</c> the conversion resolves points at the wrong picture - which nothing
    /// downstream could notice.
    /// </summary>
    [Theory]
    [InlineData(0, 0xFF, 0x00, 0x00)]
    [InlineData(1, 0x00, 0xFF, 0x00)]
    [InlineData(2, 0x00, 0x00, 0xFF)]
    public void Extract_KeepsImageOrderAndColour(int index, byte red, byte green, byte blue)
    {
        var payload = ImageListStreamWriter.Write(ThreeColours, 16, 16, transparentCorner: false);

        var image = ReadPng(ImageListExtractor.TryExtract(payload)!.Images[index]);

        Assert.Equal((red, green, blue, (byte)255), image.PixelAt(8, 8));
    }

    /// <summary>
    /// The mask is the only thing that makes an extracted icon usable: WinForms drew these over
    /// the control's own background, so without it every icon arrives as a rectangle of whatever
    /// colour the original form happened to be.
    /// </summary>
    [Fact]
    public void Extract_TurnsTheMaskIntoAnAlphaChannel()
    {
        var payload = ImageListStreamWriter.Write(ThreeColours, 16, 16, transparentCorner: true);

        var image = ReadPng(ImageListExtractor.TryExtract(payload)!.Images[1]);

        Assert.Equal((byte)0, image.PixelAt(0, 0).A);
        Assert.Equal((byte)255, image.PixelAt(8, 8).A);
    }

    /// <summary>
    /// Six images do not fit in one row of the strip, so this is what says the reader walks the
    /// grid rather than a line - the shape every real ImageList of more than a few images has.
    /// </summary>
    [Fact]
    public void Extract_WalksTheStripAsAGrid()
    {
        (byte, byte, byte)[] colours =
        [
            (0x10, 0x00, 0x00), (0x20, 0x00, 0x00), (0x30, 0x00, 0x00),
            (0x40, 0x00, 0x00), (0x50, 0x00, 0x00), (0x60, 0x00, 0x00),
        ];
        var payload = ImageListStreamWriter.Write(colours, 16, 16, transparentCorner: false);

        var result = ImageListExtractor.TryExtract(payload);

        Assert.NotNull(result);
        Assert.Equal(6, result.Images.Count);
        Assert.Equal(
            colours,
            result.Images.Select(bytes => ReadPng(bytes).PixelAt(8, 8) is var p ? (p.R, p.G, p.B) : default));
    }

    /// <summary>
    /// 0x42 0x4D is "BM", and a strip of this colour is full of it. The mask has to be found by
    /// where the strip ends, not by looking for the next bitmap signature.
    /// </summary>
    [Fact]
    public void Extract_FindsTheMaskPastPixelsThatSpellABitmapHeader()
    {
        var payload = ImageListStreamWriter.Write(
            [(0x00, 0x4D, 0x42), (0x00, 0x4D, 0x42)], 16, 16, transparentCorner: true);

        var image = ReadPng(ImageListExtractor.TryExtract(payload)!.Images[0]);

        Assert.Equal((byte)0, image.PixelAt(0, 0).A);
        Assert.Equal(((byte)0x00, (byte)0x4D, (byte)0x42, (byte)255), image.PixelAt(8, 8));
    }

    [Fact]
    public void Extract_HandlesNonDefaultImageSizes()
    {
        var payload = ImageListStreamWriter.Write(ThreeColours, 24, 32, transparentCorner: false);

        var result = ImageListExtractor.TryExtract(payload);

        Assert.NotNull(result);
        Assert.Equal(24, result.ImageWidth);
        Assert.Equal(32, result.ImageHeight);
        Assert.All(result.Images, bytes => Assert.Equal((24, 32), (ReadPng(bytes).Width, ReadPng(bytes).Height)));
    }

    [Fact]
    public void Extract_EmitsRealPngFiles()
    {
        var payload = ImageListStreamWriter.Write(ThreeColours, 16, 16, transparentCorner: true);

        var bytes = ImageListExtractor.TryExtract(payload)!.Images[0];

        Assert.Equal<byte[]>([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A], bytes[..8]);
    }

    /// <summary>
    /// An unreadable payload has to say so rather than produce something: the pipeline turns a
    /// null into a warning naming the field, and a wrong image would be silently wrong instead.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("bm90IGEgc3RyZWFtIGF0IGFsbA==")]
    [InlineData("not base64 at all")]
    public void Extract_RefusesAPayloadItCannotRead(string payload)
    {
        Assert.Null(ImageListExtractor.TryExtract(payload));
    }

    /// <summary>The marker is there, but nothing behind it decompresses to an ILHEAD.</summary>
    [Fact]
    public void Extract_RefusesAStreamWithoutAnImageListHeader()
    {
        var payload = Convert.ToBase64String([.. "MSFt"u8.ToArray(), .. new byte[64]]);

        Assert.Null(ImageListExtractor.TryExtract(payload));
    }

    private sealed record DecodedPng(int Width, int Height, byte[] Rgba)
    {
        public (byte R, byte G, byte B, byte A) PixelAt(int x, int y)
        {
            var i = ((y * Width) + x) * 4;
            return (Rgba[i], Rgba[i + 1], Rgba[i + 2], Rgba[i + 3]);
        }
    }

    /// <summary>
    /// Reads back exactly the PNG subset the extractor writes - 8-bit RGBA, one IDAT, filter 0.
    /// Deliberately not tolerant: if the extractor ever writes something else, this should fail
    /// rather than quietly cope.
    /// </summary>
    private static DecodedPng ReadPng(byte[] png)
    {
        var width = 0;
        var height = 0;
        var idat = new MemoryStream();

        for (var offset = 8; offset + 12 <= png.Length;)
        {
            var length = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(offset));
            var type = System.Text.Encoding.ASCII.GetString(png, offset + 4, 4);
            var body = png.AsSpan(offset + 8, length);

            switch (type)
            {
                case "IHDR":
                    width = BinaryPrimitives.ReadInt32BigEndian(body);
                    height = BinaryPrimitives.ReadInt32BigEndian(body[4..]);
                    Assert.Equal(8, body[8]);
                    Assert.Equal(6, body[9]);
                    break;
                case "IDAT":
                    idat.Write(body);
                    break;
            }

            offset += 12 + length;
        }

        idat.Position = 0;
        using var inflate = new ZLibStream(idat, CompressionMode.Decompress);
        using var raw = new MemoryStream();
        inflate.CopyTo(raw);

        var scanlines = raw.ToArray();
        var pixels = new byte[width * height * 4];
        for (var y = 0; y < height; y++)
        {
            Assert.Equal(0, scanlines[y * ((width * 4) + 1)]);
            Array.Copy(scanlines, (y * ((width * 4) + 1)) + 1, pixels, y * width * 4, width * 4);
        }

        return new DecodedPng(width, height, pixels);
    }
}
