using System.Linq.Expressions;

namespace ResQ.BuildingBlocks.Application;

/// <summary>
/// A base <see cref="ISpecification{T}"/> that derived specifications build up with protected fluent
/// helpers (filter, includes, ordering, paging, tracking, split query, and query-filter opt-out).
/// </summary>
/// <typeparam name="T">The entity the specification queries.</typeparam>
public abstract class Specification<T> : ISpecification<T>
{
    private readonly List<Expression<Func<T, object?>>> _includes = [];
    private readonly List<string> _includeStrings = [];
    private readonly List<(Expression<Func<T, object?>> KeySelector, bool Descending)> _orderBy = [];

    /// <summary>Creates a specification with no filter.</summary>
    protected Specification()
    {
    }

    /// <summary>Creates a specification with an initial filter predicate.</summary>
    /// <param name="criteria">The filter predicate.</param>
    protected Specification(Expression<Func<T, bool>> criteria) => Criteria = criteria;

    /// <inheritdoc />
    public Expression<Func<T, bool>>? Criteria { get; private set; }

    /// <inheritdoc />
    public IReadOnlyList<Expression<Func<T, object?>>> Includes => _includes;

    /// <inheritdoc />
    public IReadOnlyList<string> IncludeStrings => _includeStrings;

    /// <inheritdoc />
    public IReadOnlyList<(Expression<Func<T, object?>> KeySelector, bool Descending)> OrderBy => _orderBy;

    /// <inheritdoc />
    public int? Skip { get; private set; }

    /// <inheritdoc />
    public int? Take { get; private set; }

    /// <inheritdoc />
    public bool AsNoTracking { get; private set; }

    /// <inheritdoc />
    public bool AsSplitQuery { get; private set; }

    /// <inheritdoc />
    public bool IgnoreQueryFilters { get; private set; }

    /// <summary>Adds an eager-load member selector.</summary>
    /// <param name="includeExpression">The member to include.</param>
    protected void AddInclude(Expression<Func<T, object?>> includeExpression) => _includes.Add(includeExpression);

    /// <summary>Adds an eager-load path string.</summary>
    /// <param name="includeString">The provider-specific include path.</param>
    protected void AddInclude(string includeString) => _includeStrings.Add(includeString);

    /// <summary>Appends an ascending ordering key.</summary>
    /// <param name="keySelector">The ordering key selector.</param>
    protected void ApplyOrderBy(Expression<Func<T, object?>> keySelector) => _orderBy.Add((keySelector, false));

    /// <summary>Appends a descending ordering key.</summary>
    /// <param name="keySelector">The ordering key selector.</param>
    protected void ApplyOrderByDescending(Expression<Func<T, object?>> keySelector) => _orderBy.Add((keySelector, true));

    /// <summary>Applies skip/take paging.</summary>
    /// <param name="skip">The number of rows to skip.</param>
    /// <param name="take">The maximum number of rows to take.</param>
    protected void ApplyPaging(int skip, int take)
    {
        Skip = skip;
        Take = take;
    }

    /// <summary>Runs the query without change tracking.</summary>
    protected void AsNoTrackingQuery() => AsNoTracking = true;

    /// <summary>Executes the query as a split query.</summary>
    protected void AsSplitQueryable() => AsSplitQuery = true;

    /// <summary>Ignores global query filters for this query.</summary>
    protected void IgnoreFilters() => IgnoreQueryFilters = true;
}
