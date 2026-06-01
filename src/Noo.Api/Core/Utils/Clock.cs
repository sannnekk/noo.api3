namespace Noo.Api.Core.Utils;

/// <summary>
/// Central time source for the whole application.
/// The server always operates in Moscow time (fixed UTC+3, no DST since 2014).
/// Every <see cref="DateTime"/> produced or stored by the domain represents
/// Moscow wall-clock time with <see cref="DateTimeKind.Unspecified"/>.
/// </summary>
public static class Clock
{
    public static readonly TimeZoneInfo MoscowTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow");

    /// <summary>
    /// The current moment expressed as Moscow wall-clock time.
    /// </summary>
    public static DateTime Now =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, MoscowTimeZone);

    /// <summary>
    /// The current date (at 00:00) in Moscow time.
    /// </summary>
    public static DateTime Today => Now.Date;

    /// <summary>
    /// The last whole second of the day (23:59:59) for the given Moscow date.
    /// Used to default day-granular deadlines to the end of the chosen day.
    /// Whole-second precision avoids MySQL <c>DATETIME(0)</c> rounding a
    /// fractional value up to the next day.
    /// </summary>
    public static DateTime EndOfDay(DateTime value) =>
        DateTime.SpecifyKind(value.Date.AddDays(1).AddSeconds(-1), DateTimeKind.Unspecified);

    /// <summary>
    /// Converts an arbitrary point in time to a Moscow wall-clock <see cref="DateTime"/>.
    /// Values carrying an explicit offset are converted; offset-less values are
    /// assumed to already be Moscow time.
    /// </summary>
    public static DateTime ToMoscow(DateTimeOffset value) =>
        TimeZoneInfo.ConvertTime(value, MoscowTimeZone).DateTime;

    /// <summary>
    /// Produces an unambiguous Moscow-time instant from a <see cref="DateTime"/>.
    /// <list type="bullet">
    /// <item><see cref="DateTimeKind.Utc"/> values are converted from UTC to Moscow.</item>
    /// <item><see cref="DateTimeKind.Local"/> values are converted from the host zone to Moscow.</item>
    /// <item><see cref="DateTimeKind.Unspecified"/> values are assumed to already be Moscow wall-clock.</item>
    /// </list>
    /// </summary>
    public static DateTimeOffset WithMoscowOffset(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => TimeZoneInfo.ConvertTime(new DateTimeOffset(value), MoscowTimeZone),
            DateTimeKind.Local => TimeZoneInfo.ConvertTime(new DateTimeOffset(value), MoscowTimeZone),
            _ => new DateTimeOffset(value, MoscowTimeZone.GetUtcOffset(value)),
        };
    }
}
