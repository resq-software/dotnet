using ResQ.BuildingBlocks.Adapters.Persistence;
using ResQ.Service.Application;
using ResQ.Service.Domain;

namespace ResQ.Service.Infrastructure;

/// <summary>The EF Core implementation of <see cref="ISampleRepository"/>.</summary>
public sealed class SampleRepository(SampleDbContext dbContext)
    : Repository<Sample, SampleId>(dbContext), ISampleRepository
{
}
