namespace Emby.Server.Implementations
{
    public interface IStartupOptions
    {
        /// <summary>
        /// --ffmpeg
        /// </summary>
        string FFmpegPath { get; }

        /// <summary>
        /// --service
        /// </summary>
        bool IsService { get; }

        /// <summary>
        /// --noautorunwebapp
        /// </summary>
        bool NoAutoRunWebApp { get; }

        /// <summary>
        /// --package-name
        /// </summary>
        string PackageName { get; }

        /// <summary>
        /// --restartpath
        /// </summary>
        string RestartPath { get; }

        /// <summary>
        /// --restartargs
        /// </summary>
        string RestartArgs { get; }
    }
}
