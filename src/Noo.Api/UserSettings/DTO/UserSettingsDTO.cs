using System.Text.Json.Serialization;
using Noo.Api.Media.DTO;
using Noo.Api.UserSettings.Types;

namespace Noo.Api.UserSettings.DTO;

public record UserSettingsDTO
{
    [JsonPropertyName("theme")]
    public UserTheme? Theme { get; init; }

    [JsonPropertyName("fontSize")]
    public FontSize? FontSize { get; init; }

    [JsonPropertyName("backgroundImage")]
    public MediaDTO? BackgroundImage { get; init; }
}
