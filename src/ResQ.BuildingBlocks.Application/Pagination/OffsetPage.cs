namespace ResQ.BuildingBlocks.Application;

/// <summary>A single page of results addressed by page number and size, with the total row count.</summary>
/// <typeparam name="T">The item type.</typeparam>
/// <param name="Items">The items on this page.</param>
/// <param name="Page">The 1-based page number.</param>
/// <param name="PageSize">The maximum number of items per page.</param>
/// <param name="TotalRows">The total number of rows across all pages.</param>
public sealed record OffsetPage<T>(IReadOnlyList<T> Items, int Page, int PageSize, long TotalRows)
{
    /// <summary>The total number of pages, or <c>0</c> when <see cref="PageSize"/> is not positive.</summary>
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalRows / (double)PageSize);
}
