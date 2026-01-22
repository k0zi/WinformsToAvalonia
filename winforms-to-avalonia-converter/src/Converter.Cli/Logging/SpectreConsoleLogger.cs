using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Converter.Cli.Models;

namespace Converter.Cli.Logging;

/// <summary>
/// Logger implementation that captures log messages in a queue for display by Spectre.Console
/// </summary>
public class SpectreConsoleLogger : ILogger
{
    private const int MaxQueueSize = 100;
    private readonly ConcurrentQueue<LogMessage> _messageQueue;
    private readonly string _categoryName;

    public SpectreConsoleLogger(ConcurrentQueue<LogMessage> messageQueue, string categoryName)
    {
        _messageQueue = messageQueue;
        _categoryName = categoryName;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var message = formatter(state, exception);
        if (string.IsNullOrEmpty(message) && exception == null)
        {
            return;
        }

        var logMessage = new LogMessage(
            logLevel,
            message,
            exception,
            DateTime.Now,
            _categoryName
        );

        _messageQueue.Enqueue(logMessage);

        // Maintain max queue size by removing oldest messages
        while (_messageQueue.Count > MaxQueueSize)
        {
            _messageQueue.TryDequeue(out _);
        }
    }
}
