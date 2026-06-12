using System.Text.Json.Serialization;
using Noo.Api.Media.DTO;

namespace Noo.Api.Core.Response;

public record ApiResponseDTO<T>(
    [property: JsonPropertyName("data")]
    T? Data,

    [property: JsonPropertyName("meta")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    object? Metadata
) : IHasPresignedMedia
{
    public IEnumerable<MediaDTO?> GetMediaForPresigning()
    {
        return PresignedMedia.Collect(Data);
    }
}
