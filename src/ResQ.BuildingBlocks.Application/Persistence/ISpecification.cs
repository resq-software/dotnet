using System.Linq.Expressions;

namespace ResQ.BuildingBlocks.Application;

/// <summary>
/// A query specification: a storage-agnostic description of a filter, its includes, ordering, and
/// paging. Adapters translate it onto their query engine (e.g. an EF <c>IQueryable</c>); it depends
/// only on <see cref="System.Linq.Expressions"/> and carries no persistence dependency.
/// </summary>
/// <typeparam name="T">The entity the specification queries.</typeparam>
public interface ISpecification<T>
{
    /// <summary>The filter predicate, or <see langword="null"/> to match everything.</summary>
    Expression<Func<T, bool>>? Criteria { get; }

    /// <summary>Related data to eager-load, expressed as member selectors.</summary>
    IReadOnlyList<Expression<Func<T, object?>>> Includes { get; }

    /// <summary>Related data to eager-load, expressed as provider-specific include paths.</summary>
    IReadOnlyList<string> IncludeStrings { get; }

    /// <summary>Ordering key selectors paired with a descending flag, applied in order.</summary>
    IReadOnlyList<(Expression<Func<T, object?>> KeySelector, bool Descending)> OrderBy { get; }

    /// <summary>The number of rows to skip, or <see langword="null"/> for none.</summary>
    int? Skip { get; }

    /// <summary>The maximum number of rows to take, or <see langword="null"/> for no limit.</summary>
    int? Take { get; }

    /// <summary>When <see langword="true"/>, the query runs without change tracking.</summary>
    bool AsNoTracking { get; }

    /// <summary>When <see langword="true"/>, the query is executed as a split query.</summary>
    bool AsSplitQuery { get; }

    /// <summary>When <see langword="true"/>, global query filters are ignored.</summary>
    bool IgnoreQueryFilters { get; }
}
