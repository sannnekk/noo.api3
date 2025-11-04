using Google.Apis.Drive.v3;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Noo.Api.Core.Utils.DI;

namespace Noo.Api.Core.ThirdPartyServices.Google;

[RegisterSingleton(typeof(IGoogleSheetsService))]
public class GoogleSheetsService : IGoogleSheetsService
{
    private const string _applicationName = "Noo.Api";

    public GoogleSheet CreateSheet(GoogleAuth auth, string name)
    {
        return new GoogleSheet(name);
    }

    public Task<GoogleSheet> GetSheetAsync(GoogleAuth auth, string spreadsheetId)
    {
        // Only metadata needed now; data will be overwritten on Save
        var sheet = new GoogleSheet("LoadedSheet") { SpreadsheetId = spreadsheetId };
        return Task.FromResult(sheet);
    }

    public async Task<string> SaveAsync(GoogleAuth auth, GoogleSheet sheet)
    {
        var (table, tableDirty, tags, tagsDirty) = sheet.Snapshot();
        var sheetsService = auth.CreateService(i => new SheetsService(i), _applicationName);
        var driveService = auth.CreateService(i => new DriveService(i), _applicationName);

        string? spreadsheetId = sheet.SpreadsheetId;

        if (spreadsheetId is null)
        {
            // Create new spreadsheet
            var createRequest = new Spreadsheet
            {
                Properties = new SpreadsheetProperties
                {
                    Title = sheet.Name
                }
            };

            var created = await sheetsService.Spreadsheets.Create(createRequest).ExecuteAsync();
            spreadsheetId = created.SpreadsheetId;
            sheet.SpreadsheetId = spreadsheetId;
            sheet.MarkClean();
        }

        if (tableDirty && table is not null && spreadsheetId is not null)
        {
            // Clear existing first sheet then write values
            var firstSheetName = sheet.Name; // assume first sheet title same as spreadsheet title
            var valueRange = new ValueRange
            {
                Values = BuildValueMatrix(table)
            };
            // Write starting at A1
            var update = sheetsService.Spreadsheets.Values.Update(valueRange, spreadsheetId, "A1");
            update.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
            await update.ExecuteAsync();
        }

        if (tagsDirty && spreadsheetId is not null && tags.Any())
        {
            // Update Drive file properties (appProperties) to store tags as comma-separated list
            var file = new global::Google.Apis.Drive.v3.Data.File
            {
                AppProperties = new Dictionary<string, string>
                {
                    ["tags"] = string.Join(',', tags)
                }
            };
            await driveService.Files.Update(file, spreadsheetId).ExecuteAsync();
        }

        return spreadsheetId!;
    }

    private static IList<IList<object?>> BuildValueMatrix(DataTable table)
    {
        var matrix = new List<IList<object?>>()
        {
            table.Headers.Cast<object?>().ToList()
        };

        foreach (var row in table.Rows)
        {
            matrix.Add(row.ToList());
        }

        return matrix;
    }
}
