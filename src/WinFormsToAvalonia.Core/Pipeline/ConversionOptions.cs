namespace WinFormsToAvalonia.Core.Pipeline;

public sealed record ConversionOptions(
    string SourceProjectPath,
    string OutputDirectory,
    bool Force = false,
    bool DryRun = false,
    bool Verbose = false,
    bool NoFallbackControls = false,
    bool SkipCodeBehindComments = false,
    string? LogFile = null,
    /// <summary>
    /// Replace files already in the output directory instead of preserving them and writing the
    /// generated version alongside as `*.w2a-new`. Only matters when re-converting over output a
    /// human has already started migrating.
    /// </summary>
    bool OverwriteAll = false);
