using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Serilog.Extensions.Logging;

namespace Jellyfin.Server.Helpers;

/// <summary>
/// Forwards the log events Serilog writes to the <see cref="ILoggerProvider"/>s of the running host.
/// </summary>
/// <remarks>
/// The Serilog sink writing to providers is part of the process-wide logger and so outlives a host,
/// while a <see cref="LoggerProviderCollection"/> can only grow. Each host gets its own collection,
/// attached here while it runs, so the providers of a stopped host are released with it.
/// </remarks>
internal sealed class HostLoggerProviders : ILoggerProvider
{
    private LoggerProviderCollection? _current;

    /// <summary>
    /// Creates the collection the next host adds its providers to, and forwards to it from now on.
    /// </summary>
    /// <returns>The collection to pass to the host's Serilog logger factory.</returns>
    public LoggerProviderCollection Attach()
    {
        var providers = new LoggerProviderCollection();
        Volatile.Write(ref _current, providers);
        return providers;
    }

    /// <summary>
    /// Stops forwarding to the providers of a host.
    /// </summary>
    /// <param name="providers">The collection returned by <see cref="Attach"/>.</param>
    public void Detach(LoggerProviderCollection providers)
        => Interlocked.CompareExchange(ref _current, null, providers);

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName)
    {
        var current = Volatile.Read(ref _current);
        if (current is null)
        {
            return NullLogger.Instance;
        }

        List<ILogger>? loggers = null;
        foreach (var provider in current.Providers)
        {
            (loggers ??= []).Add(provider.CreateLogger(categoryName));
        }

        return loggers switch
        {
            null => NullLogger.Instance,
            [var single] => single,
            _ => new ForwardingLogger(loggers)
        };
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // The providers belong to their host, which disposes them.
    }

    private sealed class ForwardingLogger : ILogger
    {
        private readonly List<ILogger> _loggers;

        public ForwardingLogger(List<ILogger> loggers) => _loggers = loggers;

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
            => null;

        public bool IsEnabled(LogLevel logLevel) => _loggers.Exists(l => l.IsEnabled(logLevel));

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            foreach (var logger in _loggers)
            {
                logger.Log(logLevel, eventId, state, exception, formatter);
            }
        }
    }
}
