namespace Noo.Api.Core.Config;

[Flags]
public enum ConsoleLogDecoration
{
    None = 0,
    Color = 1,
    Emoji = 2,
    All = Color | Emoji
}
