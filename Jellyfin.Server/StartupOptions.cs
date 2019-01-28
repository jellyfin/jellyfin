using CommandLine;
using Emby.Server.Implementations;

namespace Jellyfin.Server
{
    /// <summary>
    /// Class used by CommandLine package when parsing the command line arguments.
    /// </summary>
    public class StartupOptions : IStartupOptions
    {
        [Option('d', "datadir", Required = false, HelpText = "Path to use for the data folder (databases files etc.).")]
        public string DataDir { get; set; }

        [Option('c', "configdir", Required = false, HelpText = "Path to use for config data (user policies and puctures).")]
        public string ConfigDir { get; set; }

        [Option('l', "logdir", Required = false, HelpText = "Path to use for writing log files.")]
        public string LogDir { get; set; }

        [Option("ffmpeg", Required = false, HelpText = "Path to external FFmpeg exe to use in place of built-in.")]
        public string FFmpegPath { get; set; }

        [Option("ffprobe", Required = false, HelpText = "ffmpeg and ffprobe switches must be supplied together.")]
        public string FFprobePath { get; set; }

        [Option("service", Required = false, HelpText = "Run as headless service.")]
        public bool IsService { get; set; }

        [Option("noautorunwebapp", Required = false, HelpText = "Run headless if startup wizard is complete.")]
        public bool NoAutoRunWebApp { get; set; }

        [Option("package-name", Required = false, HelpText = "Used when packaging Jellyfin (example, synology).")]
        public string PackageName { get; set; }

        [Option("restartpath", Required = false, HelpText = "Path to reset script.")]
        public string RestartPath { get; set; }

        [Option("restartargs", Required = false, HelpText = "Arguments for restart script.")]
        public string RestartArgs { get; set; }
    }
}
