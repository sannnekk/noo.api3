using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.ThirdPartyServices.Google;
using Noo.Api.GoogleSheetsIntegrations.Types;

namespace Noo.Api.GoogleSheetsIntegrations.Exports;

/// <summary>
/// Declares one kind of export: what it is called, who may run it, what parameters it accepts,
/// and how to turn those parameters into a spreadsheet.
/// </summary>
public interface IExportProfile
{
    public GoogleSheetsIntegrationType Type { get; }

    /// <summary>
    /// Coarse role gate. Data-dependent checks belong in <see cref="AuthorizeAsync"/>.
    /// </summary>
    public UserRoles[] AllowedRoles { get; }

    /// <summary>
    /// Checks that the parameters make sense for this export before anything is persisted or run.
    /// </summary>
    /// <exception cref="Noo.Api.Core.Exceptions.Http.BadRequestException">
    /// The parameters are missing, contradictory, or otherwise unusable.
    /// </exception>
    public void Validate(ExportParameters parameters);

    /// <summary>
    /// Whether this specific user may export this specific data. Re-evaluated on every scheduled
    /// rerun, not just at creation, so that losing access also stops the exports.
    /// </summary>
    public Task<bool> AuthorizeAsync(
        Ulid userId,
        UserRoles role,
        ExportParameters parameters,
        CancellationToken ct = default
    );

    public Task<SheetData> BuildAsync(
        ExportParameters parameters,
        CancellationToken ct = default
    );
}
