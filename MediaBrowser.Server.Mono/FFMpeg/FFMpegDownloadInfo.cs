
namespace MediaBrowser.ServerApplication.FFMpeg
{
    public static class FFMpegDownloadInfo
    {
        public static string Version = "ffmpeg20130904";

        public static string[] FfMpegUrls = new[]
                {
					"http://ffmpeg.gusari.org/static/32bit/ffmpeg.static.32bit.2013-09-04.tar.gz",

					"https://www.dropbox.com/s/y7f4nk96rxmbb30/ffmpeg.static.32bit.2013-09-04.tar.gz?dl=1"
                };

        public static string FFMpegFilename = "ffmpeg";
        public static string FFProbeFilename = "ffprobe";

        public static string ArchiveType = "gz";
    }
}
