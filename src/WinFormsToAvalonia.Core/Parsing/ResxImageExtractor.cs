using System.Buffers.Binary;

namespace WinFormsToAvalonia.Core.Parsing;

/// <summary>The image bytes recovered from a .resx base64 payload, and the extension they belong in.</summary>
public sealed record ExtractedImage(byte[] Bytes, string FileExtension);

/// <summary>
/// Recovers the real image file out of a .resx base64 payload.
/// </summary>
/// <remarks>
/// WinForms stores a <c>PictureBox.Image</c> as a BinaryFormatter-serialized
/// <c>System.Drawing.Bitmap</c>, whose byte stream is a short object header followed by the
/// verbatim contents of the original .png/.jpg/.gif/.bmp file. BinaryFormatter cannot be run on
/// modern .NET (and would drag in System.Drawing.Common), so the file is located by scanning the
/// decoded bytes for a magic header instead - and the slice from there to the end of the payload
/// is the image.
///
/// Deliberately conservative: only formats with a header strong enough to identify with
/// confidence are recognized, and the two weaker ones (BMP, ICO) are additionally validated
/// against their own length/count fields so a run of zeroes inside the serializer's header
/// cannot masquerade as an image. Anything unrecognized returns false, and the caller reports
/// the asset as un-migrated rather than writing a file that would not decode.
///
/// The recovered slice can carry a byte or two of BinaryFormatter end-of-stream marker after the
/// image data. Every decoder in practice - Avalonia's included - stops at the format's own
/// end-of-image marker and ignores the tail.
/// </remarks>
public static class ResxImageExtractor
{
    /// <summary>
    /// How far into the payload a header may start. The serializer's preamble for a byte[] is on
    /// the order of a hundred bytes; scanning further would only raise the chance of matching
    /// something inside the pixel data itself.
    /// </summary>
    private const int MaxHeaderOffset = 512;

    private static readonly byte[] PngMagic = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] JpegMagic = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] Gif87Magic = "GIF87a"u8.ToArray();
    private static readonly byte[] Gif89Magic = "GIF89a"u8.ToArray();

    public static bool TryExtract(string base64, out ExtractedImage image)
    {
        image = null!;

        byte[] payload;
        try
        {
            payload = Convert.FromBase64String(base64);
        }
        catch (FormatException)
        {
            return false;
        }

        // Strong magics first: a false positive on one of these is essentially impossible, so
        // they must win over the length-validated formats below even at a later offset.
        var limit = Math.Min(payload.Length, MaxHeaderOffset);
        for (var offset = 0; offset < limit; offset++)
        {
            var extension = MatchStrongMagic(payload, offset);
            if (extension is not null)
            {
                image = new ExtractedImage(payload[offset..], extension);
                return true;
            }
        }

        for (var offset = 0; offset < limit; offset++)
        {
            if (IsBitmapHeader(payload, offset))
            {
                image = new ExtractedImage(payload[offset..], ".bmp");
                return true;
            }

            if (IsIconHeader(payload, offset))
            {
                image = new ExtractedImage(payload[offset..], ".ico");
                return true;
            }
        }

        return false;
    }

    private static string? MatchStrongMagic(byte[] payload, int offset)
    {
        if (StartsWith(payload, offset, PngMagic))
        {
            return ".png";
        }

        if (StartsWith(payload, offset, JpegMagic))
        {
            return ".jpg";
        }

        return StartsWith(payload, offset, Gif87Magic) || StartsWith(payload, offset, Gif89Magic) ? ".gif" : null;
    }

    /// <summary>"BM" is only two bytes, so the declared file size must corroborate it.</summary>
    private static bool IsBitmapHeader(byte[] payload, int offset)
    {
        if (offset + 6 > payload.Length || payload[offset] != (byte)'B' || payload[offset + 1] != (byte)'M')
        {
            return false;
        }

        var declaredSize = BinaryPrimitives.ReadUInt32LittleEndian(payload.AsSpan(offset + 2, 4));
        var available = (uint)(payload.Length - offset);

        // Allow a small BinaryFormatter tail after the bitmap, but nothing may be missing.
        return declaredSize >= 26 && declaredSize <= available && available - declaredSize <= 16;
    }

    /// <summary>An icon header is 00 00 01 00 plus an image count its directory must have room for.</summary>
    private static bool IsIconHeader(byte[] payload, int offset)
    {
        if (offset + 6 > payload.Length
            || payload[offset] != 0x00 || payload[offset + 1] != 0x00
            || payload[offset + 2] != 0x01 || payload[offset + 3] != 0x00)
        {
            return false;
        }

        var imageCount = BinaryPrimitives.ReadUInt16LittleEndian(payload.AsSpan(offset + 4, 2));
        return imageCount is >= 1 and <= 255 && payload.Length - offset >= 6 + (16 * imageCount);
    }

    private static bool StartsWith(byte[] payload, int offset, byte[] magic) =>
        offset + magic.Length <= payload.Length && payload.AsSpan(offset, magic.Length).SequenceEqual(magic);
}
