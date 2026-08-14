using ResQ.BuildingBlocks.Application;

namespace Widgets.Application;

/// <summary>The cross-service fact published when a widget is created.</summary>
public sealed record WidgetCreatedIntegrationEvent(Guid WidgetId, string Name, int Quantity) : IntegrationEvent;
