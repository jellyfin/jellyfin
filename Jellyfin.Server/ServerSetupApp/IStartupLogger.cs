using System;
using Morestachio.Helper.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Jellyfin.Server.ServerSetupApp;

/// <summary>
/// Defines the Startup Logger. This logger acts an an aggregate logger that will push though all log messages to both the attached logger as well as the startup UI.
/// </summary>
public interface IStartupLogger : ILogger
{
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
}
