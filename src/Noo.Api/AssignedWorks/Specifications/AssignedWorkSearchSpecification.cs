using Ardalis.Specification;
using Noo.Api.AssignedWorks.Models;
using Noo.Api.Core.Security.Authorization;

namespace Noo.Api.AssignedWorks.Specifications;

public class AssignedWorkSearchSpecification : Specification<AssignedWorkModel>
{
    public AssignedWorkSearchSpecification(UserRoles userRole)
    {
        Query.Include(aw => aw.Work).ThenInclude(w => w!.Subject);

        if (userRole == UserRoles.Student)
        {
            Query.Include(aw => aw.MainMentor);
        }
        else if (userRole == UserRoles.Mentor)
        {
            Query.Include(aw => aw.Student).Include(aw => aw.HelperMentor);
        }
        else
        {
            Query.Include(aw => aw.Student).Include(aw => aw.MainMentor);
        }

        Query.AsSplitQuery();
    }
}
