using ResQ.BuildingBlocks.Domain;

namespace ResQ.Service.Domain;

/// <summary>Raised when a sample is renamed.</summary>
public sealed record SampleRenamed(Guid SampleId, string Name, DateTimeOffset OccurredOnUtc) : IDomainEvent;
