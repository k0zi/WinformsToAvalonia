using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Converter.Cli.Models;

namespace Converter.Cli.Logging;

/// <summary>
/// Logger provider that creates SpectreConsoleLogger instances sharing a common message queue
/// </summary>
public class SpectreConsoleLoggerProvider : ILoggerProvider
{
    private static readonly ConcurrentQueue<LogMessage> _messageQueue = new();

    public ILogger CreateLogger(string categoryName)
    {
        return new SpectreConsoleLogger(_messageQueue, categoryName);
    }

    /// <summary>
    /// Retrieves and clears all queued log messages
    /// </summary>
    public static List<LogMessage> GetQueuedLogs()
    {
        var messages = new List<LogMessage>();
        while (_messageQueue.TryDequeue(out var message))
        {
            messages.Add(message);
        }
        return messages;
    }

    public void Dispose()
    {
        _messageQueue.Clear();
    }
}
