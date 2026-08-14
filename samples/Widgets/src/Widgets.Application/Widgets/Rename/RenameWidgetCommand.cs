using ResQ.BuildingBlocks.Application;

namespace Widgets.Application;

/// <summary>Renames an existing widget.</summary>
public sealed record RenameWidgetCommand(Guid Id, string Name) : ICommand;
