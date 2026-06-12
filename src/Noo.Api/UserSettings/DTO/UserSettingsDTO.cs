using System.Text.Json.Serialization;
using Noo.Api.Media.DTO;
using Noo.Api.UserSettings.Types;

namespace Noo.Api.UserSettings.DTO;

public record UserSettingsDTO : IHasPresignedMedia
{
    [JsonPropertyName("theme")]
    public UserTheme? Theme { get; init; }

    [JsonPropertyName("fontSize")]
    public FontSize? FontSize { get; init; }

    [JsonPropertyName("backgroundImage")]
    public MediaDTO? BackgroundImage { get; init; }

    public IEnumerable<MediaDTO?> GetMediaForPresigning()
    {
        return PresignedMedia.Collect(BackgroundImage);
    }
}
