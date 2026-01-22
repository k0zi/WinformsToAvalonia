using System.Text;
using Converter.Core.Models;

namespace Converter.Documentation.Generators;

/// <summary>
/// Generates comprehensive migration guide documentation.
/// </summary>
public class MigrationGuideGenerator
{
    /// <summary>
    /// Generate a complete migration guide in Markdown format.
    /// </summary>
    public string Generate(MigrationGuideContext context)
    {
        var sb = new StringBuilder();

        // Title and overview
        WriteHeader(sb, context);
        WriteExecutiveSummary(sb, context);
        WriteArchitecturalDifferences(sb);
        WriteConversionDetails(sb, context);
        WriteLayoutDecisions(sb, context);
        WritePropertyMappings(sb, context);
        WriteEventConversions(sb, context);
        WriteManualSteps(sb, context);
        WriteRecommendations(sb);
        WriteNextSteps(sb);
        WriteAppendix(sb);

        return sb.ToString();
    }

    private void WriteHeader(StringBuilder sb, MigrationGuideContext context)
    {
        sb.AppendLine($"# Migration Guide: {context.ProjectName}");
        sb.AppendLine();
        sb.AppendLine($"**Generated**: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"**Converter Version**: 1.0.0");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
    }

    private void WriteExecutiveSummary(StringBuilder sb, MigrationGuideContext context)
    {
        sb.AppendLine("## Executive Summary");
        sb.AppendLine();
        sb.AppendLine($"This document describes the migration of **{context.ProjectName}** from Windows Forms to Avalonia UI.");
        sb.AppendLine();
        sb.AppendLine("### Conversion Statistics");
        sb.AppendLine();
        sb.AppendLine($"- **Total Controls**: {context.Statistics.TotalControls}");
        sb.AppendLine($"- **Successfully Converted**: {context.Statistics.ConvertedControls} ({GetPercentage(context.Statistics.ConvertedControls, context.Statistics.TotalControls)}%)");
        sb.AppendLine($"- **Partial Conversions**: {context.Statistics.PartialControls}");
        sb.AppendLine($"- **Placeholders**: {context.Statistics.PlaceholderControls}");
        sb.AppendLine($"- **Total Properties Mapped**: {context.Statistics.MappedProperties}/{context.Statistics.TotalProperties}");
        sb.AppendLine($"- **Events Converted to Commands**: {context.Statistics.ConvertedToCommands}");
        sb.AppendLine($"- **Styles Extracted**: {context.Statistics.ExtractedStyles}");
        sb.AppendLine();
    }

    private void WriteArchitecturalDifferences(StringBuilder sb)
    {
        sb.AppendLine("## Architectural Differences");
        sb.AppendLine();
        sb.AppendLine("### WinForms vs. Avalonia");
        sb.AppendLine();
        sb.AppendLine("| Aspect | Windows Forms | Avalonia |");
        sb.AppendLine("|--------|--------------|----------|");
        sb.AppendLine("| **Pattern** | Event-driven | MVVM with data binding |");
        sb.AppendLine("| **Layout** | Absolute positioning | Declarative panels |");
        sb.AppendLine("| **UI Thread** | STA thread required | Cross-platform threading |");
        sb.AppendLine("| **Resources** | .resx files | .axaml dictionaries |");
        sb.AppendLine("| **Styling** | Per-control properties | Styles and themes |");
        sb.AppendLine("| **Platform** | Windows only | Cross-platform |");
        sb.AppendLine();
    }

    private void WriteConversionDetails(StringBuilder sb, MigrationGuideContext context)
    {
        sb.AppendLine("## Conversion Details");
        sb.AppendLine();
        
        if (context.ConvertedForms.Count > 0)
        {
            sb.AppendLine("### Forms Converted");
            sb.AppendLine();
            sb.AppendLine("| WinForms Class | Avalonia Class | Controls | Layout | Status |");
            sb.AppendLine("|----------------|----------------|----------|--------|--------|");
            
            foreach (var form in context.ConvertedForms)
            {
                sb.AppendLine($"| {form.OriginalName} | {form.AvaloniaName} | {form.ControlCount} | {form.LayoutType} | {form.Status} |");
            }
            
            sb.AppendLine();
        }
    }

    private void WriteLayoutDecisions(StringBuilder sb, MigrationGuideContext context)
    {
        sb.AppendLine("## Layout Decisions");
        sb.AppendLine();
        sb.AppendLine("The converter analyzed control positioning to determine the best layout strategy for each form.");
        sb.AppendLine();
        
        foreach (var form in context.ConvertedForms)
        {
            sb.AppendLine($"### {form.OriginalName}");
            sb.AppendLine();
            sb.AppendLine($"- **Layout Type**: {form.LayoutType}");
            sb.AppendLine($"- **Confidence**: {form.LayoutConfidence}%");
            sb.AppendLine($"- **Reason**: {form.LayoutReason}");
            sb.AppendLine();
        }
    }

    private void WritePropertyMappings(StringBuilder sb, MigrationGuideContext context)
    {
        sb.AppendLine("## Property Mappings");
        sb.AppendLine();
        sb.AppendLine("Common property mappings applied during conversion:");
        sb.AppendLine();
        sb.AppendLine("| WinForms Property | Avalonia Property | Notes |");
        sb.AppendLine("|-------------------|-------------------|-------|");
        sb.AppendLine("| `Text` | `Text` / `Content` | Direct mapping |");
        sb.AppendLine("| `BackColor` | `Background` | Converted to Brush |");
        sb.AppendLine("| `ForeColor` | `Foreground` | Converted to Brush |");
        sb.AppendLine("| `Font` | `FontFamily`, `FontSize`, `FontWeight` | Split into multiple properties |");
        sb.AppendLine("| `Location` | `Canvas.Left`, `Canvas.Top` | For Canvas layout |");
        sb.AppendLine("| `Dock` | `DockPanel.Dock` | For DockPanel layout |");
        sb.AppendLine("| `Anchor` | `Grid.Row`, `Grid.Column` | Converted to Grid positioning |");
        sb.AppendLine();
    }

    private void WriteEventConversions(StringBuilder sb, MigrationGuideContext context)
    {
        sb.AppendLine("## Event to Command Conversions");
        sb.AppendLine();
        sb.AppendLine("Events have been converted to ICommand using CommunityToolkit.Mvvm:");
        sb.AppendLine();
        sb.AppendLine("```csharp");
        sb.AppendLine("// WinForms");
        sb.AppendLine("private void button1_Click(object sender, EventArgs e)");
        sb.AppendLine("{");
        sb.AppendLine("    // Handle click");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("// Avalonia ViewModel");
        sb.AppendLine("[RelayCommand]");
        sb.AppendLine("private void Button1Click()");
        sb.AppendLine("{");
        sb.AppendLine("    // Handle click");
        sb.AppendLine("}");
        sb.AppendLine("```");
        sb.AppendLine();
    }

    private void WriteManualSteps(StringBuilder sb, MigrationGuideContext context)
    {
        sb.AppendLine("## Manual Steps Required");
        sb.AppendLine();
        
        if (context.ManualSteps.Count > 0)
        {
            sb.AppendLine("The following items require manual attention:");
            sb.AppendLine();
            
            foreach (var step in context.ManualSteps.GroupBy(s => s.Category))
            {
                sb.AppendLine($"### {step.Key}");
                sb.AppendLine();
                
                foreach (var item in step)
                {
                    sb.AppendLine($"- **{item.Title}**");
                    sb.AppendLine($"  - Location: `{item.Location}`");
                    sb.AppendLine($"  - Description: {item.Description}");
                    sb.AppendLine();
                }
            }
        }
        else
        {
            sb.AppendLine("No manual steps required - all controls were successfully converted!");
            sb.AppendLine();
        }
    }

    private void WriteRecommendations(StringBuilder sb)
    {
        sb.AppendLine("## Recommendations");
        sb.AppendLine();
        sb.AppendLine("### MVVM Best Practices");
        sb.AppendLine();
        sb.AppendLine("1. **Use Data Binding**: Leverage Avalonia's binding system instead of direct control manipulation");
        sb.AppendLine("2. **Implement INotifyPropertyChanged**: Use `[ObservableProperty]` from CommunityToolkit.Mvvm");
        sb.AppendLine("3. **Commands Over Events**: Convert remaining events to ICommand for better testability");
        sb.AppendLine("4. **Dependency Injection**: Consider using DI for ViewModels and services");
        sb.AppendLine();
        sb.AppendLine("### Testing");
        sb.AppendLine();
        sb.AppendLine("1. **UI Testing**: Use Avalonia.Headless for automated UI tests");
        sb.AppendLine("2. **ViewModel Testing**: Test ViewModels independently of views");
        sb.AppendLine("3. **Integration Testing**: Test the full application flow");
        sb.AppendLine();
    }

    private void WriteNextSteps(StringBuilder sb)
    {
        sb.AppendLine("## Next Steps");
        sb.AppendLine();
        sb.AppendLine("- [ ] Review and test all converted forms");
        sb.AppendLine("- [ ] Implement TODO comments in ViewModels");
        sb.AppendLine("- [ ] Replace placeholder controls with Avalonia alternatives");
        sb.AppendLine("- [ ] Test cross-platform compatibility (if applicable)");
        sb.AppendLine("- [ ] Optimize layouts and styling");
        sb.AppendLine("- [ ] Add unit tests for ViewModels");
        sb.AppendLine("- [ ] Update deployment process");
        sb.AppendLine();
    }

    private void WriteAppendix(StringBuilder sb)
    {
        sb.AppendLine("## Appendix");
        sb.AppendLine();
        sb.AppendLine("### Resources");
        sb.AppendLine();
        sb.AppendLine("- [Avalonia Documentation](https://docs.avaloniaui.net/)");
        sb.AppendLine("- [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/)");
        sb.AppendLine("- [Avalonia Samples](https://github.com/AvaloniaUI/Avalonia.Samples)");
        sb.AppendLine();
    }

    private int GetPercentage(int value, int total)
    {
        return total > 0 ? (int)((value / (double)total) * 100) : 0;
    }
}

/// <summary>
/// Context for migration guide generation.
/// </summary>
public class MigrationGuideContext
{
    public required string ProjectName { get; init; }
    public required ConversionStatistics Statistics { get; init; }
    public List<FormConversionInfo> ConvertedForms { get; init; } = [];
    public List<ManualStepInfo> ManualSteps { get; init; } = [];
}

public class FormConversionInfo
{
    public required string OriginalName { get; init; }
    public required string AvaloniaName { get; init; }
    public required int ControlCount { get; init; }
    public required string LayoutType { get; init; }
    public required int LayoutConfidence { get; init; }
    public required string LayoutReason { get; init; }
    public required string Status { get; init; }
}

public class ManualStepInfo
{
    public required string Category { get; init; }
    public required string Title { get; init; }
    public required string Location { get; init; }
    public required string Description { get; init; }
}
