using System.Text.Json.Serialization;

namespace Noo.Api.Core.ThirdPartyServices.Kinescope.Models;

public record KinescopeVideo
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("duration")]
    public double? Duration { get; init; }

    [JsonPropertyName("play_link")]
    public string? PlayLink { get; init; }

    [JsonPropertyName("embed_link")]
    public string? EmbedLink { get; init; }

    [JsonPropertyName("hls_link")]
    public string? HlsLink { get; init; }

    [JsonPropertyName("poster")]
    public KinescopePoster? Poster { get; init; }
}
