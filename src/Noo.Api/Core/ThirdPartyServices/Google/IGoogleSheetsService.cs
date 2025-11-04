
namespace Noo.Api.Core.ThirdPartyServices.Google;

public interface IGoogleSheetsService
{
    public GoogleSheet CreateSheet(GoogleAuth auth, string name);
    public Task<GoogleSheet> GetSheetAsync(GoogleAuth auth, string spreadsheetId);
    public Task<string> SaveAsync(GoogleAuth auth, GoogleSheet sheet);
}

