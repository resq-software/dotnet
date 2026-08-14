using ResQ.BuildingBlocks.Application;

namespace Widgets.Application;

/// <summary>Loads a single widget by its identity.</summary>
public sealed record GetWidgetByIdQuery(Guid Id) : IQuery<WidgetDto>;
