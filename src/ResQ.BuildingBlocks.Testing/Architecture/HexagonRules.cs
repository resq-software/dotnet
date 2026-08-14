namespace ResQ.BuildingBlocks.Testing;

/// <summary>
/// Machine-readable statement of the hexagonal dependency rule, intended to drive a consumer's
/// architecture tests (for example, NetArchTest assertions). No architecture-test framework is
/// referenced here, so unit-only consumers pull nothing extra.
/// </summary>
public static class HexagonRules
{
    /// <summary>
    /// Gets the dependency rule as a list of <c>(Inner, MayNotBeReferencedBy)</c> pairs, where
    /// <c>Inner</c> is a ring's assembly/namespace prefix and <c>MayNotBeReferencedBy</c> lists the
    /// outer-ring prefixes that <c>Inner</c> must not depend on. The Domain ring depends on nothing;
    /// Application may depend only on Domain; adapters may depend only on Application and Domain.
    /// </summary>
    public static IReadOnlyList<(string Inner, string[] MayNotBeReferencedBy)> DependencyRule { get; } =
    [
        ("ResQ.BuildingBlocks.Domain", ["ResQ.BuildingBlocks.Application", "ResQ.BuildingBlocks.Adapters", "ResQ.BuildingBlocks.ServiceDefaults"]),
        ("ResQ.BuildingBlocks.Application", ["ResQ.BuildingBlocks.Adapters", "ResQ.BuildingBlocks.ServiceDefaults"]),
    ];
}
