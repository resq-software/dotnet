using ResQ.BuildingBlocks.Application;
using Widgets.Domain;

namespace Widgets.Application;

/// <summary>Selects a single widget by its identity, without change tracking.</summary>
public sealed class WidgetByIdSpec : Specification<Widget>
{
    /// <summary>Creates the specification for the given <paramref name="id"/>.</summary>
    public WidgetByIdSpec(WidgetId id)
        : base(widget => widget.Id == id) => AsNoTrackingQuery();
}
