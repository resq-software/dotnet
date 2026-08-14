using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;
using ResQ.BuildingBlocks.Testing;
using ResQ.Service.Application;
using ResQ.Service.Domain;
using ResQ.Service.Infrastructure;
using Xunit;

namespace ResQ.Service.ArchitectureTests;

/// <summary>
/// Enforces the hexagonal dependency rule for both the shipped building blocks (driven by
/// <see cref="HexagonRules.DependencyRule"/>) and this service's rings. The Domain is adapter-free, so
/// every rule below holds.
/// </summary>
public sealed class HexagonDependencyTests
{
    private static readonly Assembly DomainAssembly = typeof(Sample).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(CreateSampleCommand).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(SampleDbContext).Assembly;

    public static TheoryData<string, string[]> HexagonRuleData()
    {
        var data = new TheoryData<string, string[]>();
        foreach (var (inner, mayNotDependOn) in HexagonRules.DependencyRule)
        {
            data.Add(inner, mayNotDependOn);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(HexagonRuleData))]
    public void Building_block_ring_does_not_depend_on_outer_rings(string inner, string[] forbidden)
    {
        var result = Types.InAssembly(ResolveBuildingBlockAssembly(inner))
            .ShouldNot()
            .HaveDependencyOnAny(forbidden)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(Because(result));
    }

    [Fact]
    public void Domain_depends_on_nothing_outward()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "ResQ.Service.Application",
                "ResQ.Service.Infrastructure",
                "ResQ.Service.Api",
                "ResQ.BuildingBlocks.Application",
                "ResQ.BuildingBlocks.Adapters",
                "ResQ.BuildingBlocks.ServiceDefaults")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(Because(result));
    }

    [Fact]
    public void Application_depends_on_domain_only()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "ResQ.Service.Infrastructure",
                "ResQ.Service.Api",
                "ResQ.BuildingBlocks.Adapters",
                "ResQ.BuildingBlocks.ServiceDefaults")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(Because(result));
    }

    [Fact]
    public void Infrastructure_stays_inward_and_never_references_the_api()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn("ResQ.Service.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(Because(result));
    }

    private static Assembly ResolveBuildingBlockAssembly(string name) => name switch
    {
        "ResQ.BuildingBlocks.Domain" => typeof(ResQ.BuildingBlocks.Domain.IDomainEvent).Assembly,
        "ResQ.BuildingBlocks.Application" => typeof(ResQ.BuildingBlocks.Application.ICommand).Assembly,
        _ => Assembly.Load(name),
    };

    private static string Because(TestResult result) =>
        result.FailingTypeNames is { Count: > 0 } failing
            ? $"these types violate the hexagon rule: {string.Join(", ", failing)}"
            : "the hexagon dependency rule must hold";
}
