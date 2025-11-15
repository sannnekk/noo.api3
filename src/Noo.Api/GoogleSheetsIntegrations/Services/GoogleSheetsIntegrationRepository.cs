using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Utils.DI;
using Noo.Api.GoogleSheetsIntegrations.Models;

namespace Noo.Api.GoogleSheetsIntegrations.Services;

[RegisterScoped(typeof(IGoogleSheetsIntegrationRepository))]
public class GoogleSheetsIntegrationRepository : Repository<GoogleSheetsIntegrationModel>, IGoogleSheetsIntegrationRepository
{
    public GoogleSheetsIntegrationRepository(NooDbContext dbContext) : base(dbContext)
    {
    }
}
