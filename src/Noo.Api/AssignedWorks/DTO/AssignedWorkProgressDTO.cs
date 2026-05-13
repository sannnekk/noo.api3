using System.Text.Json.Serialization;
using Noo.Api.AssignedWorks.Types;

namespace Noo.Api.AssignedWorks.DTO;

public record AssignedWorkProgressDTO
{
    [JsonPropertyName("assignedWorkId")]
    public Ulid AssignedWorkId { get; init; }

    [JsonPropertyName("solveStatus")]
    public AssignedWorkSolveStatus? SolveStatus { get; init; }

    [JsonPropertyName("solvedAt")]
    public DateTime? SolvedAt { get; init; }

    [JsonPropertyName("checkStatus")]
    public AssignedWorkCheckStatus? CheckStatus { get; init; }

    [JsonPropertyName("checkedAt")]
    public DateTime? CheckedAt { get; init; }

    [JsonPropertyName("score")]
    public int? Score { get; init; }

    [JsonPropertyName("maxScore")]
    public int? MaxScore { get; init; }

    [JsonPropertyName("attempt")]
    public int Attempt { get; init; } = 1;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; init; }
}
