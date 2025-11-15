using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Noo.Api.GoogleSheetsIntegrations.Types;

namespace Noo.Api.GoogleSheetsIntegrations.DTO;

public record GoogleSheetsIntegrationDTO
{
    [Required]
    [JsonPropertyName("_entityName")]
    public string EntityName { get; init; } = "GoogleSheetsIntegration";

    [Required]
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [Required]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("type")]
    public GoogleSheetsIntegrationType Type { get; set; } = default!;

    [Required]
    [JsonPropertyName("selectorValue")]
    public string SelectorValue { get; set; } = string.Empty;

    [JsonPropertyName("lastRunAt")]
    public DateTime? LastRunAt { get; set; }

    [Required]
    [JsonPropertyName("status")]
    public GoogleSheetsIntegrationStatus Status { get; set; } = GoogleSheetsIntegrationStatus.Active;

    [JsonPropertyName("lastErrorText")]
    public string? LastErrorText { get; set; }

    [Required]
    [JsonPropertyName("cronPattern")]
    public string CronPattern { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("googleAccount")]
    public string GoogleAccount { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }
}
