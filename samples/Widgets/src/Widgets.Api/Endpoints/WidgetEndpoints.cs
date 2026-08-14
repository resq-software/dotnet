using Microsoft.Extensions.Options;
using ResQ.BuildingBlocks.Adapters.Web;
using ResQ.BuildingBlocks.Application;
using Widgets.Application;

namespace Widgets.Api;

/// <summary>Maps the widget REST resource (create, get-by-id, list) onto the versioned <c>/api</c> group.</summary>
public sealed class WidgetEndpoints : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var versionSet = app.CreateResqVersionSet(1.0);
        var group = app.MapVersionedGroup("/api", versionSet).MapGroup("/widgets");

        group.MapPost("", CreateWidgetAsync).WithValidation<CreateWidgetRequest>();
        group.MapGet("{id:guid}", GetWidgetAsync);
        group.MapGet("", ListWidgetsAsync);
    }

    private static async Task<IResult> CreateWidgetAsync(
        CreateWidgetRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(request.ToCommand(), cancellationToken);
        return result.ToHttpResult(id => TypedResults.Created($"/api/widgets/{id}", id));
    }

    private static async Task<IResult> GetWidgetAsync(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetWidgetByIdQuery(id), cancellationToken);
        return result.ToHttpResult(widget =>
            TypedResults.Ok(new WidgetResponse(widget.Id, widget.Name, widget.Quantity)));
    }

    private static async Task<IResult> ListWidgetsAsync(
        [AsParameters] PageRequest page,
        ISender sender,
        IOptions<PaginationOptions> paginationOptions,
        CancellationToken cancellationToken)
    {
        var normalized = page.Normalize(paginationOptions.Value);
        var result = await sender.Send(new ListWidgetsQuery(normalized.Page, normalized.PageSize), cancellationToken);

        return result.ToHttpResult(offsetPage =>
        {
            var items = offsetPage.Items
                .Select(widget => new WidgetResponse(widget.Id, widget.Name, widget.Quantity))
                .ToList();

            return TypedResults.Ok(
                new PagedResponse<WidgetResponse>(items, offsetPage.Page, offsetPage.PageSize, offsetPage.TotalRows));
        });
    }
}
