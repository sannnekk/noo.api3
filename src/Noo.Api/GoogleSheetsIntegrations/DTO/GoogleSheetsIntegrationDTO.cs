using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Noo.Api.GoogleSheetsIntegrations.Types;

namespace Noo.Api.GoogleSheetsIntegrations.DTO;

public record GoogleSheetsIntegrationDTO
{
    [Required]
    [JsonPropertyName("_entityName")]
    public string EntityName => "GoogleSheetsIntegration";

    [Required]
    [JsonPropertyName("id")]
    public Ulid Id { get; set; }

    [Required]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("type")]
    public GoogleSheetsIntegrationType Type { get; set; } = default!;

    [Required]
    [JsonPropertyName("parameters")]
    public ExportParametersDTO Parameters { get; set; } = new();

    [Required]
    [JsonPropertyName("schedule")]
    public GoogleSheetsIntegrationSchedule Schedule { get; set; }

    [JsonPropertyName("nextRunAt")]
    public DateTime? NextRunAt { get; set; }

    [JsonPropertyName("lastRunAt")]
    public DateTime? LastRunAt { get; set; }

    [Required]
    [JsonPropertyName("status")]
    public GoogleSheetsIntegrationStatus Status { get; set; } =
        GoogleSheetsIntegrationStatus.Active;

    [Required]
    [JsonPropertyName("runState")]
    public GoogleSheetsIntegrationRunState RunState { get; set; }

    [JsonPropertyName("lastErrorText")]
    public string? LastErrorText { get; set; }

    [JsonPropertyName("lastRowCount")]
    public int? LastRowCount { get; set; }

    [JsonPropertyName("googleAccount")]
    public string? GoogleAccount { get; set; }

    [JsonPropertyName("spreadsheetUrl")]
    public string? SpreadsheetUrl { get; set; }

    [Required]
    [JsonPropertyName("ownerId")]
    public Ulid OwnerId { get; set; }

    [Required]
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }
}
