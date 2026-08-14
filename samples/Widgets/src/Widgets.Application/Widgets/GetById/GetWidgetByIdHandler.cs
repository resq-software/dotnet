using ResQ.BuildingBlocks.Application;
using ResQ.BuildingBlocks.Domain;
using Widgets.Domain;

namespace Widgets.Application;

/// <summary>Handles <see cref="GetWidgetByIdQuery"/> via <see cref="WidgetByIdSpec"/>.</summary>
public sealed class GetWidgetByIdHandler(IWidgetRepository repository)
    : IQueryHandler<GetWidgetByIdQuery, WidgetDto>
{
    /// <inheritdoc />
    public async Task<Result<WidgetDto>> Handle(GetWidgetByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var widget = await repository.FirstOrDefaultAsync(new WidgetByIdSpec(new WidgetId(query.Id)), cancellationToken);
        if (widget is null)
        {
            return Result.Failure<WidgetDto>(Error.NotFound("widget.not_found", $"Widget '{query.Id}' was not found."));
        }

        return Result.Success(new WidgetDto(widget.Id.Value, widget.Name, widget.Quantity));
    }
}
