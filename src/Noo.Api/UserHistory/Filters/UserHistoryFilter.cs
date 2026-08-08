using AutoFilterer.Attributes;
using AutoFilterer.Types;
using Noo.Api.UserHistory.Models;
using Noo.Api.UserHistory.Types;

namespace Noo.Api.UserHistory.Filters;

[PossibleSortings(nameof(UserHistoryModel.CreatedAt))]
public class UserHistoryFilter : PaginationFilterBase
{
    [ArraySearchFilter]
    public IEnumerable<UserHistoryType>? Type { get; set; }

    public Range<DateTime>? CreatedAt { get; set; }
}
