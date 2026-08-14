namespace Widgets.Application;

/// <summary>A read model projecting a <see cref="Widgets.Domain.Widget"/>.</summary>
public sealed record WidgetDto(Guid Id, string Name, int Quantity);
