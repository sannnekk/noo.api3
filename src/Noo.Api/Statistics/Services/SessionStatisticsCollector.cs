using Noo.Api.Core.Utils.DI;
using Noo.Api.Core.Utils.UserAgent;
using Noo.Api.Sessions.Services;
using Noo.Api.Sessions.Types;
using Noo.Api.Statistics.DTO;

namespace Noo.Api.Statistics.Services;

[RegisterScoped(typeof(ISessionStatisticsCollector))]
public class SessionStatisticsCollector : ISessionStatisticsCollector
{
    private readonly ISessionRepository _sessionRepository;

    public SessionStatisticsCollector(ISessionRepository sessionRepository)
    {
        _sessionRepository = sessionRepository;
    }

    public async Task<StatisticsBlockDTO> GetDeviceStatisticsAsync(DateTime from, DateTime to)
    {
        var browsers = await _sessionRepository.GetUserCountByBrowserAsync(from, to);
        var deviceTypes = await _sessionRepository.GetUserCountByDeviceTypeAsync(from, to);

        return new StatisticsBlockDTO
        {
            Title = "Устройства",
            Description =
                "Здесь отображается, чем пользовались заходившие на платформу за выбранный промежуток времени. Тот, кто заходил с нескольких устройств или браузеров, учитывается в каждом из них.",
            Distributions = [BuildBrowsers(browsers), BuildDeviceTypes(deviceTypes)],
        };
    }

    private static StatisticsDistributionDTO BuildBrowsers(IReadOnlyList<BrowserUserCount> counts)
    {
        var perKind = counts
            .GroupBy(c => ParseBrowser(c.Browser))
            .ToDictionary(g => g.Key, g => g.Sum(c => c.UserCount));

        return new StatisticsDistributionDTO
        {
            Title = "Браузеры",
            Entries = ToEntries(perKind, kind => kind.Translate(), kind => kind.ToWireName()),
        };
    }

    private static StatisticsDistributionDTO BuildDeviceTypes(
        IReadOnlyList<DeviceTypeUserCount> counts
    )
    {
        // Every kind is listed, zeroes included: an absent tablet share reads as missing data.
        var perType = Enum.GetValues<DeviceType>()
            .ToDictionary(
                type => type,
                type => counts.FirstOrDefault(c => c.DeviceType == type)?.UserCount ?? 0
            );

        return new StatisticsDistributionDTO
        {
            Title = "Типы устройств",
            Entries = ToEntries(perType, type => type.Translate(), type => type.ToWireName()),
        };
    }

    /// <summary>
    /// Older sessions hold whatever string the parser of the day produced, so anything that is no
    /// longer a known browser falls into <see cref="BrowserKind.Unknown"/>.
    /// </summary>
    private static BrowserKind ParseBrowser(string? browser)
    {
        return Enum.TryParse<BrowserKind>(browser, ignoreCase: true, out var kind)
            ? kind
            : BrowserKind.Unknown;
    }

    private static List<StatisticsDistributionEntryDTO> ToEntries<TKey>(
        Dictionary<TKey, int> counts,
        Func<TKey, string> label,
        Func<TKey, string> icon
    )
        where TKey : notnull
    {
        return counts
            .OrderByDescending(kvp => kvp.Value)
            .ThenBy(kvp => label(kvp.Key))
            .Select(kvp => new StatisticsDistributionEntryDTO
            {
                Label = label(kvp.Key),
                Value = kvp.Value,
                Icon = icon(kvp.Key),
            })
            .ToList();
    }
}
