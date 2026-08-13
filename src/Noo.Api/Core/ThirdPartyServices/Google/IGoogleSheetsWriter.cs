namespace Noo.Api.Core.ThirdPartyServices.Google;

public interface IGoogleSheetsWriter
{
    /// <summary>
    /// Replaces the contents of a spreadsheet with <paramref name="data"/>, creating the
    /// spreadsheet when <paramref name="spreadsheetId"/> is null or no longer reachable.
    /// </summary>
    /// <returns>The spreadsheet actually written to, and the number of data rows written.</returns>
    public Task<SheetWriteResult> WriteAsync(
        GoogleAuth auth,
        string? spreadsheetId,
        string title,
        SheetData data,
        CancellationToken ct = default
    );
}
