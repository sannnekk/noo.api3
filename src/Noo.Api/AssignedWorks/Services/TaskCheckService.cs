using Noo.Api.AssignedWorks.Models;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Works.Models;
using Noo.Api.Works.Types;

namespace Noo.Api.AssignedWorks.Services;

[RegisterTransient(typeof(ITaskCheckService))]
public class TaskCheckService : ITaskCheckService
{
    public int CheckTasks(
        IEnumerable<AssignedWorkAnswerModel> answers,
        IEnumerable<WorkTaskModel> tasks
    )
    {
        var totalScore = 0;

        foreach (var task in tasks.Where(t => t.IsAutomaticallyCheckable))
        {
            var answer = answers.FirstOrDefault(a =>
                a.TaskId == task.Id && a.Status == Types.AssignedWorkAnswerStatus.Submitted
            );

            if (answer == null)
            {
                continue;
            }

            var score = task.CheckStrategy switch
            {
                WorkTaskCheckStrategy.ExactMatchOrZero => CheckExactMatchOrZero(task, ref answer),
                WorkTaskCheckStrategy.ExactMatchWithWrongCharacter => CheckExactMatchWithWrongCharacter(task, ref answer),
                WorkTaskCheckStrategy.MultipleChoice => CheckMultipleChoice(task, ref answer),
                WorkTaskCheckStrategy.Sequence => CheckSequence(task, ref answer),
                _ => (int?)null,
            };

            if (score == null)
            {
                continue;
            }

            answer.Score = score;
            totalScore += score.Value;
        }

        return totalScore;
    }

    /// <summary>
    /// Identical to the answer key: max points, otherwise 0.
    /// </summary>
    private static int CheckExactMatchOrZero(WorkTaskModel task, ref AssignedWorkAnswerModel answer)
    {
        return CheckBestOf(
            task,
            answer,
            static (word, exact, maxScore) => word == exact ? maxScore : 0
        );
    }

    /// <summary>
    /// For every wrong character (compared position by position) minus one point.
    /// </summary>
    private static int CheckExactMatchWithWrongCharacter(
        WorkTaskModel task,
        ref AssignedWorkAnswerModel answer
    )
    {
        return CheckBestOf(
            task,
            answer,
            static (word, exact, maxScore) =>
            {
                word = word.PadRight(exact.Length);
                var score = maxScore;

                for (var i = 0; i < word.Length; i++)
                {
                    if (i >= exact.Length || word[i] != exact[i])
                    {
                        score--;
                    }
                }

                return score < 0 ? 0 : score;
            }
        );
    }

    /// <summary>
    /// Order does not matter: for every missing letter minus one,
    /// for every extra letter minus one. A mismatch in the amount of extra
    /// letters means the whole answer is wrong.
    /// </summary>
    private static int CheckMultipleChoice(WorkTaskModel task, ref AssignedWorkAnswerModel answer)
    {
        return CheckBestOf(
            task,
            answer,
            static (word, exact, maxScore) =>
            {
                var score = maxScore;

                for (var i = 0; i < exact.Length; i++)
                {
                    if (!word.Contains(exact[i]))
                    {
                        score--;
                    }
                }

                var missingLetters = 0;

                if (exact.Length < word.Length)
                {
                    for (var i = 0; i < word.Length; i++)
                    {
                        if (!exact.Contains(word[i]))
                        {
                            missingLetters++;
                        }
                    }

                    if (word.Length - exact.Length != missingLetters)
                    {
                        return 0;
                    }
                }

                score -= missingLetters;

                return score < 0 ? 0 : score;
            }
        );
    }

    /// <summary>
    /// Fully correct order: maximum points; up to two wrong characters minus one;
    /// every extra/missing character also subtracts a point.
    /// </summary>
    private static int CheckSequence(WorkTaskModel task, ref AssignedWorkAnswerModel answer)
    {
        return CheckBestOf(
            task,
            answer,
            static (word, exact, maxScore) =>
            {
                maxScore -= Math.Abs(word.Length - exact.Length);

                word = word.PadRight(exact.Length);

                var errorCount = 0;

                for (var i = 0; i < word.Length; i++)
                {
                    if (i >= exact.Length || word[i] != exact[i])
                    {
                        errorCount++;
                    }
                }

                return errorCount == 0 ? maxScore
                    : errorCount <= 2 ? maxScore - 1
                    : 0;
            }
        );
    }

    /// <summary>
    /// Normalizes the answer and the answer key, then returns the highest score
    /// the answer achieves against any of the accepted right answers.
    /// </summary>
    private static int CheckBestOf(
        WorkTaskModel task,
        AssignedWorkAnswerModel answer,
        Func<string, string, int, int> check
    )
    {
        var word = Normalize(answer.WordContent);

        if (string.IsNullOrEmpty(word) || task.RightAnswers == null)
        {
            return 0;
        }

        var bestScore = 0;

        foreach (var rightAnswer in task.RightAnswers)
        {
            var exact = Normalize(rightAnswer);

            if (string.IsNullOrEmpty(exact))
            {
                continue;
            }

            bestScore = Math.Max(bestScore, check(word, exact, task.MaxScore));
        }

        return bestScore;
    }

    private static string Normalize(string? value)
    {
        return value?.ToLowerInvariant().Replace(" ", string.Empty) ?? string.Empty;
    }
}
