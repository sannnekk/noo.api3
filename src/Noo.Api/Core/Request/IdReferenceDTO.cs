using System.Text.Json.Serialization;

namespace Noo.Api.Core.Request;

public record IdReferenceDTO
{
    [JsonPropertyName("id")]
    public Ulid Id { get; init; }
}
