using System.Globalization;
using Noo.Api.Core.Config.Env;
using Noo.Api.Core.Utils;

namespace Noo.Api.Core.Logging;

public class TelegramLogger : ILogger
{
    private readonly string _categoryName;
    private readonly LogConfig _options;

    public TelegramLogger(string categoryName, LogConfig options)
    {
        _categoryName = categoryName;
        _options = options;
    }

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= _options.MinLevel;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        var message = formatter(state, exception);
        var logMessage = FormatMessage(logLevel, message, exception);

        // TODO: Implement actual Telegram sending logic
        // Use _options.TelegramLogToken and _options.TelegramChatIds
        SendToTelegramAsync(logMessage).ConfigureAwait(false);
    }

    private string FormatMessage(LogLevel logLevel, string message, Exception? exception)
    {
        var emoji = GetLogLevelEmoji(logLevel);
        var timestamp = Clock.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

        var formatted = $"{emoji} *[{logLevel}]* `{timestamp}`\n" +
                       $"📁 `{_categoryName}`\n" +
                       $"💬 {EscapeMarkdown(message)}";

        if (exception != null)
        {
            formatted += $"\n\n🔥 *Exception:*\n```\n{exception.Message}\n```";
            if (exception.StackTrace != null)
            {
                var stackTrace = exception.StackTrace.Length > 500
                    ? exception.StackTrace[..500] + "..."
                    : exception.StackTrace;
                formatted += $"\n```\n{stackTrace}\n```";
            }
        }

        return formatted;
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

    private static string EscapeMarkdown(string text)
    {
        return text
            .Replace("_", "\\_")
            .Replace("*", "\\*")
            .Replace("[", "\\[")
            .Replace("`", "\\`");
    }

    private Task SendToTelegramAsync(string message)
    {
        // TODO: Implement actual Telegram Bot API call
        // POST to https://api.telegram.org/bot{_options.TelegramLogToken}/sendMessage
        // with chat_id from _options.TelegramChatIds and text = message, parse_mode = "Markdown"

        // Placeholder: Log to console that we would send to Telegram
        // Remove this when implementing actual Telegram integration
        Console.WriteLine($"[TELEGRAM LOG STUB] Would send to {_options.TelegramChatIds.Length} chat(s): {message[..Math.Min(100, message.Length)]}...");

        return Task.CompletedTask;
    }

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();
        public void Dispose() { }
    }
}
