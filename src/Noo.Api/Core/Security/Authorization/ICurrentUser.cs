namespace Noo.Api.Core.Security.Authorization;

public interface ICurrentUser
{
    public Ulid? UserId { get; }

    public UserRoles? UserRole { get; }

    public bool IsAuthenticated { get; }

    public bool IsInRole(params UserRoles[] role);

    /// <summary>
    /// Returns the current user id or throws if there is no authenticated user.
    /// </summary>
    public Ulid RequireUserId() =>
        UserId ?? throw new InvalidOperationException("Current user ID is not set.");

    /// <summary>
    /// Returns the current user role or throws if there is no authenticated user.
    /// </summary>
    public UserRoles RequireUserRole() =>
        UserRole ?? throw new InvalidOperationException("Current user role is not set.");
}
