using System;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jellyfin.Server.ServerSetupApp;

/// <inheritdoc/>
public class StartupLogger : IStartupLogger
{
    private readonly StartupLogTopic? _topic;

    /// <summary>
    /// Initializes a new instance of the <see cref="StartupLogger"/> class.
    /// </summary>
    /// <param name="logger">The underlying base logger.</param>
    public StartupLogger(ILogger logger)
    {
        BaseLogger = logger;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StartupLogger"/> class.
    /// </summary>
    /// <param name="logger">The underlying base logger.</param>
    /// <param name="topic">The group for this logger.</param>
    internal StartupLogger(ILogger logger, StartupLogTopic? topic) : this(logger)
    {
        _topic = topic;
    }

    internal static IStartupLogger Logger { get; set; } = new StartupLogger(NullLogger.Instance);

    /// <inheritdoc/>
    public StartupLogTopic? Topic => _topic;

    /// <summary>
    /// Gets or Sets the underlying base logger.
    /// </summary>
    protected ILogger BaseLogger { get; set; }

    /// <inheritdoc/>
    public IStartupLogger BeginGroup(FormattableString logEntry)
    {
        return new StartupLogger(BaseLogger, AddToTopic(logEntry));
    }

    /// <inheritdoc/>
    public IStartupLogger With(ILogger logger)
    {
        return new StartupLogger(logger, Topic);
    }

    /// <inheritdoc/>
    public IStartupLogger<TCategory> With<TCategory>(ILogger logger)
    {
        return new StartupLogger<TCategory>(logger, Topic);
    }

    /// <inheritdoc/>
    public IStartupLogger<TCategory> BeginGroup<TCategory>(FormattableString logEntry)
    {
        return new StartupLogger<TCategory>(BaseLogger, AddToTopic(logEntry));
    }

    private StartupLogTopic AddToTopic(FormattableString logEntry)
    {
        var startupEntry = new StartupLogTopic()
        {
            Content = logEntry.ToString(CultureInfo.InvariantCulture),
            DateOfCreation = DateTimeOffset.Now
        };

        if (Topic is null)
        {
            SetupServer.LogQueue?.Enqueue(startupEntry);
        }
        else
        {
            Topic.Children.Add(startupEntry);
        }

        return startupEntry;
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
        if (BaseLogger.IsEnabled(logLevel))
        {
            // if enabled allow the base logger also to receive the message
            BaseLogger.Log(logLevel, eventId, state, exception, formatter);
        }

        var startupEntry = new StartupLogTopic()
        {
            LogLevel = logLevel,
            Content = formatter(state, exception),
            DateOfCreation = DateTimeOffset.Now
        };

        if (Topic is null)
        {
            SetupServer.LogQueue?.Enqueue(startupEntry);
        }
        else
        {
            Topic.Children.Add(startupEntry);
        }
    }
}
