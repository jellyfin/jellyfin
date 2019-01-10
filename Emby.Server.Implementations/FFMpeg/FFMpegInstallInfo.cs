namespace Emby.Server.Implementations.FFMpeg
{
    public class FFMpegInstallInfo
    {
        public string Version { get; }
        public string FFMpegFilename { get; }
        public string FFProbeFilename { get; }
        public string ArchiveType { get; }

        private FFMpegInstallInfo(
            string version,
            string ffMpegFilename,
            string ffProbeFilename,
            string archiveType)
        {
            Version = version;
            FFMpegFilename = ffMpegFilename;
            FFProbeFilename = ffProbeFilename;
            ArchiveType = archiveType;
        }

        public static FFMpegInstallInfo Linux = new FFMpegInstallInfo(
            "20170308",
            "ffmpeg",
            "ffprobe",
            "7z");

        public static FFMpegInstallInfo Windows = new FFMpegInstallInfo(
            "20170308",
            "ffmpeg.exe",
            "ffprobe.exe",
            "7z");

        public static FFMpegInstallInfo OSX = new FFMpegInstallInfo(
            version:"20170308",
            ffMpegFilename:"ffmpeg",
            ffProbeFilename:"ffprobe",
            archiveType:"7z");

        public static FFMpegInstallInfo Default = new FFMpegInstallInfo("Path", "ffmpeg", "ffprobe", null);
    }
}
