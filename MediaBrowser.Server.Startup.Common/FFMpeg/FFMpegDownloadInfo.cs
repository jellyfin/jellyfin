
namespace MediaBrowser.Server.Startup.Common.FFMpeg
{
    public class FFMpegDownloadInfo
    {
        public string Version { get; set; }
        public string FFMpegFilename { get; set; }
        public string FFProbeFilename { get; set; }
        public string ArchiveType { get; set; }
        public string[] DownloadUrls { get; set; }

        public FFMpegDownloadInfo()
        {
            DownloadUrls = new string[] { };
            Version = "Path";
            FFMpegFilename = "ffmpeg";
            FFProbeFilename = "ffprobe";
        }

        public static FFMpegDownloadInfo GetInfo(NativeEnvironment environment)
        {
            var info = new FFMpegDownloadInfo();

            // Windows builds: http://ffmpeg.zeranoe.com/builds/
            // Linux builds: http://ffmpeg.gusari.org/static/
            // OS X builds: http://ffmpegmac.net/
            // OS X x64: http://www.evermeet.cx/ffmpeg/

            switch (environment.OperatingSystem)
            {
                case OperatingSystem.Bsd:
                    break;
                case OperatingSystem.Linux:

                    info.ArchiveType = "gz";

                    switch (environment.SystemArchitecture)
                    {
                        case Architecture.X86_X64:
                            info.Version = "20140716";
                            break;
                        case Architecture.X86:
                            info.Version = "20140923";
                            break;
                    }
                    break;
                case OperatingSystem.Osx:

                    info.ArchiveType = "7z";

                    switch (environment.SystemArchitecture)
                    {
                        case Architecture.X86_X64:
                            info.Version = "20140923";
                            break;
                        case Architecture.X86:
                            info.Version = "20140716";
                            break;
                    }
                    break;

                case OperatingSystem.Windows:

                    info.FFMpegFilename = "ffmpeg.exe";
                    info.FFProbeFilename = "ffprobe.exe";
                    info.Version = "20141005";
                    info.ArchiveType = "7z";

                    switch (environment.SystemArchitecture)
                    {
                        case Architecture.X86_X64:
                            break;
                        case Architecture.X86:
                            break;
                    }
                    break;
            }

            info.DownloadUrls = GetDownloadUrls(environment);

            return info;
        }

        private static string[] GetDownloadUrls(NativeEnvironment environment)
        {
            switch (environment.OperatingSystem)
            {
                case OperatingSystem.Windows:

                    switch (environment.SystemArchitecture)
                    {
                        case Architecture.X86_X64:
                            return new[]
                            {
                                "http://ffmpeg.zeranoe.com/builds/win64/static/ffmpeg-20141005-git-e079d43-win64-static.7z",
                                "https://github.com/MediaBrowser/MediaBrowser.Resources/raw/master/ffmpeg/windows/ffmpeg-20141005-git-e079d43-win64-static.7z"
                            };
                        case Architecture.X86:
                            return new[]
                            {
                                "http://ffmpeg.zeranoe.com/builds/win32/static/ffmpeg-20141005-git-e079d43-win32-static.7z",
                                "https://github.com/MediaBrowser/MediaBrowser.Resources/raw/master/ffmpeg/windows/ffmpeg-20141005-git-e079d43-win32-static.7z"
                            };
                    }
                    break;

                case OperatingSystem.Osx:

                    switch (environment.SystemArchitecture)
                    {
                        case Architecture.X86_X64:
                            return new[]
                            {
                                "https://github.com/MediaBrowser/MediaBrowser.Resources/raw/master/ffmpeg/osx/ffmpeg-x64-2.4.1.7z"
                            };
                        case Architecture.X86:
                            return new[]
                            {
                                "https://github.com/MediaBrowser/MediaBrowser.Resources/raw/master/ffmpeg/osx/ffmpeg-x86-2.4.2.7z"
                            };
                    }
                    break;

                case OperatingSystem.Linux:

                    switch (environment.SystemArchitecture)
                    {
                        case Architecture.X86_X64:
                            return new[]
                            {
                                "http://ffmpeg.gusari.org/static/64bit/ffmpeg.static.64bit.latest.tar.gz",
                                "https://github.com/MediaBrowser/MediaBrowser.Resources/raw/master/ffmpeg/linux/ffmpeg.static.64bit.2014-07-16.tar.gz"
                            };
                        case Architecture.X86:
                            return new[]
                            {
                                "http://ffmpeg.gusari.org/static/32bit/ffmpeg.static.32bit.latest.tar.gz",
                                "https://github.com/MediaBrowser/MediaBrowser.Resources/raw/master/ffmpeg/linux/ffmpeg.static.32bit.2014-07-16.tar.gz"
                            };
                    }
                    break;
            }

            // No version available 
            return new string[] { };
        }
    }
}