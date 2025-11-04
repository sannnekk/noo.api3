using Noo.Api.Statistics.DTO;
using Noo.Api.Works.Types;

namespace Noo.Api.Statistics.Services;

public interface IAssignedWorkStatisticsCollector
{
    public Task<StatisticsBlockDTO> GetAssignedWorkStatisticsAsync(WorkType? workType, DateTime fromDate, DateTime toDate);
    public Task<StatisticsBlockDTO> GetMentorAssignedWorkStatisticsAsync(Ulid mentorId, WorkType? workType, DateTime from, DateTime to);
    public Task<StatisticsBlockDTO> GetOverallStudentAssignedWorkStatisticsAsync(Ulid studentId, WorkType? workType);
    public Task<StatisticsBlockDTO> GetStudentAssignedWorkStatisticsAsync(Ulid studentId, WorkType? workType, DateTime from, DateTime to);
}
