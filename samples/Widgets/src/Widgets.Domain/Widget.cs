using ResQ.BuildingBlocks.Domain;

namespace Widgets.Domain;

/// <summary>
/// A trivially generic aggregate that exercises the whole hexagon. Timestamps are set inside the
/// domain methods from an injected instant (the application handler passes <c>IClock.UtcNow</c>), so the
/// aggregate needs no <c>IAuditable</c> and the domain takes no persistence dependency.
/// </summary>
public sealed class Widget : AggregateRoot<WidgetId>
{
    private Widget(WidgetId id, string name, int quantity, DateTimeOffset createdOnUtc, DateTimeOffset updatedOnUtc)
        : base(id)
    {
        Name = name;
        Quantity = quantity;
        CreatedOnUtc = createdOnUtc;
        UpdatedOnUtc = updatedOnUtc;
    }

    /// <summary>The widget's display name.</summary>
    public string Name { get; private set; }

    /// <summary>The quantity currently in stock.</summary>
    public int Quantity { get; private set; }

    /// <summary>When the widget was created (UTC).</summary>
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <summary>When the widget was last modified (UTC).</summary>
    public DateTimeOffset UpdatedOnUtc { get; private set; }

    /// <summary>Creates a widget, raising <see cref="WidgetCreated"/> and stamping both timestamps.</summary>
    public static Widget Create(WidgetId id, string name, int quantity, DateTimeOffset nowUtc)
    {
        var validName = Guard.AgainstNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegative(quantity);

        var widget = new Widget(id, validName, quantity, nowUtc, nowUtc);
        widget.Raise(new WidgetCreated(id.Value, validName, quantity, nowUtc));
        return widget;
    }

    /// <summary>Renames the widget, raising <see cref="WidgetRenamed"/> and refreshing the timestamp.</summary>
    public void Rename(string name, DateTimeOffset nowUtc)
    {
        var validName = Guard.AgainstNullOrWhiteSpace(name);

        Name = validName;
        UpdatedOnUtc = nowUtc;
        Raise(new WidgetRenamed(Id.Value, validName, nowUtc));
    }

    /// <summary>Adjusts stock by <paramref name="delta"/>, raising <see cref="WidgetRestocked"/>.</summary>
    public void Restock(int delta, DateTimeOffset nowUtc)
    {
        var newQuantity = Quantity + delta;
        ArgumentOutOfRangeException.ThrowIfNegative(newQuantity);

        Quantity = newQuantity;
        UpdatedOnUtc = nowUtc;
        Raise(new WidgetRestocked(Id.Value, delta, newQuantity, nowUtc));
    }
}
