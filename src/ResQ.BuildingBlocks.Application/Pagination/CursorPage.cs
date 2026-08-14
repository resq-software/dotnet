namespace ResQ.BuildingBlocks.Application;

/// <summary>A page of results addressed by opaque cursors (keyset pagination).</summary>
/// <remarks>
/// <b>Experimental / optional.</b> Keyset pagination has no first-cut consumer; the reference sample
/// uses <see cref="OffsetPage{T}"/> only. The shape may change before it becomes load-bearing.
/// </remarks>
/// <typeparam name="T">The item type.</typeparam>
/// <param name="Items">The items on this page.</param>
/// <param name="NextCursor">An opaque cursor for the next page, or <see langword="null"/> at the end.</param>
/// <param name="PrevCursor">An opaque cursor for the previous page, or <see langword="null"/> at the start.</param>
/// <param name="HasMore">Whether more items follow this page.</param>
public sealed record CursorPage<T>(IReadOnlyList<T> Items, string? NextCursor, string? PrevCursor, bool HasMore);
