
namespace MediaBrowser.Server.Startup.Common.FFMpeg
{
    public class FFMpegInstallInfo
    {
        public string Version { get; set; }
        public string FFMpegFilename { get; set; }
        public string FFProbeFilename { get; set; }
        public string ArchiveType { get; set; }
        public string[] DownloadUrls { get; set; }

        public FFMpegInstallInfo()
        {
            DownloadUrls = new string[] { };
            Version = "Path";
            FFMpegFilename = "ffmpeg";
            FFProbeFilename = "ffprobe";
        }
    }
}