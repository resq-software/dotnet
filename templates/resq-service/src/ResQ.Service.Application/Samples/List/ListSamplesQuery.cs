using ResQ.BuildingBlocks.Application;

namespace ResQ.Service.Application;

/// <summary>Lists samples as an offset page, newest first.</summary>
public sealed record ListSamplesQuery(int Page, int PageSize) : IQuery<OffsetPage<SampleDto>>;
