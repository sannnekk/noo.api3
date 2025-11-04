using Noo.Api.Statistics.DTO;
using Noo.Api.Works.Types;

namespace Noo.Api.Statistics.Services;

public interface IStatisticsService
{
    public Task<StatisticsDTO> GetMentorStatisticsAsync(Ulid mentorId, WorkType? workType, DateTime? from, DateTime? to);
    public Task<StatisticsDTO> GetPlatformStatisticsAsync(WorkType? workType, DateTime? from = null, DateTime? to = null);
    public Task<StatisticsDTO> GetStudentStatisticsAsync(Ulid studentId, WorkType? workType, DateTime? from = null, DateTime? to = null);
    public Task<StatisticsDTO> GetUserStatisticsAsync(Ulid userId, WorkType? workType, DateTime? from, DateTime? to);
}
