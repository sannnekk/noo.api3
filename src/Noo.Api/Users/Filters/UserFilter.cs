using AutoFilterer.Attributes;
using AutoFilterer.Types;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Users.Models;

namespace Noo.Api.Users.Filters;

[PossibleSortings(
    nameof(UserModel.Name),
    nameof(UserModel.Username),
    nameof(UserModel.Email),
    nameof(UserModel.TelegramUsername),
    nameof(UserModel.CreatedAt)
)]
public class UserFilter : PaginationFilterBase
{
    // Global Search: one field that compares to multiple props
    [CompareTo(nameof(UserModel.Name))]
    [CompareTo(nameof(UserModel.Username))]
    [CompareTo(nameof(UserModel.Email))]
    [CompareTo(nameof(UserModel.TelegramUsername))]
    [ToLowerContainsComparison]
    public string? Search { get; set; }

    [ArraySearchFilter]
    public IEnumerable<UserRoles>? Role { get; set; }

    public Range<DateTime>? CreatedAt { get; set; }
}
