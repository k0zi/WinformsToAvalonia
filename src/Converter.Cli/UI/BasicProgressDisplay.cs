using Converter.Cli.Models;

namespace Converter.Cli.UI;

/// <summary>
/// Simple progress display for non-ANSI terminals
/// </summary>
public class BasicProgressDisplay : IProgress<ConversionProgress>
{
    private ConversionProgress? _lastProgress;
    private readonly object _lock = new();

    public void Report(ConversionProgress value)
    {
        lock (_lock)
        {
            _lastProgress = value;

            if (value.IsRollingBack)
            {
                Console.Write("\r🔄 Rolling back changes...                              ");
                return;
            }

            var formsPercent = value.TotalForms > 0
                ? (int)((double)value.FormsProcessed / value.TotalForms * 100)
                : 0;

            var filesPercent = value.TotalFilesToGenerate > 0
                ? (int)((double)value.FilesGenerated / value.TotalFilesToGenerate * 100)
                : 0;

            var progressBar = CreateProgressBar(formsPercent, 20);
            var status = value.CurrentOperation switch
            {
                OperationType.Parsing => "Parsing",
                OperationType.ConvertingForm => "Converting",
                OperationType.GeneratingFiles => "Generating",
                OperationType.Complete => "Complete",
                _ => "Processing"
            };

            Console.Write($"\r{status}: [{progressBar}] {formsPercent}% ({value.FormsProcessed}/{value.TotalForms} forms, {value.FilesGenerated}/{value.TotalFilesToGenerate} files)");

            if (value.CurrentOperation == OperationType.Complete)
            {
                Console.WriteLine(); // Move to next line when complete
            }
        }
    }

    private static string CreateProgressBar(int percentage, int width)
    {
        var filled = (int)(percentage / 100.0 * width);
        var empty = width - filled;
        return new string('#', filled) + new string('.', empty);
    }

    public void Clear()
    {
        lock (_lock)
        {
            Console.Write("\r" + new string(' ', 100) + "\r");
        }
    }
}
