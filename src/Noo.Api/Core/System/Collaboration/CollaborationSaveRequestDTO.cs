using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Noo.Api.Core.System.Collaboration;

public record CollaborationSaveRequestDTO
{
    /// <summary>
    /// The sequence number the client had applied when it pressed Save. The save is refused if
    /// the draft has moved since, so an operation still in flight is never quietly dropped.
    /// </summary>
    [Required]
    [Range(0, long.MaxValue)]
    [JsonPropertyName("expectedSeq")]
    public long ExpectedSeq { get; set; }
}
