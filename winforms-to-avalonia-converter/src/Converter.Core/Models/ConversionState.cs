namespace Converter.Core.Models;

/// <summary>
/// Represents conversion state for tracking and checkpointing.
/// </summary>
public class ConversionState
{
    /// <summary>
    /// Project being converted.
    /// </summary>
    public required string ProjectPath { get; init; }

    /// <summary>
    /// Output directory.
    /// </summary>
    public required string OutputPath { get; init; }

    /// <summary>
    /// Conversion start time.
    /// </summary>
    public DateTime StartTime { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Files that have been successfully converted.
    /// </summary>
    public HashSet<string> CompletedFiles { get; init; } = [];

    /// <summary>
    /// Files currently being processed.
    /// </summary>
    public HashSet<string> InProgressFiles { get; init; } = [];

    /// <summary>
    /// Files that failed conversion.
    /// </summary>
    public Dictionary<string, string> FailedFiles { get; init; } = [];

    /// <summary>
    /// File hashes for change detection.
    /// </summary>
    public Dictionary<string, string> FileHashes { get; init; } = [];

    /// <summary>
    /// Rollback manifest - files created during this conversion.
    /// </summary>
    public List<string> GeneratedFiles { get; init; } = [];

    /// <summary>
    /// Conversion statistics.
    /// </summary>
    public ConversionStatistics Statistics { get; init; } = new();
}

/// <summary>
/// Statistics about the conversion process.
/// </summary>
public class ConversionStatistics
{
    public int TotalControls { get; set; }
    public int ConvertedControls { get; set; }
    public int PartialControls { get; set; }
    public int PlaceholderControls { get; set; }
    public int TotalProperties { get; set; }
    public int MappedProperties { get; set; }
    public int UnmappedProperties { get; set; }
    public int TotalEvents { get; set; }
    public int ConvertedToCommands { get; set; }
    public int ExtractedStyles { get; set; }
    public int LocalizationKeys { get; set; }
    public int CheckpointsSaved { get; set; }
    public int RollbacksPerformed { get; set; }
}
