using System;

namespace MediaBrowser.ServerApplication.FFMpeg
{
    public static class FFMpegDownloadInfo
    {
        // Windows builds: http://ffmpeg.zeranoe.com/builds/
        // Linux builds: http://ffmpeg.gusari.org/static/

        public static string Version = ffmpegOsType("Version");

        public static string[] FfMpegUrls = GetDownloadUrls();

        public static string FFMpegFilename = ffmpegOsType("FFMpegFilename");
        public static string FFProbeFilename = ffmpegOsType("FFProbeFilename");

        public static string ArchiveType = ffmpegOsType("ArchiveType");

        private static string ffmpegOsType(string arg)
        {
            OperatingSystem os = Environment.OSVersion;
            PlatformID pid = os.Platform;
            switch (pid)
            {
                case PlatformID.Win32NT:
                    switch (arg)
                    {
                        case "Version":
                            return "20140105";
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
                            return "20140104";
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

        private static string[] GetDownloadUrls()
        {
            var pid = Environment.OSVersion.Platform;
            
            switch (pid)
            {
                case PlatformID.Win32NT:
                    return new[]
                    {
                        "http://ffmpeg.zeranoe.com/builds/win32/static/ffmpeg-20140105-git-70937d9-win32-static.7z",
                        "https://www.dropbox.com/s/oghurnp5zh292ry/ffmpeg-20140105-git-70937d9-win32-static.7z?dl=1"
                    };
           
                case PlatformID.Unix:
                case PlatformID.MacOSX:
                    return new[]
                    {
                        "http://ffmpeg.gusari.org/static/32bit/ffmpeg.static.32bit.2014-01-04.tar.gz",
                        "https://www.dropbox.com/s/b7nkg71sil812hp/ffmpeg.static.32bit.2014-01-04.tar.gz?dl=1"
                    };
            }

            return new string[] {};
        }
    }
}
