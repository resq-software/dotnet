using ResQ.BuildingBlocks.Domain;

namespace ResQ.Service.Domain;

/// <summary>Raised when a sample's quantity changes.</summary>
public sealed record SampleRestocked(Guid SampleId, int Delta, int Quantity, DateTimeOffset OccurredOnUtc) : IDomainEvent;
