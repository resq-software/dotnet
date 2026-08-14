namespace ResQ.Service.Api;

/// <summary>The HTTP representation of a sample.</summary>
public sealed record SampleResponse(Guid Id, string Name, int Quantity);
