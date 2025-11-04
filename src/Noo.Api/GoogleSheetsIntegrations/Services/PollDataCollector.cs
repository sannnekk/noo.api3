using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Exceptions.Http;
using Noo.Api.Core.ThirdPartyServices.Google;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Polls.Models;
using Noo.Api.Polls.Services;

namespace Noo.Api.GoogleSheetsIntegrations.Services;

[RegisterScoped(typeof(IPollDataCollector))]
public class PollDataCollector : IPollDataCollector
{
    private readonly IPollParticipationRepository _participationRepository;

    private readonly IPollRepository _pollRepository;

    public PollDataCollector(IUnitOfWork unitOfWork)
    {
        _participationRepository = unitOfWork.PollParticipationRepository();
        _pollRepository = unitOfWork.PollRepository();
    }

    public async Task<DataTable> GetPollResultsAsync(Ulid pollId)
    {
        var poll = await _pollRepository.GetWithQuestionsAsync(pollId);

        if (poll == null)
        {
            throw new NotFoundException();
        }

        var pollParticipations = await _participationRepository.GetByPollIdAsync(pollId);

        var table = new DataTable([
            "Имя",
            "Email",
            "Имя пользователя",
            "Telegram"
        ]);

        foreach (var question in poll.Questions)
        {
            table.AddColumn(question.Title);
        }

        foreach (var participation in pollParticipations)
        {
            List<object?> row = [
                participation.User?.Name,
                participation.User?.Email,
                participation.User?.Username,
                participation.User?.TelegramUsername
            ];

            foreach (var question in poll.Questions)
            {
                row.Add(StringifyAnswer(question.Id, participation));
            }

            table.AddRow(row.ToArray());
        }

        return table;
    }

    private string? StringifyAnswer(Ulid questionId, PollParticipationModel participation)
    {
        var answer = participation.Answers.FirstOrDefault(a => a.PollQuestionId == questionId);

        if (answer == null)
        {
            return null;
        }

        return answer.StringValue();
    }
}
