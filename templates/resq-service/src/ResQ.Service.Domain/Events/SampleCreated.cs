using ResQ.BuildingBlocks.Domain;

namespace ResQ.Service.Domain;

/// <summary>Raised when a sample is created.</summary>
public sealed record SampleCreated(Guid SampleId, string Name, int Quantity, DateTimeOffset OccurredOnUtc) : IDomainEvent;
