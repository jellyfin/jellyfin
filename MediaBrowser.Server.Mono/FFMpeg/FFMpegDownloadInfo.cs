using System;

namespace MediaBrowser.ServerApplication.FFMpeg
{
    public static class FFMpegDownloadInfo
    {
        public static string Version = ffmpegOsType("Version");

        public static string[] FfMpegUrls = ffmpegOsType("FfMpegUrls").Split(',');

        public static string FFMpegFilename = ffmpegOsType("FFMpegFilename");
        public static string FFProbeFilename = ffmpegOsType("FFProbeFilename");

        public static string ArchiveType = ffmpegOsType("ArchiveType");

        private static string ffmpegOsType(string arg)
        {
            OperatingSystem os = Environment.OSVersion;
            PlatformID     pid = os.Platform;
            switch (pid)
            {
                case PlatformID.Win32NT:
                    switch (arg)
                    {
                        case "Version":
                            return "ffmpeg20131221";
                        case "FfMpegUrls":
                            return "http://ffmpeg.zeranoe.com/builds/win32/static/ffmpeg-20131221-git-70d6ce7-win32-static.7z,https://www.dropbox.com/s/d38uj7857trbw1g/ffmpeg-20131209-git-a12f679-win32-static.7z?dl=1";
                        case "FFMpegFilename":
                            return "ffmpeg.exe";
                        case "FFProbeFilename":
                            return "ffprobe.exe";
                        case "ArchiveType":
                            return "7z";
                    }
                    break;
                case PlatformID.Unix:
                case PlatformID.MacOSX:
                    switch (arg)
                    {
                        case "Version":
                            return "ffmpeg20131221";
                        case "FfMpegUrls":
                            return "http://ffmpeg.gusari.org/static/32bit/ffmpeg.static.32bit.2013-12-21.tar.gz,https://www.dropbox.com/s/b9v17h105cps7p0/ffmpeg.static.32bit.2013-10-11.tar.gz?dl=1";
                        case "FFMpegFilename":
                            return "ffmpeg";
                        case "FFProbeFilename":
                            return "ffprobe";
                        case "ArchiveType":
                            return "gz";
                    }
                    break;
            }
            return "";
        }
    }
}
