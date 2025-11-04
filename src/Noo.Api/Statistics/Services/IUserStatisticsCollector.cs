using Noo.Api.Statistics.DTO;

namespace Noo.Api.Statistics.Services;

public interface IUserStatisticsCollector
{
    public Task<StatisticsBlockDTO> GetUserStatisticsAsync(DateTime from, DateTime to);
}
