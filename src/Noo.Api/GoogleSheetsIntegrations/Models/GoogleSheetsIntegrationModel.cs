using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction;
using Noo.Api.Core.DataAbstraction.Model;
using Noo.Api.Core.DataAbstraction.Model.Attributes;
using Noo.Api.Core.ThirdPartyServices.Google;
using Noo.Api.Core.Utils.Json;
using Noo.Api.GoogleSheetsIntegrations.Exports;
using Noo.Api.GoogleSheetsIntegrations.Types;
using Noo.Api.Users.Models;

namespace Noo.Api.GoogleSheetsIntegrations.Models;

[Model("google_sheets_integration")]
public class GoogleSheetsIntegrationModel : BaseModel
{
    [Column("name", TypeName = DbDataTypes.Varchar255)]
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [Column("type", TypeName = GoogleSheetsIntegrationEnumDataDbTypes.GoogleIntegrationTypes)]
    [Required]
    public GoogleSheetsIntegrationType Type { get; set; } = default!;

    [JsonColumn("parameters", Converter = typeof(ExportParametersConverter))]
    [Required]
    public ExportParameters Parameters { get; set; }

    [Column(
        "schedule",
        TypeName = GoogleSheetsIntegrationEnumDataDbTypes.GoogleSheetsIntegrationSchedule
    )]
    [Required]
    public GoogleSheetsIntegrationSchedule Schedule { get; set; } =
        GoogleSheetsIntegrationSchedule.Manual;

    /// <summary>
    /// When the dispatcher should next pick this up. Null for manual integrations and for
    /// integrations that are not currently active.
    /// </summary>
    [Column("next_run_at", TypeName = DbDataTypes.DateTimeWithoutTZ)]
    public DateTime? NextRunAt { get; set; }

    [Column("last_run_at", TypeName = DbDataTypes.DateTimeWithoutTZ)]
    public DateTime? LastRunAt { get; set; }

    /// <summary>
    /// Whether the user has this integration enabled, and whether it has failed too often.
    /// Independent of <see cref="RunState"/>.
    /// </summary>
    [Column("status", TypeName = GoogleSheetsIntegrationEnumDataDbTypes.GoogleSheetsIntegrationStatus)]
    [Required]
    public GoogleSheetsIntegrationStatus Status { get; set; } =
        GoogleSheetsIntegrationStatus.Active;

    [Column(
        "run_state",
        TypeName = GoogleSheetsIntegrationEnumDataDbTypes.GoogleSheetsIntegrationRunState
    )]
    [Required]
    public GoogleSheetsIntegrationRunState RunState { get; set; } =
        GoogleSheetsIntegrationRunState.Idle;

    /// <summary>
    /// When the current run was claimed. Lets the dispatcher reclaim runs abandoned by a
    /// replica that died mid-export.
    /// </summary>
    [Column("run_started_at", TypeName = DbDataTypes.DateTimeWithoutTZ)]
    public DateTime? RunStartedAt { get; set; }

    [Column("last_error_text", TypeName = DbDataTypes.Text)]
    public string? LastErrorText { get; set; }

    [Column("last_row_count", TypeName = DbDataTypes.Int)]
    public int? LastRowCount { get; set; }

    [Column("consecutive_failure_count", TypeName = DbDataTypes.TinyIntUnsigned)]
    [Required]
    public int ConsecutiveFailureCount { get; set; }

    [JsonColumn("google_auth_data", Converter = typeof(GoogleAuthDataConverter))]
    [Required]
    public GoogleAuthData GoogleAuthData { get; set; } = default!;

    [Column("spreadsheet_id", TypeName = DbDataTypes.Varchar127)]
    public string? SpreadsheetId { get; set; }

    /// <summary>
    /// Who created the integration. Their permissions are re-checked on every scheduled rerun,
    /// so an integration cannot outlive the access that justified it.
    /// </summary>
    [Column("owner_id", TypeName = DbDataTypes.Ulid)]
    [ForeignKey(nameof(Owner))]
    [Required]
    public Ulid OwnerId { get; set; }

    #region Navigation Properties

    [DeleteBehavior(DeleteBehavior.Cascade)]
    public UserModel Owner { get; set; } = default!;

    #endregion

    public string? SpreadsheetUrl =>
        SpreadsheetId is null
            ? null
            : $"https://docs.google.com/spreadsheets/d/{SpreadsheetId}/edit";
}
