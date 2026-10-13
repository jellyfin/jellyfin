using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Tests.Migrations;

internal sealed class CollectingLogger : ILogger
{
    public List<LogEntry> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
        => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var properties = state is IEnumerable<KeyValuePair<string, object?>> pairs
            ? pairs.ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal)
            : [];
        Entries.Add(new LogEntry(logLevel, formatter(state, exception), properties));
    }

    /// <summary>
    /// A log entry, with the properties that the message template captured and the template itself as the
    /// <c>{OriginalFormat}</c> property.
    /// </summary>
    /// <param name="Level">The level of the entry.</param>
    /// <param name="Message">The formatted message.</param>
    /// <param name="Properties">The properties of the entry.</param>
    internal sealed record LogEntry(LogLevel Level, string Message, IReadOnlyDictionary<string, object?> Properties)
    {
        public string? OriginalFormat => Properties.TryGetValue("{OriginalFormat}", out var format) ? format as string : null;
    }
}
