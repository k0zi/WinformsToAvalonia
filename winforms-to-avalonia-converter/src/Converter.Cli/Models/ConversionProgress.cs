namespace Converter.Cli.Models;

/// <summary>
/// Represents the current progress state of the conversion operation
/// </summary>
public class ConversionProgress
{
    public OperationType CurrentOperation { get; set; }
    public string? CurrentSubOperation { get; set; }
    public int FormsProcessed { get; set; }
    public int TotalForms { get; set; }
    public string? CurrentFormName { get; set; }
    public int FilesGenerated { get; set; }
    public int TotalFilesToGenerate { get; set; }
    
    // Statistics
    public int TotalControls { get; set; }
    public int ConvertedControls { get; set; }
    public int TotalProperties { get; set; }
    public int MappedProperties { get; set; }
    public int TotalEvents { get; set; }
    public int ConvertedEvents { get; set; }
    public int Warnings { get; set; }
    public int Errors { get; set; }
    
    public TimeSpan ElapsedTime { get; set; }
    public bool IsGeneratingReport { get; set; }
    public bool IsRollingBack { get; set; }
}
