using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Polls.Models;

namespace Noo.Api.Polls.Services;

public interface IPollRepository : IRepository<PollModel>
{
    public Task<PollModel?> GetWithQuestionsAsync(Ulid pollId);
}

