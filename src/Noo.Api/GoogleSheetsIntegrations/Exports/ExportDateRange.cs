using Noo.Api.Core.Utils;

namespace Noo.Api.GoogleSheetsIntegrations.Exports;

public static class ExportDateRange
{
    /// <summary>
    /// Resolves the inclusive upper bound of a creation-date filter. A bare date (midnight) is
    /// widened to the end of that day, so "to 5 August" includes everything created on 5 August
    /// rather than only the instant it began.
    /// </summary>
    public static DateTime? InclusiveEnd(DateTime? createdTo)
    {
        if (createdTo is not { } value)
        {
            return null;
        }

        return value.TimeOfDay == TimeSpan.Zero ? Clock.EndOfDay(value) : value;
    }
}
