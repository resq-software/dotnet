// SPDX-License-Identifier: Apache-2.0

namespace Widgets.Api;

/// <summary>A single page of results in HTTP form.</summary>
/// <typeparam name="T">The item type.</typeparam>
public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, long TotalRows);
