using Noo.Api.Core.Security.Authorization;

namespace Noo.Api.Users.Types;

// Both types use init-only properties rather than positional parameters on purpose: EF projects
// a grouping into a member initializer happily, but cannot translate one projected into a
// constructor call — something the InMemory provider used in tests accepts and MySQL rejects.

/// <summary>
/// How many users hold one role, as aggregated by the database.
/// </summary>
public record UserRoleCount
{
    public UserRoles Role { get; init; }
    public int Count { get; init; }
}

/// <summary>
/// How many users registered on one day, as aggregated by the database.
/// </summary>
public record UserRegistrationCount
{
    public DateTime Day { get; init; }
    public int Count { get; init; }
}
