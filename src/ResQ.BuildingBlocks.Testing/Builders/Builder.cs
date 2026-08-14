namespace ResQ.BuildingBlocks.Testing;

/// <summary>
/// Base class for fluent test-data builders. Derive from it, expose <c>With…</c> mutators, and
/// implement <see cref="Build"/>; the implicit conversion lets a builder be passed anywhere a
/// <typeparamref name="T"/> is expected.
/// </summary>
/// <typeparam name="T">The type produced by the builder.</typeparam>
public abstract class Builder<T>
{
    /// <summary>Materializes the configured <typeparamref name="T"/> instance.</summary>
    /// <returns>The built value.</returns>
    public abstract T Build();

    /// <summary>Implicitly builds the value, so a <see cref="Builder{T}"/> can be used as a <typeparamref name="T"/>.</summary>
    /// <param name="builder">The builder to materialize.</param>
    /// <returns>The result of <see cref="Build"/>.</returns>
    public static implicit operator T(Builder<T> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.Build();
    }
}
