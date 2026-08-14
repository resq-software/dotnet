using ResQ.BuildingBlocks.Application;
using Widgets.Domain;

namespace Widgets.Application;

/// <summary>
/// Orders newest-first and pages the widget set. The parameterless constructor omits paging so it can
/// back an unbounded <c>CountAsync</c> for the page total.
/// </summary>
public sealed class WidgetsPageSpec : Specification<Widget>
{
    /// <summary>Creates a match-all specification (no paging) for counting.</summary>
    public WidgetsPageSpec() => AsNoTrackingQuery();

    /// <summary>Creates a newest-first, paged specification.</summary>
    /// <param name="page">The 1-based page number.</param>
    /// <param name="pageSize">The page size.</param>
    public WidgetsPageSpec(int page, int pageSize)
    {
        ApplyOrderByDescending(widget => widget.CreatedOnUtc);
        ApplyPaging((page - 1) * pageSize, pageSize);
        AsNoTrackingQuery();
    }
}
