using System.Net;
using Google;
using Google.Apis.Drive.v3;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Microsoft.Extensions.Options;
using Noo.Api.Core.Config.Env;
using Noo.Api.Core.Utils.DI;
using SheetRequest = Google.Apis.Sheets.v4.Data.Request;

namespace Noo.Api.Core.ThirdPartyServices.Google;

[RegisterSingleton(typeof(IGoogleSheetsWriter))]
public class GoogleSheetsWriter : IGoogleSheetsWriter
{
    private const string _applicationName = "Noo.Api";
    private const string _sheetTitle = "Данные";
    private const int _chunkSize = 5000;
    private const int _maxRetries = 5;

    private static readonly HttpStatusCode[] _retryableStatuses =
    [
        HttpStatusCode.TooManyRequests,
        HttpStatusCode.InternalServerError,
        HttpStatusCode.BadGateway,
        HttpStatusCode.ServiceUnavailable,
        HttpStatusCode.GatewayTimeout,
    ];

    private readonly GoogleConfig _config;

    private readonly ILogger<GoogleSheetsWriter> _logger;

    public GoogleSheetsWriter(IOptions<GoogleConfig> config, ILogger<GoogleSheetsWriter> logger)
    {
        _config = config.Value;
        _logger = logger;
    }

    public async Task<SheetWriteResult> WriteAsync(
        GoogleAuth auth,
        string? spreadsheetId,
        string title,
        SheetData data,
        CancellationToken ct = default
    )
    {
        using var sheets = auth.CreateService(i => new SheetsService(i), _applicationName);
        using var drive = auth.CreateService(i => new DriveService(i), _applicationName);

        var (id, sheetId) = await ResolveSpreadsheetAsync(sheets, spreadsheetId, title, ct);

        await ClearAsync(sheets, id, ct);
        await AppendChunkAsync(sheets, id, [[.. data.Headers]], ct);

        var rowCount = await AppendRowsAsync(sheets, id, data, ct);

        await FormatAsync(sheets, id, sheetId, data.Headers.Count, ct);
        await TagAsync(drive, id, ct);

        return new SheetWriteResult(id, rowCount);
    }

    private async Task<(string Id, int SheetId)> ResolveSpreadsheetAsync(
        SheetsService sheets,
        string? spreadsheetId,
        string title,
        CancellationToken ct
    )
    {
        if (!string.IsNullOrWhiteSpace(spreadsheetId))
        {
            try
            {
                var existing = await ExecuteWithRetryAsync(
                    () => sheets.Spreadsheets.Get(spreadsheetId).ExecuteAsync(ct),
                    ct
                );

                return (existing.SpreadsheetId, existing.Sheets[0].Properties.SheetId ?? 0);
            }
            catch (GoogleApiException exception)
                when (exception.HttpStatusCode
                        is HttpStatusCode.NotFound
                            or HttpStatusCode.Forbidden
                )
            {
                // The user deleted the spreadsheet, or it was created by a different
                // Google account. Fall through and make a fresh one rather than failing
                // the integration permanently.
                _logger.LogWarning(
                    "Spreadsheet {SpreadsheetId} is no longer reachable, creating a new one.",
                    spreadsheetId
                );
            }
        }

        var created = await ExecuteWithRetryAsync(
            () =>
                sheets
                    .Spreadsheets.Create(
                        new Spreadsheet
                        {
                            Properties = new SpreadsheetProperties { Title = title },
                            Sheets =
                            [
                                new Sheet
                                {
                                    Properties = new SheetProperties { Title = _sheetTitle },
                                },
                            ],
                        }
                    )
                    .ExecuteAsync(ct),
            ct
        );

        return (created.SpreadsheetId, created.Sheets[0].Properties.SheetId ?? 0);
    }

    private Task ClearAsync(SheetsService sheets, string spreadsheetId, CancellationToken ct)
    {
        return ExecuteWithRetryAsync(
            () =>
                sheets
                    .Spreadsheets.Values.Clear(new ClearValuesRequest(), spreadsheetId, _sheetTitle)
                    .ExecuteAsync(ct),
            ct
        );
    }

    private async Task<int> AppendRowsAsync(
        SheetsService sheets,
        string spreadsheetId,
        SheetData data,
        CancellationToken ct
    )
    {
        var chunk = new List<IList<object?>>(_chunkSize);
        var rowCount = 0;

        await foreach (var row in data.Rows.WithCancellation(ct))
        {
            chunk.Add(row);
            rowCount++;

            if (rowCount > _config.MaxExportRows)
            {
                throw new InvalidOperationException(
                    $"Экспорт превышает лимит в {_config.MaxExportRows} строк. Сузьте параметры выгрузки."
                );
            }

            if (chunk.Count == _chunkSize)
            {
                await AppendChunkAsync(sheets, spreadsheetId, chunk, ct);
                chunk.Clear();
            }
        }

        if (chunk.Count > 0)
        {
            await AppendChunkAsync(sheets, spreadsheetId, chunk, ct);
        }

        return rowCount;
    }

