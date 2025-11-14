using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Subjects.Models;

namespace Noo.Api.Subjects.Services;

[RegisterScoped(typeof(ISubjectRepository))]
public class SubjectRepository : Repository<SubjectModel>, ISubjectRepository
{
    public SubjectRepository(NooDbContext dbContext) : base(dbContext)
    {
    }
}
