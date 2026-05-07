using AutoMapper;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Exceptions;
using Noo.Api.Core.Request.Patching;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Subjects.DTO;
using Noo.Api.Subjects.Filters;
using Noo.Api.Subjects.Models;
using SystemTextJsonPatch;

namespace Noo.Api.Subjects.Services;

[RegisterScoped(typeof(ISubjectService))]
public class SubjectService : ISubjectService
{
    private readonly ISubjectRepository _subjectRepository;
    private readonly IJsonPatchUpdateService _jsonPatchUpdateService;
    private readonly IMapper _mapper;

    public SubjectService(
        ISubjectRepository subjectRepository,
        IJsonPatchUpdateService jsonPatchUpdateService,
        IMapper mapper
    )
    {
        _subjectRepository = subjectRepository;
        _jsonPatchUpdateService = jsonPatchUpdateService;
        _mapper = mapper;
    }

    public Ulid CreateSubject(SubjectCreationDTO subject)
    {
        var subjectModel = _mapper.Map<SubjectModel>(subject);

        _subjectRepository.Add(subjectModel);

        return subjectModel.Id;
    }

    public void DeleteSubject(Ulid id)
    {
        _subjectRepository.DeleteById(id);
    }

    public Task<SubjectModel?> GetSubjectByIdAsync(Ulid id)
    {
        return _subjectRepository.GetByIdAsync(id);
    }

    public Task<SearchResult<SubjectModel>> GetSubjectsAsync(SubjectFilter filter)
    {
        return _subjectRepository.SearchAsync(filter);
    }

    public async Task UpdateSubjectAsync(
        Ulid id,
        JsonPatchDocument<SubjectUpdateDTO> updateSubjectDto
    )
    {
        var model = await _subjectRepository.GetByIdAsync(id);

        model.ThrowNotFoundIfNull();

        _jsonPatchUpdateService.ApplyPatch(model, updateSubjectDto);
    }
}
