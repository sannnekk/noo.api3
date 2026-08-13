namespace Noo.Api.GoogleSheetsIntegrations.Exports;

/// <summary>
/// One spreadsheet column: a header and how to read its value out of a row.
/// </summary>
public sealed record ExportColumn<TRow>(string Header, Func<TRow, object?> Value);
