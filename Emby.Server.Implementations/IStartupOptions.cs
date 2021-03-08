#pragma warning disable CS1591
#nullable enable
using System;

namespace Emby.Server.Implementations
{
    public interface IStartupOptions
    {
        /// <summary>
        /// Gets the value of the --ffmpeg command line option.
        /// </summary>
        string? FFmpegPath { get; }

        /// <summary>
        /// Gets the value of the --service command line option.
        /// </summary>
        bool IsService { get; }

        /// <summary>
        /// Gets the value of the --package-name command line option.
        /// </summary>
        string? PackageName { get; }

        /// <summary>
        /// Gets the value of the --restartpath command line option.
        /// </summary>
        string? RestartPath { get; }

        /// <summary>
        /// Gets the value of the --restartargs command line option.
        /// </summary>
        string? RestartArgs { get; }

        /// <summary>
        /// Gets the value of the --published-server-url command line option.
        /// </summary>
        string? PublishedServerUrl { get; }
    }
}
