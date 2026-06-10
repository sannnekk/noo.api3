using Ardalis.Specification;
using Noo.Api.AssignedWorks.Models;

namespace Noo.Api.AssignedWorks.Specifications;

public class HistorySpecification : Specification<AssignedWorkHistoryModel>
{
    public HistorySpecification()
    {
        Query.Include(x => x.ChangedBy);
    }
}
