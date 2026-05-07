using Noo.Api.Core.DataAbstraction.Model;
using Noo.Api.Core.Utils.DI;

namespace Noo.Api.Core.DataAbstraction.Db;

[RegisterScoped(typeof(IEntityReferenceFactory))]
public class EntityReferenceFactory : IEntityReferenceFactory
{
    private readonly NooDbContext _context;

    public EntityReferenceFactory(NooDbContext context)
    {
        _context = context;
    }

    public TEntity Reference<TEntity>(Ulid id) where TEntity : BaseModel, new()
    {
        var set = _context.Set<TEntity>();

        var tracked = set.Local.FirstOrDefault(e => e.Id == id);
        if (tracked != null)
        {
            return tracked;
        }

        var stub = new TEntity { Id = id };
        set.Attach(stub);
        return stub;
    }

    public ICollection<TEntity> References<TEntity>(IEnumerable<Ulid>? ids) where TEntity : BaseModel, new()
    {
        if (ids == null)
        {
            return [];
        }

        return ids.Select(Reference<TEntity>).ToList();
    }
}
