using Noo.Api.AssignedWorks.Models;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Utils.DI;

namespace Noo.Api.AssignedWorks.Services;

[RegisterScoped(typeof(IAssignedWorkCommentRepository))]
public class AssignedWorkCommentRepository : Repository<AssignedWorkCommentModel>, IAssignedWorkCommentRepository
{
    public AssignedWorkCommentRepository(NooDbContext dbContext) : base(dbContext)
    {
    }
}
