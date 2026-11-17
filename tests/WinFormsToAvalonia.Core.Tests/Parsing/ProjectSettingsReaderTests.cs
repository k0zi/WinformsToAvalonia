using WinFormsToAvalonia.Core.Emission;
using WinFormsToAvalonia.Core.Parsing;
using Xunit;

namespace WinFormsToAvalonia.Core.Tests.Parsing;

public sealed class ProjectSettingsReaderTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), "w2a-settings-" + Guid.NewGuid() + ".settings");

    public void Dispose() => File.Delete(_path);

    private ProjectSettings Read(string settings)
    {
        File.WriteAllText(_path,
            "<?xml version='1.0' encoding='utf-8'?>\n"
            + "<SettingsFile xmlns=\"http://schemas.microsoft.com/VisualStudio/2004/01/settings\" CurrentProfile=\"(Default)\">\n"
            + $"<Profiles /><Settings>{settings}</Settings></SettingsFile>");
        return ProjectSettingsReader.Read(_path);
    }

    [Fact]
    public void Read_TypeScopeAndDefault()
    {
        var settings = Read(
            "<Setting Name=\"UserName\" Type=\"System.String\" Scope=\"User\"><Value Profile=\"(Default)\">guest</Value></Setting>"
            + "<Setting Name=\"Url\" Type=\"System.String\" Scope=\"Application\"><Value Profile=\"(Default)\">x</Value></Setting>");

        Assert.Equal(
            [
                new ProjectSettingEntry("UserName", "System.String", true, "guest"),
                new ProjectSettingEntry("Url", "System.String", false, "x"),
            ],
            settings.Entries);
    }

    [Fact]
    public void Read_NoFile_IsNone() =>
        Assert.Empty(ProjectSettingsReader.Read(_path + ".missing").Entries);

    [Theory]
    [InlineData("System.String", "a \"b\"", "\"a \\\"b\\\"\"")]
    [InlineData("System.String", null, "\"\"")]
    [InlineData("System.Boolean", "True", "true")]
    [InlineData("System.Int32", "42", "42")]
    [InlineData("System.Int64", "42", "42L")]
    [InlineData("System.Double", "0.75", "0.75d")]
    [InlineData("System.Single", "1.5", "1.5f")]
    [InlineData("System.Decimal", "9.99", "9.99m")]
    [InlineData("System.TimeSpan", "00:00:01", "new TimeSpan(10000000L)")]
    [InlineData("System.Int32", "", "default")]
    [InlineData("System.Int32", "many", null)]
    [InlineData("System.Drawing.Color", "Red", null)]
    public void DefaultLiteral_IsACSharpLiteralOfTheType(string type, string? value, string? expected) =>
        Assert.Equal(expected, ProjectSettingsEmitter.DefaultLiteral(type, value));

    [Fact]
    public void Emit_UserScopeIsWritable_ApplicationScopeIsNot_AndUnsupportedIsNamed()
    {
        var settings = Read(
            "<Setting Name=\"UserName\" Type=\"System.String\" Scope=\"User\"><Value Profile=\"(Default)\">guest</Value></Setting>"
            + "<Setting Name=\"Url\" Type=\"System.String\" Scope=\"Application\"><Value Profile=\"(Default)\">x</Value></Setting>"
            + "<Setting Name=\"Accent\" Type=\"System.Drawing.Color\" Scope=\"User\"><Value Profile=\"(Default)\">Red</Value></Setting>"
            + "<Setting Name=\"Save\" Type=\"System.Boolean\" Scope=\"User\"><Value Profile=\"(Default)\">True</Value></Setting>");

        var carried = ProjectSettingsEmitter.Emit(settings, "Out");

        Assert.NotNull(carried.File);
        Assert.Equal("Properties/Settings.cs", carried.File.RelativePath);
        Assert.Contains("public string UserName { get; set; } = \"guest\";", carried.File.Text);
        Assert.Contains("[JsonIgnore]\n    public string Url { get; } = \"x\";", carried.File.Text);
        Assert.Contains("        UserName = other.UserName;\n", carried.File.Text);
        Assert.DoesNotContain("Url = other.Url", carried.File.Text);
        Assert.Equal("Out.Properties.Settings.Default", carried.Access.InstanceExpression);
        Assert.Equal(["Url", "UserName"], carried.Access.Readable.Order(StringComparer.Ordinal));
        Assert.Equal(["UserName"], carried.Access.Writable);
        Assert.Contains(carried.Warnings, w => w.Contains("'Accent' (System.Drawing.Color)") && w.Contains("'Save' (System.Boolean)"));
    }

    [Fact]
    public void Emit_NoSettings_WritesNothing()
    {
        var carried = ProjectSettingsEmitter.Emit(ProjectSettings.None, "Out");

        Assert.Null(carried.File);
        Assert.True(carried.Access.IsEmpty);
        Assert.Empty(carried.Warnings);
    }
}
