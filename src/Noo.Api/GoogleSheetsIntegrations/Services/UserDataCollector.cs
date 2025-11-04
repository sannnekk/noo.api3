using Noo.Api.AssignedWorks.Services;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.ThirdPartyServices.Google;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Courses.Services;
using Noo.Api.Users.Services;

namespace Noo.Api.GoogleSheetsIntegrations.Services;

[RegisterScoped(typeof(IUserDataCollector))]
public class UserDataCollector : IUserDataCollector
{
    private readonly IUserRepository _userRepository;

    private readonly ICourseMembershipRepository _membershipRepository;

    private readonly IAssignedWorkRepository _assignedWorkRepository;

    public UserDataCollector(IUnitOfWork unitOfWork)
    {
        _userRepository = unitOfWork.UserRepository();
        _membershipRepository = unitOfWork.CourseMembershipRepository();
        _assignedWorkRepository = unitOfWork.AssignedWorkRepository();
    }

    public async Task<DataTable> GetUsersFromCourseAsync(Ulid courseId)
    {
        var users = await _membershipRepository.GetUsersByCourseIdAsync(courseId);

        var table = new DataTable([
            "Имя",
            "Email",
            "Никнейм",
            "Telegram"
        ]);

        foreach (var user in users)
        {
            table.AddRow([user.Name, user.Email, user.Username, user.TelegramUsername]);
        }

        return table;
    }

    public async Task<DataTable> GetUsersFromRoleAsync(UserRoles role)
    {
        var users = await _userRepository.GetUsersByRoleAsync(role);

        var table = new DataTable([
            "Имя",
            "Email",
            "Никнейм",
            "Telegram"
        ]);

        foreach (var user in users)
        {
            table.AddRow([user.Name, user.Email, user.Username, user.TelegramUsername]);
        }

        return table;
    }

    public async Task<DataTable> GetUsersFromWorkAsync(Ulid workId)
    {
        var users = await _assignedWorkRepository.GetUsersByWorkIdAsync(workId);

        var table = new DataTable([
            "Имя",
            "Email",
            "Никнейм",
            "Telegram"
        ]);

        foreach (var user in users)
        {
            table.AddRow([user.Name, user.Email, user.Username, user.TelegramUsername]);
        }

        return table;
    }
}
