using Noo.Api.Auth.Models;

namespace Noo.Api.Auth.Services;

public enum RefreshOutcomeStatus
{
    /// <summary>
    /// The token does not exist or has expired.
    /// </summary>
    Invalid,

    /// <summary>
    /// The token was already rotated away (replay of a stolen token).
    /// The whole session should be revoked.
    /// </summary>
    Reused,

    /// <summary>
    /// The token is valid and has been marked as used.
    /// </summary>
    Valid,
}

public record RefreshOutcome(RefreshOutcomeStatus Status, RefreshTokenModel? Token = null);
