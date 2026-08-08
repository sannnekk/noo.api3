using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction.Db;

namespace Noo.Api.UserHistory.Models;

[OnModelCreationExtension]
public class OnModelCreationExtension : IOnModelCreationExtension
{
    public void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Store the kind as its name rather than an ordinal or a MySQL ENUM: new kinds are added
        // continuously here, and neither of those alternatives tolerates that without a migration.
        modelBuilder.Entity<UserHistoryModel>()
            .Property(x => x.Type)
            .HasConversion<string>();
    }
}
