using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction.Db;

namespace Noo.Api.Notifications.Models;

[OnModelCreationExtension]
public class OnModelCreationExtension : IOnModelCreationExtension
{
    public void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Push endpoints carry case-sensitive tokens, and ascii keeps the unique index within MySQL's key length.
        modelBuilder.Entity<PushSubscriptionModel>()
            .Property(s => s.Endpoint)
            .HasCharSet("ascii")
            .UseCollation("ascii_bin");
    }
}
