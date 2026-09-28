// SPDX-License-Identifier: Apache-2.0

using ResQ.BuildingBlocks.Domain;

namespace Widgets.Domain;

/// <summary>Raised when a widget is created.</summary>
public sealed record WidgetCreated(Guid WidgetId, string Name, int Quantity, DateTimeOffset OccurredOnUtc) : IDomainEvent;
