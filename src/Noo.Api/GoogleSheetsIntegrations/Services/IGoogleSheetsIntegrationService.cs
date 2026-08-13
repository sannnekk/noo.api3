using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.GoogleSheetsIntegrations.DTO;
using Noo.Api.GoogleSheetsIntegrations.Filters;
using Noo.Api.GoogleSheetsIntegrations.Models;

namespace Noo.Api.GoogleSheetsIntegrations.Services;

/// <summary>
/// Request-side operations on integrations. Every method authorizes against the current user;
/// running an export is the dispatcher's job, not this service's.
/// </summary>
public interface IGoogleSheetsIntegrationService
{
    public GoogleOAuthUrlDTO CreateOAuthUrl();

    public Task<SearchResult<GoogleSheetsIntegrationModel>> GetIntegrationsAsync(
        GoogleSheetsIntegrationFilter filter
    );

    public Task<Ulid> CreateIntegrationAsync(
        CreateGoogleSheetsIntegrationDTO request,
        CancellationToken ct = default
    );

    public Task UpdateIntegrationAsync(
        Ulid integrationId,
        UpdateGoogleSheetsIntegrationDTO request
    );

    /// <summary>
    /// Queues an integration for the dispatcher and returns immediately — an export of this
    /// size cannot run inside a request.
    /// </summary>
    public Task QueueIntegrationAsync(Ulid integrationId, CancellationToken ct = default);

    public Task DeleteIntegrationAsync(Ulid integrationId);
}
