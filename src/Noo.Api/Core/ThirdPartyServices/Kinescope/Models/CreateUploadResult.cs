using System.Text.Json.Serialization;

namespace Noo.Api.Core.ThirdPartyServices.Kinescope.Models;

public record CreateUploadResult
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("endpoint")]
    public required string Endpoint { get; init; }
}
