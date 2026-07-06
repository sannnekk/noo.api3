using System.Linq.Expressions;
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

        // Ordering may already be defined by a query specification
        if (IsOrdered(query))
        {
            return query;
        }

        return query.OrderByDescending(e => e.Id);
    }

    private static bool IsOrdered<T>(IQueryable<T> query)
    {
        var finder = new OrderingMethodFinder();
        finder.Visit(query.Expression);

        return finder.OrderingFound;
    }

    private sealed class OrderingMethodFinder : ExpressionVisitor
    {
        public bool OrderingFound { get; private set; }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == typeof(Queryable) &&
                node.Method.Name is nameof(Queryable.OrderBy)
                    or nameof(Queryable.OrderByDescending)
                    or nameof(Queryable.ThenBy)
                    or nameof(Queryable.ThenByDescending))
            {
                OrderingFound = true;
            }

            return base.VisitMethodCall(node);
        }
    }
}
