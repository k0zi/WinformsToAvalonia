using System.Buffers.Binary;

namespace WinFormsToAvalonia.Core.Tests.TestSupport;

/// <summary>
/// Builds an <c>ImageList.ImageStream</c> resx payload in the shape WinForms writes one.
/// </summary>
/// <remarks>
/// <para>
/// A real payload could not be committed here. The two this format was read off are files in
/// other people's repositories under their own licences (GPL and MS-PL), and vendoring a binary
/// blob out of them into this project to use as test data is not something a licence audit should
/// have to discover later. Producing one instead needs WinForms, which does not run on the machine
/// this converter is developed on.
/// </para>
/// <para>
/// So this writes one. That makes the round-trip test a real test of the extractor - the RLE, the
/// ILHEAD field offsets, the tiling, the mask-to-alpha step and the PNG output all have to be
/// right for it to pass - and explicitly *not* a test of "is this the shape WinForms emits". That
/// half was checked by hand against those two real payloads, and is the reason the writer looks
/// the way it does.
/// </para>
/// </remarks>
public static class ImageListStreamWriter
{
    /// <summary>How many tiles a row of the strip holds, matching the real payloads this was read off.</summary>
    private const int Columns = 4;

    /// <param name="tiles">One colour per image, in ImageList order.</param>
    /// <param name="transparentCorner">
    /// When set, the top-left pixel of every tile is marked transparent in the mask - which is how
    /// the test tells a mask that was applied from one that was ignored.
    /// </param>
    /// <remarks>
    /// The strip is sized for the list's *capacity*, not its count, and laid out in rows - the
    /// shape a real ImageList has, and the one that makes the count field matter. A single-row
    /// strip sized exactly to the tiles cannot tell a reader that gets the count wrong from one
    /// that gets it right: the extractor's own bounds check stops it at the edge either way.
    /// </remarks>
    public static string Write(
        IReadOnlyList<(byte R, byte G, byte B)> tiles, int width, int height, bool transparentCorner)
    {
        var capacity = tiles.Count + 3;
        var stripWidth = Columns * width;
        var stripHeight = ((capacity + Columns - 1) / Columns) * height;

        var stream = new List<byte>();
        stream.AddRange(Header(tiles.Count, capacity, width, height));
        stream.AddRange(Bitmap24(tiles, stripWidth, stripHeight, width, height));
        stream.AddRange(Mask(tiles.Count, stripWidth, stripHeight, width, height, transparentCorner));

        // The BinaryFormatter envelope is never parsed - the reader scans for the marker - so an
        // arbitrary preamble in front of it is exactly as good as the real serializer's.
        var payload = new List<byte>();
        payload.AddRange(new byte[64]);
        payload.AddRange("MSFt"u8.ToArray());
        payload.AddRange(RunLengthEncode(stream));
        return Convert.ToBase64String([.. payload]);
    }

    private static byte[] Header(int count, int capacity, int width, int height)
    {
        var header = new byte[28];
        BinaryPrimitives.WriteUInt16LittleEndian(header, 0x4C49);            // "IL"
        BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(2), 0x0101);  // version
        BinaryPrimitives.WriteInt16LittleEndian(header.AsSpan(4), (short)count);

        // Every field after cCurImage gets a value of its own, none of them equal to another.
        // Written the obvious way - capacity == count, cGrow == cx - a misread offset lands on a
        // field that happens to hold the right number and the test passes anyway; that is exactly
        // how the first version of the reader read cCurImage out of cMaxImage undetected.
        BinaryPrimitives.WriteInt16LittleEndian(header.AsSpan(6), (short)capacity);
        BinaryPrimitives.WriteInt16LittleEndian(header.AsSpan(8), 7);        // cGrow
        BinaryPrimitives.WriteInt16LittleEndian(header.AsSpan(10), (short)width);
        BinaryPrimitives.WriteInt16LittleEndian(header.AsSpan(12), (short)height);
        return header;
    }

    private static byte[] Bitmap24(
        IReadOnlyList<(byte R, byte G, byte B)> tiles,
        int stripWidth,
        int stripHeight,
        int tileWidth,
        int tileHeight)
    {
        var stride = ((stripWidth * 24) + 31) / 32 * 4;
        var pixels = new byte[stride * stripHeight];

        for (var y = 0; y < stripHeight; y++)
        {
            for (var x = 0; x < stripWidth; x++)
            {
                var tile = ((y / tileHeight) * Columns) + (x / tileWidth);

                // Slots past the end of the list are the capacity the ImageList has not used yet.
                // Left as they come out of the allocation - black - which is what a reader that
                // takes the capacity for the count would hand back as an image.
                if (tile >= tiles.Count)
                {
                    continue;
                }

                var (r, g, b) = tiles[tile];
                var i = ((stripHeight - 1 - y) * stride) + (x * 3);
                pixels[i] = b;
                pixels[i + 1] = g;
                pixels[i + 2] = r;
            }
        }

        return [.. BitmapHeaders(stripWidth, stripHeight, 24, paletteEntries: 0), .. pixels];
    }

    private static byte[] Mask(
        int count, int stripWidth, int stripHeight, int tileWidth, int tileHeight, bool transparentCorner)
    {
        var stride = ((stripWidth * 1) + 31) / 32 * 4;
        var pixels = new byte[stride * stripHeight];

        if (transparentCorner)
        {
            for (var tile = 0; tile < count; tile++)
            {
                var x = tile % Columns * tileWidth;
                var y = tile / Columns * tileHeight;
                var row = (stripHeight - 1 - y) * stride;
                pixels[row + (x / 8)] |= (byte)(0x80 >> (x % 8));
            }
        }

        var palette = new byte[8];
        palette[4] = palette[5] = palette[6] = 0xFF;
        return [.. BitmapHeaders(stripWidth, stripHeight, 1, paletteEntries: 2), .. palette, .. pixels];
    }

    private static byte[] BitmapHeaders(int width, int height, int bitsPerPixel, int paletteEntries)
    {
        var headers = new byte[54];
        headers[0] = (byte)'B';
        headers[1] = (byte)'M';
        BinaryPrimitives.WriteUInt32LittleEndian(headers.AsSpan(10), (uint)(54 + (paletteEntries * 4)));
        BinaryPrimitives.WriteUInt32LittleEndian(headers.AsSpan(14), 40);
        BinaryPrimitives.WriteInt32LittleEndian(headers.AsSpan(18), width);
        BinaryPrimitives.WriteInt32LittleEndian(headers.AsSpan(22), height);
        BinaryPrimitives.WriteUInt16LittleEndian(headers.AsSpan(26), 1);
        BinaryPrimitives.WriteUInt16LittleEndian(headers.AsSpan(28), (ushort)bitsPerPixel);
        return headers;
    }

    /// <summary>Pairs of (count, value), with a run never longer than a byte can say.</summary>
    private static byte[] RunLengthEncode(IReadOnlyList<byte> data)
    {
        var encoded = new List<byte>(data.Count * 2);

        for (var i = 0; i < data.Count;)
        {
            var run = 1;
            while (run < 255 && i + run < data.Count && data[i + run] == data[i])
            {
                run++;
            }

            encoded.Add((byte)run);
            encoded.Add(data[i]);
            i += run;
        }

        return [.. encoded];
    }
}
