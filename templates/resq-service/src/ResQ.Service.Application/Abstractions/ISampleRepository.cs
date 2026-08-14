using ResQ.BuildingBlocks.Application;
using ResQ.Service.Domain;

namespace ResQ.Service.Application;

/// <summary>The aggregate-specific repository port for <see cref="Sample"/>.</summary>
public interface ISampleRepository : IRepository<Sample, SampleId>;
