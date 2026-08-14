using ResQ.BuildingBlocks.Application;

namespace Widgets.Application;

/// <summary>Lists widgets as an offset page, newest first.</summary>
public sealed record ListWidgetsQuery(int Page, int PageSize) : IQuery<OffsetPage<WidgetDto>>;
