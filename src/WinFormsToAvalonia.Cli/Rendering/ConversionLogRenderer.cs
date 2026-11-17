using Spectre.Console;
using WinFormsToAvalonia.Core.Pipeline;

namespace WinFormsToAvalonia.Cli.Rendering;

/// <summary>
/// Runs a conversion with its <see cref="ConversionLog"/> shown: as live lines under
/// <c>--verbose</c>/<c>--trace</c>, otherwise as the text of the spinner. Every entry is also
/// collected, whatever is shown, so <c>--log-file</c> can carry the whole of it.
/// </summary>
public sealed class ConversionLogRenderer(IAnsiConsole console, ConversionLogLevel? shownLevel)
{
    private readonly List<ConversionLogEntry> _entries = [];

    public IReadOnlyList<ConversionLogEntry> Entries => _entries;

    public T Run<T>(string title, Func<ConversionLog, T> run)
    {
        if (shownLevel is { } level)
        {
            return run(new ConversionLog(entry =>
            {
                _entries.Add(entry);
                if (entry.Level <= level)
                {
                    Render(entry);
                }
            }));
        }

        return console.Status()
            .Spinner(Spinner.Known.Dots)
            .Start(title, context => run(new ConversionLog(entry =>
            {
                _entries.Add(entry);
                if (entry.Level == ConversionLogLevel.Stage)
                {
                    context.Status(Markup.Escape(entry.Message));
                }
            })));
    }

    private void Render(ConversionLogEntry entry)
    {
        var time = $"[grey]{entry.Elapsed.TotalSeconds,7:0.00}s[/]";
        var message = Markup.Escape(entry.Message);
        console.MarkupLine(entry.Level switch
        {
            ConversionLogLevel.Stage => $"{time} [bold]{message}[/]",
            ConversionLogLevel.Detail => $"{time}   {message}",
            _ => $"{time}     [grey]{message}[/]",
        });
    }
}
