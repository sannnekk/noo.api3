using System.Text.Json.Serialization;
using Noo.Api.UserSettings.Types;

namespace Noo.Api.UserSettings.DTO;

public record UserSettingsUpdateDTO
{
    [JsonPropertyName("theme")]
    public UserTheme? Theme { get; init; }

    [JsonPropertyName("fontSize")]
    public FontSize? FontSize { get; init; }

    [JsonPropertyName("backgroundImageId")]
    public Ulid? BackgroundImageId { get; init; }
}
