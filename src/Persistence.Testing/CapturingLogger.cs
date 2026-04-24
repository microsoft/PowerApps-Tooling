// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Testing;

/// <summary>
/// A logger implementation which captures log entries so they can be inspected in tests.
/// </summary>
public sealed class CapturingLogger<T> : ILogger<T>
{
    public record LogEntry(EventId EventId, LogLevel Level, string Message);

    private readonly List<LogEntry> _entries = [];

    public IReadOnlyList<LogEntry> Entries => _entries;

    public IEnumerable<LogEntry> EntriesByName(string name) => _entries.Where(e => e.EventId.Name == name);

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        _entries.Add(new(eventId, logLevel, formatter(state, exception)));
    }
}
