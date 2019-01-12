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
            version: "20170308",
            ffMpegFilename: "ffmpeg",
            ffProbeFilename: "ffprobe",
            archiveType: "7z");

        public static FFMpegInstallInfo Windows = new FFMpegInstallInfo(
            version: "20170308",
            ffMpegFilename:"ffmpeg.exe",
            ffProbeFilename:"ffprobe.exe",
            archiveType: "7z");

        public static FFMpegInstallInfo OSX = new FFMpegInstallInfo(
            version: "20170308",
            ffMpegFilename: "ffmpeg",
            ffProbeFilename: "ffprobe",
            archiveType: "7z");

        public static FFMpegInstallInfo Default = new FFMpegInstallInfo(
            version: "Path",
            ffMpegFilename: "ffmpeg",
            ffProbeFilename: "ffprobe",
            archiveType: null);
    }
}
