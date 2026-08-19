using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using Noo.Api.Core.Exceptions.Http;
using Noo.Api.Core.Utils.DI;

namespace Noo.Api.Core.DataAbstraction.Db;

[RegisterScoped(typeof(IUnitOfWork))]
public class UnitOfWork : IUnitOfWork
{
    public NooDbContext Context { get; init; }

    public UnitOfWork(NooDbContext context)
    {
        Context = context;
    }

    /// <summary>
    /// Writes the request's work away. A unique index turning a duplicate away is an
    /// answer, not a failure: it comes back as a conflict rather than as a server error,
    /// so relying on the database to enforce uniqueness does not cost a 500.
    /// </summary>
    public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await Context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsDuplicateKey(exception))
        {
            throw new ConflictException();
        }
    }

    private static bool IsDuplicateKey(DbUpdateException exception)
    {
        return exception.InnerException is MySqlException { Number: _duplicateKeyErrorNumber };
    }

    /// <summary>
    /// MySQL's ER_DUP_ENTRY — a row rejected by a unique index.
    /// </summary>
    private const int _duplicateKeyErrorNumber = 1062;

    public void Rollback()
    {
        foreach (var entry in Context.ChangeTracker.Entries())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.State = EntityState.Detached;
                    break;
                case EntityState.Modified:
                case EntityState.Deleted:
                    entry.Reload();
                    break;
            }
        }
    }

    public void Dispose()
    {
        Context.Dispose();
        GC.SuppressFinalize(this);
    }
}
