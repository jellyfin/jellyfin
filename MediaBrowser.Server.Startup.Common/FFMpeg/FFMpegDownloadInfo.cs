
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
                            info.Version = "20150124";
                            break;
                        case Architecture.X86:
                            info.Version = "20150124";
                            break;
                    }
                    break;
                case OperatingSystem.Osx:

                    info.ArchiveType = "7z";

                    switch (environment.SystemArchitecture)
                    {
                        case Architecture.X86_X64:
                            info.Version = "20150110";
                            break;
                        case Architecture.X86:
                            info.Version = "20150110";
                            break;
                    }
                    break;

                case OperatingSystem.Windows:

                    info.FFMpegFilename = "ffmpeg.exe";
                    info.FFProbeFilename = "ffprobe.exe";
                    info.Version = "20150110";
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
                                "http://ffmpeg.zeranoe.com/builds/win64/static/ffmpeg-20150110-git-4df01d5-win64-static.7z",
                                "https://github.com/MediaBrowser/MediaBrowser.Resources/raw/master/ffmpeg/windows/ffmpeg-20150110-git-4df01d5-win64-static.7z"
                            };
                        case Architecture.X86:
                            return new[]
                            {
                                "http://ffmpeg.zeranoe.com/builds/win32/static/ffmpeg-20150110-git-4df01d5-win32-static.7z",
                                "https://github.com/MediaBrowser/MediaBrowser.Resources/raw/master/ffmpeg/windows/ffmpeg-20150110-git-4df01d5-win32-static.7z"
                            };
                    }
                    break;

                case OperatingSystem.Osx:

                    switch (environment.SystemArchitecture)
                    {
                        case Architecture.X86_X64:
                            return new[]
                            {
                                "https://github.com/MediaBrowser/MediaBrowser.Resources/raw/master/ffmpeg/osx/ffmpeg-x64-2.5.3.7z"
                            };
                        case Architecture.X86:
                            return new[]
                            {
                                "https://github.com/MediaBrowser/MediaBrowser.Resources/raw/master/ffmpeg/osx/ffmpeg-x86-2.5.3.7z"
                            };
                    }
                    break;

                case OperatingSystem.Linux:

                    switch (environment.SystemArchitecture)
                    {
                        case Architecture.X86_X64:
                            return new[]
                            {
                                "http://johnvansickle.com/ffmpeg/releases/ffmpeg-release-64bit-static.tar.xz",
                                "https://github.com/MediaBrowser/MediaBrowser.Resources/raw/master/ffmpeg/linux/ffmpeg-release-64bit-static.tar.xz"
                            };
                        case Architecture.X86:
                            return new[]
                            {
                                "http://johnvansickle.com/ffmpeg/releases/ffmpeg-release-32bit-static.tar.xz",
                                "https://github.com/MediaBrowser/MediaBrowser.Resources/raw/master/ffmpeg/linux/ffmpeg-release-32bit-static.tar.xz"
                            };
                    }
                    break;
            }

            // No version available 
            return new string[] { };
        }
    }
}