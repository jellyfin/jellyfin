
namespace MediaBrowser.ServerApplication.FFMpeg
{
    public static class FFMpegDownloadInfo
    {
        public static string Version = "ffmpeg20131110.1";

        public static string[] FfMpegUrls = new[]
                {
                    "https://github.com/MediaBrowser/MediaBrowser.Resources/raw/master/ffmpeg/windows/ffmpeg-20131110-git-8cdf4e0-win32-static.7z",

                    "http://ffmpeg.zeranoe.com/builds/win32/static/ffmpeg-20131110-git-8cdf4e0-win32-static.7z",
                    "https://www.dropbox.com/s/5clspc636v9hie6/ffmpeg-20131110-git-8cdf4e0-win32-static.7z?dl=1"
                };

        public static string FFMpegFilename = "ffmpeg.exe";
        public static string FFProbeFilename = "ffprobe.exe";

        public static string ArchiveType = "7z";
    }
}
