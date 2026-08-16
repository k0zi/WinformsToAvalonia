namespace WinFormsToAvalonia.Core.Pipeline;

public sealed record ConversionOptions(
    string SourceProjectPath,
    string OutputDirectory,
    bool Force = false,
    bool DryRun = false,
    bool Verbose = false,
    bool NoFallbackControls = false,
    bool SkipCodeBehindComments = false,
    string? LogFile = null);
