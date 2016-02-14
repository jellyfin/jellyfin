
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
            // Linux builds: http://johnvansickle.com/ffmpeg/
            // OS X builds: http://ffmpegmac.net/
            // OS X x64: http://www.evermeet.cx/ffmpeg/

            switch (environment.OperatingSystem)
            {
                case OperatingSystem.Bsd:
                    break;
                case OperatingSystem.Linux:

                    info.ArchiveType = "7z";
                    info.Version = "20150917";
                    break;
                case OperatingSystem.Osx:

                    info.ArchiveType = "7z";

                    switch (environment.SystemArchitecture)
                    {
                        case Architecture.X86_X64:
                            info.Version = "20160124";
                            break;
                        case Architecture.X86:
                            info.Version = "20150110";
                            break;
                    }
                    break;

                case OperatingSystem.Windows:

                    info.FFMpegFilename = "ffmpeg.exe";
                    info.FFProbeFilename = "ffprobe.exe";
                    info.Version = "20160131";
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
                                "https://github.com/MediaBrowser/Emby.Resources/raw/master/ffmpeg/windows/ffmpeg-20160131-win64.7z",
                                "http://ffmpeg.zeranoe.com/builds/win64/static/ffmpeg-20151109-git-480bad7-win64-static.7z"
                            };
                        case Architecture.X86:
                            return new[]
                            {
                                "https://github.com/MediaBrowser/Emby.Resources/raw/master/ffmpeg/windows/ffmpeg-20160131-win32.7z",
                                "http://ffmpeg.zeranoe.com/builds/win32/static/ffmpeg-20151109-git-480bad7-win32-static.7z"
                            };
                    }
                    break;

                case OperatingSystem.Osx:

                    switch (environment.SystemArchitecture)
                    {
                        case Architecture.X86_X64:
                            return new[]
                            {
                                "https://github.com/MediaBrowser/Emby.Resources/raw/master/ffmpeg/osx/ffmpeg-x64-2.8.5.7z"
                            };
                        case Architecture.X86:
                            return new[]
                            {
                                "https://github.com/MediaBrowser/Emby.Resources/raw/master/ffmpeg/osx/ffmpeg-x86-2.5.3.7z"
                            };
                    }
                    break;

                case OperatingSystem.Linux:

                    switch (environment.SystemArchitecture)
                    {
                        case Architecture.X86_X64:
                            return new[]
                            {
                                "https://github.com/MediaBrowser/Emby.Resources/raw/master/ffmpeg/linux/ffmpeg-2.8.0-64bit-static.7z"
                            };
                        case Architecture.X86:
                            return new[]
                            {
                                "https://github.com/MediaBrowser/Emby.Resources/raw/master/ffmpeg/linux/ffmpeg-2.8.0-32bit-static.7z"
                            };
                        case Architecture.Arm:
                            return new[]
                            {
                                "https://github.com/MediaBrowser/Emby.Resources/raw/master/ffmpeg/linux/ffmpeg-arm.7z"
                            };
                    }
                    break;
            }

            // No version available 
            return new string[] { };
        }
    }
}