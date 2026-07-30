using Ardalis.Specification;
using AutoFilterer.Abstractions;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Polls.Models;

namespace Noo.Api.Polls.Services;

public interface IPollRepository : IRepository<PollModel>
{
    public Task<PollModel?> GetWithQuestionsAsync(Ulid id);

    /// <summary>
    /// Same as <see cref="GetWithQuestionsAsync"/>, but tracked — use it on the write path
    /// so the questions can be merged into the tracked collection.
    /// </summary>
    public Task<PollModel?> GetWithQuestionsForUpdateAsync(Ulid id);

    public Task<SearchResult<PollModel>> SearchWithParticipationsCountAsync(
        IPaginationFilter filter,
        IEnumerable<ISpecification<PollModel>>? specifications = default
    );
}

