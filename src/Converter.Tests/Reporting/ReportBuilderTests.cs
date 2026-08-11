using Converter.Core.Models;
using Converter.Reporting.Builders;

namespace Converter.Tests.Reporting;

public class ReportBuilderTests
{
    private static ConversionReport BuildReportWithManualSteps() => new()
    {
        ProjectName = "SampleApp",
        Timestamp = DateTime.Now,
        Duration = TimeSpan.FromSeconds(1),
        Status = ConversionStatus.Success,
        Statistics = new ConversionStatistics(),
        ManualSteps =
        [
            new ManualStepInfo
            {
                Category = "Unmapped Controls",
                Title = "CustomGrid \"grid1\" has no Avalonia mapping",
                Location = "Form1.Designer.cs",
                Description = "Needs a manual replacement."
            }
        ]
    };

    [Fact]
    public void Generate_Markdown_ZeroErrorsButManualStepsPresent_IncludesManualStepsSection()
    {
        // The specific confusion this guards against: Errors.Count == 0 must never read as
        // "nothing left to do" when ManualSteps is non-empty - the report has to say so.
        var report = BuildReportWithManualSteps();
        Assert.Empty(report.Errors);

        var markdown = new ReportBuilder().Generate(report, ReportFormat.Markdown);

        Assert.Contains("Manual Steps Required (1)", markdown);
        Assert.Contains("Unmapped Controls", markdown);
        Assert.Contains("CustomGrid \"grid1\" has no Avalonia mapping", markdown);
    }

    [Fact]
    public void Generate_Markdown_NoManualSteps_OmitsManualStepsSection()
    {
        var report = new ConversionReport
        {
            ProjectName = "SampleApp",
            Timestamp = DateTime.Now,
            Duration = TimeSpan.FromSeconds(1),
            Status = ConversionStatus.Success,
            Statistics = new ConversionStatistics()
        };

        var markdown = new ReportBuilder().Generate(report, ReportFormat.Markdown);

        Assert.DoesNotContain("Manual Steps Required", markdown);
    }

    [Fact]
    public void Generate_Html_ManualStepsPresent_IncludesManualStepsSection()
    {
        var report = BuildReportWithManualSteps();

        var html = new ReportBuilder().Generate(report, ReportFormat.Html);

        Assert.Contains("Manual Steps Required (1)", html);
        Assert.Contains("Unmapped Controls", html);
    }

    [Fact]
    public void Generate_Json_IncludesManualSteps()
    {
        var report = BuildReportWithManualSteps();

        var json = new ReportBuilder().Generate(report, ReportFormat.Json);

        Assert.Contains("manualSteps", json);
        Assert.Contains("Unmapped Controls", json);
    }
}
