using ResQ.BuildingBlocks.Application;

namespace ResQ.Service.Application;

/// <summary>Renames an existing sample.</summary>
public sealed record RenameSampleCommand(Guid Id, string Name) : ICommand;
