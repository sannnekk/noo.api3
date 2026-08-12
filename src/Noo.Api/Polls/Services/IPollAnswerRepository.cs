using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Polls.Models;

namespace Noo.Api.Polls.Services;

public interface IPollAnswerRepository : IRepository<PollAnswerModel>
{
    /// <summary>
    /// Loads a tracked answer together with the question it answers and the files it
    /// carries — everything an update has to check the new answer against.
    /// </summary>
    public Task<PollAnswerModel?> GetForUpdateAsync(Ulid answerId);
}
