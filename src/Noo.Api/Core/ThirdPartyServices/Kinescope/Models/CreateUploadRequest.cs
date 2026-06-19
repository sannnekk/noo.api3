using System.Text.Json.Serialization;

namespace Noo.Api.Core.ThirdPartyServices.Kinescope.Models;

public record CreateUploadRequest
{
    [JsonPropertyName("type")]
    public KinescopeUploadType Type { get; init; } = KinescopeUploadType.Video;

    [JsonPropertyName("filesize")]
    public required long FileSize { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("parent_id")]
    public string? ParentId { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("subtitle")]
    public string? Subtitle { get; init; }

    [JsonPropertyName("filename")]
    public string? FileName { get; init; }
}
