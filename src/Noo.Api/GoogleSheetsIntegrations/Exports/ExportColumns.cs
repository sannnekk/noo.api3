using Noo.Api.Core.Utils;

namespace Noo.Api.GoogleSheetsIntegrations.Exports;

/// <summary>
/// Factories for the column kinds exports actually use. Routing every column through here keeps
/// value formatting — dates in particular — identical across every export.
/// </summary>
public static class ExportColumns
{
    private const string _dateTimeFormat = "dd.MM.yyyy HH:mm";
    private const string _dateFormat = "dd.MM.yyyy";

    public static ExportColumn<TRow> Text<TRow>(string header, Func<TRow, string?> value)
    {
        return new ExportColumn<TRow>(header, row => value(row));
    }

    public static ExportColumn<TRow> Number<TRow>(string header, Func<TRow, int?> value)
    {
        return new ExportColumn<TRow>(header, row => value(row));
    }

    /// <summary>
    /// Dates are written as pre-formatted Moscow wall-clock strings. Every
    /// <see cref="DateTime"/> in the domain is already Moscow time (see <see cref="Clock"/>),
    /// so no conversion happens here — only formatting.
    /// </summary>
    public static ExportColumn<TRow> Date<TRow>(
        string header,
        Func<TRow, DateTime?> value,
        bool includeTime = true
    )
    {
        return new ExportColumn<TRow>(
            header,
            row =>
                value(row)?.ToString(
                    includeTime ? _dateTimeFormat : _dateFormat,
                    System.Globalization.CultureInfo.InvariantCulture
                )
        );
    }

    public static ExportColumn<TRow> Bool<TRow>(string header, Func<TRow, bool> value)
    {
        return new ExportColumn<TRow>(header, row => value(row) ? "Да" : "Нет");
    }

    public static ExportColumn<TRow> Enum<TRow>(string header, Func<TRow, string?> translate)
    {
        return new ExportColumn<TRow>(header, row => translate(row));
    }

    /// <summary>
    /// Score as a whole percentage of the maximum. Yields null rather than 0 when there is no
    /// score yet, so an unchecked work reads as blank instead of a real zero.
    /// </summary>
    public static ExportColumn<TRow> Percent<TRow>(
        string header,
        Func<TRow, int?> score,
        Func<TRow, int> maxScore
    )
    {
        return new ExportColumn<TRow>(
            header,
            row =>
            {
                var value = score(row);
                var max = maxScore(row);

                if (value is null || max <= 0)
                {
                    return null;
                }

                return (int)Math.Round((double)value.Value / max * 100);
            }
        );
    }
}
