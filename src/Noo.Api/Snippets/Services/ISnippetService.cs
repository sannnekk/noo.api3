using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Snippets.DTO;
using Noo.Api.Snippets.Models;
using SystemTextJsonPatch;

namespace Noo.Api.Snippets.Services;

public interface ISnippetService
{
    public Task CreateSnippetAsync(Ulid userId, CreateSnippetDTO createSnippetDto);
    public Task UpdateSnippetAsync(Ulid userId, Ulid snippetId, JsonPatchDocument<UpdateSnippetDTO> updateSnippetDto);
    public Task DeleteSnippetAsync(Ulid userId, Ulid snippetId);
    public Task<SearchResult<SnippetModel>> GetSnippetsAsync(Ulid userId);
}
