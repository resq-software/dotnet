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
    /// <typeparam name="TSource">The message-source implementation.</typeparam>
    /// <returns>The same builder for chaining.</returns>
    public MessagingBuilder AddConsumer<TSource>()
        where TSource : class, IMessageSource
    {
        Services.AddSingleton<IMessageSource, TSource>();
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
