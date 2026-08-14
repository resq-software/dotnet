using ResQ.BuildingBlocks.Application;

namespace Widgets.Application;

/// <summary>Creates a widget and returns its new identity.</summary>
public sealed record CreateWidgetCommand(string Name, int Quantity) : ICommand<Guid>;
