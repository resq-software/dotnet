using ResQ.BuildingBlocks.Application;

namespace ResQ.Service.Application;

/// <summary>Loads a single sample by its identity.</summary>
public sealed record GetSampleByIdQuery(Guid Id) : IQuery<SampleDto>;
