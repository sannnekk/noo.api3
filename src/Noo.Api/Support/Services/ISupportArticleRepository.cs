using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Support.Models;

namespace Noo.Api.Support.Services;

public interface ISupportArticleRepository : IRepository<SupportArticleModel>
{
    public Task<SupportArticleModel?> GetBySlugAsync(string slug);
}
