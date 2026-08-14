using ResQ.BuildingBlocks.Domain;

namespace Widgets.Domain;

/// <summary>Raised when a widget's stock level changes.</summary>
public sealed record WidgetRestocked(Guid WidgetId, int Delta, int Quantity, DateTimeOffset OccurredOnUtc) : IDomainEvent;
