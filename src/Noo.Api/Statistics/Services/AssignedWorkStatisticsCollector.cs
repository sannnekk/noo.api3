using System.Linq.Expressions;
using Noo.Api.AssignedWorks.Models;
using Noo.Api.AssignedWorks.Services;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Statistics.DTO;
using Noo.Api.Works.Types;

namespace Noo.Api.Statistics.Services;

[RegisterScoped(typeof(IAssignedWorkStatisticsCollector))]
public class AssignedWorkStatisticsCollector : IAssignedWorkStatisticsCollector
{
    private readonly IAssignedWorkRepository _assignedWorkRepository;

    public AssignedWorkStatisticsCollector(IUnitOfWork unitOfWork)
    {
        _assignedWorkRepository = unitOfWork.AssignedWorkRepository();
    }

    public async Task<StatisticsBlockDTO> GetAssignedWorkStatisticsAsync(WorkType? workType, DateTime from, DateTime to)
    {
        var solvedTask = _assignedWorkRepository.GetByDateRangeAsync(aw => aw.Type == workType, from, to);
        var checkedTask = _assignedWorkRepository.GetByDateRangeAsync(aw => aw.Type == workType, from, to);
        var createdCountTask = _assignedWorkRepository.GetCountAsync(aw => aw.Type == workType, from, to);

        await Task.WhenAll(solvedTask, checkedTask, createdCountTask);

        var solvedWorks = solvedTask.Result;
        var checkedWorks = checkedTask.Result;
        var createdCount = createdCountTask.Result;

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
                        Values = StatisticsHelpers.NormalizeDictionary(checkedWorks)
                    },
                    new()
                    {
                        Name = "Решено",
                        Values = StatisticsHelpers.NormalizeDictionary(solvedWorks)
                    }
                ]
            },
            NumberBlocks =
            [
                new()
                {
                    Title = "Решено",
                    Value = solvedTotal,
                    SubValues = new()
                    {
                        { "Начато", createdCount },
                        { "Проверено", checkedTotal }
                    }
                }
            ]
        };
    }

    public async Task<StatisticsBlockDTO> GetMentorAssignedWorkStatisticsAsync(Ulid mentorId, WorkType? workType, DateTime from, DateTime to)
    {
        var checkedInDeadlineTask = _assignedWorkRepository.GetByDateRangeAsync(
            aw => (aw.MainMentorId == mentorId || aw.HelperMentorId == mentorId)
                && (workType == null || aw.Type == workType)
                && aw.CheckedAt <= aw.CheckDeadlineAt,
            from, to);
        var checkedAfterDeadlineTask = _assignedWorkRepository.GetByDateRangeAsync(
            aw => (aw.MainMentorId == mentorId || aw.HelperMentorId == mentorId)
                && (workType == null || aw.Type == workType)
                && aw.CheckedAt > aw.CheckDeadlineAt,
            from, to);
        var deadlineShiftsCountTask = _assignedWorkRepository.GetCountAsync(
            aw => (aw.MainMentorId == mentorId || aw.HelperMentorId == mentorId)
                && (workType == null || aw.Type == workType)
                && aw.IsCheckDeadlineShifted,
            from, to);

        await Task.WhenAll(checkedInDeadlineTask, checkedAfterDeadlineTask, deadlineShiftsCountTask);

        var checkedInDeadline = checkedInDeadlineTask.Result;
        var checkedAfterDeadline = checkedAfterDeadlineTask.Result;
        var deadlineShiftsCount = deadlineShiftsCountTask.Result;

        var checkedInDeadlineTotal = checkedInDeadline.Values.Sum();
        var checkedAfterDeadlineTotal = checkedAfterDeadline.Values.Sum();
        var checkedTotal = checkedInDeadlineTotal + checkedAfterDeadlineTotal;

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
                        Name = "Проверено в дедлайн",
                        Values = StatisticsHelpers.NormalizeDictionary(checkedInDeadline)
                    },
                    new()
                    {
                        Name = "Проверено после дедлайна",
                        Values = StatisticsHelpers.NormalizeDictionary(checkedAfterDeadline)
                    }
                ]
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
                        { "После дедлайна", checkedAfterDeadlineTotal }
                    }
                },
                new()
                {
                    Title = "Сдвиги дедлайна",
                    Value = deadlineShiftsCount
                }
            ]
        };
    }

    public async Task<StatisticsBlockDTO> GetOverallStudentAssignedWorkStatisticsAsync(Ulid studentId, WorkType? workType)
    {
        var averageScores = await _assignedWorkRepository.GetMonthAverageScoresAsync(studentId, workType);

        return new StatisticsBlockDTO
        {
            Title = "Статистика за все время",
            Graph = new StatisticsGraphDTO
            {
                Label = "Средний балл по работам",
                Lines =
                [
                    new()
                    {
                        Name = "Средний балл",
                        Values = StatisticsHelpers.NormalizeDictionary(averageScores)
                    },
                ]
            }
        };
    }

    public async Task<StatisticsBlockDTO> GetStudentAssignedWorkStatisticsAsync(Ulid studentId, WorkType? workType, DateTime from, DateTime to)
    {
        Expression<Func<AssignedWorkModel, bool>> predicate = aw => aw.StudentId == studentId && aw.Type == workType;

        var solvedInDeadlineTask = _assignedWorkRepository.GetByDateRangeAsync(predicate, from, to);
        var solvedAfterDeadlineTask = _assignedWorkRepository.GetByDateRangeAsync(predicate, from, to);
        var deadlineShiftsCountTask = _assignedWorkRepository.GetCountAsync(predicate, from, to);

        await Task.WhenAll(solvedInDeadlineTask, solvedAfterDeadlineTask, deadlineShiftsCountTask);

        var checkedInDeadline = solvedInDeadlineTask.Result;
        var checkedAfterDeadline = solvedAfterDeadlineTask.Result;
        var deadlineShiftsCount = deadlineShiftsCountTask.Result;

        var checkedInDeadlineTotal = checkedInDeadline.Values.Sum();
        var checkedAfterDeadlineTotal = checkedAfterDeadline.Values.Sum();
        var checkedTotal = checkedInDeadlineTotal + checkedAfterDeadlineTotal;

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
                        Name = "Проверено в дедлайн",
                        Values = StatisticsHelpers.NormalizeDictionary(checkedInDeadline)
                    },
                    new()
                    {
                        Name = "Проверено после дедлайна",
                        Values = StatisticsHelpers.NormalizeDictionary(checkedAfterDeadline)
                    }
                ]
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
                        { "После дедлайна", checkedAfterDeadlineTotal }
                    }
                },
                new()
                {
                    Title = "Сдвиги дедлайна",
                    Value = deadlineShiftsCount
                }
            ]
        };
    }
}
