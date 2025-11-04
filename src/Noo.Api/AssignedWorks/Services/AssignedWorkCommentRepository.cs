using Noo.Api.AssignedWorks.Models;
using Noo.Api.Core.DataAbstraction.Db;

namespace Noo.Api.AssignedWorks.Services;

public class AssignedWorkCommentRepository : Repository<AssignedWorkCommentModel>, IAssignedWorkCommentRepository;


public static class IUnitOfWorkAssignedWorkCommentRepositoryExtensions
{
    public static IAssignedWorkCommentRepository AssignedWorkCommentRepository(this IUnitOfWork unitOfWork)
    {
        return new AssignedWorkCommentRepository()
        {
            Context = unitOfWork.Context,
        };
    }
}
