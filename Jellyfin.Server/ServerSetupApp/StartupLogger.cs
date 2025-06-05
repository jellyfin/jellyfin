using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Jellyfin.Server.Migrations.Routines;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.ServerSetupApp;

/// <inheritdoc/>
public class StartupLogger : IStartupLogger
{
    private readonly SetupServer.StartupLogEntry? _groupEntry;

    /// <summary>
    /// Initializes a new instance of the <see cref="StartupLogger"/> class.
    /// </summary>
    public StartupLogger()
    {
        Loggers = [];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StartupLogger"/> class.
    /// </summary>
    private StartupLogger(SetupServer.StartupLogEntry? groupEntry) : this()
    {
        _groupEntry = groupEntry;
    }

    internal static IStartupLogger Logger { get; } = new StartupLogger();

    private List<ILogger> Loggers { get; set; }

    /// <inheritdoc/>
    public IStartupLogger BeginGroup(FormattableString logEntry)
    {
        var startupEntry = new SetupServer.StartupLogEntry()
        {
            Content = logEntry.ToString(CultureInfo.InvariantCulture),
            DateOfCreation = DateTimeOffset.Now
        };

        if (_groupEntry is null)
        {
            SetupServer.LogQueue?.Enqueue(startupEntry);
        }
        else
        {
            _groupEntry.Children.Add(startupEntry);
        }

        return new StartupLogger(startupEntry);
    }

    /// <inheritdoc/>
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        return null;
    }

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    /// <inheritdoc/>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        foreach (var item in Loggers.Where(e => e.IsEnabled(logLevel)))
        {
            item.Log(logLevel, eventId, state, exception, formatter);
        }

        var startupEntry = new SetupServer.StartupLogEntry()
        {
            LogLevel = logLevel,
            Content = formatter(state, exception),
            DateOfCreation = DateTimeOffset.Now
        };

        if (_groupEntry is null)
        {
            SetupServer.LogQueue?.Enqueue(startupEntry);
        }
        else
        {
            _groupEntry.Children.Add(startupEntry);
        }
    }

    /// <inheritdoc/>
    public IStartupLogger With(ILogger logger)
    {
        return new StartupLogger(_groupEntry)
        {
            Loggers = [.. Loggers, logger]
        };
    }
}
