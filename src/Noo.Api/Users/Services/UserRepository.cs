using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Users.Models;

namespace Noo.Api.Users.Services;

[RegisterScoped(typeof(IUserRepository))]
public class UserRepository : Repository<UserModel>, IUserRepository
{
    public UserRepository(NooDbContext context) : base(context)
    {
    }

    public Task<UserModel?> GetByUsernameOrEmailAsync(string usernameOrEmail)
    {
        var repository = Context.GetDbSet<UserModel>();

        return repository
            .Where(x => x.Username == usernameOrEmail || x.Email == usernameOrEmail)
            .FirstOrDefaultAsync();
    }

    public Task<bool> ExistsByUsernameOrEmailAsync(string? username, string? email)
    {
        var repository = Context.GetDbSet<UserModel>();

        if (string.IsNullOrEmpty(username) && string.IsNullOrEmpty(email))
        {
            return Task.FromResult(false);
        }

        if (string.IsNullOrEmpty(username))
        {
            return repository.AnyAsync(x => x.Email == email);
        }

        if (string.IsNullOrEmpty(email))
        {
            return repository.AnyAsync(x => x.Username == username);
        }

        return repository.AnyAsync(x => x.Username == username || x.Email == email);
    }

    public Task<bool> IsBlockedAsync(Ulid id)
    {
        var repository = Context.GetDbSet<UserModel>();

        return repository
            .Where(x => x.Id == id)
            .Select(x => x.IsBlocked)
            .FirstOrDefaultAsync();
    }

    public async Task BlockUserAsync(Ulid id)
    {
        var repository = Context.GetDbSet<UserModel>();

        await repository
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(x => x.SetProperty(y => y.IsBlocked, true));
    }

    public async Task UnblockUserAsync(Ulid id)
    {
        var repository = Context.GetDbSet<UserModel>();

        await repository
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(x => x.SetProperty(y => y.IsBlocked, false));
    }

    public Task<bool> MentorExistsAsync(Ulid mentorId)
    {
        var repository = Context.GetDbSet<UserModel>();

        return repository
            .Where(x => x.Id == mentorId && x.Role == UserRoles.Mentor)
            .AnyAsync();
    }

    public Task<Dictionary<UserRoles, int>> GetTotalUsersByRolesAsync(DateTime fromDate, DateTime toDate)
    {
        var repository = Context.GetDbSet<UserModel>();

        return repository
            .Where(x => x.CreatedAt >= fromDate && x.CreatedAt <= toDate)
            .GroupBy(x => x.Role)
            .ToDictionaryAsync(g => g.Key, g => g.Count());
    }

    public Task<Dictionary<DateTime, int>> GetRegistrationsByDateRangeAsync(DateTime fromDate, DateTime toDate)
    {
        var repository = Context.GetDbSet<UserModel>();

        return repository
            .Where(x => x.CreatedAt >= fromDate && x.CreatedAt <= toDate)
            .GroupBy(x => x.CreatedAt.Date)
            .ToDictionaryAsync(g => g.Key, g => g.Count());
    }

    public Task<List<UserModel>> GetUsersByRoleAsync(UserRoles role)
    {
        var repository = Context.GetDbSet<UserModel>();

        return repository
            .Where(x => x.Role == role)
            .AsNoTracking()
            .ToListAsync();
    }
}