    private Task AppendChunkAsync(
        SheetsService sheets,
        string spreadsheetId,
        IList<IList<object?>> values,
        CancellationToken ct
    )
    {
        return ExecuteWithRetryAsync(
            () =>
            {
                // Append grows the grid on its own, so no explicit resize is needed even
                // though a new spreadsheet starts at 1000 rows.
                var request = sheets.Spreadsheets.Values.Append(
                    new ValueRange { Values = values },
                    spreadsheetId,
                    _sheetTitle
                );

                request.ValueInputOption = SpreadsheetsResource
                    .ValuesResource
                    .AppendRequest
                    .ValueInputOptionEnum
                    .RAW;
                request.InsertDataOption = SpreadsheetsResource
                    .ValuesResource
                    .AppendRequest
                    .InsertDataOptionEnum
                    .OVERWRITE;

                return request.ExecuteAsync(ct);
            },
            ct
        );
    }

    private Task FormatAsync(
        SheetsService sheets,
        string spreadsheetId,
        int sheetId,
        int columnCount,
        CancellationToken ct
    )
    {
        var requests = new List<SheetRequest>
        {
            new()
            {
                UpdateSheetProperties = new UpdateSheetPropertiesRequest
                {
                    Properties = new SheetProperties
                    {
                        SheetId = sheetId,
                        GridProperties = new GridProperties { FrozenRowCount = 1 },
                    },
                    Fields = "gridProperties.frozenRowCount",
                },
            },
            new()
            {
                RepeatCell = new RepeatCellRequest
                {
                    Range = new GridRange
                    {
                        SheetId = sheetId,
                        StartRowIndex = 0,
                        EndRowIndex = 1,
                    },
                    Cell = new CellData
                    {
                        UserEnteredFormat = new CellFormat
                        {
                            TextFormat = new TextFormat { Bold = true },
                        },
                    },
                    Fields = "userEnteredFormat.textFormat.bold",
                },
            },
            new()
            {
                AutoResizeDimensions = new AutoResizeDimensionsRequest
                {
                    Dimensions = new DimensionRange
                    {
                        SheetId = sheetId,
                        Dimension = "COLUMNS",
                        StartIndex = 0,
                        EndIndex = columnCount,
                    },
                },
            },
        };

        return ExecuteWithRetryAsync(
            () =>
                sheets
                    .Spreadsheets.BatchUpdate(
                        new BatchUpdateSpreadsheetRequest { Requests = requests },
                        spreadsheetId
                    )
                    .ExecuteAsync(ct),
            ct
        );
    }

    private async Task TagAsync(DriveService drive, string spreadsheetId, CancellationToken ct)
    {
        try
        {
            var file = new global::Google.Apis.Drive.v3.Data.File
            {
                AppProperties = new Dictionary<string, string>
                {
                    ["tags"] = string.Join(',', GoogleDriveTags.SheetTags),
                },
            };

            await ExecuteWithRetryAsync(
                () => drive.Files.Update(file, spreadsheetId).ExecuteAsync(ct),
                ct
            );
        }
        catch (GoogleApiException exception)
        {
            // Tagging is cosmetic — never fail a completed export over it.
            _logger.LogWarning(
                exception,
                "Could not tag spreadsheet {SpreadsheetId}.",
                spreadsheetId
            );
        }
    }

    private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> action, CancellationToken ct)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await action();
            }
            catch (Exception exception)
                when (!ct.IsCancellationRequested
                    && IsRetryable(exception)
                    && attempt < _maxRetries
                )
            {
                var delay = TimeSpan.FromMilliseconds(
                    (Math.Pow(2, attempt) * 250) + Random.Shared.Next(0, 250)
                );

                _logger.LogWarning(
                    exception,
                    "Google Sheets call failed (attempt {Attempt}/{MaxRetries}), retrying in {Delay}.",
                    attempt,
                    _maxRetries,
                    delay
                );

                await Task.Delay(delay, ct);
            }
        }
    }

    private static bool IsRetryable(Exception exception)
    {
        return exception switch
        {
            GoogleApiException google => _retryableStatuses.Contains(google.HttpStatusCode),
            HttpRequestException => true,
            TaskCanceledException => true,
            _ => false,
        };
    }
}
