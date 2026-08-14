using Microsoft.AspNetCore.Mvc;

namespace ResQ.BuildingBlocks.Adapters.Web;

/// <summary>
/// An offset-pagination query request bound from <c>?page=&amp;pageSize=</c>. Use with
/// <c>[AsParameters]</c> on a minimal-API handler, then call
/// <see cref="Normalize(PaginationOptions)"/> to clamp the values.
/// </summary>
public sealed record PageRequest
{
    /// <summary>The 1-based page number.</summary>
    [FromQuery(Name = "page")]
    public int Page { get; init; } = 1;

    /// <summary>The requested page size.</summary>
    [FromQuery(Name = "pageSize")]
    public int PageSize { get; init; } = 20;

    /// <summary>
    /// Returns a normalized copy with <c>Page &gt;= 1</c> and <c>PageSize</c> clamped to
    /// <c>[1, <see cref="PaginationOptions.MaxPageSize"/>]</c> (falling back to
    /// <see cref="PaginationOptions.DefaultPageSize"/> when non-positive).
    /// </summary>
    /// <param name="options">The pagination bounds.</param>
    /// <returns>A normalized <see cref="PageRequest"/>.</returns>
    public PageRequest Normalize(PaginationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var page = Page < 1 ? 1 : Page;
        var size = PageSize < 1 ? options.DefaultPageSize : Math.Min(PageSize, options.MaxPageSize);
        return this with { Page = page, PageSize = size };
    }
}

/// <summary>
/// A keyset (cursor) pagination query request bound from <c>?cursor=&amp;pageSize=</c>. Part of the
/// EXPERIMENTAL cursor surface — see <see cref="CursorCodec"/>.
/// </summary>
public sealed record CursorRequest
{
    /// <summary>The opaque, base64url-encoded cursor, or <see langword="null"/> for the first page.</summary>
    [FromQuery(Name = "cursor")]
    public string? Cursor { get; init; }

    /// <summary>The requested page size.</summary>
    [FromQuery(Name = "pageSize")]
    public int PageSize { get; init; } = 20;
}
