using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Noo.Api.Core.Request;
using Noo.Api.Core.Response;
using Noo.Api.Core.Utils.Versioning;
using Noo.Api.Statistics.DTO;
using Noo.Api.Statistics.Services;
using Noo.Api.Works.Types;
using ProducesAttribute = Noo.Api.Core.Documentation.ProducesAttribute;

namespace Noo.Api.Statistics;

[ApiVersion(NooApiVersions.Current)]
[ApiController]
[Route("statistics")]
public class StatisticsController : ApiController
{
    private readonly IStatisticsService _statisticsService;

    public StatisticsController(IStatisticsService statisticsService, IMapper mapper)
        : base(mapper)
    {
        _statisticsService = statisticsService;
    }

    /// <summary>
    /// Retrieves the current statistics of the platform.
    /// </summary>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpGet("platform")]
    [Authorize(Policy = StatisticsPolicies.CanGetPlatformStatistics)]
    [Produces(
        typeof(ApiResponseDTO<StatisticsDTO>), StatusCodes.Status200OK,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden
    )]
    public async Task<IActionResult> GetPlatformStatisticsAsync(
        [FromQuery] WorkType? workType = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null
    )
    {
        var statistics = await _statisticsService.GetPlatformStatisticsAsync(workType, from, to);

        return SendResponse(statistics);
    }

    /// <summary>
    /// Retrieves the statistics of a specific student.
    /// </summary>
    /// <remarks>
    /// It returns 404 only if the student does not exist or is not a student.
    /// </remarks>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpGet("user/{userId}")]
    [Authorize(Policy = StatisticsPolicies.CanGetUserStatistics)]
    [Produces(
        typeof(ApiResponseDTO<StatisticsDTO>), StatusCodes.Status200OK,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden
    )]
    public async Task<IActionResult> GetUserStatisticsAsync(
        [FromRoute] Ulid userId,
        [FromRoute] WorkType? workType = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null
    )
    {
        var statistics = await _statisticsService.GetUserStatisticsAsync(userId, workType, from, to);

        return SendResponse(statistics);
    }
}
