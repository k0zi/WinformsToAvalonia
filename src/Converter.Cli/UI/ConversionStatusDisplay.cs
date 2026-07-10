using Spectre.Console;
using Spectre.Console.Rendering;
using Microsoft.Extensions.Logging;
using Converter.Cli.Models;
using Converter.Cli.Logging;

namespace Converter.Cli.UI;

/// <summary>
/// Live display component showing conversion progress and status
/// </summary>
public class ConversionStatusDisplay : IRenderable
{
    private readonly ConversionProgress _progress;
    private readonly CancellationToken _cancellationToken;
    private readonly ConverterTheme _theme;
    private readonly List<LogMessage> _recentLogs = new(10);

    public ConversionStatusDisplay(
        ConversionProgress progress,
        CancellationToken cancellationToken,
        ConverterTheme theme)
    {
        _progress = progress;
        _cancellationToken = cancellationToken;
        _theme = theme;
    }

    public Measurement Measure(RenderOptions options, int maxWidth)
    {
        return new Measurement(maxWidth, maxWidth);
    }

    public IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        // Dequeue new log messages
        var newLogs = SpectreConsoleLoggerProvider.GetQueuedLogs();
        foreach (var log in newLogs)
        {
            _recentLogs.Add(log);
        }

        // Keep only last 10 messages
        while (_recentLogs.Count > 10)
        {
            _recentLogs.RemoveAt(0);
        }

        IRenderable content;

        // Show rollback state
        if (_progress.IsRollingBack)
        {
            content = CreateRollbackPanel();
        }
        // Show cancellation overlay
        else if (_cancellationToken.IsCancellationRequested)
        {
            content = CreateCancellationOverlay();
        }
        // Show normal progress
        else
        {
            content = CreateNormalDisplay();
        }

