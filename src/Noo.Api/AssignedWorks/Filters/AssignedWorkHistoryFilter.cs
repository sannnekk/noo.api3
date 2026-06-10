using AutoFilterer.Types;

namespace Noo.Api.AssignedWorks.Filters;

public class AssignedWorkHistoryFilter : PaginationFilterBase
{
    public Ulid? AssignedWorkId { get; set; }
}
