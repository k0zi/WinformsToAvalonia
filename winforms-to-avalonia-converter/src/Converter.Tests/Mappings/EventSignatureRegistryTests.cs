using Converter.Mappings.BuiltIn;

namespace Converter.Tests.Mappings;

public class EventSignatureRegistryTests
{
    [Fact]
    public void GetSignature_GotFocus_V11_ReturnsGotFocusEventArgs()
    {
        var signature = EventSignatureRegistry.GetSignature("GotFocus", avaloniaMajorVersion: 11);

        Assert.Equal("Avalonia.Input.GotFocusEventArgs", signature.EventArgsType);
    }

    [Fact]
    public void GetSignature_GotFocus_V12_ReturnsFocusChangedEventArgs()
    {
        var signature = EventSignatureRegistry.GetSignature("GotFocus", avaloniaMajorVersion: 12);

        Assert.Equal("Avalonia.Input.FocusChangedEventArgs", signature.EventArgsType);
    }

    [Fact]
    public void GetSignature_LostFocus_V11_ReturnsRoutedEventArgs()
    {
        var signature = EventSignatureRegistry.GetSignature("LostFocus", avaloniaMajorVersion: 11);

        Assert.Equal("Avalonia.Interactivity.RoutedEventArgs", signature.EventArgsType);
    }

    [Fact]
    public void GetSignature_LostFocus_V12_ReturnsFocusChangedEventArgs()
    {
        var signature = EventSignatureRegistry.GetSignature("LostFocus", avaloniaMajorVersion: 12);

        Assert.Equal("Avalonia.Input.FocusChangedEventArgs", signature.EventArgsType);
    }

    [Fact]
    public void GetSignature_DefaultsToV12()
    {
        var signature = EventSignatureRegistry.GetSignature("GotFocus");

        Assert.Equal("Avalonia.Input.FocusChangedEventArgs", signature.EventArgsType);
    }

    [Theory]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(13)]
    public void GetSignature_UnaffectedEvent_SameAcrossVersions(int majorVersion)
    {
        var signature = EventSignatureRegistry.GetSignature("PointerPressed", majorVersion);

        Assert.Equal("Avalonia.Input.PointerPressedEventArgs", signature.EventArgsType);
    }

    [Theory]
    [InlineData("12.0.0", 12)]
    [InlineData("11.2.0", 11)]
    [InlineData("12.0.0-preview1", 12)]
    [InlineData("not-a-version", 11)]
    public void ParseMajorVersion_ParsesOrFallsBackTo11(string versionString, int expectedMajor)
    {
        Assert.Equal(expectedMajor, EventSignatureRegistry.ParseMajorVersion(versionString));
    }
}
