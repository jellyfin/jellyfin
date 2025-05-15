using Microsoft.Extensions.Logging;

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
    ILogger With(ILogger logger);
}
