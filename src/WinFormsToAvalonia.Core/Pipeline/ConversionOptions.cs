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
    bool OverwriteAll = false,
    /// <summary>
    /// Emit the cross-platform three-project layout - a shared library plus a desktop and a
    /// browser (WebAssembly) head - instead of the single self-contained desktop project.
    /// </summary>
    /// <remarks>
    /// Opt-in, and deliberately so: the browser head needs the `wasm-tools` workload, and the
    /// split changes the shape of the output directory. Without it the generated files are
    /// byte-for-byte what they have always been.
    /// </remarks>
    bool WithWeb = false);
