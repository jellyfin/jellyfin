
namespace MediaBrowser.ServerApplication.FFMpeg
{
    public static class FFMpegDownloadInfo
    {
        public static string Version = "ffmpeg20130904";

        public static string[] FfMpegUrls = new[]
                {
					"http://ffmpeg.gusari.org/static/32bit/ffmpeg.static.32bit.2013-10-11.tar.gz",

					"https://www.dropbox.com/s/b9v17h105cps7p0/ffmpeg.static.32bit.2013-10-11.tar.gz?dl=1"
                };

        public static string FFMpegFilename = "ffmpeg";
        public static string FFProbeFilename = "ffprobe";

        public static string ArchiveType = "gz";
    }
}
