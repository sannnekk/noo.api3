using Noo.Api.Core.Utils;

namespace Noo.Api.GoogleSheetsIntegrations.Types;

public enum GoogleSheetsIntegrationSchedule
{
    /// <summary>
    /// Never reruns on its own; only the "run now" action refreshes the sheet.
    /// </summary>
    Manual,

    Hourly,

    Daily,

    Weekly,
}

public static class GoogleSheetsIntegrationScheduleExtensions
{
    /// <summary>
    /// When an integration on this schedule should next run, counted from <paramref name="from"/>.
    /// Null for <see cref="GoogleSheetsIntegrationSchedule.Manual"/>, which never self-schedules.
    /// </summary>
    public static DateTime? NextRunAt(
        this GoogleSheetsIntegrationSchedule schedule,
        DateTime? from = null
    )
    {
        var origin = from ?? Clock.Now;

        return schedule switch
        {
            GoogleSheetsIntegrationSchedule.Hourly => origin.AddHours(1),
            GoogleSheetsIntegrationSchedule.Daily => origin.AddDays(1),
            GoogleSheetsIntegrationSchedule.Weekly => origin.AddDays(7),
            _ => null,
        };
    }
}
