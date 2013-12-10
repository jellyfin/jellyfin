
namespace MediaBrowser.ServerApplication.FFMpeg
{
    public static class FFMpegDownloadInfo
    {
        public static string Version = "ffmpeg20131209";

        public static string[] FfMpegUrls = new[]
                {
                    "http://ffmpeg.zeranoe.com/builds/win32/static/ffmpeg-20131209-git-a12f679-win32-static.7z",
                    "https://www.dropbox.com/s/d38uj7857trbw1g/ffmpeg-20131209-git-a12f679-win32-static.7z?dl=1"
                };

        public static string FFMpegFilename = "ffmpeg.exe";
        public static string FFProbeFilename = "ffprobe.exe";

        public static string ArchiveType = "7z";
    }
}
