using System;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Jellyfin.Server.ServerSetupApp;

/// <summary>
/// Defines the Startup Logger. This logger acts an an aggregate logger that will push though all log messages to both the attached logger as well as the startup UI.
/// </summary>
public interface IStartupLogger : ILogger
{
    /// <summary>
    /// Gets the topic this logger is assigned to.
    /// </summary>
    StartupLogTopic? Topic { get; }

    /// <summary>
    /// Adds another logger instance to this logger for combined logging.
    /// </summary>
    /// <param name="logger">Other logger to rely messages to.</param>
    /// <returns>A combined logger.</returns>
    IStartupLogger With(ILogger logger);

    /// <summary>
    /// Opens a new Group logger within the parent logger.
    /// </summary>
    /// <param name="logEntry">Defines the log message that introduces the new group.</param>
    /// <returns>A new logger that can write to the group.</returns>
    IStartupLogger BeginGroup(FormattableString logEntry);

    /// <summary>
    /// Adds another logger instance to this logger for combined logging.
    /// </summary>
    /// <param name="logger">Other logger to rely messages to.</param>
    /// <returns>A combined logger.</returns>
    /// <typeparam name="TCategory">The logger cateogry.</typeparam>
    IStartupLogger<TCategory> With<TCategory>(ILogger logger);

    /// <summary>
    /// Opens a new Group logger within the parent logger.
    /// </summary>
    /// <param name="logEntry">Defines the log message that introduces the new group.</param>
    /// <returns>A new logger that can write to the group.</returns>
    /// <typeparam name="TCategory">The logger cateogry.</typeparam>
    IStartupLogger<TCategory> BeginGroup<TCategory>(FormattableString logEntry);
}

/// <summary>
/// Defines a logger that can be injected via DI to get a startup logger initialised with an logger framework connected <see cref="ILogger"/>.
/// </summary>
/// <typeparam name="TCategory">The logger cateogry.</typeparam>
public interface IStartupLogger<TCategory> : IStartupLogger
{
    /// <summary>
    /// Adds another logger instance to this logger for combined logging.
    /// </summary>
    /// <param name="logger">Other logger to rely messages to.</param>
    /// <returns>A combined logger.</returns>
    new IStartupLogger<TCategory> With(ILogger logger);

    /// <summary>
    /// Opens a new Group logger within the parent logger.
    /// </summary>
    /// <param name="logEntry">Defines the log message that introduces the new group.</param>
    /// <returns>A new logger that can write to the group.</returns>
    new IStartupLogger<TCategory> BeginGroup(FormattableString logEntry);
}
