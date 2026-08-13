using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Courses.Models;
using Noo.Api.GoogleSheetsIntegrations.Types;

namespace Noo.Api.GoogleSheetsIntegrations.Exports.Profiles;

public sealed record CoursesExportRow(
    string Name,
    string? SubjectName,
    DateTime CreatedAt,
    DateTime? StartDate,
    DateTime? EndDate,
    int MemberCount,
    bool IsArchived,
    string? Authors
);

/// <summary>
/// Courses, optionally narrowed by subject and creation date.
/// </summary>
[RegisterScoped(typeof(IExportProfile))]
public sealed class CoursesExportProfile : ExportProfile<CoursesExportRow>
{
    private readonly NooDbContext _db;

    public CoursesExportProfile(NooDbContext db)
    {
        _db = db;
    }

    public override GoogleSheetsIntegrationType Type => GoogleSheetsIntegrationType.Courses;

    public override UserRoles[] AllowedRoles => [UserRoles.Admin, UserRoles.Teacher];

    protected override IQueryable<CoursesExportRow> Query(ExportParameters parameters)
    {
        var query = _db.GetDbSet<CourseModel>().Where(course => !course.IsDeleted);

        if (parameters.SubjectId is { } subjectId)
        {
            query = query.Where(course => course.SubjectId == subjectId);
        }

        if (parameters.CreatedFrom is { } from)
        {
            query = query.Where(course => course.CreatedAt >= from);
        }

        if (ExportDateRange.InclusiveEnd(parameters.CreatedTo) is { } to)
        {
            query = query.Where(course => course.CreatedAt <= to);
        }

        return query
            .OrderBy(course => course.Id)
            .Select(course => new CoursesExportRow(
                course.Name,
                course.Subject!.Name,
                course.CreatedAt,
                course.StartDate,
                course.EndDate,
                course.Memberships.Count,
                course.IsArchived,
                string.Join(", ", course.Authors.Select(author => author.Name))
            ));
    }

    protected override Task<IReadOnlyList<ExportColumn<CoursesExportRow>>> ColumnsAsync(
        ExportParameters parameters,
        CancellationToken ct
    )
    {
        IReadOnlyList<ExportColumn<CoursesExportRow>> columns =
        [
            ExportColumns.Text<CoursesExportRow>("Название", row => row.Name),
            ExportColumns.Text<CoursesExportRow>("Предмет", row => row.SubjectName),
            ExportColumns.Date<CoursesExportRow>("Дата создания", row => row.CreatedAt),
            ExportColumns.Date<CoursesExportRow>(
                "Дата начала",
                row => row.StartDate,
                includeTime: false
            ),
            ExportColumns.Date<CoursesExportRow>(
                "Дата окончания",
                row => row.EndDate,
                includeTime: false
            ),
            ExportColumns.Number<CoursesExportRow>("Участников", row => row.MemberCount),
            ExportColumns.Bool<CoursesExportRow>("В архиве", row => row.IsArchived),
            ExportColumns.Text<CoursesExportRow>("Авторы", row => row.Authors),
        ];

        return Task.FromResult(columns);
    }
}
