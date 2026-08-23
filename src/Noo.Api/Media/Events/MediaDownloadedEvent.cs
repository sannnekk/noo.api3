using Noo.Api.Core.System.Events;
using Noo.Api.Media.Types;

namespace Noo.Api.Media.Events;

/// <summary>
/// Raised when a user is handed a download URL for a file, after the access rules have allowed it.
/// </summary>
/// <remarks>
/// Carries the user id rather than leaving it to be read from the request: handlers run in their own
/// scope on a background dispatcher, where there is no <c>HttpContext</c> to resolve a current user from.
/// </remarks>
public sealed record MediaDownloadedEvent(Ulid MediaId, Ulid UserId, MediaCategory Category)
    : IDomainEvent;
