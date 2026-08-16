using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ResQ.BuildingBlocks.Application;

namespace ResQ.BuildingBlocks.Adapters.Messaging;

/// <summary>
/// A fluent builder for selecting the messaging transport and reliability components. Obtained inside the
/// <c>configure</c> callback of <c>AddResqMessaging</c>; each method mutates the underlying
/// <see cref="IServiceCollection"/> and returns the builder for chaining.
/// </summary>
public sealed class MessagingBuilder
{
    internal MessagingBuilder(IServiceCollection services) => Services = services;

    /// <summary>The service collection being configured.</summary>
    public IServiceCollection Services { get; }

    /// <summary>Uses the in-memory <see cref="ChannelMessageBroker"/> as both publisher and message source.</summary>
    /// <returns>The same builder for chaining.</returns>
    public MessagingBuilder UseInMemory()
    {
        Services.RemoveAll<ChannelMessageBroker>();
        Services.AddSingleton<ChannelMessageBroker>();
        Services.RemoveAll<IIntegrationEventPublisher>();
        Services.AddSingleton<IIntegrationEventPublisher>(sp => sp.GetRequiredService<ChannelMessageBroker>());
        Services.AddSingleton<IMessageSource>(sp => sp.GetRequiredService<ChannelMessageBroker>());
        return this;
    }

    /// <summary>Registers <typeparamref name="TPublisher"/> as the outbound integration-event publisher.</summary>
    /// <typeparam name="TPublisher">The publisher implementation.</typeparam>
    /// <returns>The same builder for chaining.</returns>
    public MessagingBuilder UsePublisher<TPublisher>()
        where TPublisher : class, IIntegrationEventPublisher
    {
        Services.RemoveAll<IIntegrationEventPublisher>();
        Services.AddSingleton<IIntegrationEventPublisher, TPublisher>();
        return this;
    }

    /// <summary>Adds <typeparamref name="TSource"/> as an inbound message source for consumers to drain.</summary>
    /// <remarks>
    /// Registers the source under the default (unkeyed) <see cref="IMessageSource"/>, which a single
    /// <see cref="MessageConsumerService"/> subclass resolves automatically. For a multi-source setup —
    /// several consumers each draining a distinct source — register each source with
    /// <see cref="AddKeyedMessageSource{TSource}"/> instead and have every consumer subclass select its
    /// source with <c>[FromKeyedServices(sourceKey)]</c> on the base-constructor <c>source</c> parameter.
    /// </remarks>
    /// <typeparam name="TSource">The message-source implementation.</typeparam>
    /// <returns>The same builder for chaining.</returns>
    public MessagingBuilder AddMessageSource<TSource>()
        where TSource : class, IMessageSource
    {
        Services.AddSingleton<IMessageSource, TSource>();
        return this;
    }

    /// <summary>
    /// Adds <typeparamref name="TSource"/> as a <i>keyed</i> inbound message source, so distinct consumers
    /// can each drain a distinct source in a multi-source setup.
    /// </summary>
    /// <remarks>
    /// A <see cref="MessageConsumerService"/> subclass selects this source by annotating its base-constructor
    /// <c>source</c> parameter with <c>[FromKeyedServices(sourceKey)]</c> using the same
    /// <paramref name="sourceKey"/>.
    /// </remarks>
    /// <typeparam name="TSource">The message-source implementation.</typeparam>
    /// <param name="sourceKey">The DI service key a consumer subclass uses to select this source.</param>
    /// <returns>The same builder for chaining.</returns>
    public MessagingBuilder AddKeyedMessageSource<TSource>(object sourceKey)
        where TSource : class, IMessageSource
    {
        ArgumentNullException.ThrowIfNull(sourceKey);
        Services.AddKeyedSingleton<IMessageSource, TSource>(sourceKey);
        return this;
    }

    /// <summary>Replaces the default dead-letter sink with <typeparamref name="TSink"/>.</summary>
    /// <typeparam name="TSink">The dead-letter sink implementation.</typeparam>
    /// <returns>The same builder for chaining.</returns>
    public MessagingBuilder UseDeadLetterSink<TSink>()
        where TSink : class, IDeadLetterSink
    {
        Services.RemoveAll<IDeadLetterSink>();
        Services.AddSingleton<IDeadLetterSink, TSink>();
        return this;
    }
}