        return ((IRenderable)content).Render(options, maxWidth);
    }

    private IRenderable CreateRollbackPanel()
    {
        return new Panel(
            new Rows(
                new Markup($"[{_theme.Warning}]{_theme.RollbackIcon} Rolling Back Changes[/]"),
                new Text(""),
                new Markup("Restoring workspace to original state..."),
                new Text("")
            ))
        {
            Border = BoxBorder.Double,
            BorderStyle = new Style(_theme.Warning),
            Padding = new Padding(2, 1)
        };
    }

    private IRenderable CreateCancellationOverlay()
    {
        var rows = new List<IRenderable>
        {
            CreateHeader(),
            CreateProgressBars(),
            new Panel(
                new Markup($"[{_theme.Warning}]{_theme.PauseIcon} Cancellation requested, finishing current operation...[/]"))
            {
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(_theme.Warning)
            }
        };

        return new Rows(rows);
    }

    private IRenderable CreateNormalDisplay()
    {
        var rows = new List<IRenderable>
        {
            CreateHeader(),
            CreateProgressBars(),
            CreateCurrentOperationPanel(),
            CreateStatisticsTable(),
            CreateRecentLogsPanel(),
            CreateFooter()
        };

        return new Rows(rows);
    }

    private IRenderable CreateHeader()
    {
        return new FigletText("WinForms → Avalonia")
            .Centered()
            .Color(_theme.Primary);
    }

    private IRenderable CreateProgressBars()
    {
        var formsPercentage = _progress.TotalForms > 0
            ? (int)((double)_progress.FormsProcessed / _progress.TotalForms * 100)
            : 0;

        var filesPercentage = _progress.TotalFilesToGenerate > 0
            ? (int)((double)_progress.FilesGenerated / _progress.TotalFilesToGenerate * 100)
            : 0;

        var formsBar = new BarChart()
            .Width(60)
            .Label($"[{_theme.Success}]Forms[/]")
            .CenterLabel()
            .AddItem("Progress", _progress.TotalForms > 0 ? _progress.FormsProcessed : 0, _theme.Success);

        var filesBar = new BarChart()
            .Width(60)
            .Label($"[{_theme.Info}]Files[/]")
            .CenterLabel()
            .AddItem("Progress", _progress.TotalFilesToGenerate > 0 ? _progress.FilesGenerated : 0, _theme.Info);

        return new Rows(
            new Markup($"[{_theme.Success}]Forms:[/] {formsPercentage}% ({_progress.FormsProcessed}/{_progress.TotalForms})"),
            new Markup($"[{_theme.Info}]Files:[/]  {filesPercentage}% ({_progress.FilesGenerated}/{_progress.TotalFilesToGenerate})")
        );
    }

    private IRenderable CreateCurrentOperationPanel()
    {
        var operationText = _progress.CurrentOperation switch
        {
            OperationType.GitInit => "Initializing git repository",
            OperationType.Parsing => "Parsing WinForms designer files",
            OperationType.ConvertingForm => $"Converting form: {_progress.CurrentFormName}",
            OperationType.GeneratingFiles => $"Generating files: {_progress.CurrentSubOperation}",
            OperationType.GeneratingProjectFiles => "Generating project files",
            OperationType.GeneratingMigrationGuide => "Generating migration guide",
            OperationType.GeneratingReport => "Generating conversion report",
            OperationType.Complete => "Conversion complete",
            _ => "Processing..."
        };

        IRenderable content;
        if (!string.IsNullOrEmpty(_progress.CurrentSubOperation) && _progress.CurrentOperation != OperationType.ConvertingForm)
        {
            content = new Rows(
                new Markup($"[{_theme.Info}]{operationText}[/]"),
                new Markup($"[dim]{_progress.CurrentSubOperation}[/]")
            );
        }
        else
        {
            content = new Markup($"[{_theme.Info}]{operationText}[/]");
        }

        return new Panel(content)
        {
            Header = new PanelHeader("Current Operation"),
            Border = BoxBorder.Rounded,
            Padding = new Padding(1, 0)
        };
    }

    private IRenderable CreateStatisticsTable()
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn(new TableColumn("Metric").Centered())
            .AddColumn(new TableColumn("Progress").Centered());

        table.AddRow(
            new Markup("Controls"),
            new Markup($"[{_theme.Success}]{_progress.ConvertedControls}[/] / {_progress.TotalControls}")
        );

        table.AddRow(
            new Markup("Properties"),
            new Markup($"[{_theme.Info}]{_progress.MappedProperties}[/] / {_progress.TotalProperties}")
        );

        table.AddRow(
            new Markup("Events"),
            new Markup($"[{_theme.Primary}]{_progress.ConvertedEvents}[/] / {_progress.TotalEvents}")
        );

        return table;
    }

    private IRenderable CreateRecentLogsPanel()
    {
        var logLines = new List<IRenderable>();

        foreach (var log in _recentLogs.TakeLast(10))
        {
            var (color, icon) = log.Level switch
            {
                LogLevel.Error => (_theme.Error, _theme.ErrorIcon),
                LogLevel.Warning => (_theme.Warning, _theme.WarningIcon),
                LogLevel.Information => (_theme.Info, _theme.InfoIcon),
                LogLevel.Debug => (_theme.Debug, _theme.DebugIcon),
                _ => (_theme.Secondary, _theme.InfoIcon)
            };

            logLines.Add(new Markup($"[{color}]{icon}[/] [dim]{log.Timestamp:HH:mm:ss}[/] {Markup.Escape(log.Message)}"));
        }

        if (logLines.Count == 0)
        {
            logLines.Add(new Markup("[dim]No recent activity[/]"));
        }

        return new Panel(new Rows(logLines))
        {
            Header = new PanelHeader("Recent Activity"),
            Border = BoxBorder.Rounded,
            Padding = new Padding(1, 0)
        };
    }

    private IRenderable CreateFooter()
    {
        return new Columns(
            new Markup($"[{_theme.Warning}]{_theme.WarningIcon} {_progress.Warnings} warning(s)[/]"),
            new Markup($"[{_theme.Error}]{_theme.ErrorIcon} {_progress.Errors} error(s)[/]"),
            new Markup($"[dim]Elapsed: {_progress.ElapsedTime:mm\\:ss}[/]")
        );
    }
}
