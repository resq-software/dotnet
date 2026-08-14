using ResQ.BuildingBlocks.Application;

namespace ResQ.Service.Application;

/// <summary>Creates a sample and returns its new identity.</summary>
public sealed record CreateSampleCommand(string Name, int Quantity) : ICommand<Guid>;
