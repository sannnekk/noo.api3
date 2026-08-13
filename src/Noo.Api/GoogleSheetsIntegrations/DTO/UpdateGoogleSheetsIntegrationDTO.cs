using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Noo.Api.GoogleSheetsIntegrations.Types;

namespace Noo.Api.GoogleSheetsIntegrations.DTO;

/// <summary>
/// The parts of an integration that can be changed after creation. Notably not the export type,
/// its parameters, or the Google account — changing any of those makes it a different export,
/// and the permission check that justified the original would not have been repeated.
/// Omitted members are left as they are.
/// </summary>
public record UpdateGoogleSheetsIntegrationDTO
{
    [JsonPropertyName("name")]
    [MinLength(1)]
    [MaxLength(255)]
    public string? Name { get; set; }

    [JsonPropertyName("schedule")]
    public GoogleSheetsIntegrationSchedule? Schedule { get; set; }

    /// <summary>
    /// Only <see cref="GoogleSheetsIntegrationStatus.Active"/> and
    /// <see cref="GoogleSheetsIntegrationStatus.Inactive"/> are accepted;
    /// <see cref="GoogleSheetsIntegrationStatus.Error"/> is set by the dispatcher alone.
    /// </summary>
    [JsonPropertyName("status")]
    public GoogleSheetsIntegrationStatus? Status { get; set; }
}
