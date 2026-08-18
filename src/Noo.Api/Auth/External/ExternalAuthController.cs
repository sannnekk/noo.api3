using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Noo.Api.Auth.External.DTO;
using Noo.Api.Auth.External.Models;
using Noo.Api.Auth.External.Services;
using Noo.Api.Auth.External.Types;
using Noo.Api.Core.Request;
using Noo.Api.Core.Response;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.Security.RateLimiter;
using Noo.Api.Core.Utils.Versioning;
using Noo.Api.Media;
using ProducesAttribute = Noo.Api.Core.Documentation.ProducesAttribute;

namespace Noo.Api.Auth.External;

/// <summary>
/// Signing in and linking accounts through external identity providers.
/// </summary>
/// <remarks>
/// The browser is redirected to the provider full-page and comes back to a single
/// registered redirect URI, so login and linking share one callback endpoint.
/// </remarks>
[ApiVersion(NooApiVersions.Current)]
[ApiController]
[Route("auth/external")]
public class ExternalAuthController : ApiController
{
    private readonly IExternalAuthService _externalAuthService;

    private readonly IWebHostEnvironment _environment;

    public ExternalAuthController(
        IExternalAuthService externalAuthService,
        IWebHostEnvironment environment,
        IMapper mapper
    )
        : base(mapper)
    {
        _externalAuthService = externalAuthService;
        _environment = environment;
    }

    private bool CookieSecure => !_environment.IsDevelopment();

    /// <summary>
    /// Lists the providers this environment can sign users in with.
    /// </summary>
    [HttpGet("providers")]
    [MapToApiVersion(NooApiVersions.Current)]
    [AllowAnonymous]
    [Produces(typeof(ApiResponseDTO<IEnumerable<ExternalAuthProviderDTO>>), StatusCodes.Status200OK)]
    public IActionResult GetProviders()
    {
        var providers = _externalAuthService.GetProviders();

        return SendResponse<IEnumerable<ExternalAuthProviderDTO>>(providers);
    }

    /// <summary>
    /// Starts a login through the given provider and returns the URL to redirect the browser to.
    /// </summary>
    [HttpPost("{provider}/start")]
    [MapToApiVersion(NooApiVersions.Current)]
    [AllowAnonymous]
    [EnableRateLimiting(LoginRateLimitPolicy.Name)]
    [Produces(
        typeof(ApiResponseDTO<ExternalAuthUrlDTO>),
        StatusCodes.Status200OK,
        StatusCodes.Status400BadRequest
    )]
    public async Task<IActionResult> StartLoginAsync(
        [FromRoute] ExternalAuthProviderType provider,
        [FromBody] StartExternalAuthDTO request
    )
    {
        var url = await _externalAuthService.StartAsync(
            provider,
            ExternalAuthIntent.Login,
            request.ReturnUrl,
            userId: null
        );

        return SendResponse(new ExternalAuthUrlDTO { Url = url });
    }

    /// <summary>
    /// Starts linking the given provider to the account of the caller.
    /// </summary>
    /// <remarks>
    /// A separate endpoint from the login start so the intent rides on the authorization
    /// requirement instead of an optional bearer token.
    /// </remarks>
    [HttpPost("{provider}/link/start")]
    [MapToApiVersion(NooApiVersions.Current)]
    [Authorize(Policy = ExternalAuthPolicies.CanManageOwnIdentities)]
    [Produces(
        typeof(ApiResponseDTO<ExternalAuthUrlDTO>),
        StatusCodes.Status200OK,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized
    )]
    public async Task<IActionResult> StartLinkAsync(
        [FromRoute] ExternalAuthProviderType provider,
        [FromBody] StartExternalAuthDTO request
    )
    {
        var url = await _externalAuthService.StartAsync(
            provider,
            ExternalAuthIntent.Link,
            request.ReturnUrl,
            User.GetId()
        );

        return SendResponse(new ExternalAuthUrlDTO { Url = url });
    }

    /// <summary>
    /// Redeems the callback the provider sent the browser back with.
    /// </summary>
    /// <remarks>
    /// Anonymous because a login callback has no session yet; the intent comes from the
    /// server-side challenge, never from the request.
    /// </remarks>
    [HttpPost("{provider}/callback")]
    [MapToApiVersion(NooApiVersions.Current)]
    [AllowAnonymous]
    [EnableRateLimiting(LoginRateLimitPolicy.Name)]
    [Produces(
        typeof(ApiResponseDTO<ExternalAuthResultDTO>),
        StatusCodes.Status200OK,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status409Conflict
    )]
    public async Task<IActionResult> CompleteAsync(
        [FromRoute] ExternalAuthProviderType provider,
        [FromBody] ExternalAuthCallbackDTO request,
        CancellationToken cancellationToken
    )
    {
        var outcome = await _externalAuthService.CompleteAsync(
            provider,
            request.Parameters,
            cancellationToken
        );

        if (outcome.Session is not null)
        {
            Response.SetRefreshToken(
                outcome.Session.RefreshToken,
                outcome.Session.RefreshTokenExpiresAt,
                CookieSecure
            );
            Response.SetMediaToken(
                outcome.Session.Response.AccessToken,
                outcome.Session.Response.ExpiresAt,
                CookieSecure
            );
        }

        return SendResponse(
            new ExternalAuthResultDTO
            {
                Intent = outcome.Intent,
                Provider = outcome.Provider,
                ReturnUrl = outcome.ReturnUrl,
                Session = outcome.Session?.Response,
            }
        );
    }

    /// <summary>
    /// Lists the providers linked to the account of the caller.
    /// </summary>
    [HttpGet("identities")]
    [MapToApiVersion(NooApiVersions.Current)]
    [Authorize(Policy = ExternalAuthPolicies.CanManageOwnIdentities)]
    [Produces(
        typeof(ApiResponseDTO<IEnumerable<LinkedIdentityDTO>>),
        StatusCodes.Status200OK,
        StatusCodes.Status401Unauthorized
    )]
    public async Task<IActionResult> GetIdentitiesAsync()
    {
        var identities = await _externalAuthService.GetIdentitiesAsync(User.GetId());

        return SendResponse<IEnumerable<UserIdentityModel>, IEnumerable<LinkedIdentityDTO>>(
            identities
        );
    }

    /// <summary>
    /// Unlinks a provider from the account of the caller.
    /// </summary>
    [HttpDelete("identities/{provider}")]
    [MapToApiVersion(NooApiVersions.Current)]
    [Authorize(Policy = ExternalAuthPolicies.CanManageOwnIdentities)]
    [Produces(
        null,
        StatusCodes.Status204NoContent,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status404NotFound,
        StatusCodes.Status409Conflict
    )]
    public async Task<IActionResult> UnlinkAsync([FromRoute] ExternalAuthProviderType provider)
    {
        await _externalAuthService.UnlinkAsync(User.GetId(), provider);

        return SendResponse();
    }
}
