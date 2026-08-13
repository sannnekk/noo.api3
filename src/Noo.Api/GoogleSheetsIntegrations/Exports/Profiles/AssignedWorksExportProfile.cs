using Microsoft.EntityFrameworkCore;
using Noo.Api.AssignedWorks.Models;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.Utils.DI;
using Noo.Api.GoogleSheetsIntegrations.Types;
using Noo.Api.Users.Models;

namespace Noo.Api.GoogleSheetsIntegrations.Exports.Profiles;

public sealed record AssignedWorksExportRow(
    string StudentName,
    string StudentEmail,
    string? StudentTelegram,
    string Title,
    string? SubjectName,
    int? Score,
    int MaxScore,
    DateTime? SolveDeadlineAt,
    DateTime? SolvedAt,
    DateTime? CheckDeadlineAt,
    DateTime? CheckedAt,
    bool IsMainMentor
);

/// <summary>
/// Assigned works for a single student, or every work a single mentor is attached to.
/// </summary>
[RegisterScoped(typeof(IExportProfile))]
public sealed class AssignedWorksExportProfile : ExportProfile<AssignedWorksExportRow>
{
    private readonly NooDbContext _db;

    public AssignedWorksExportProfile(NooDbContext db)
    {
        _db = db;
    }

    public override GoogleSheetsIntegrationType Type => GoogleSheetsIntegrationType.AssignedWorks;

    public override UserRoles[] AllowedRoles =>
        [UserRoles.Admin, UserRoles.Teacher, UserRoles.Mentor];

    public override void Validate(ExportParameters parameters)
    {
        Require(
            parameters.StudentId.HasValue ^ parameters.MentorId.HasValue,
            "Укажите либо ученика, либо куратора — но не обоих сразу."
        );
    }

    public override async Task<bool> AuthorizeAsync(
        Ulid userId,
        UserRoles role,
        ExportParameters parameters,
        CancellationToken ct = default
    )
    {
        if (!AllowedRoles.Contains(role))
        {
            return false;
        }

        if (role != UserRoles.Mentor)
        {
            return true;
        }

        // A mentor may export their own workload, or the works of a student assigned to them.
        if (parameters.MentorId is { } mentorId)
        {
            return mentorId == userId;
        }

        if (parameters.StudentId is { } studentId)
        {
            return await _db.GetDbSet<MentorAssignmentModel>()
                .AnyAsync(
                    assignment =>
                        assignment.MentorId == userId && assignment.StudentId == studentId,
                    ct
                );
        }

        return false;
    }

    protected override IQueryable<AssignedWorksExportRow> Query(ExportParameters parameters)
    {
        var query = _db.GetDbSet<AssignedWorkModel>().AsQueryable();

        if (parameters.StudentId is { } studentId)
        {
            query = query.Where(work => work.StudentId == studentId);
        }

        if (parameters.MentorId is { } mentorId)
        {
            query = query.Where(work =>
                work.MainMentorId == mentorId || work.HelperMentorId == mentorId
            );
        }

        var mentorIdOrEmpty = parameters.MentorId ?? Ulid.Empty;

        return query
            .OrderBy(work => work.Id)
            .Select(work => new AssignedWorksExportRow(
                work.Student.Name,
                work.Student.Email,
                work.Student.TelegramUsername,
                work.Title,
                work.Work!.Subject!.Name,
                work.Score,
                work.MaxScore,
                work.SolveDeadlineAt,
                work.SolvedAt,
                work.CheckDeadlineAt,
                work.CheckedAt,
                work.MainMentorId == mentorIdOrEmpty
            ));
    }

    protected override Task<IReadOnlyList<ExportColumn<AssignedWorksExportRow>>> ColumnsAsync(
        ExportParameters parameters,
        CancellationToken ct
    )
    {
        List<ExportColumn<AssignedWorksExportRow>> columns =
        [
            ExportColumns.Text<AssignedWorksExportRow>("Ученик", row => row.StudentName),
            ExportColumns.Text<AssignedWorksExportRow>("Email", row => row.StudentEmail),
            ExportColumns.Text<AssignedWorksExportRow>("Telegram", row => row.StudentTelegram),
            ExportColumns.Text<AssignedWorksExportRow>("Название работы", row => row.Title),
            ExportColumns.Text<AssignedWorksExportRow>("Предмет", row => row.SubjectName),
            ExportColumns.Number<AssignedWorksExportRow>("Балл", row => row.Score),
            ExportColumns.Number<AssignedWorksExportRow>("Макс. балл", row => row.MaxScore),
            ExportColumns.Percent<AssignedWorksExportRow>(
                "Процент",
                row => row.Score,
                row => row.MaxScore
            ),
            ExportColumns.Date<AssignedWorksExportRow>(
                "Дедлайн сдачи",
                row => row.SolveDeadlineAt
            ),
            ExportColumns.Date<AssignedWorksExportRow>("Сдано", row => row.SolvedAt),
            ExportColumns.Date<AssignedWorksExportRow>(
                "Дедлайн проверки",
                row => row.CheckDeadlineAt
            ),
            ExportColumns.Date<AssignedWorksExportRow>("Проверено", row => row.CheckedAt),
        ];

        // Only meaningful when the export is scoped to one mentor — for a single student's
        // works the mentor varies per row and the label would be about nobody in particular.
        if (parameters.MentorId.HasValue)
        {
            columns.Add(
                ExportColumns.Enum<AssignedWorksExportRow>(
                    "Роль куратора",
                    row => row.IsMainMentor ? "Основной" : "Помощник"
                )
            );
        }

        return Task.FromResult<IReadOnlyList<ExportColumn<AssignedWorksExportRow>>>(columns);
    }
}
