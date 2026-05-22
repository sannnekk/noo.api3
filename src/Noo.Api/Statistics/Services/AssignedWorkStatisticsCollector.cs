using System.Linq.Expressions;
using Noo.Api.AssignedWorks.Models;
using Noo.Api.AssignedWorks.Services;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Statistics.DTO;
using Noo.Api.Works.Types;

namespace Noo.Api.Statistics.Services;

[RegisterScoped(typeof(IAssignedWorkStatisticsCollector))]
public class AssignedWorkStatisticsCollector : IAssignedWorkStatisticsCollector
{
    private readonly IAssignedWorkRepository _assignedWorkRepository;

    public AssignedWorkStatisticsCollector(IAssignedWorkRepository assignedWorkRepository)
    {
        _assignedWorkRepository = assignedWorkRepository;
    }

    public async Task<StatisticsBlockDTO> GetAssignedWorkStatisticsAsync(
        WorkType? workType,
        DateTime from,
        DateTime to
    )
    {
        var solvedWorks = await _assignedWorkRepository.GetByDateRangeAsync(
            aw => aw.Type == workType,
            from,
            to
        );
        var checkedWorks = await _assignedWorkRepository.GetByDateRangeAsync(
            aw => aw.Type == workType,
            from,
            to
        );
        var createdCount = await _assignedWorkRepository.GetCountAsync(
            aw => aw.Type == workType,
            from,
            to
        );

        var solvedTotal = solvedWorks.Values.Sum();
        var checkedTotal = checkedWorks.Values.Sum();

        return new StatisticsBlockDTO
        {
            Title = "Работы",
            Graph = new StatisticsGraphDTO
            {
                Label = "Динамика работ",
                Lines =
                [
                    new()
                    {
                        Name = "Проверено",
                        Values = StatisticsHelpers.NormalizeDictionary(checkedWorks),
                    },
                    new()
                    {
                        Name = "Решено",
                        Values = StatisticsHelpers.NormalizeDictionary(solvedWorks),
                    },
                ],
            },
            NumberBlocks =
            [
                new()
                {
                    Title = "Решено",
                    Value = solvedTotal,
                    SubValues = new() { { "Начато", createdCount }, { "Проверено", checkedTotal } },
                },
            ],
        };
    }

    public async Task<StatisticsBlockDTO> GetMentorAssignedWorkStatisticsAsync(
        Ulid mentorId,
        WorkType? workType,
        DateTime from,
        DateTime to
    )
    {
        var checkedInDeadline = await _assignedWorkRepository.GetByDateRangeAsync(
            aw =>
                (aw.MainMentorId == mentorId || aw.HelperMentorId == mentorId)
                && (workType == null || aw.Type == workType)
                && aw.CheckedAt <= aw.CheckDeadlineAt,
            from,
            to
        );
        var checkedAfterDeadline = await _assignedWorkRepository.GetByDateRangeAsync(
            aw =>
                (aw.MainMentorId == mentorId || aw.HelperMentorId == mentorId)
                && (workType == null || aw.Type == workType)
                && aw.CheckedAt > aw.CheckDeadlineAt,
            from,
            to
        );
        var deadlineShiftsCount = await _assignedWorkRepository.GetCountAsync(
            aw =>
                (aw.MainMentorId == mentorId || aw.HelperMentorId == mentorId)
                && (workType == null || aw.Type == workType)
                && aw.IsCheckDeadlineShifted,
            from,
            to
        );

        var checkedInDeadlineTotal = checkedInDeadline.Values.Sum();
        var checkedAfterDeadlineTotal = checkedAfterDeadline.Values.Sum();
        var checkedTotal = checkedInDeadlineTotal + checkedAfterDeadlineTotal;

        return new StatisticsBlockDTO
        {
            Title = "Работы",
            Description =
                "Здесь отображается статистика по работам выбранного типа, если тип выбран, или по всем работам, если тип не выбран. Статистика охватывает только выбранный промежуток времени.",
            Graph = new StatisticsGraphDTO
            {
                Label = "Динамика работ",
                Lines =
                [
                    new()
                    {
                        Name = "Проверено в дедлайн",
                        Values = StatisticsHelpers.NormalizeDictionary(checkedInDeadline),
                    },
                    new()
                    {
                        Name = "Проверено после дедлайна",
                        Values = StatisticsHelpers.NormalizeDictionary(checkedAfterDeadline),
                    },
                ],
            },
            NumberBlocks =
            [
                new()
                {
                    Title = "Проверено",
                    Value = checkedTotal,
                    SubValues = new()
                    {
                        { "В дедлайн", checkedInDeadlineTotal },
                        { "После дедлайна", checkedAfterDeadlineTotal },
                    },
                },
                new() { Title = "Сдвиги дедлайна", Value = deadlineShiftsCount },
            ],
        };
    }

