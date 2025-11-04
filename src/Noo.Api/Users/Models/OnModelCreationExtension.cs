using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction.Db;

namespace Noo.Api.Users.Models;

[OnModelCreationExtension]
public class OnModelCreationExtension : IOnModelCreationExtension
{
    public void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserModel>()
            .Navigation(w => w.Avatar)
            .AutoInclude();

        modelBuilder.Entity<UserAvatarModel>()
            .Navigation(w => w.Media)
            .AutoInclude();
    }
}
