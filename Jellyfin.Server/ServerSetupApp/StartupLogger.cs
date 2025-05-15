using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Server.Migrations.Routines;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.ServerSetupApp;

/// <inheritdoc/>
public class StartupLogger : IStartupLogger
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StartupLogger"/> class.
    /// </summary>
    public StartupLogger()
    {
        Loggers = [new SetupServer.SetupServerLogger()];
    }

    private List<ILogger> Loggers { get; set; }

    /// <inheritdoc/>
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        return null;
    }

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel)
    {
        return Loggers.Any(f => f.IsEnabled(logLevel));
    }

    /// <inheritdoc/>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        foreach (var item in Loggers.Where(e => e.IsEnabled(logLevel)))
        {
            item.Log(logLevel, eventId, state, exception, formatter);
        }
    }

    /// <inheritdoc/>
    public ILogger With(ILogger logger)
    {
        return new StartupLogger()
        {
            Loggers = [.. Loggers, logger]
        };
    }
}
