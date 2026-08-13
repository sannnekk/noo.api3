namespace Noo.Api.Core.ThirdPartyServices.Google;

/// <summary>
/// A table destined for a spreadsheet. Rows are streamed rather than materialized so that
/// exports of tens of thousands of rows never sit in memory in full.
/// </summary>
public sealed record SheetData(
    IReadOnlyList<string> Headers,
    IAsyncEnumerable<object?[]> Rows
);

public readonly record struct SheetWriteResult(string SpreadsheetId, int RowCount);
