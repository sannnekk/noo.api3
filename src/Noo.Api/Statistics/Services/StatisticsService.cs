using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Exceptions.Http;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Statistics.DTO;
using Noo.Api.Statistics.Exceptions;
using Noo.Api.Users.Services;
using Noo.Api.Works.Types;

namespace Noo.Api.Statistics.Services;

[RegisterScoped(typeof(IStatisticsService))]
public class StatisticsService : IStatisticsService
{
    private readonly IUserRepository _userRepository;

    private readonly IUserStatisticsCollector _userStatisticsCollector;

    private readonly IAssignedWorkStatisticsCollector _assignedWorkStatisticsCollector;

    public StatisticsService(IUserStatisticsCollector userStatisticsCollector, IAssignedWorkStatisticsCollector assignedWorkStatisticsCollector, IUnitOfWork unitOfWork)
    {
        _userStatisticsCollector = userStatisticsCollector;
        _assignedWorkStatisticsCollector = assignedWorkStatisticsCollector;
        _userRepository = unitOfWork.UserRepository();
    }

    public async Task<StatisticsDTO> GetUserStatisticsAsync(Ulid userId, WorkType? workType, DateTime? from, DateTime? to)
    {
        var (fromDate, toDate) = NormalizeDateRange(from, to);

        var user = await _userRepository.GetByIdAsync(userId) ?? throw new NotFoundException();

        return user.Role switch
        {
            UserRoles.Student => await GetStudentStatisticsAsync(user.Id, workType, fromDate, toDate),
            UserRoles.Mentor => await GetMentorStatisticsAsync(user.Id, workType, fromDate, toDate),
            _ => throw new NoStatisticsForRoleException(),
        };
    }

    public async Task<StatisticsDTO> GetPlatformStatisticsAsync(WorkType? workType, DateTime? from = null, DateTime? to = null)
    {
        var (fromDate, toDate) = NormalizeDateRange(from, to);

        return new StatisticsDTO
        {
            Blocks = [
                await _userStatisticsCollector.GetUserStatisticsAsync(fromDate, toDate),
                await _assignedWorkStatisticsCollector.GetAssignedWorkStatisticsAsync(workType, fromDate, toDate)
            ]
        };
    }

    public async Task<StatisticsDTO> GetMentorStatisticsAsync(Ulid mentorId, WorkType? workType, DateTime? from, DateTime? to)
    {
        var (fromDate, toDate) = NormalizeDateRange(from, to);

        return new StatisticsDTO
        {
            Blocks = [
                await _assignedWorkStatisticsCollector.GetMentorAssignedWorkStatisticsAsync(mentorId, workType, fromDate, toDate)
            ]
        };
    }

    public async Task<StatisticsDTO> GetStudentStatisticsAsync(Ulid studentId, WorkType? workType, DateTime? from = null, DateTime? to = null)
    {
        var (fromDate, toDate) = NormalizeDateRange(from, to);

        return new StatisticsDTO
        {
            Blocks = [
                await _assignedWorkStatisticsCollector.GetStudentAssignedWorkStatisticsAsync(studentId, workType, fromDate, toDate),
                await _assignedWorkStatisticsCollector.GetOverallStudentAssignedWorkStatisticsAsync(studentId, workType)
            ]
        };
    }

    private static (DateTime from, DateTime to) NormalizeDateRange(DateTime? from, DateTime? to)
    {
        var now = DateTime.UtcNow.Date;
        var toDate = (to ?? now).Date;
        var fromDate = (from ?? toDate.AddDays(-29)).Date;

        if (fromDate > toDate)
        {
            (fromDate, toDate) = (toDate, fromDate);
        }

        if ((toDate - fromDate).TotalDays > StatisticsConfig.MaxStatisticsDaysRange)
        {
            fromDate = toDate.AddDays(-StatisticsConfig.MaxStatisticsDaysRange);
        }

        return (fromDate, toDate);
    }
}
