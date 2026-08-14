namespace ResQ.Service.Domain;

/// <summary>Strongly-typed identity for a <see cref="Sample"/>.</summary>
public readonly record struct SampleId(Guid Value)
{
    /// <summary>Creates a fresh, unique identity.</summary>
    public static SampleId New() => new(Guid.NewGuid());
}
