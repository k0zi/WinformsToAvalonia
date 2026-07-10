using System.Text;
using System.Text.Json;
using Converter.Core.Models;

namespace Converter.Reporting.Builders;

/// <summary>
/// Builds conversion reports in multiple formats.
/// </summary>
public class ReportBuilder
{
    /// <summary>
    /// Generate report in the specified format.
    /// </summary>
    public string Generate(ConversionReport report, ReportFormat format)
    {
        return format switch
        {
            ReportFormat.Markdown => GenerateMarkdown(report),
            ReportFormat.Html => GenerateHtml(report),
            ReportFormat.Json => GenerateJson(report),
            ReportFormat.Csv => GenerateCsv(report),
            _ => throw new ArgumentException($"Unsupported format: {format}")
        };
    }

    private string GenerateMarkdown(ConversionReport report)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"# Conversion Report: {report.ProjectName}");
        sb.AppendLine();
        sb.AppendLine($"**Date**: {report.Timestamp:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"**Duration**: {report.Duration.TotalSeconds:F2}s");
        sb.AppendLine($"**Status**: {report.Status}");
        sb.AppendLine();

        // Statistics
        sb.AppendLine("## Statistics");
        sb.AppendLine();
        sb.AppendLine($"- Total Controls: {report.Statistics.TotalControls}");
        sb.AppendLine($"- Converted: {report.Statistics.ConvertedControls}");
        sb.AppendLine($"- Partial: {report.Statistics.PartialControls}");
        sb.AppendLine($"- Placeholders: {report.Statistics.PlaceholderControls}");
        sb.AppendLine($"- Properties Mapped: {report.Statistics.MappedProperties}/{report.Statistics.TotalProperties}");
        sb.AppendLine($"- Commands Created: {report.Statistics.ConvertedToCommands}");
        sb.AppendLine($"- Styles Extracted: {report.Statistics.ExtractedStyles}");
        sb.AppendLine();

        // Forms
        if (report.Forms.Count > 0)
        {
            sb.AppendLine("## Forms");
            sb.AppendLine();
            sb.AppendLine("| Form | Controls | Layout | Status |");
            sb.AppendLine("|------|----------|--------|--------|");
            
            foreach (var form in report.Forms)
            {
                sb.AppendLine($"| {form.Name} | {form.ControlCount} | {form.Layout} | {form.Status} |");
            }
            sb.AppendLine();
        }

        // Warnings
        if (report.Warnings.Count > 0)
        {
            sb.AppendLine("## Warnings");
            sb.AppendLine();
            foreach (var warning in report.Warnings)
            {
                sb.AppendLine($"- **{warning.Location}**: {warning.Message}");
            }
            sb.AppendLine();
        }

        // Errors
        if (report.Errors.Count > 0)
        {
            sb.AppendLine("## Errors");
            sb.AppendLine();
            foreach (var error in report.Errors)
            {
                sb.AppendLine($"- **{error.Location}**: {error.Message}");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private string GenerateHtml(ConversionReport report)
    {
        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html>");
        sb.AppendLine("<head>");
        sb.AppendLine($"    <title>Conversion Report: {report.ProjectName}</title>");
        sb.AppendLine("    <style>");
        sb.AppendLine("        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; margin: 40px; }");
        sb.AppendLine("        h1 { color: #333; }");
        sb.AppendLine("        .stats { display: grid; grid-template-columns: repeat(3, 1fr); gap: 20px; margin: 20px 0; }");
        sb.AppendLine("        .stat-card { background: #f5f5f5; padding: 20px; border-radius: 8px; }");
        sb.AppendLine("        .stat-value { font-size: 32px; font-weight: bold; color: #007acc; }");
        sb.AppendLine("        .stat-label { color: #666; margin-top: 5px; }");
        sb.AppendLine("        table { border-collapse: collapse; width: 100%; margin: 20px 0; }");
        sb.AppendLine("        th, td { padding: 12px; text-align: left; border-bottom: 1px solid #ddd; }");
        sb.AppendLine("        th { background-color: #007acc; color: white; }");
        sb.AppendLine("        .warning { background-color: #fff3cd; padding: 10px; margin: 5px 0; border-left: 4px solid #ffc107; }");
        sb.AppendLine("        .error { background-color: #f8d7da; padding: 10px; margin: 5px 0; border-left: 4px solid #dc3545; }");
        sb.AppendLine("        .success { color: #28a745; }");
        sb.AppendLine("        .failed { color: #dc3545; }");
        sb.AppendLine("    </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        sb.AppendLine($"    <h1>Conversion Report: {report.ProjectName}</h1>");
        sb.AppendLine($"    <p><strong>Date:</strong> {report.Timestamp:yyyy-MM-dd HH:mm:ss}</p>");
        sb.AppendLine($"    <p><strong>Duration:</strong> {report.Duration.TotalSeconds:F2}s</p>");
        
        var statusClass = report.Status == ConversionStatus.Success ? "success" : "failed";
        sb.AppendLine($"    <p><strong>Status:</strong> <span class=\"{statusClass}\">{report.Status}</span></p>");

        // Statistics
        sb.AppendLine("    <h2>Statistics</h2>");
        sb.AppendLine("    <div class=\"stats\">");
        sb.AppendLine($"        <div class=\"stat-card\"><div class=\"stat-value\">{report.Statistics.ConvertedControls}</div><div class=\"stat-label\">Controls Converted</div></div>");
        sb.AppendLine($"        <div class=\"stat-card\"><div class=\"stat-value\">{report.Statistics.MappedProperties}</div><div class=\"stat-label\">Properties Mapped</div></div>");
        sb.AppendLine($"        <div class=\"stat-card\"><div class=\"stat-value\">{report.Statistics.ConvertedToCommands}</div><div class=\"stat-label\">Commands Created</div></div>");
        sb.AppendLine("    </div>");

        // Forms
        if (report.Forms.Count > 0)
        {
            sb.AppendLine("    <h2>Forms</h2>");
            sb.AppendLine("    <table>");
            sb.AppendLine("        <tr><th>Form</th><th>Controls</th><th>Layout</th><th>Status</th></tr>");
            
            foreach (var form in report.Forms)
            {
                sb.AppendLine($"        <tr><td>{form.Name}</td><td>{form.ControlCount}</td><td>{form.Layout}</td><td>{form.Status}</td></tr>");
            }
            
            sb.AppendLine("    </table>");
        }

        // Warnings
        if (report.Warnings.Count > 0)
        {
            sb.AppendLine("    <h2>Warnings</h2>");
            foreach (var warning in report.Warnings)
            {
                sb.AppendLine($"    <div class=\"warning\"><strong>{warning.Location}</strong>: {warning.Message}</div>");
            }
        }

        // Errors
        if (report.Errors.Count > 0)
        {
            sb.AppendLine("    <h2>Errors</h2>");
            foreach (var error in report.Errors)
            {
                sb.AppendLine($"    <div class=\"error\"><strong>{error.Location}</strong>: {error.Message}</div>");
            }
        }

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    private string GenerateJson(ConversionReport report)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return JsonSerializer.Serialize(report, options);
    }

    private string GenerateCsv(ConversionReport report)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine("Form,Controls,Layout,Status,Warnings,Errors");

        // Forms
        foreach (var form in report.Forms)
        {
            var warningCount = report.Warnings.Count(w => w.Location.Contains(form.Name));
            var errorCount = report.Errors.Count(e => e.Location.Contains(form.Name));
            
            sb.AppendLine($"\"{form.Name}\",{form.ControlCount},\"{form.Layout}\",\"{form.Status}\",{warningCount},{errorCount}");
        }

        return sb.ToString();
    }
}

/// <summary>
/// Report format options.
/// </summary>
public enum ReportFormat
{
    Markdown,
    Html,
    Json,
    Csv
}

/// <summary>
/// Complete conversion report.
/// </summary>
public class ConversionReport
{
    public required string ProjectName { get; init; }
    public required DateTime Timestamp { get; init; }
    public required TimeSpan Duration { get; init; }
    public required ConversionStatus Status { get; init; }
    public required ConversionStatistics Statistics { get; init; }
    public List<FormReportInfo> Forms { get; init; } = [];
    public List<ReportMessage> Warnings { get; init; } = [];
    public List<ReportMessage> Errors { get; init; } = [];
}

public class FormReportInfo
{
    public required string Name { get; init; }
    public required int ControlCount { get; init; }
    public required string Layout { get; init; }
    public required string Status { get; init; }
}

public class ReportMessage
{
    public required string Location { get; init; }
    public required string Message { get; init; }
}

public enum ConversionStatus
{
    Success,
    PartialSuccess,
    Failed
}
