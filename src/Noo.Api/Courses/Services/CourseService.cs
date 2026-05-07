using AutoMapper;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Exceptions;
using Noo.Api.Core.Request.Patching;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Courses.DTO;
using Noo.Api.Courses.Filters;
using Noo.Api.Courses.Models;
using Noo.Api.Courses.QuerySpecifications;
using Noo.Api.Media.Models;
using Noo.Api.Media.Services;
using Noo.Api.NooTube.Models;
using SystemTextJsonPatch;

namespace Noo.Api.Courses.Services;

[RegisterScoped(typeof(ICourseService))]
public class CourseService : ICourseService
{
    private readonly ICourseRepository _courseRepository;
    private readonly ICourseContentRepository _courseContentRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;
    private readonly IJsonPatchUpdateService _jsonPatchUpdateService;
    private readonly IMediaUrlEnricher _mediaUrlEnricher;
    private readonly IEntityReferenceFactory _entityReferences;

    public CourseService(
        ICourseRepository courseRepository,
        ICourseContentRepository courseContentRepository,
        ICurrentUser currentUser,
        IMapper mapper,
        IJsonPatchUpdateService jsonPatchUpdateService,
        IMediaUrlEnricher mediaUrlEnricher,
        IEntityReferenceFactory entityReferences
    )
    {
        _courseRepository = courseRepository;
        _courseContentRepository = courseContentRepository;
        _currentUser = currentUser;
        _mapper = mapper;
        _jsonPatchUpdateService = jsonPatchUpdateService;
        _mediaUrlEnricher = mediaUrlEnricher;
        _entityReferences = entityReferences;
    }

    public async Task<Ulid> CreateAsync(CreateCourseDTO dto)
    {
        var model = _mapper.Map<CourseModel>(dto);

        _courseRepository.Add(model);

        return model.Id;
    }

    public async Task<Ulid> CreateMaterialContentAsync(CreateCourseMaterialContentDTO dto)
    {
        var model = _mapper.Map<CourseMaterialContentModel>(dto);

        model.Medias = _entityReferences.References<MediaModel>(dto.MediaIds);
        model.NooTubeVideos = _entityReferences.References<NooTubeVideoModel>(dto.NooTubeVideoIds);

        _courseContentRepository.Add(model);

        return model.Id;
    }

    public async Task<CourseModel?> GetByIdAsync(Ulid id, bool includeInactive)
    {
        var course = await _courseRepository.GetWithChapterTreeAsync(
            id,
            true /* includeInactive */
        );

        await _mediaUrlEnricher.EnrichAsync(course);
        return course;
    }

    public async Task<CourseMaterialContentModel?> GetContentByIdAsync(Ulid contentId)
    {
        var content = await _courseContentRepository.GetAsync(contentId);

        await _mediaUrlEnricher.EnrichAsync(content);
        return content;
    }

    public async Task<SearchResult<CourseModel>> SearchAsync(CourseFilter filter)
    {
        var result = await _courseRepository.SearchAsync(
            filter,
            [new CourseSpecification(_currentUser.UserRole, _currentUser.UserId)]
        );

        await _mediaUrlEnricher.EnrichAsync(result.Items);
        return result;
    }

    public async Task SoftDeleteAsync(Ulid courseId)
    {
        var course = await _courseRepository.GetByIdAsync(courseId);
        if (course == null)
            return;

        course.IsDeleted = true;
        _courseRepository.Update(course);
    }

    public async Task UpdateAsync(Ulid courseId, JsonPatchDocument<UpdateCourseDTO> courseUpdateDto)
    {
        var model = await _courseRepository.GetWithChapterTreeAsync(
            courseId,
            includeInactive: true
        );

        model.ThrowNotFoundIfNull();

        _jsonPatchUpdateService.ApplyPatch(model, courseUpdateDto);
    }

    public async Task UpdateContentAsync(
        Ulid contentId,
        JsonPatchDocument<UpdateCourseMaterialContentDTO> contentUpdateDto
    )
    {
        var model = await _courseContentRepository.GetAsync(contentId);

        model.ThrowNotFoundIfNull();

        var patched = _jsonPatchUpdateService.ApplyPatch(model, contentUpdateDto);

        if (patched.Medias != null)
        {
            model.Medias = _entityReferences.References<MediaModel>(
                patched.Medias.Values.Select(r => r.Id)
            );
        }

        if (patched.NooTubeVideos != null)
        {
            model.NooTubeVideos = _entityReferences.References<NooTubeVideoModel>(
                patched.NooTubeVideos.Values.Select(r => r.Id)
            );
        }
    }
}
