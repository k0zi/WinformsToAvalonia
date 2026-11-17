using System.Diagnostics;

namespace WinFormsToAvalonia.Core.Pipeline;

/// <summary>How much a log line says: a stage starting, what it found, or one decision inside it.</summary>
public enum ConversionLogLevel
{
    /// <summary>A stage starting - what a spinner shows.</summary>
    Stage,

    /// <summary>What a stage found or produced, as counts and names.</summary>
    Detail,

    /// <summary>One decision: a control type, a handler, a package lookup, a file.</summary>
    Trace,
}

/// <param name="Elapsed">Time since the log was created, so a slow stage shows itself.</param>
public sealed record ConversionLogEntry(TimeSpan Elapsed, ConversionLogLevel Level, string Message);

/// <summary>
/// What the conversion is doing while it does it - for a human watching a long run, not for the
/// result. Nothing the pipeline decides may depend on it: the report and the generated files are
/// the same with or without a listener.
/// </summary>
/// <remarks>
/// A constructor argument of the pipeline rather than a <see cref="ConversionOptions"/> field: the
/// options are the user's intent for the output, and this is plumbing around the run.
/// </remarks>
public sealed class ConversionLog
{
    private readonly Action<ConversionLogEntry>? _sink;
    private readonly Stopwatch _clock = Stopwatch.StartNew();

    public ConversionLog(Action<ConversionLogEntry> sink) => _sink = sink;

    private ConversionLog() => _sink = null;

    /// <summary>Discards everything.</summary>
    public static ConversionLog None { get; } = new();

    public void Stage(string message) => Write(ConversionLogLevel.Stage, message);

    public void Detail(string message) => Write(ConversionLogLevel.Detail, message);

    public void Trace(string message) => Write(ConversionLogLevel.Trace, message);

    private void Write(ConversionLogLevel level, string message) =>
        _sink?.Invoke(new ConversionLogEntry(_clock.Elapsed, level, message));
}
