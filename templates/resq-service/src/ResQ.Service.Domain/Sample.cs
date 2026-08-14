using ResQ.BuildingBlocks.Domain;

namespace ResQ.Service.Domain;

/// <summary>
/// A trivially generic aggregate that exercises the whole hexagon. Timestamps are set inside the
/// domain methods from an injected instant (the application handler passes <c>IClock.UtcNow</c>), so the
/// aggregate needs no <c>IAuditable</c> and the domain takes no persistence dependency.
/// </summary>
public sealed class Sample : AggregateRoot<SampleId>
{
    private Sample(SampleId id, string name, int quantity, DateTimeOffset createdOnUtc, DateTimeOffset updatedOnUtc)
        : base(id)
    {
        Name = name;
        Quantity = quantity;
        CreatedOnUtc = createdOnUtc;
        UpdatedOnUtc = updatedOnUtc;
    }

    /// <summary>The sample's display name.</summary>
    public string Name { get; private set; }

    /// <summary>An illustrative integer property.</summary>
    public int Quantity { get; private set; }

    /// <summary>When the sample was created (UTC).</summary>
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <summary>When the sample was last modified (UTC).</summary>
    public DateTimeOffset UpdatedOnUtc { get; private set; }

    /// <summary>Creates a sample, raising <see cref="SampleCreated"/> and stamping both timestamps.</summary>
    public static Sample Create(SampleId id, string name, int quantity, DateTimeOffset nowUtc)
    {
        var validName = Guard.AgainstNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegative(quantity);

        var sample = new Sample(id, validName, quantity, nowUtc, nowUtc);
        sample.Raise(new SampleCreated(id.Value, validName, quantity, nowUtc));
        return sample;
    }

    /// <summary>Renames the sample, raising <see cref="SampleRenamed"/> and refreshing the timestamp.</summary>
    public void Rename(string name, DateTimeOffset nowUtc)
    {
        var validName = Guard.AgainstNullOrWhiteSpace(name);

        Name = validName;
        UpdatedOnUtc = nowUtc;
        Raise(new SampleRenamed(Id.Value, validName, nowUtc));
    }

    /// <summary>Adjusts <see cref="Quantity"/> by <paramref name="delta"/>, raising <see cref="SampleRestocked"/>.</summary>
    public void Restock(int delta, DateTimeOffset nowUtc)
    {
        var newQuantity = Quantity + delta;
        ArgumentOutOfRangeException.ThrowIfNegative(newQuantity);

        Quantity = newQuantity;
        UpdatedOnUtc = nowUtc;
        Raise(new SampleRestocked(Id.Value, delta, newQuantity, nowUtc));
    }
}
