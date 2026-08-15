using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using ResQ.BuildingBlocks.Domain;

namespace ResQ.BuildingBlocks.Application;

/// <summary>
/// The default <see cref="ISender"/>. Registered <b>scoped</b>; resolves each request's handler and
/// its ordered behavior chain from the scoped <see cref="IServiceProvider"/> it is constructed with.
/// </summary>
/// <remarks>
/// A process-wide static cache maps each request CLR type to a reusable, stateless wrapper that knows
/// how to resolve and fold the pipeline for that request. Because the wrappers hold no per-request or
/// per-scope state, the cache is shared safely across every scope.
/// </remarks>
public sealed class Sender(IServiceProvider provider) : ISender
{
    private static readonly ConcurrentDictionary<Type, RequestHandlerWrapperBase> Wrappers = new();

    /// <inheritdoc />
    [RequiresDynamicCode("Resolves a closed generic query-handler wrapper via MakeGenericType.")]
    public async Task<Result<TResponse>> Send<TResponse>(IQuery<TResponse> query, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var wrapper = (RequestResponseHandlerWrapper<TResponse>)Wrappers.GetOrAdd(
            query.GetType(),
            static (type, response) => (RequestHandlerWrapperBase)Activator.CreateInstance(
                typeof(QueryHandlerWrapper<,>).MakeGenericType(type, response))!,
            typeof(TResponse));

        return await wrapper.Handle(query, provider, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    [RequiresDynamicCode("Resolves a closed generic command-handler wrapper via MakeGenericType.")]
    public async Task<Result> Send(ICommand command, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var wrapper = (VoidCommandHandlerWrapper)Wrappers.GetOrAdd(
            command.GetType(),
            static type => (RequestHandlerWrapperBase)Activator.CreateInstance(
                typeof(CommandHandlerWrapper<>).MakeGenericType(type))!);

        return await wrapper.Handle(command, provider, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    [RequiresDynamicCode("Resolves a closed generic command-handler wrapper via MakeGenericType.")]
    public async Task<Result<TResponse>> Send<TResponse>(ICommand<TResponse> command, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var wrapper = (RequestResponseHandlerWrapper<TResponse>)Wrappers.GetOrAdd(
            command.GetType(),
            static (type, response) => (RequestHandlerWrapperBase)Activator.CreateInstance(
                typeof(CommandHandlerWrapper<,>).MakeGenericType(type, response))!,
            typeof(TResponse));

        return await wrapper.Handle(command, provider, ct).ConfigureAwait(false);
    }
}

/// <summary>Non-generic base so heterogeneous request wrappers can share one cache.</summary>
internal abstract class RequestHandlerWrapperBase;

/// <summary>A wrapper whose pipeline produces a <see cref="Result{TResponse}"/> (queries and value-returning commands).</summary>
/// <typeparam name="TResponse">The value carried on success.</typeparam>
internal abstract class RequestResponseHandlerWrapper<TResponse> : RequestHandlerWrapperBase
{
    public abstract Task<Result<TResponse>> Handle(object request, IServiceProvider provider, CancellationToken cancellationToken);
}

/// <summary>A wrapper whose pipeline produces a non-generic <see cref="Result"/> (value-less commands).</summary>
internal abstract class VoidCommandHandlerWrapper : RequestHandlerWrapperBase
{
    public abstract Task<Result> Handle(object request, IServiceProvider provider, CancellationToken cancellationToken);
}

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated reflectively by Sender's wrapper cache.")]
internal sealed class QueryHandlerWrapper<TQuery, TResponse> : RequestResponseHandlerWrapper<TResponse>
    where TQuery : IQuery<TResponse>
{
    public override Task<Result<TResponse>> Handle(object request, IServiceProvider provider, CancellationToken cancellationToken)
    {
        var query = (TQuery)request;
        var handler = provider.GetRequiredService<IQueryHandler<TQuery, TResponse>>();
        RequestHandlerDelegate<Result<TResponse>> pipeline = () => handler.Handle(query, cancellationToken);

        foreach (var behavior in provider.GetServices<IPipelineBehavior<TQuery, Result<TResponse>>>().Reverse())
        {
            var next = pipeline;
            pipeline = () => behavior.Handle(query, next, cancellationToken);
        }

        return pipeline();
    }
}

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated reflectively by Sender's wrapper cache.")]
internal sealed class CommandHandlerWrapper<TCommand> : VoidCommandHandlerWrapper
    where TCommand : ICommand
{
    public override Task<Result> Handle(object request, IServiceProvider provider, CancellationToken cancellationToken)
    {
        var command = (TCommand)request;
        var handler = provider.GetRequiredService<ICommandHandler<TCommand>>();
        RequestHandlerDelegate<Result> pipeline = () => handler.Handle(command, cancellationToken);

        foreach (var behavior in provider.GetServices<IPipelineBehavior<TCommand, Result>>().Reverse())
        {
            var next = pipeline;
            pipeline = () => behavior.Handle(command, next, cancellationToken);
        }

        return pipeline();
    }
}

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated reflectively by Sender's wrapper cache.")]
internal sealed class CommandHandlerWrapper<TCommand, TResponse> : RequestResponseHandlerWrapper<TResponse>
    where TCommand : ICommand<TResponse>
{
    public override Task<Result<TResponse>> Handle(object request, IServiceProvider provider, CancellationToken cancellationToken)
    {
        var command = (TCommand)request;
        var handler = provider.GetRequiredService<ICommandHandler<TCommand, TResponse>>();
        RequestHandlerDelegate<Result<TResponse>> pipeline = () => handler.Handle(command, cancellationToken);

        foreach (var behavior in provider.GetServices<IPipelineBehavior<TCommand, Result<TResponse>>>().Reverse())
        {
            var next = pipeline;
            pipeline = () => behavior.Handle(command, next, cancellationToken);
        }

        return pipeline();
    }
}
