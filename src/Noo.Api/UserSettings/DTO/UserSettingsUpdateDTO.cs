using System.Text.Json.Serialization;
using Noo.Api.UserSettings.Types;

namespace Noo.Api.UserSettings.DTO;

public record UserSettingsUpdateDTO
{
    [JsonPropertyName("fontSize")]
    public FontSize? FontSize { get; init; }

    [JsonPropertyName("backgroundImageId")]
    public Ulid? BackgroundImageId { get; init; }
}
