namespace Converter.Cli.Models;

/// <summary>
/// Represents the type of operation currently being performed during conversion
/// </summary>
public enum OperationType
{
    GitInit,
    Parsing,
    ConvertingForm,
    GeneratingFiles,
    GeneratingProjectFiles,
    GeneratingMigrationGuide,
    GeneratingReport,
    RollingBack,
    Complete,
    Cancelled
}
