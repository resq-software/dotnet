using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace ResQ.BuildingBlocks.Testing;

/// <summary>
/// An <see cref="ILoggerProvider"/> that captures every formatted log message in memory, letting a
/// test assert on what was logged.
/// </summary>
public sealed class CapturingLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentQueue<string> _entries = new();

    /// <summary>Gets a snapshot of the formatted log entries captured so far, in log order.</summary>
    public IReadOnlyList<string> Entries => _entries.ToArray();

    /// <summary>Creates a logger that appends its formatted messages to <see cref="Entries"/>.</summary>
    /// <param name="categoryName">The logger category.</param>
    /// <returns>A capturing <see cref="ILogger"/>.</returns>
    public ILogger CreateLogger(string categoryName) => new CapturingLogger(categoryName, _entries);

    /// <summary>Releases the provider. Nothing to dispose; captured entries remain readable.</summary>
    public void Dispose() => GC.SuppressFinalize(this);

    private sealed class CapturingLogger(string categoryName, ConcurrentQueue<string> sink) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            ArgumentNullException.ThrowIfNull(formatter);
            sink.Enqueue($"{logLevel}: {categoryName}: {formatter(state, exception)}");
        }
    }
}

/// <summary>
/// An <see cref="ILoggerProvider"/> that forwards log messages to an xUnit
/// <see cref="ITestOutputHelper"/>, so logs surface in test output.
/// </summary>
/// <param name="output">The xUnit test output sink to write to.</param>
public sealed class XUnitLoggerProvider(ITestOutputHelper output) : ILoggerProvider
{
    /// <summary>Creates a logger that writes its formatted messages to the test output.</summary>
    /// <param name="categoryName">The logger category.</param>
    /// <returns>An <see cref="ILogger"/> backed by the xUnit output helper.</returns>
    public ILogger CreateLogger(string categoryName) => new XUnitLogger(output, categoryName);

    /// <summary>Releases the provider. The output helper is owned by xUnit, so nothing is disposed here.</summary>
    public void Dispose() => GC.SuppressFinalize(this);

    private sealed class XUnitLogger(ITestOutputHelper output, string categoryName) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            ArgumentNullException.ThrowIfNull(formatter);
            try
            {
                output.WriteLine($"{logLevel}: {categoryName}: {formatter(state, exception)}");
            }
            catch (InvalidOperationException)
            {
                // The test has already finished; there is no active output sink to write to.
            }
        }
    }
}
