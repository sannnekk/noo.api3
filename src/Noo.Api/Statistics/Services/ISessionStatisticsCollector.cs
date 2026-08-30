using Noo.Api.Statistics.DTO;

namespace Noo.Api.Statistics.Services;

public interface ISessionStatisticsCollector
{
    public Task<StatisticsBlockDTO> GetDeviceStatisticsAsync(DateTime from, DateTime to);
}
