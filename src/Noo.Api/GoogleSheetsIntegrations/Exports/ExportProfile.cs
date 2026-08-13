using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.Exceptions.Http;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.ThirdPartyServices.Google;
using Noo.Api.GoogleSheetsIntegrations.Types;

namespace Noo.Api.GoogleSheetsIntegrations.Exports;

/// <summary>
/// Base for every export. Subclasses supply a query and a column list; this class turns the two
/// into a streamed <see cref="SheetData"/>.
/// </summary>
public abstract class ExportProfile<TRow> : IExportProfile
    where TRow : class
{
    public abstract GoogleSheetsIntegrationType Type { get; }

    public abstract UserRoles[] AllowedRoles { get; }

    public virtual void Validate(ExportParameters parameters) { }

    public virtual Task<bool> AuthorizeAsync(
        Ulid userId,
        UserRoles role,
        ExportParameters parameters,
        CancellationToken ct = default
    )
    {
        return Task.FromResult(AllowedRoles.Contains(role));
    }

    /// <summary>
    /// The rows to export, projected flat so the database only reads the columns actually needed.
    /// <para>
    /// Rows are streamed straight off this query, so the projection must be self-contained —
    /// anything a column needs has to come from here, including nested collections. A profile must
    /// not issue a second query while the stream is open: MySQL allows only one open reader per
    /// connection.
    /// </para>
    /// </summary>
    protected abstract IQueryable<TRow> Query(ExportParameters parameters);

    /// <summary>
    /// The columns to write. Parameter-aware and asynchronous so that exports whose shape is only
    /// known at runtime — poll results, with one column per question — fit the same abstraction.
    /// Runs before streaming starts, so it may query freely.
    /// </summary>
    protected abstract Task<IReadOnlyList<ExportColumn<TRow>>> ColumnsAsync(
        ExportParameters parameters,
        CancellationToken ct
    );

    public async Task<SheetData> BuildAsync(
        ExportParameters parameters,
        CancellationToken ct = default
    )
    {
        Validate(parameters);

        var columns = await ColumnsAsync(parameters, ct);

        return new SheetData(
            [.. columns.Select(column => column.Header)],
            StreamRowsAsync(parameters, columns, ct)
        );
    }

    private async IAsyncEnumerable<object?[]> StreamRowsAsync(
        ExportParameters parameters,
        IReadOnlyList<ExportColumn<TRow>> columns,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct
    )
    {
        var rows = Query(parameters).AsNoTracking().AsAsyncEnumerable();

        await foreach (var row in rows.WithCancellation(ct))
        {
            yield return [.. columns.Select(column => column.Value(row))];
        }
    }

    protected static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new BadRequestException(message);
        }
    }
}
