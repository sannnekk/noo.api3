using Noo.Api.Core.DataAbstraction.Model;

namespace Noo.Api.Core.DataAbstraction.Db;

/// <summary>
/// Produces tracked references to existing entities by id without loading them
/// from the database. Use when assigning a navigation property to an entity
/// that already exists (e.g. linking media to a course-material content).
/// </summary>
public interface IEntityReferenceFactory
{
    public TEntity Reference<TEntity>(Ulid id) where TEntity : BaseModel, new();

    public ICollection<TEntity> References<TEntity>(IEnumerable<Ulid>? ids) where TEntity : BaseModel, new();
}
