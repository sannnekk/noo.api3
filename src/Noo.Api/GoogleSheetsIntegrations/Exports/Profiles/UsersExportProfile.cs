using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.Utils.DI;
using Noo.Api.GoogleSheetsIntegrations.Types;
using Noo.Api.Users.Models;

namespace Noo.Api.GoogleSheetsIntegrations.Exports.Profiles;

public sealed record UsersExportRow(
    string Name,
    string Username,
    string Email,
    string? Phone,
    string? TelegramUsername,
    UserRoles Role,
    DateTime CreatedAt,
    bool IsVerified,
    bool IsBlocked
);

/// <summary>
/// Users, optionally narrowed by role, course membership and registration date.
/// Every parameter is optional — none of them set means every user.
/// </summary>
[RegisterScoped(typeof(IExportProfile))]
public sealed class UsersExportProfile : ExportProfile<UsersExportRow>
{
    private readonly NooDbContext _db;

    public UsersExportProfile(NooDbContext db)
    {
        _db = db;
    }

    public override GoogleSheetsIntegrationType Type => GoogleSheetsIntegrationType.Users;

    public override UserRoles[] AllowedRoles => [UserRoles.Admin, UserRoles.Teacher];

    protected override IQueryable<UsersExportRow> Query(ExportParameters parameters)
    {
        var query = _db.GetDbSet<UserModel>().AsQueryable();

        if (parameters.Role is { } role)
        {
            query = query.Where(user => user.Role == role);
        }

        if (parameters.CourseId is { } courseId)
        {
            query = query.Where(user =>
                user.CoursesAsMember.Any(membership => membership.CourseId == courseId)
            );
        }

        if (parameters.CreatedFrom is { } from)
        {
            query = query.Where(user => user.CreatedAt >= from);
        }

        if (ExportDateRange.InclusiveEnd(parameters.CreatedTo) is { } to)
        {
            query = query.Where(user => user.CreatedAt <= to);
        }

        // Ordering by the primary key is both the cheapest option and chronological,
        // since ULIDs sort by creation time.
        return query
            .OrderBy(user => user.Id)
            .Select(user => new UsersExportRow(
                user.Name,
                user.Username,
                user.Email,
                user.Phone,
                user.TelegramUsername,
                user.Role,
                user.CreatedAt,
                user.IsVerified,
                user.IsBlocked
            ));
    }

    protected override Task<IReadOnlyList<ExportColumn<UsersExportRow>>> ColumnsAsync(
        ExportParameters parameters,
        CancellationToken ct
    )
    {
        IReadOnlyList<ExportColumn<UsersExportRow>> columns =
        [
            ExportColumns.Text<UsersExportRow>("Имя", row => row.Name),
            ExportColumns.Text<UsersExportRow>("Никнейм", row => row.Username),
            ExportColumns.Text<UsersExportRow>("Email", row => row.Email),
            ExportColumns.Text<UsersExportRow>("Телефон", row => row.Phone),
            ExportColumns.Text<UsersExportRow>("Telegram", row => row.TelegramUsername),
            ExportColumns.Enum<UsersExportRow>("Роль", row => row.Role.Translate()),
            ExportColumns.Date<UsersExportRow>("Дата регистрации", row => row.CreatedAt),
            ExportColumns.Bool<UsersExportRow>("Подтверждён", row => row.IsVerified),
            ExportColumns.Bool<UsersExportRow>("Заблокирован", row => row.IsBlocked),
        ];

        return Task.FromResult(columns);
    }
}
