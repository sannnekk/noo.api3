using Noo.Api.Core.Utils.UserAgent;

namespace Noo.Api.Sessions.Types;

// Both types use init-only properties rather than positional parameters on purpose: EF projects
// a grouping into a member initializer happily, but cannot translate one projected into a
// constructor call — something the InMemory provider used in tests accepts and MySQL rejects.

/// <summary>
/// How many distinct users were seen on one browser, as aggregated by the database.
/// </summary>
public record BrowserUserCount
{
    /// <summary>
    /// The stored browser name, which is a <see cref="BrowserKind"/> for every session written by
    /// the current parser and an arbitrary string for older ones.
    /// </summary>
    public string? Browser { get; init; }
    public int UserCount { get; init; }
}

/// <summary>
/// How many distinct users were seen on one kind of device, as aggregated by the database.
/// </summary>
public record DeviceTypeUserCount
{
    public DeviceType DeviceType { get; init; }
    public int UserCount { get; init; }
}
