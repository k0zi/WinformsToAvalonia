using System.Buffers.Binary;
using System.IO.Compression;

namespace WinFormsToAvalonia.Core.Parsing;

/// <param name="Images">One PNG per image, in the order the ImageList held them.</param>
public sealed record ExtractedImageList(int ImageWidth, int ImageHeight, IReadOnlyList<byte[]> Images);

/// <summary>
/// Recovers the individual images from a WinForms <c>ImageList.ImageStream</c> resx payload.
/// </summary>
/// <remarks>
/// <para>
/// Unlike <see cref="ResxImageExtractor"/>, which finds a single image by scanning for a file
/// header, an ImageList is a real structure and has to be read as one: a
/// <c>BinaryFormatter</c> envelope (which is never deserialized - only searched for its marker),
/// then <c>MSFt</c>, then a run-length-encoded block holding comctl32's <c>ILHEAD</c>, a bitmap
/// with every image tiled into a grid, and a second 1bpp bitmap that is the transparency mask.
/// </para>
/// <para>
/// The format is not documented by Microsoft, so it was read off real payloads rather than off a
/// specification - see ImageListExtractorTests, which carries one built by the writer in its own
/// test support. That is worth being honest about: the shape below is what two independently
/// produced ImageStreams from real WinForms projects agree on, not a contract anyone published.
/// </para>
/// <para>
/// Output is PNG rather than BMP: the mask has to become an alpha channel for the images to look
/// right on anything but the original background, and BMP alpha is read inconsistently.
/// </para>
/// </remarks>
public static class ImageListExtractor
{
    private static ReadOnlySpan<byte> StreamMarker => "MSFt"u8;

    /// <summary>comctl32's ILHEAD magic - "IL" - which is what says the decompression worked.</summary>
    private const ushort HeaderMagic = 0x4C49;

    private const int HeaderLength = 28;

    /// <summary>The resx entry as it is stored: base64 around the BinaryFormatter envelope.</summary>
    public static ExtractedImageList? TryExtract(string base64)
    {
        try
        {
            return TryExtract(Convert.FromBase64String(base64));
        }
        catch (FormatException)
        {
            return null;
        }
    }

    public static ExtractedImageList? TryExtract(byte[] payload)
    {
        var start = IndexOf(payload, StreamMarker);
        if (start < 0)
        {
            return null;
        }

        var data = Decompress(payload.AsSpan(start + StreamMarker.Length));
        if (data.Length < HeaderLength
            || BinaryPrimitives.ReadUInt16LittleEndian(data) != HeaderMagic)
        {
            return null;
        }

        // ILHEAD: magic, version, cCurImage, cMaxImage, cGrow, cx, cy - two bytes each.
        var count = BinaryPrimitives.ReadInt16LittleEndian(data.AsSpan(4));
        var width = BinaryPrimitives.ReadInt16LittleEndian(data.AsSpan(10));
        var height = BinaryPrimitives.ReadInt16LittleEndian(data.AsSpan(12));

        if (count <= 0 || width <= 0 || height <= 0)
        {
            return null;
        }

        // The strip first, then the mask - two complete BMP files back to back.
        var stripOffset = IndexOf(data, "BM"u8, HeaderLength);
        if (stripOffset < 0 || Dib.TryRead(data, stripOffset) is not { } strip)
        {
            return null;
        }

        // Read at the strip's exact end rather than by scanning for the next "BM": the strip's own
        // pixels routinely contain that pair, and a colour that happens to spell it would put the
        // mask somewhere inside the image.
        var mask = Dib.TryRead(data, strip.EndOffset);

        var columns = strip.Width / width;
        if (columns <= 0)
        {
            return null;
        }

        var images = new List<byte[]>(count);
        for (var i = 0; i < count; i++)
        {
            var left = i % columns * width;
            var top = i / columns * height;
            if (left + width > strip.Width || top + height > strip.Height)
            {
                break;
            }

            images.Add(EncodePng(width, height, strip.ReadTile(left, top, width, height, mask)));
        }

        return images.Count == 0 ? null : new ExtractedImageList(width, height, images);
    }

