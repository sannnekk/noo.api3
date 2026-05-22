using Microsoft.Extensions.Options;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Sessions;
using Noo.Api.Sessions.Services;
using Noo.Api.Statistics.DTO;
using Noo.Api.Users.Services;

namespace Noo.Api.Statistics.Services;

[RegisterScoped(typeof(IUserStatisticsCollector))]
public class UserStatisticsCollector : IUserStatisticsCollector
{
    private readonly IUserRepository _userRepository;

    private readonly IOnlineService _onlineService;

    private readonly IActiveUserService _activeUserService;

    private readonly SessionConfig _sessionOptions;

    public UserStatisticsCollector(
        IOnlineService onlineService,
        IUserRepository userRepository,
        IActiveUserService activeUserService,
        IOptions<SessionConfig> sessionOptions
    )
    {
        _userRepository = userRepository;
        _onlineService = onlineService;
        _activeUserService = activeUserService;
        _sessionOptions = sessionOptions.Value;
    }

    public async Task<StatisticsBlockDTO> GetUserStatisticsAsync(DateTime from, DateTime to)
    {
        var onlinePerRole = await _onlineService.GetOnlineCountByRolesAsync();
        var activePerRole = await _activeUserService.GetActiveCountByRolesAsync();
        var registrations = await _userRepository.GetRegistrationsByDateRangeAsync(from, to);
        var totalPerRole = await _userRepository.GetTotalUsersByRolesAsync(from, to);

        return new StatisticsBlockDTO
        {
            Title = "Пользователи",
            Graph = new StatisticsGraphDTO
            {
                Label = "Регистрации по дням",
                Lines =
                [
                    new()
                    {
                        Name = "Регистрации по дням",
                        Values = StatisticsHelpers.NormalizeDictionary(registrations),
                    },
                ],
            },
            NumberBlocks =
            [
                new()
                {
                    Title = "Всего пользователей",
                    Value = totalPerRole.Values.Sum(),
                    SubValues = StatisticsHelpers.NormalizeDictionary(totalPerRole),
                },
                new()
                {
                    Title = "Онлайн",
                    Description = $"TTL ~ {_sessionOptions.OnlineTtlMinutes} мин",
                    Value = onlinePerRole.Values.Sum(),
                    SubValues = StatisticsHelpers.NormalizeDictionary(onlinePerRole),
                },
                new()
                {
                    Title = "Активные",
                    Description = $"За последние {_sessionOptions.ActiveTtlDays} дней",
                    Value = activePerRole.Values.Sum(),
                    SubValues = StatisticsHelpers.NormalizeDictionary(activePerRole),
                },
            ],
        };
    }
}
