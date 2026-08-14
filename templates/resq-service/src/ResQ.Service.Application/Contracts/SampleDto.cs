namespace ResQ.Service.Application;

/// <summary>A read model projecting a <see cref="ResQ.Service.Domain.Sample"/>.</summary>
public sealed record SampleDto(Guid Id, string Name, int Quantity);
