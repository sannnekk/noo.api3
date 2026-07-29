using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Noo.Api.NooTube.Types;

namespace Noo.Api.NooTube.DTO;

/// <summary>
/// Reactions of every user on a single video. Unlike course material
/// reactions, video reaction counts are public: everyone who may watch the
/// video sees them.
/// </summary>
public record NooTubeVideoReactionsDTO
{
    /// <summary>
    /// The reaction of the current user, or null if they did not react yet.
    /// </summary>
    [JsonPropertyName("myReaction")]
    public VideoReaction? MyReaction { get; init; }

    /// <summary>
    /// How many users picked each reaction. Reactions nobody picked are omitted.
    /// </summary>
    [Required]
    [JsonPropertyName("counts")]
    public IReadOnlyDictionary<VideoReaction, int> Counts { get; init; } =
        new Dictionary<VideoReaction, int>();
}
