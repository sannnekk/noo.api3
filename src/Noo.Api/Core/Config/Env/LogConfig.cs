using System.ComponentModel.DataAnnotations;

namespace Noo.Api.Core.Config.Env;

public class LogConfig : IConfig
{
    public static string SectionName => "Logs";

    [Required]
    public required LogMode Mode { get; init; }

    public LogLevel MinLevel { get; init; } = LogLevel.Information;

    public ConsoleLogDecoration ConsoleDecorations { get; init; } = ConsoleLogDecoration.Color;

    public string? TelegramLogToken { get; init; } = null;

    public string[] TelegramChatIds { get; init; } = [];
}
