
namespace Emby.Server.Implementations.FFMpeg
{
    public class FFMpegInstallInfo
    {
        public string Version { get; set; }
        public string FFMpegFilename { get; set; }
        public string FFProbeFilename { get; set; }
        public string ArchiveType { get; set; }

        public FFMpegInstallInfo()
        {
            Version = "Path";
            FFMpegFilename = "ffmpeg";
            FFProbeFilename = "ffprobe";
        }
    }
}