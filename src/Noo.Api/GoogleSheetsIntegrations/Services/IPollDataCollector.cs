using Noo.Api.Core.ThirdPartyServices.Google;

namespace Noo.Api.GoogleSheetsIntegrations.Services;

public interface IPollDataCollector
{
    public Task<DataTable> GetPollResultsAsync(Ulid pollId);
}
