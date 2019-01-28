namespace Emby.Server.Implementations.ParsedStartupOptions
{
    public interface IStartupOptions
    {
        /// <summary>
        /// --datadir
        /// </summary>
        string DataDir { get; }

        /// <summary>
        /// --configdir
        /// </summary>
        string ConfigDir { get; }

        /// <summary>
        /// --logdir
        /// </summary>
        string LogDir { get; }

        /// <summary>
        /// --ffmpeg
        /// </summary>
        string FFmpegPath { get; }

        /// <summary>
        /// --ffprobe
        /// </summary>
        string FFprobePath { get; }

        /// <summary>
        /// --service
        /// </summary>
        bool IsService { get; }

        /// <summary>
        /// --noautorunwebapp
        /// </summary>
        bool AutoRunWebApp { get; }

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
