using ResQ.BuildingBlocks.Application;

namespace ResQ.Service.Application;

/// <summary>The cross-service fact published when a sample is created.</summary>
public sealed record SampleCreatedIntegrationEvent(Guid SampleId, string Name, int Quantity) : IntegrationEvent;
