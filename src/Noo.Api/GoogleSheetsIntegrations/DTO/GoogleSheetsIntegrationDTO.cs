using System.Text.Json.Serialization;
using Noo.Api.GoogleSheetsIntegrations.Types;

namespace Noo.Api.GoogleSheetsIntegrations.DTO;

public record GoogleSheetsIntegrationDTO
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public GoogleSheetsIntegrationType Type { get; set; } = default!;

    [JsonPropertyName("selectorValue")]
    public string SelectorValue { get; set; } = string.Empty;

    [JsonPropertyName("lastRunAt")]
    public DateTime? LastRunAt { get; set; }

    [JsonPropertyName("status")]
    public GoogleSheetsIntegrationStatus Status { get; set; } = GoogleSheetsIntegrationStatus.Active;

    [JsonPropertyName("lastErrorText")]
    public string? LastErrorText { get; set; }

    [JsonPropertyName("cronPattern")]
    public string CronPattern { get; set; } = string.Empty;

    [JsonPropertyName("googleAccount")]
    public string GoogleAccount { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }
}
