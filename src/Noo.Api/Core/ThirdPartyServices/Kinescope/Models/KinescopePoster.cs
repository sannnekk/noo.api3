using System.Text.Json.Serialization;

namespace Noo.Api.Core.ThirdPartyServices.Kinescope.Models;

public record KinescopePoster
{
    [JsonPropertyName("original")]
    public string? Original { get; init; }

    [JsonPropertyName("md")]
    public string? Md { get; init; }

    [JsonPropertyName("sm")]
    public string? Sm { get; init; }

    [JsonPropertyName("xs")]
    public string? Xs { get; init; }
}
