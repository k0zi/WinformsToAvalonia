namespace Converter.Core.Models;

/// <summary>
/// A single "you need to do this by hand" item surfaced during conversion - lives in
/// Converter.Core (rather than Converter.Documentation, where MigrationGuideGenerator
/// consumes it) so Converter.Reporting's ConversionReport can carry the list too, without
/// Reporting needing to depend on Documentation. Written into MIGRATION_GUIDE.md's "Manual
/// Steps Required" section, and also surfaced directly in the CLI's completion summary -
/// conversion completing with zero exceptions does not mean the generated project builds;
/// this list is the actual signal for that.
/// </summary>
public class ManualStepInfo
{
    public required string Category { get; init; }
    public required string Title { get; init; }
    public required string Location { get; init; }
    public required string Description { get; init; }
}
