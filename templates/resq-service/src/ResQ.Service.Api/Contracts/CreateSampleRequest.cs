using ResQ.Service.Application;

namespace ResQ.Service.Api;

/// <summary>The POST body for creating a sample.</summary>
public sealed record CreateSampleRequest(string Name, int Quantity)
{
    /// <summary>Maps the request to its application command.</summary>
    public CreateSampleCommand ToCommand() => new(Name, Quantity);
}
