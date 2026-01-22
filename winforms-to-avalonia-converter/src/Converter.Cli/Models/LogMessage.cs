using Microsoft.Extensions.Logging;

namespace Converter.Cli.Models;

/// <summary>
/// Represents a log message captured from the logging infrastructure
/// </summary>
/// <param name="Level">The severity level of the log message</param>
/// <param name="Message">The formatted log message</param>
/// <param name="Exception">Optional exception associated with the log</param>
/// <param name="Timestamp">When the log message was created</param>
/// <param name="CategoryName">The category/logger name that produced the message</param>
public record LogMessage(
    LogLevel Level,
    string Message,
    Exception? Exception,
    DateTime Timestamp,
    string CategoryName
);
