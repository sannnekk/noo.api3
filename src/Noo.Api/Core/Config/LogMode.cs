namespace Noo.Api.Core.Config;

[Flags]
public enum LogMode
{
    None = 0,
    Console = 1,
    Telegram = 2,
    All = Console | Telegram
}
