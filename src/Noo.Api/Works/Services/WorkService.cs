using AutoMapper;
using Noo.Api.Core.DataAbstraction.Cache;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Exceptions;
using Noo.Api.Core.Request.Patching;
using Noo.Api.Core.Utils;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Courses.Utils;
using Noo.Api.Works.DTO;
using Noo.Api.Works.Filters;
using Noo.Api.Works.Models;
using Noo.Api.Works.Types;
using SystemTextJsonPatch;

namespace Noo.Api.Works.Services;

[RegisterScoped(typeof(IWorkService))]
public class WorkService : IWorkService
{
    private static readonly TimeSpan _statisticsCacheTtl = TimeSpan.FromMinutes(15);

    private readonly IWorkRepository _workRepository;

    private readonly IMapper _mapper;

    private readonly IJsonPatchUpdateService _patchUpdateService;

    private readonly ICacheRepository _cache;

    public WorkService(
        IWorkRepository workRepository,
        IMapper mapper,
        IJsonPatchUpdateService patchUpdateService,
        ICacheRepository cache
    )
    {
        _workRepository = workRepository;
        _mapper = mapper;
        _patchUpdateService = patchUpdateService;
        _cache = cache;
    }

    public Ulid CreateWork(CreateWorkDTO work)
    {
        var model = _mapper.Map<WorkModel>(work);

        model.MaxScore = model.Tasks?.Sum(t => t.MaxScore) ?? 0;
        _workRepository.Add(model);

        return model.Id;
    }

    public Task<WorkModel?> GetWorkAsync(Ulid id)
    {
        return _workRepository.GetWithTasksAsync(id);
    }

    public Task<SearchResult<WorkModel>> GetWorksAsync(WorkFilter filter)
    {
        return _workRepository.SearchAsync(filter);
    }

    public async Task UpdateWorkAsync(Ulid id, JsonPatchDocument<UpdateWorkDTO> updateWorkDto)
    {
        var workModel = await _workRepository.GetWithTasksAsync(id);

        workModel.ThrowNotFoundIfNull();

        _patchUpdateService.ApplyPatch(workModel, updateWorkDto);

        workModel.MaxScore = workModel.Tasks?.Sum(t => t.MaxScore) ?? 0;
    }

    public void DeleteWork(Ulid id)
    {
        _workRepository.DeleteById(id);
    }

    public async Task<WorkStatistics?> GetWorkStatisticsAsync(Ulid id)
    {
        var work = await _workRepository.GetWithTasksAsync(id);

        if (work is null)
        {
            return null;
        }

        var statistics = await _cache.GetOrSetAsync(
            StatisticsCacheKey(id),
            () => BuildStatisticsAsync(id, work.MaxScore),
            _statisticsCacheTtl
        );

        statistics!.Work = work;

        return statistics;
    }

    private async Task<WorkStatistics> BuildStatisticsAsync(Ulid id, int maxScore)
    {
        var scores = await _workRepository.GetScoresAsync(id);
        var taskSummaries = await _workRepository.GetTaskSummariesAsync(id);
        var solveCount = await _workRepository.CountSolvedAsync(id);

        return new WorkStatistics
        {
            TaskSummaries = taskSummaries,
            AverageWorkScore = new()
            {
                Absolute = scores.AverageOrNull(),
                Percentage = scores.AveragePercentageOrNull(maxScore),
            },
            MedianWorkScore = new()
            {
                Absolute = scores.MedianOrNull(),
                Percentage = scores.MedianPercentageOrNull(maxScore),
            },
            WorkSolveCount = solveCount,
        };
    }

    private static string StatisticsCacheKey(Ulid id)
    {
        return $"work:statistics:{id}";
    }

    public async Task<IEnumerable<WorkRelation>> GetWorkRelationsAsync(Ulid workId)
    {
        var relations = await _workRepository.GetWorkRelationsAsync(workId);

        foreach (var relation in relations)
        {
            var material = relation.Assignment.CourseMaterialContent.Material;
            var course = material.Chapter.Course;

            relation.Path = CourseMaterialPath.Build(
                course.Name,
                course.Chapters.ToDictionary(chapter => chapter.Id),
                material.ChapterId,
                material.Title
            );
        }

        return relations;
    }
}
