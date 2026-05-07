using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.GoogleSheetsIntegrations.DTO;
using Noo.Api.GoogleSheetsIntegrations.Filters;
using Noo.Api.GoogleSheetsIntegrations.Models;

namespace Noo.Api.GoogleSheetsIntegrations.Services;

public interface IGoogleSheetsIntegrationService
{
    public Task<SearchResult<GoogleSheetsIntegrationModel>> GetIntegrationsAsync(
        GoogleSheetsIntegrationFilter filter
    );
    public Task<Ulid> CreateIntegrationAsync(CreateGoogleSheetsIntegrationDTO request);
    public void DeleteIntegration(Ulid integrationId);
    public Task RunIntegrationAsync(Ulid integrationId);
}