    public async Task<StatisticsBlockDTO> GetOverallStudentAssignedWorkStatisticsAsync(
        Ulid studentId,
        WorkType? workType
    )
    {
        var averageScores = await _assignedWorkRepository.GetMonthAverageScoresAsync(
            studentId,
            workType
        );

        return new StatisticsBlockDTO
        {
            Title = "Статистика за все время",
            Description =
                "Здесь отображается статистика по всем работам выбранного типа, если тип выбран, или по всем работам, если тип не выбран. Здесь видно все работы, которые когда-либо были проверены, и их средний балл. Статистика не ограничивается промежутком времени.",
            Graph = new StatisticsGraphDTO
            {
                Label = "Средний балл по работам",
                Lines =
                [
                    new()
                    {
                        Name = "Средний балл",
                        Values = StatisticsHelpers.NormalizeDictionary(averageScores),
                    },
                ],
            },
        };
    }

    public async Task<StatisticsBlockDTO> GetStudentAssignedWorkStatisticsAsync(
        Ulid studentId,
        WorkType? workType,
        DateTime from,
        DateTime to
    )
    {
        Expression<Func<AssignedWorkModel, bool>> predicate = aw =>
            aw.StudentId == studentId && aw.Type == workType;

        var checkedInDeadline = await _assignedWorkRepository.GetByDateRangeAsync(
            predicate,
            from,
            to
        );
        var checkedAfterDeadline = await _assignedWorkRepository.GetByDateRangeAsync(
            predicate,
            from,
            to
        );
        var deadlineShiftsCount = await _assignedWorkRepository.GetCountAsync(predicate, from, to);

        var checkedInDeadlineTotal = checkedInDeadline.Values.Sum();
        var checkedAfterDeadlineTotal = checkedAfterDeadline.Values.Sum();
        var checkedTotal = checkedInDeadlineTotal + checkedAfterDeadlineTotal;

        return new StatisticsBlockDTO
        {
            Title = "Работы",
            Description =
                "Здесь отображается статистика по работам выбранного типа, если тип выбран, или по всем работам, если тип не выбран. Статистика охватывает только выбранный промежуток времени.",
            Graph = new StatisticsGraphDTO
            {
                Label = "Динамика работ",
                Lines =
                [
                    new()
                    {
                        Name = "Проверено в дедлайн",
                        Values = StatisticsHelpers.NormalizeDictionary(checkedInDeadline),
                    },
                    new()
                    {
                        Name = "Проверено после дедлайна",
                        Values = StatisticsHelpers.NormalizeDictionary(checkedAfterDeadline),
                    },
                ],
            },
            NumberBlocks =
            [
                new()
                {
                    Title = "Проверено",
                    Value = checkedTotal,
                    SubValues = new()
                    {
                        { "В дедлайн", checkedInDeadlineTotal },
                        { "После дедлайна", checkedAfterDeadlineTotal },
                    },
                },
                new() { Title = "Сдвиги дедлайна", Value = deadlineShiftsCount },
            ],
        };
    }
}
