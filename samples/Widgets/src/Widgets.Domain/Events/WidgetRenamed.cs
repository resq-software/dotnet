using ResQ.BuildingBlocks.Domain;

namespace Widgets.Domain;

/// <summary>Raised when a widget is renamed.</summary>
public sealed record WidgetRenamed(Guid WidgetId, string Name, DateTimeOffset OccurredOnUtc) : IDomainEvent;
