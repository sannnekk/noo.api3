using System.Globalization;
using Noo.Api.Core.Config;
using Noo.Api.Core.Config.Env;

namespace Noo.Api.Core.Logging;

public class ConsoleLogger : ILogger
{
    private readonly string _categoryName;
    private readonly LogConfig _options;

    public ConsoleLogger(string categoryName, LogConfig options)
    {
        _categoryName = categoryName;
        _options = options;
    }

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => null!;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= _options.MinLevel;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        var useColor = _options.ConsoleDecorations.HasFlag(ConsoleLogDecoration.Color);
        var useEmoji = _options.ConsoleDecorations.HasFlag(ConsoleLogDecoration.Emoji);
        var message = formatter(state, exception);
        var emojiPrefix = useEmoji ? $"{GetLogLevelEmoji(logLevel)} " : string.Empty;
        var logMessage = $"[{DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)}] " +
                         $"[{logLevel}] " +
                         $"{emojiPrefix}[{_categoryName}] " +
                         $"{message}";

        var originalColor = Console.ForegroundColor;

        try
        {
            if (useColor)
            {
                SetConsoleColor(logLevel);
            }

            Console.WriteLine(logMessage);

            if (exception != null)
            {
                Console.WriteLine($"Exception: {exception}");
            }
        }
        finally
        {
            if (useColor)
            {
                Console.ForegroundColor = originalColor;
            }
        }
    }

    private static string GetLogLevelEmoji(LogLevel logLevel) => logLevel switch
    {
        LogLevel.Trace => "🔍",
        LogLevel.Debug => "🐛",
        LogLevel.Information => "ℹ️",
        LogLevel.Warning => "⚠️",
        LogLevel.Error => "❌",
        LogLevel.Critical => "🚨",
        _ => "📝"
    };

    private static void SetConsoleColor(LogLevel logLevel)
    {
        Console.ForegroundColor = logLevel switch
        {
            LogLevel.Trace => ConsoleColor.DarkGray,
            LogLevel.Debug => ConsoleColor.Gray,
            LogLevel.Information => ConsoleColor.White,
            LogLevel.Warning => ConsoleColor.Yellow,
            LogLevel.Error => ConsoleColor.Red,
            LogLevel.Critical => ConsoleColor.DarkRed,
            _ => Console.ForegroundColor
        };
    }
}
