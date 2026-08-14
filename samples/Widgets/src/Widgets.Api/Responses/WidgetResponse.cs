namespace Widgets.Api;

/// <summary>The HTTP representation of a widget.</summary>
public sealed record WidgetResponse(Guid Id, string Name, int Quantity);
