using ResQ.BuildingBlocks.Application;
using ResQ.BuildingBlocks.Domain;

namespace Widgets.Application;

/// <summary>Handles <see cref="ListWidgetsQuery"/> using <see cref="WidgetsPageSpec"/>.</summary>
public sealed class ListWidgetsHandler(IWidgetRepository repository)
    : IQueryHandler<ListWidgetsQuery, OffsetPage<WidgetDto>>
{
    /// <inheritdoc />
    public async Task<Result<OffsetPage<WidgetDto>>> Handle(ListWidgetsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var widgets = await repository.ListAsync(new WidgetsPageSpec(query.Page, query.PageSize), cancellationToken);
        var total = await repository.CountAsync(new WidgetsPageSpec(), cancellationToken);

        var items = widgets
            .Select(widget => new WidgetDto(widget.Id.Value, widget.Name, widget.Quantity))
            .ToList();

        return Result.Success(new OffsetPage<WidgetDto>(items, query.Page, query.PageSize, total));
    }
}
