using AutoFilterer.Abstractions;
using Noo.Api.Core.DataAbstraction.Model;

namespace Noo.Api.Core.DataAbstraction.Db;

public static class DefaultOrderingExtensions
{
    public static IQueryable<T> ApplyDefaultOrdering<T>(this IQueryable<T> query, IPaginationFilter filter)
        where T : BaseModel
    {
        if (filter is IOrderable orderable && !string.IsNullOrEmpty(orderable.Sort))
        {
            return query;
        }

        return query.OrderByDescending(e => e.Id);
    }
}
