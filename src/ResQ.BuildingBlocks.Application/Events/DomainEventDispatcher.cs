using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using ResQ.BuildingBlocks.Domain;

namespace ResQ.BuildingBlocks.Application;

/// <summary>
/// The default <see cref="IDomainEventDispatcher"/>. Registered <b>scoped</b>; for each event it
/// resolves every closed <see cref="IDomainEventHandler{TEvent}"/> for the event's <b>runtime</b> type
/// from the scoped provider and awaits them.
/// </summary>
/// <remarks>
/// A process-wide static cache maps each event CLR type to a reusable invoker so the closed handler
/// interface is resolved by reflection only once per event type.
/// </remarks>
public sealed class DomainEventDispatcher(IServiceProvider provider) : IDomainEventDispatcher
{
    private static readonly ConcurrentDictionary<Type, DomainEventInvoker> Invokers = new();

    /// <inheritdoc />
    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvents);

        foreach (var domainEvent in domainEvents)
        {
            var invoker = Invokers.GetOrAdd(
                domainEvent.GetType(),
                static type => (DomainEventInvoker)Activator.CreateInstance(
                    typeof(DomainEventInvoker<>).MakeGenericType(type))!);

            await invoker.DispatchAsync(domainEvent, provider, cancellationToken).ConfigureAwait(false);
        }
    }
}

/// <summary>Non-generic base so heterogeneous event invokers can share one cache.</summary>
internal abstract class DomainEventInvoker
{
    public abstract Task DispatchAsync(IDomainEvent domainEvent, IServiceProvider provider, CancellationToken cancellationToken);
}

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated reflectively by DomainEventDispatcher's invoker cache.")]
internal sealed class DomainEventInvoker<TEvent> : DomainEventInvoker
    where TEvent : IDomainEvent
{
    public override async Task DispatchAsync(IDomainEvent domainEvent, IServiceProvider provider, CancellationToken cancellationToken)
    {
        var typed = (TEvent)domainEvent;
        foreach (var handler in provider.GetServices<IDomainEventHandler<TEvent>>())
        {
            await handler.Handle(typed, cancellationToken).ConfigureAwait(false);
        }
    }
}
