using ResQ.BuildingBlocks.Application;
using ResQ.BuildingBlocks.Domain;

namespace ResQ.Service.Application;

/// <summary>Handles <see cref="ListSamplesQuery"/> using <see cref="SamplesPageSpec"/>.</summary>
public sealed class ListSamplesHandler(ISampleRepository repository)
    : IQueryHandler<ListSamplesQuery, OffsetPage<SampleDto>>
{
    /// <inheritdoc />
    public async Task<Result<OffsetPage<SampleDto>>> Handle(ListSamplesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var samples = await repository.ListAsync(new SamplesPageSpec(query.Page, query.PageSize), cancellationToken);
        var total = await repository.CountAsync(new SamplesPageSpec(), cancellationToken);

        var items = samples
            .Select(sample => new SampleDto(sample.Id.Value, sample.Name, sample.Quantity))
            .ToList();

        return Result.Success(new OffsetPage<SampleDto>(items, query.Page, query.PageSize, total));
    }
}
