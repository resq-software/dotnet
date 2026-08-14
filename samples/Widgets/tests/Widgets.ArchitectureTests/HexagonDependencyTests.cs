using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;
using ResQ.BuildingBlocks.Testing;
using Widgets.Application;
using Widgets.Domain;
using Widgets.Infrastructure;
using Xunit;

namespace Widgets.ArchitectureTests;

/// <summary>
/// Enforces the hexagonal dependency rule for both the shipped building blocks (driven by
/// <see cref="HexagonRules.DependencyRule"/>) and the Widgets sample rings. The sample's Domain is
/// adapter-free, so every rule below holds.
/// </summary>
public sealed class HexagonDependencyTests
{
    private static readonly Assembly DomainAssembly = typeof(Widget).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(CreateWidgetCommand).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(WidgetsDbContext).Assembly;

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
                "Widgets.Application",
                "Widgets.Infrastructure",
                "Widgets.Api",
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
                "Widgets.Infrastructure",
                "Widgets.Api",
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
            .HaveDependencyOn("Widgets.Api")
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
