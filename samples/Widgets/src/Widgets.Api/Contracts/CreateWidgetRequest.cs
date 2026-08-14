using Widgets.Application;

namespace Widgets.Api;

/// <summary>The POST body for creating a widget.</summary>
public sealed record CreateWidgetRequest(string Name, int Quantity)
{
    /// <summary>Maps the request to its application command.</summary>
    public CreateWidgetCommand ToCommand() => new(Name, Quantity);
}
