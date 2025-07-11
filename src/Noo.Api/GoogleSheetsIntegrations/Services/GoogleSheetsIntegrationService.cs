using Noo.Api.Core.DataAbstraction.Criteria;
using Noo.Api.Core.Utils.DI;
using Noo.Api.GoogleSheetsIntegrations.DTO;
using Noo.Api.GoogleSheetsIntegrations.Models;

namespace Noo.Api.GoogleSheetsIntegrations.Services;

[RegisterScoped(typeof(IGoogleSheetsIntegrationService))]
public class GoogleSheetsIntegrationService : IGoogleSheetsIntegrationService
{
    public Task<Ulid> CreateIntegrationAsync(CreateGoogleSheetsIntegrationDTO request)
    {
        throw new NotImplementedException();
    }

    public Task DeleteIntegrationAsync(Ulid integrationId)
    {
        throw new NotImplementedException();
    }

    public Task<(IEnumerable<GoogleSheetsIntegrationDTO>, int)> GetIntegrationsAsync(Criteria<GoogleSheetsInegrationModel> criteria)
    {
        throw new NotImplementedException();
    }

    public Task RunIntegrationAsync(Ulid integrationId)
    {
        throw new NotImplementedException();
    }
}
