using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Subjects.DTO;
using Noo.Api.Subjects.Filters;
using Noo.Api.Subjects.Models;
using SystemTextJsonPatch;

namespace Noo.Api.Subjects.Services;

public interface ISubjectService
{
    public Task<SubjectModel?> GetSubjectByIdAsync(Ulid id);
    public Task<SearchResult<SubjectModel>> GetSubjectsAsync(SubjectFilter filter);
    public Task<Ulid> CreateSubjectAsync(SubjectCreationDTO subject);
    public Task UpdateSubjectAsync(Ulid id, JsonPatchDocument<SubjectUpdateDTO> subject);
    public Task DeleteSubjectAsync(Ulid id);
}
