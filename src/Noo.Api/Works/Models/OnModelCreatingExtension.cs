using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction.Db;

namespace Noo.Api.Works.Models;

[OnModelCreationExtension]
public class OnModelCreationExtension : IOnModelCreationExtension
{
    public void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkModel>()
            .Navigation(w => w.Subject)
            .AutoInclude();
    }
}
