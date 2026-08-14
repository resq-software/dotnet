using ResQ.BuildingBlocks.Application;
using ResQ.BuildingBlocks.Domain;
using ResQ.Service.Domain;

namespace ResQ.Service.Application;

/// <summary>Handles <see cref="GetSampleByIdQuery"/> via <see cref="SampleByIdSpec"/>.</summary>
public sealed class GetSampleByIdHandler(ISampleRepository repository)
    : IQueryHandler<GetSampleByIdQuery, SampleDto>
{
    /// <inheritdoc />
    public async Task<Result<SampleDto>> Handle(GetSampleByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var sample = await repository.FirstOrDefaultAsync(new SampleByIdSpec(new SampleId(query.Id)), cancellationToken);
        if (sample is null)
        {
            return Result.Failure<SampleDto>(Error.NotFound("sample.not_found", $"Sample '{query.Id}' was not found."));
        }

        return Result.Success(new SampleDto(sample.Id.Value, sample.Name, sample.Quantity));
    }
}
