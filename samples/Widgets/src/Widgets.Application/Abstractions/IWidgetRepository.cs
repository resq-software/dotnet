// SPDX-License-Identifier: Apache-2.0

using ResQ.BuildingBlocks.Application;
using Widgets.Domain;

namespace Widgets.Application;

/// <summary>The aggregate-specific repository port for <see cref="Widget"/>.</summary>
public interface IWidgetRepository : IRepository<Widget, WidgetId>;
