using WinFormsToAvalonia.Cli.Commands;
using Xunit;

namespace WinFormsToAvalonia.Integration.Tests.Rendering;

/// <summary>
/// `convert` has taken a solution since multi-project support landed; `analyze` accepting only a
/// `.csproj` meant you could not preview the very thing you were about to convert.
/// </summary>
public class AnalyzeSourceValidationTests
{
    [Theory]
    [InlineData("MultiProjectSolution.slnx")]
    public void Validate_SolutionSource_IsAccepted(string fileName)
    {
        var source = Path.Combine(AppContext.BaseDirectory, "SampleApps", "MultiProjectSolution", fileName);

        var result = new AnalyzeCommandSettings { Source = source }.Validate();

        Assert.True(result.Successful, result.Message);
    }

    [Fact]
    public void Validate_SomethingThatIsNeither_IsRejected()
    {
        var result = new AnalyzeCommandSettings { Source = "app.txt" }.Validate();

        Assert.False(result.Successful);
        Assert.Contains(".csproj, .sln or .slnx", result.Message);
    }
}
