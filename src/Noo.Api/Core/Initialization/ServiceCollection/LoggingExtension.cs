using Noo.Api.Core.Config;
using Noo.Api.Core.Config.Env;
using Noo.Api.Core.Initialization.Configuration;
using Noo.Api.Core.Logging;

namespace Noo.Api.Core.Initialization.ServiceCollection;

public static class LoggingExtension
{
    public static void AddLogger(this IServiceCollection services, IConfiguration config)
    {
        var logConfig = config.GetSection(LogConfig.SectionName).GetOrThrow<LogConfig>();

        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.SetMinimumLevel(logConfig.MinLevel);

            if (logConfig.Mode.HasFlag(LogMode.Console))
            {
                loggingBuilder.AddProvider(new ConsoleLoggerProvider(logConfig));
            }

            if (logConfig.Mode.HasFlag(LogMode.Telegram))
            {
                if (string.IsNullOrWhiteSpace(logConfig.TelegramLogToken))
                {
                    throw new InvalidOperationException(
                        "Telegram logging is enabled but TelegramLogToken is not configured.");
                }

                if (logConfig.TelegramChatIds.Length == 0)
                {
                    throw new InvalidOperationException(
                        "Telegram logging is enabled but TelegramChatIds is empty.");
                }

                loggingBuilder.AddProvider(new TelegramLoggerProvider(logConfig));
            }

            if (logConfig.Mode == LogMode.None)
            {
                throw new InvalidOperationException("At least one logging mode must be enabled.");
            }
        });
    }
}
