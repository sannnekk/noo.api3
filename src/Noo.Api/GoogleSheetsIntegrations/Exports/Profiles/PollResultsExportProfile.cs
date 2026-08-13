using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Exceptions.Http;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.Utils.DI;
using Noo.Api.GoogleSheetsIntegrations.Types;
using Noo.Api.Polls.Models;
using Noo.Api.Polls.Types;

namespace Noo.Api.GoogleSheetsIntegrations.Exports.Profiles;

public sealed record PollAnswerExportValue(
    Ulid QuestionId,
    PollAnswerValue Value,
    List<string> MediaNames
);

public sealed record PollResultsExportRow(
    string? Name,
    string? Email,
    string? Username,
    string? Telegram,
    string? ExternalIdentifier,
    DateTime CreatedAt,
    List<PollAnswerExportValue> Answers
);

/// <summary>
/// Every participation in a single poll, with one column per poll question. The only export
/// whose column list is not known until run time.
/// </summary>
[RegisterScoped(typeof(IExportProfile))]
public sealed class PollResultsExportProfile : ExportProfile<PollResultsExportRow>
{
    private readonly NooDbContext _db;

    public PollResultsExportProfile(NooDbContext db)
    {
        _db = db;
    }

    public override GoogleSheetsIntegrationType Type => GoogleSheetsIntegrationType.PollResults;

    public override UserRoles[] AllowedRoles => [UserRoles.Admin, UserRoles.Teacher];

    public override void Validate(ExportParameters parameters)
    {
        Require(parameters.PollId.HasValue, "Укажите опрос для выгрузки результатов.");
    }

    protected override IQueryable<PollResultsExportRow> Query(ExportParameters parameters)
    {
        return _db.GetDbSet<PollParticipationModel>()
            .Where(participation => participation.PollId == parameters.PollId)
            .OrderBy(participation => participation.Id)
            .Select(participation => new PollResultsExportRow(
                participation.User!.Name,
                participation.User!.Email,
                participation.User!.Username,
                participation.User!.TelegramUsername,
                participation.UserExternalIdentifier,
                participation.CreatedAt,
                participation
                    .Answers.Select(answer => new PollAnswerExportValue(
                        answer.PollQuestionId,
                        answer.Value,
                        answer.Medias!.Select(media => media.ActualName).ToList()
                    ))
                    .ToList()
            ));
    }

    protected override async Task<IReadOnlyList<ExportColumn<PollResultsExportRow>>> ColumnsAsync(
        ExportParameters parameters,
        CancellationToken ct
    )
    {
        var questions = await _db.GetDbSet<PollQuestionModel>()
            .Where(question => question.PollId == parameters.PollId)
            .OrderBy(question => question.Order)
            .Select(question => new { question.Id, question.Title })
            .ToListAsync(ct);

        if (questions.Count == 0 && !await PollExistsAsync(parameters.PollId!.Value, ct))
        {
            throw new NotFoundException();
        }

        List<ExportColumn<PollResultsExportRow>> columns =
        [
            ExportColumns.Text<PollResultsExportRow>("Имя", row => row.Name),
            ExportColumns.Text<PollResultsExportRow>("Email", row => row.Email),
            ExportColumns.Text<PollResultsExportRow>("Никнейм", row => row.Username),
            ExportColumns.Text<PollResultsExportRow>("Telegram", row => row.Telegram),
            ExportColumns.Text<PollResultsExportRow>(
                "Внешний идентификатор",
                row => row.ExternalIdentifier
            ),
            ExportColumns.Date<PollResultsExportRow>("Дата ответа", row => row.CreatedAt),
        ];

        foreach (var question in questions)
        {
            var questionId = question.Id;

            columns.Add(
                ExportColumns.Text<PollResultsExportRow>(
                    question.Title,
                    row => Stringify(row, questionId)
                )
            );
        }

        return columns;
    }

    private Task<bool> PollExistsAsync(Ulid pollId, CancellationToken ct)
    {
        return _db.GetDbSet<PollModel>().AnyAsync(poll => poll.Id == pollId, ct);
    }

    private static string? Stringify(PollResultsExportRow row, Ulid questionId)
    {
        var answer = row.Answers.Find(a => a.QuestionId == questionId);

        return answer is null
            ? null
            : PollAnswerFormatter.Stringify(answer.Value, answer.MediaNames);
    }
}
