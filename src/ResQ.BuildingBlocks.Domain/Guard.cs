using System.Runtime.CompilerServices;

namespace ResQ.BuildingBlocks.Domain;

/// <summary>Guard clauses for enforcing invariants at the boundaries of the domain.</summary>
public static class Guard
{
    /// <summary>Throws if <paramref name="value"/> is null.</summary>
    public static T AgainstNull<T>(T? value, [CallerArgumentExpression(nameof(value))] string? name = null)
        => value ?? throw new ArgumentNullException(name);

    /// <summary>Throws if <paramref name="value"/> is null, empty, or whitespace.</summary>
    public static string AgainstNullOrWhiteSpace(
        string? value, [CallerArgumentExpression(nameof(value))] string? name = null)
        => string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value cannot be null or whitespace.", name)
            : value;

    /// <summary>Throws if <paramref name="value"/> is not greater than zero.</summary>
    public static int AgainstNonPositive(int value, [CallerArgumentExpression(nameof(value))] string? name = null)
        => value <= 0 ? throw new ArgumentOutOfRangeException(name, value, "Value must be greater than zero.") : value;
}
