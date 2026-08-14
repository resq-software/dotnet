using ResQ.BuildingBlocks.Application;
using ResQ.Service.Domain;

namespace ResQ.Service.Application;

/// <summary>
/// Orders newest-first and pages the sample set. The parameterless constructor omits paging so it can
/// back an unbounded <c>CountAsync</c> for the page total.
/// </summary>
public sealed class SamplesPageSpec : Specification<Sample>
{
    /// <summary>Creates a match-all specification (no paging) for counting.</summary>
    public SamplesPageSpec() => AsNoTrackingQuery();

    /// <summary>Creates a newest-first, paged specification.</summary>
    /// <param name="page">The 1-based page number.</param>
    /// <param name="pageSize">The page size.</param>
    public SamplesPageSpec(int page, int pageSize)
    {
        ApplyOrderByDescending(sample => sample.CreatedOnUtc);
        ApplyPaging((page - 1) * pageSize, pageSize);
        AsNoTrackingQuery();
    }
}
