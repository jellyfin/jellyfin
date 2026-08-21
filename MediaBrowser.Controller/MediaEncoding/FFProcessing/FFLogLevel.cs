using System;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Controller.MediaEncoding.FFProcessing;

/// <summary>
/// Derives FFmpeg's <c>-loglevel</c> argument from the server's own log level.
/// </summary>
public static class FFLogLevel
{
    /// <summary>
    /// Maps a logger's level, but never below <c>info</c>. FFmpeg reports filter results — the
    /// ebur128 loudness summary, for one — at info, so an action that reads its own standard error
    /// as the answer gets nothing at all under a quieter level.
    /// </summary>
    /// <param name="logger">The logger to inspect.</param>
    /// <returns>The value for <c>-loglevel</c>.</returns>
    public static string ForPayload(ILogger logger)
    {
        var level = For(logger);

        return level is "debug" or "verbose" ? level : "info";
    }

    /// <summary>
    /// Maps a logger's effective level onto an FFmpeg log level.
    /// </summary>
    /// <param name="logger">The logger to inspect.</param>
    /// <returns>The value for <c>-loglevel</c>.</returns>
    public static string For(ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        if (logger.IsEnabled(LogLevel.Trace))
        {
            return "debug";
        }

        if (logger.IsEnabled(LogLevel.Debug))
        {
            return "verbose";
        }

        if (logger.IsEnabled(LogLevel.Warning))
        {
            return "warning";
        }

        return logger.IsEnabled(LogLevel.Error) ? "error" : "fatal";
    }
}
