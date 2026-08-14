using System.Collections.Concurrent;

namespace ResQ.BuildingBlocks.Application;

/// <summary>
/// The single source of truth for open-generic CQRS-marker detection. Classifies a request CLR type
/// as a command or a query via its implemented interfaces, with the answer cached per type.
/// </summary>
/// <remarks>
/// Because <see cref="ICommand{TResponse}"/> deliberately does not inherit <see cref="ICommand"/>,
/// a naive <c>request is ICommand</c> check silently misses every value-returning command. Any code
/// that must branch on command-vs-query — the persistence transaction behavior and consumer-authored
/// <see cref="IPipelineBehavior{TRequest,TResponse}"/> implementations alike — should classify through
/// this shared helper rather than re-deriving (and mis-handling) that rule.
/// </remarks>
public static class CqrsRequest
{
    private static readonly ConcurrentDictionary<Type, bool> CommandCache = new();
    private static readonly ConcurrentDictionary<Type, bool> QueryCache = new();

    /// <summary>Returns <see langword="true"/> when <paramref name="requestType"/> is a command (with or without a response).</summary>
    /// <param name="requestType">The request CLR type to classify.</param>
    /// <returns><see langword="true"/> if the type implements <see cref="ICommand"/> or <see cref="ICommand{TResponse}"/>.</returns>
    public static bool IsCommand(Type requestType) =>
        CommandCache.GetOrAdd(requestType, static type =>
            typeof(ICommand).IsAssignableFrom(type) ||
            type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>)));

    /// <summary>Returns <see langword="true"/> when <paramref name="requestType"/> is a query.</summary>
    /// <param name="requestType">The request CLR type to classify.</param>
    /// <returns><see langword="true"/> if the type implements <see cref="IQuery{TResponse}"/>.</returns>
    public static bool IsQuery(Type requestType) =>
        QueryCache.GetOrAdd(requestType, static type =>
            type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQuery<>)));
}
