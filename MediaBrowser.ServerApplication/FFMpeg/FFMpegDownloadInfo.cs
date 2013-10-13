
namespace MediaBrowser.ServerApplication.FFMpeg
{
    public static class FFMpegDownloadInfo
    {
        public static string Version = "ffmpeg20130904.1";

        public static string[] FfMpegUrls = new[]
                {
                    "https://github.com/MediaBrowser/MediaBrowser.Resources/raw/master/ffmpeg/windows/ffmpeg-20130904-git-f974289-win32-static.7z",

                    "http://ffmpeg.zeranoe.com/builds/win32/static/ffmpeg-20130904-git-f974289-win32-static.7z",
                    "https://www.dropbox.com/s/a81cb2ob23fwcfs/ffmpeg-20130904-git-f974289-win32-static.7z?dl=1"
                };

        public static string FFMpegFilename = "ffmpeg.exe";
        public static string FFProbeFilename = "ffprobe.exe";

        public static string ArchiveType = "7z";
    }
}
