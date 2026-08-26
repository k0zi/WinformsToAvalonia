using WinFormsToAvalonia.Core.Parsing;
using Xunit;

namespace WinFormsToAvalonia.Core.Tests.Parsing;

public class ResxImageExtractorTests
{
    /// <summary>A real 1x1 RGBA PNG (70 bytes).</summary>
    private const string RawPngBase64 =
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==";

    /// <summary>
    /// The same PNG as WinForms actually stores it: a BinaryFormatter byte[] preamble, the
    /// verbatim file, then the serializer's end-of-message marker.
    /// </summary>
    private const string BinaryFormatterWrappedPngBase64 =
        "AAEAAAD/////AQAAAAAAAAAHAQAAAAIAAAAJAgAAAA8CAAAARgAAAAKJUE5HDQoaCgAAAA1JSERSAAAAAQAAAAEIBgAA" +
        "AB8VxIkAAAANSURBVHjaY2Rg+F8PAAKHAYDrR7qSAAAAAElFTkSuQmCCCw==";

    [Fact]
    public void TryExtract_BinaryFormatterWrappedPng_RecoversTheFileFromInsideTheEnvelope()
    {
        Assert.True(ResxImageExtractor.TryExtract(BinaryFormatterWrappedPngBase64, out var image));

        Assert.Equal(".png", image.FileExtension);

        // The whole original file is there, starting at its own signature.
        var expected = Convert.FromBase64String(RawPngBase64);
        Assert.Equal(expected, image.Bytes[..expected.Length]);

        // Only the serializer's single end-of-message byte trails it.
        Assert.Equal(expected.Length + 1, image.Bytes.Length);
    }

    [Fact]
    public void TryExtract_RawPngWithNoEnvelope_IsRecoveredUnchanged()
    {
        Assert.True(ResxImageExtractor.TryExtract(RawPngBase64, out var image));

        Assert.Equal(Convert.FromBase64String(RawPngBase64), image.Bytes);
        Assert.Equal(".png", image.FileExtension);
    }

    [Theory]
    [InlineData(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 }, ".jpg")]
    [InlineData(new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 0x01, 0x00 }, ".gif")]
    [InlineData(new byte[] { 0x47, 0x49, 0x46, 0x38, 0x37, 0x61, 0x01, 0x00 }, ".gif")]
    public void TryExtract_OtherStrongMagics_AreRecognized(byte[] payload, string expectedExtension)
    {
        Assert.True(ResxImageExtractor.TryExtract(Convert.ToBase64String(payload), out var image));

        Assert.Equal(expectedExtension, image.FileExtension);
    }

    /// <summary>"BM" alone is two bytes of noise; the declared file size is what confirms it.</summary>
    [Fact]
    public void TryExtract_BitmapWithMatchingDeclaredSize_IsRecognized()
    {
        var bitmap = new byte[40];
        bitmap[0] = (byte)'B';
        bitmap[1] = (byte)'M';
        BitConverter.GetBytes(40).CopyTo(bitmap, 2);

        Assert.True(ResxImageExtractor.TryExtract(Convert.ToBase64String(bitmap), out var image));
        Assert.Equal(".bmp", image.FileExtension);
    }

    [Fact]
    public void TryExtract_BitmapWhoseDeclaredSizeDoesNotMatch_IsRejected()
    {
        var notABitmap = new byte[40];
        notABitmap[0] = (byte)'B';
        notABitmap[1] = (byte)'M';
        BitConverter.GetBytes(999_999).CopyTo(notABitmap, 2);

        Assert.False(ResxImageExtractor.TryExtract(Convert.ToBase64String(notABitmap), out _));
    }

    [Fact]
    public void TryExtract_IconWithAPlausibleDirectory_IsRecognized()
    {
        var icon = new byte[6 + 16];
        icon[2] = 0x01;
        icon[4] = 0x01; // one image in the directory

        Assert.True(ResxImageExtractor.TryExtract(Convert.ToBase64String(icon), out var image));
        Assert.Equal(".ico", image.FileExtension);
    }

    /// <summary>
    /// A run of zeroes looks like an icon header until the directory it promises is checked -
    /// this is why the weak magics are length-validated rather than trusted.
    /// </summary>
    [Fact]
    public void TryExtract_ZeroesTooShortForTheIconDirectory_AreRejected()
    {
        var zeroes = new byte[8];
        zeroes[2] = 0x01;
        zeroes[4] = 0x05; // claims five images, but there is no room for them

        Assert.False(ResxImageExtractor.TryExtract(Convert.ToBase64String(zeroes), out _));
    }

    /// <summary>
    /// A serializer preamble containing an icon-shaped run of zeroes must not beat the real PNG
    /// that follows it - strong magics are searched first for exactly this reason.
    /// </summary>
    [Fact]
    public void TryExtract_IconLookalikeBeforeARealPng_StillRecoversThePng()
    {
        var decoy = new byte[] { 0x00, 0x00, 0x01, 0x00, 0x02, 0x00 };
        var png = Convert.FromBase64String(RawPngBase64);
        var payload = decoy.Concat(new byte[64]).Concat(png).ToArray();

        Assert.True(ResxImageExtractor.TryExtract(Convert.ToBase64String(payload), out var image));

        Assert.Equal(".png", image.FileExtension);
        Assert.Equal(png, image.Bytes);
    }

    [Fact]
    public void TryExtract_UnrecognizedPayload_ReturnsFalse()
    {
        Assert.False(ResxImageExtractor.TryExtract(Convert.ToBase64String("just some text"u8.ToArray()), out _));
    }

    [Fact]
    public void TryExtract_InvalidBase64_ReturnsFalseInsteadOfThrowing()
    {
        Assert.False(ResxImageExtractor.TryExtract("not base64 at all !!!", out _));
    }
}
