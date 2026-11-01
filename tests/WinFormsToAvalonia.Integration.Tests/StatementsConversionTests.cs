using WinFormsToAvalonia.Core.Pipeline;
using WinFormsToAvalonia.Integration.Tests.TestSupport;
using Xunit;

namespace WinFormsToAvalonia.Integration.Tests;

/// <summary>
/// try/catch/finally, switch, do/while, break/continue: plain C# that used to stop a handler at its
/// first line. Each is translated whole, like an <c>if</c>; this checks that nothing is left
/// unfinished and that the result - on the View and on a ViewModel, imports included - builds.
/// </summary>
public class StatementsConversionTests
{
    [Fact]
    public async Task ConvertedStatementsApp_TranslatesEveryHandlerAndBuilds()
    {
        var sourceProject = Path.Combine(AppContext.BaseDirectory, "SampleApps", "StatementsApp", "StatementsApp.csproj");
        var outputDir = Path.Combine(Path.GetTempPath(), "w2a-statements-" + Guid.NewGuid());
        try
        {
            var result = new ConversionPipeline().Run(new ConversionOptions(sourceProject, outputDir));
            Assert.True(result.Vfs.TryGetText("MIGRATION.md", out var checklist));
            Assert.Contains("## Nothing left", checklist);

            var generated = string.Join("\n", result.Vfs.RelativePaths
                .Where(p => p.EndsWith(".cs", StringComparison.Ordinal) && (p.StartsWith("Views/") || p.StartsWith("ViewModels/")))
                .Select(p => result.Vfs.TryGetText(p, out var text) ? text.Split("ORIGINAL WINFORMS CODE-BEHIND")[0] : ""));
            Assert.Contains("catch (FormatException ex) when (ex.Message != \"\")", generated);
            Assert.Contains("finally", generated);
            Assert.Contains("catch (IOException)", generated);
            Assert.Contains("using System.IO;", generated);
            Assert.Contains("switch (", generated);
            Assert.Matches(@"case 1:\s*\n\s*case 2:", generated);
            Assert.Contains("continue;", generated);
            Assert.Contains("while (count < 5);", generated);

            // A helper over DateTime/TimeSpan?, a DateTime field and a string? local all come across.
            Assert.Contains("private string Describe(DateTime since, TimeSpan? extra)", generated);
            Assert.Contains("private DateTime started;", generated);
            Assert.Contains("string? note = null;", generated);

            // YesNoCancel, as the bundled dialog's bool? - local, comparison, switch and inline.
            Assert.Contains("var answer = await MessageBoxFallback.ShowYesNoCancelAsync(this, \"Save changes?\", \"Statements\");", generated);
            Assert.Contains("if (answer == null)", generated);
            Assert.Matches(@"switch \(answer\)\s*\{\s*case true:", generated);
            Assert.Contains("case false:", generated);
            Assert.Contains("if (await MessageBoxFallback.ShowYesNoCancelAsync(this, \"Again?\", \"Statements\") != false)", generated);

            var buildResult = await DotnetRunner.RunAsync("build", outputDir);
            Assert.True(
                buildResult.ExitCode == 0,
                $"dotnet build failed with exit code {buildResult.ExitCode}.\n--- stdout ---\n{buildResult.StdOut}\n--- stderr ---\n{buildResult.StdErr}");
            Assert.DoesNotContain(": warning ", buildResult.StdOut);
        }
        finally
        {
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, recursive: true);
            }
        }
    }
}