    /// <summary>
    /// The run-length encoding the stream uses: pairs of (count, value), each expanding to
    /// <c>count</c> copies of that byte.
    /// </summary>
    private static byte[] Decompress(ReadOnlySpan<byte> compressed)
    {
        var output = new List<byte>(compressed.Length * 2);

        for (var i = 0; i + 1 < compressed.Length; i += 2)
        {
            for (var n = 0; n < compressed[i]; n++)
            {
                output.Add(compressed[i + 1]);
            }
        }

        return [.. output];
    }

    /// <summary>A BMP inside the stream, read far enough to pick pixels out of it.</summary>
    private sealed class Dib
    {
        private byte[] _data = [];
        private int _pixelOffset;
        private int _stride;
        private int _bitsPerPixel;
        private byte[] _palette = [];

        public int Width { get; private set; }

        public int Height { get; private set; }

        /// <summary>
        /// Where this bitmap ends in the stream, which is where to start looking for the next one.
        /// Computed rather than read: the size field in the file header is wrong in real payloads
        /// (54 for a 24bpp strip that is thousands of bytes long), so trusting it would put the
        /// mask's offset in the middle of the pixels.
        /// </summary>
        public int EndOffset => _pixelOffset + (_stride * Height);

        public static Dib? TryRead(byte[] data, int offset)
        {
            const int fileHeaderLength = 14;
            const int infoHeaderLength = 40;

            if (offset < 0 || offset + fileHeaderLength + infoHeaderLength > data.Length)
            {
                return null;
            }

            if (data[offset] != (byte)'B' || data[offset + 1] != (byte)'M')
            {
                return null;
            }

            var info = offset + fileHeaderLength;
            var width = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(info + 4));
            var height = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(info + 8));
            var bitsPerPixel = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(info + 14));

            if (width <= 0 || height == 0 || bitsPerPixel is not (1 or 4 or 8 or 24 or 32))
            {
                return null;
            }

            var paletteEntries = bitsPerPixel <= 8 ? 1 << bitsPerPixel : 0;
            var paletteStart = info + infoHeaderLength;
            var dib = new Dib
            {
                _data = data,
                Width = width,
                Height = Math.Abs(height),
                _bitsPerPixel = bitsPerPixel,
                _stride = ((width * bitsPerPixel + 31) / 32) * 4,
                _pixelOffset = paletteStart - offset + (paletteEntries * 4),
                _palette = paletteEntries == 0
                    ? []
                    : data.AsSpan(paletteStart, Math.Min(paletteEntries * 4, data.Length - paletteStart)).ToArray(),
            };

            // The pixel offset is relative to the bitmap's own start, so anchor it once here.
            dib._pixelOffset += offset;
            return dib._pixelOffset + (dib._stride * dib.Height) <= data.Length ? dib : null;
        }

        /// <summary>
        /// One image out of the grid, as straight BGRA. A set bit in the mask means transparent -
        /// which is what makes the extracted icons usable on any background rather than only on
        /// the one the original ImageList was drawn over.
        /// </summary>
        public byte[] ReadTile(int left, int top, int width, int height, Dib? mask)
        {
            var pixels = new byte[width * height * 4];

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var (b, g, r) = ReadPixel(left + x, top + y);
                    var transparent = mask?.IsMaskBitSet(left + x, top + y) ?? false;
                    var i = ((y * width) + x) * 4;

                    pixels[i] = r;
                    pixels[i + 1] = g;
                    pixels[i + 2] = b;
                    pixels[i + 3] = transparent ? (byte)0 : (byte)255;
                }
            }

            return pixels;
        }

        /// <summary>Rows are stored bottom-up unless the height is negative, which none of these are.</summary>
        private int RowStart(int y) => _pixelOffset + ((Height - 1 - y) * _stride);

        private (byte B, byte G, byte R) ReadPixel(int x, int y)
        {
            var row = RowStart(y);

            switch (_bitsPerPixel)
            {
                case 24:
                case 32:
                {
                    var i = row + (x * (_bitsPerPixel / 8));
                    return (_data[i], _data[i + 1], _data[i + 2]);
                }

                case 8:
                    return PaletteEntry(_data[row + x]);

                case 4:
                {
                    var packed = _data[row + (x / 2)];
                    return PaletteEntry(x % 2 == 0 ? (byte)(packed >> 4) : (byte)(packed & 0x0F));
                }

                default:
                    return PaletteEntry((byte)(IsBitSet(row, x) ? 1 : 0));
            }
        }

        private (byte B, byte G, byte R) PaletteEntry(byte index)
        {
            var i = index * 4;
            return i + 2 < _palette.Length ? (_palette[i], _palette[i + 1], _palette[i + 2]) : ((byte)0, (byte)0, (byte)0);
        }

        private bool IsMaskBitSet(int x, int y) => IsBitSet(RowStart(y), x);

        private bool IsBitSet(int rowStart, int x) => (_data[rowStart + (x / 8)] & (0x80 >> (x % 8))) != 0;
    }

    /// <summary>
    /// A minimal PNG writer: one IHDR, one zlib-compressed IDAT of unfiltered BGRA-as-RGBA
    /// scanlines, one IEND. Enough for a 16x16 icon, and it avoids taking an image library into a
    /// converter that otherwise only ever copies bytes.
    /// </summary>
    private static byte[] EncodePng(int width, int height, byte[] rgba)
    {
        var raw = new byte[height * ((width * 4) + 1)];
        for (var y = 0; y < height; y++)
        {
            raw[y * ((width * 4) + 1)] = 0;
            Array.Copy(rgba, y * width * 4, raw, (y * ((width * 4) + 1)) + 1, width * 4);
        }

        using var compressed = new MemoryStream();
        using (var deflate = new ZLibStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
        {
            deflate.Write(raw);
        }

        using var png = new MemoryStream();
        png.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

        var header = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header, width);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), height);
        header[8] = 8;   // bit depth
        header[9] = 6;   // colour type: truecolour with alpha

        WriteChunk(png, "IHDR"u8, header);
        WriteChunk(png, "IDAT"u8, compressed.ToArray());
        WriteChunk(png, "IEND"u8, []);
        return png.ToArray();
    }

    private static void WriteChunk(Stream stream, ReadOnlySpan<byte> type, byte[] data)
    {
        Span<byte> length = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(length, data.Length);
        stream.Write(length);

        var body = new byte[type.Length + data.Length];
        type.CopyTo(body);
        data.CopyTo(body.AsSpan(type.Length));
        stream.Write(body);

        Span<byte> crc = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(crc, Crc32(body));
        stream.Write(crc);
    }

    private static readonly uint[] CrcTable = BuildCrcTable();

    private static uint[] BuildCrcTable()
    {
        var table = new uint[256];

        for (var n = 0u; n < 256; n++)
        {
            var c = n;
            for (var k = 0; k < 8; k++)
            {
                c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
            }

            table[n] = c;
        }

        return table;
    }

    private static uint Crc32(ReadOnlySpan<byte> data)
    {
        var c = 0xFFFFFFFFu;
        foreach (var b in data)
        {
            c = CrcTable[(c ^ b) & 0xFF] ^ (c >> 8);
        }

        return c ^ 0xFFFFFFFFu;
    }

    private static int IndexOf(ReadOnlySpan<byte> haystack, ReadOnlySpan<byte> needle, int start = 0)
    {
        var found = haystack[start..].IndexOf(needle);
        return found < 0 ? -1 : found + start;
    }
}
