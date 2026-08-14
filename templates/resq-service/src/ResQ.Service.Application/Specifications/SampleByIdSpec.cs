using ResQ.BuildingBlocks.Application;
using ResQ.Service.Domain;

namespace ResQ.Service.Application;

/// <summary>Selects a single sample by its identity, without change tracking.</summary>
public sealed class SampleByIdSpec : Specification<Sample>
{
    /// <summary>Creates the specification for the given <paramref name="id"/>.</summary>
    public SampleByIdSpec(SampleId id)
        : base(sample => sample.Id == id) => AsNoTrackingQuery();
}
