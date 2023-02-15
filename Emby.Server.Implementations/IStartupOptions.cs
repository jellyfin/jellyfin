namespace Emby.Server.Implementations
{
    /// <summary>
    /// Specifies the contract for server startup options.
    /// </summary>
    public interface IStartupOptions
    {
        /// <summary>
        /// Gets the value of the --ffmpeg command line option.
        /// </summary>
        string? FFmpegPath { get; }

        /// <summary>
        /// Gets a value indicating whether to run as service by the --service command line option.
        /// </summary>
        bool IsService { get; }

        /// <summary>
        /// Gets the value of the --package-name command line option.
        /// </summary>
        string? PackageName { get; }

        /// <summary>
        /// Gets the value of the --published-server-url command line option.
        /// </summary>
        string? PublishedServerUrl { get; }
    }
}
