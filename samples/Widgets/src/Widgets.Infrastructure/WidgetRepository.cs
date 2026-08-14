using ResQ.BuildingBlocks.Adapters.Persistence;
using Widgets.Application;
using Widgets.Domain;

namespace Widgets.Infrastructure;

/// <summary>The EF Core implementation of <see cref="IWidgetRepository"/>.</summary>
public sealed class WidgetRepository(WidgetsDbContext dbContext)
    : Repository<Widget, WidgetId>(dbContext), IWidgetRepository
{
}
