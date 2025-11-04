using Noo.Api.Core.DataAbstraction.Db;
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

    public UserStatisticsCollector(IUnitOfWork unitOfWork, IOnlineService onlineService, IActiveUserService activeUserService)
    {
        _userRepository = unitOfWork.UserRepository();
        _onlineService = onlineService;
        _activeUserService = activeUserService;
    }

    public async Task<StatisticsBlockDTO> GetUserStatisticsAsync(DateTime from, DateTime to)
    {
        var registrationsTask = _userRepository.GetRegistrationsByDateRangeAsync(from, to);
        var totalPerRoleTask = _userRepository.GetTotalUsersByRolesAsync(from, to);
        var onlinePerRoleTask = _onlineService.GetOnlineCountByRolesAsync();
        var activePerRoleTask = _activeUserService.GetActiveCountByRolesAsync();

        await Task.WhenAll(registrationsTask, totalPerRoleTask, onlinePerRoleTask, activePerRoleTask);

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
                        Values = StatisticsHelpers.NormalizeDictionary(registrationsTask.Result)
                    }
                ]
            },
            NumberBlocks =
            [
                new()
                {
                    Title = "Всего пользователей",
                    Value = totalPerRoleTask.Result.Values.Sum(),
                    SubValues = StatisticsHelpers.NormalizeDictionary(totalPerRoleTask.Result)
                },
                new()
                {
                    Title = "Онлайн",
                    Description = $"TTL ~ {SessionConfig.OnlineTtlMinutes} мин",
                    Value = onlinePerRoleTask.Result.Values.Sum(),
                    SubValues = StatisticsHelpers.NormalizeDictionary(onlinePerRoleTask.Result)
                },
                new()
                {
                    Title = "Активные",
                    Description = $"За последние {SessionConfig.ActiveTtlDays} дн.",
                    Value = activePerRoleTask.Result.Values.Sum(),
                    SubValues = StatisticsHelpers.NormalizeDictionary(activePerRoleTask.Result)
                }
            ]
        };
    }
}
