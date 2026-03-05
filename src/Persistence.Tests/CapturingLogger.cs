// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;

namespace Persistence.Tests;

/// <summary>
/// A logger implementation which captures log entries so they can be inspected in tests.
/// </summary>
internal sealed class CapturingLogger<T> : ILogger<T>
{
    public record LogEntry(LogLevel Level, string Message);

    private readonly List<LogEntry> _entries = [];

    public IReadOnlyList<LogEntry> Entries => _entries;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        _entries.Add(new(logLevel, formatter(state, exception)));
    }
}
