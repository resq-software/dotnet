namespace Widgets.Domain;

/// <summary>Strongly-typed identity for a <see cref="Widget"/>.</summary>
public readonly record struct WidgetId(Guid Value)
{
    /// <summary>Creates a fresh, unique identity.</summary>
    public static WidgetId New() => new(Guid.NewGuid());
}
