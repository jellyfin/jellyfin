using System;
using System.Globalization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.ServerSetupApp;

/// <summary>
/// Startup logger for usage with DI that utilises an underlying logger from the DI.
/// </summary>
/// <typeparam name="TCategory">The category of the underlying logger.</typeparam>
#pragma warning disable SA1649 // File name should match first type name
public class StartupLogger<TCategory> : StartupLogger, IStartupLogger<TCategory>
#pragma warning restore SA1649 // File name should match first type name
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StartupLogger{TCategory}"/> class.
    /// </summary>
    /// <param name="logger">The injected base logger.</param>
    public StartupLogger(ILogger<TCategory> logger) : base(logger)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StartupLogger{TCategory}"/> class.
    /// </summary>
    /// <param name="logger">The underlying base logger.</param>
    /// <param name="groupEntry">The group for this logger.</param>
    internal StartupLogger(ILogger logger, StartupLogTopic? groupEntry) : base(logger, groupEntry)
    {
    }

    IStartupLogger<TCategory> IStartupLogger<TCategory>.BeginGroup(FormattableString logEntry)
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

        return new StartupLogger<TCategory>(BaseLogger, startupEntry);
    }

    IStartupLogger<TCategory> IStartupLogger<TCategory>.With(ILogger logger)
    {
        return new StartupLogger<TCategory>(logger, Topic);
    }
}
