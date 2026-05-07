using System.Collections.Concurrent;
using Noo.Api.Core.Config.Env;

namespace Noo.Api.Core.Logging;

public class TelegramLoggerProvider : ILoggerProvider
{
    private readonly LogConfig _options;
    private readonly ConcurrentDictionary<string, TelegramLogger> _loggers = new();

    public TelegramLoggerProvider(LogConfig options)
    {
        _options = options;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, name => new TelegramLogger(name, _options));
    }

    public void Dispose()
    {
        _loggers.Clear();
    }
}
