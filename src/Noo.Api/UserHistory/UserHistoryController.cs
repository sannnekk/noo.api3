using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Noo.Api.Core.Request;
using Noo.Api.Core.Response;
using Noo.Api.Core.Utils.Versioning;
using Noo.Api.UserHistory.DTO;
using Noo.Api.UserHistory.Filters;
using Noo.Api.UserHistory.Models;
using Noo.Api.UserHistory.Services;
using Noo.Api.UserHistory.Types;
using ProducesAttribute = Noo.Api.Core.Documentation.ProducesAttribute;

namespace Noo.Api.UserHistory;

[ApiVersion(NooApiVersions.Current)]
[ApiController]
[Route("user")]
public class UserHistoryController : ApiController
{
    private readonly IUserHistoryService _userHistoryService;

    public UserHistoryController(IUserHistoryService userHistoryService, IMapper mapper)
        : base(mapper)
    {
        _userHistoryService = userHistoryService;
    }

    /// <summary>
    /// Gets a user's activity log, paginated.
    ///
    /// The <c>perspective</c> parameter picks the side: <c>subject</c> (the default) returns what
    /// happened to the user, <c>actor</c> returns what the user did to others.
    /// </summary>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpGet("{userId}/history")]
    [Authorize(Policy = UserHistoryPolicies.CanGetUserHistory)]
    [Produces(
        typeof(ApiResponseDTO<IEnumerable<UserHistoryDTO>>),
        StatusCodes.Status200OK,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden
    )]
    public async Task<IActionResult> GetUserHistoryAsync(
        [FromRoute] Ulid userId,
        [FromQuery] UserHistoryFilter filter,
        [FromQuery] UserHistoryPerspective perspective = UserHistoryPerspective.Subject
    )
    {
        var result = await _userHistoryService.GetHistoryAsync(userId, perspective, filter);

        return SendResponse<UserHistoryModel, UserHistoryDTO>(result);
    }
}
