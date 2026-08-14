namespace ResQ.BuildingBlocks.Adapters.Web;

/// <summary>Bounds applied when normalizing paginated requests.</summary>
public sealed class PaginationOptions
{
    /// <summary>The page size used when a request omits or under-specifies it.</summary>
    public int DefaultPageSize { get; set; } = 20;

    /// <summary>The largest page size a request may ask for.</summary>
    public int MaxPageSize { get; set; } = 100;
}
