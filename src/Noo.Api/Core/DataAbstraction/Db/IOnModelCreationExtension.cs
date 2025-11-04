using Microsoft.EntityFrameworkCore;

namespace Noo.Api.Core.DataAbstraction.Db;

public interface IOnModelCreationExtension
{
    public void OnModelCreating(ModelBuilder modelBuilder);
}
