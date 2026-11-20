using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction.Db;

namespace Noo.Api.UserHistory.Models;

[OnModelCreationExtension]
public class OnModelCreationExtension : IOnModelCreationExtension
{
    public void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Deliberately not using MySQL enum to avoid issues with migrations
        modelBuilder.Entity<UserHistoryModel>().Property(x => x.Type).HasConversion<string>();
    }
}
