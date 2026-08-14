using Microsoft.EntityFrameworkCore;
using ResQ.BuildingBlocks.Application;

namespace ResQ.BuildingBlocks.Adapters.Persistence;

/// <summary>
/// The EF Core outbox — serializes an integration event and stages an <see cref="OutboxMessage"/> row on
/// the tracked context, so it is written in the same transaction as the aggregate change (a true
/// transactional outbox).
/// </summary>
/// <param name="dbContext">The unit-of-work context the row is staged on.</param>
/// <param name="serializer">The serializer used to encode the event payload.</param>
/// <param name="clock">The clock stamping the enqueue instant.</param>
public sealed class EfOutbox(DbContext dbContext, IMessageSerializer serializer, IClock clock) : IOutbox
{
    /// <inheritdoc />
    public void Enqueue(IntegrationEvent @event)
    {
        var message = new OutboxMessage
        {
            Id = @event.Id,
            Type = @event.EventType,
            Content = serializer.Serialize(@event, @event.GetType()),
            OccurredOnUtc = clock.UtcNow,
        };

        dbContext.Set<OutboxMessage>().Add(message);
    }
}
