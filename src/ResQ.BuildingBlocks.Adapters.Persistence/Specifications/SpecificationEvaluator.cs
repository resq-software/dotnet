using Microsoft.EntityFrameworkCore;
using ResQ.BuildingBlocks.Application;

namespace ResQ.BuildingBlocks.Adapters.Persistence;

/// <summary>
/// Translates an EF-free <see cref="ISpecification{T}"/> onto an EF Core <see cref="IQueryable{T}"/>,
/// applying its criteria, includes, ordering, paging, and query flags.
/// </summary>
public static class SpecificationEvaluator
{
    /// <summary>Builds a query from an input queryable and a specification.</summary>
    /// <typeparam name="T">The queried entity type.</typeparam>
    /// <param name="input">The root queryable (typically a <see cref="DbSet{TEntity}"/>).</param>
    /// <param name="spec">The specification to apply.</param>
    /// <returns>The composed queryable.</returns>
    public static IQueryable<T> GetQuery<T>(IQueryable<T> input, ISpecification<T> spec)
        where T : class
    {
        var query = input;

        if (spec.Criteria is not null)
        {
            query = query.Where(spec.Criteria);
        }

        query = spec.Includes.Aggregate(query, (current, include) => current.Include(include));
        query = spec.IncludeStrings.Aggregate(query, (current, include) => current.Include(include));

        if (spec.OrderBy.Count > 0)
        {
            IOrderedQueryable<T>? ordered = null;
            foreach (var (keySelector, descending) in spec.OrderBy)
            {
                ordered = ordered is null
                    ? descending ? query.OrderByDescending(keySelector) : query.OrderBy(keySelector)
                    : descending ? ordered.ThenByDescending(keySelector) : ordered.ThenBy(keySelector);
            }

            query = ordered ?? query;
        }

        if (spec.IgnoreQueryFilters)
        {
            query = query.IgnoreQueryFilters();
        }

        if (spec.AsSplitQuery)
        {
            query = query.AsSplitQuery();
        }

        if (spec.AsNoTracking)
        {
            query = query.AsNoTracking();
        }

        if (spec.Skip is { } skip)
        {
            query = query.Skip(skip);
        }

        if (spec.Take is { } take)
        {
            query = query.Take(take);
        }

        return query;
    }
}
