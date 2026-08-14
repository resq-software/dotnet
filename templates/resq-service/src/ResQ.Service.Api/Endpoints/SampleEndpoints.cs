using Microsoft.Extensions.Options;
using ResQ.BuildingBlocks.Adapters.Web;
using ResQ.BuildingBlocks.Application;
using ResQ.Service.Application;

namespace ResQ.Service.Api;

/// <summary>Maps the sample REST resource (create, get-by-id, list) onto the versioned <c>/api</c> group.</summary>
public sealed class SampleEndpoints : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var versionSet = app.CreateResqVersionSet(1.0);
        var group = app.MapVersionedGroup("/api", versionSet).MapGroup("/samples");

        group.MapPost("", CreateSampleAsync).WithValidation<CreateSampleRequest>();
        group.MapGet("{id:guid}", GetSampleAsync);
        group.MapGet("", ListSamplesAsync);
    }

    private static async Task<IResult> CreateSampleAsync(
        CreateSampleRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(request.ToCommand(), cancellationToken);
        return result.ToHttpResult(id => TypedResults.Created($"/api/v1/samples/{id}", id));
    }

    private static async Task<IResult> GetSampleAsync(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetSampleByIdQuery(id), cancellationToken);
        return result.ToHttpResult(sample =>
            TypedResults.Ok(new SampleResponse(sample.Id, sample.Name, sample.Quantity)));
    }

    private static async Task<IResult> ListSamplesAsync(
        [AsParameters] PageRequest page,
        ISender sender,
        IOptions<PaginationOptions> paginationOptions,
        CancellationToken cancellationToken)
    {
        var normalized = page.Normalize(paginationOptions.Value);
        var result = await sender.Send(new ListSamplesQuery(normalized.Page, normalized.PageSize), cancellationToken);

        return result.ToHttpResult(offsetPage =>
        {
            var items = offsetPage.Items
                .Select(sample => new SampleResponse(sample.Id, sample.Name, sample.Quantity))
                .ToList();

            return TypedResults.Ok(
                new PagedResponse<SampleResponse>(items, offsetPage.Page, offsetPage.PageSize, offsetPage.TotalRows));
        });
    }
}
