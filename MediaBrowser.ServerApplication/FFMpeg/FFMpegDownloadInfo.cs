using System;
#if __MonoCS__
using Mono.Unix.Native;
using System.Text.RegularExpressions;
using System.IO;
#endif
using System.IO;
using System.Text.RegularExpressions;

namespace MediaBrowser.ServerApplication.FFMpeg
{
    public static class FFMpegDownloadInfo
    {
        // Windows builds: http://ffmpeg.zeranoe.com/builds/
        // Linux builds: http://ffmpeg.gusari.org/static/
        // OS X builds: http://ffmpegmac.net/

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
                            return "20140612";
                        case "FFMpegFilename":
                            return "ffmpeg.exe";
                        case "FFProbeFilename":
                            return "ffprobe.exe";
                        case "ArchiveType":
                            return "7z";
                    }
                    break;

                case PlatformID.Unix:
                    if (PlatformDetection.IsMac)
                    {
                        if (PlatformDetection.IsX86_64)
                        {
                            switch (arg)
                            {
                                case "Version":
                                    return "20131121";
                                case "FFMpegFilename":
                                    return "ffmpeg";
                                case "FFProbeFilename":
                                    return "ffprobe";
                                case "ArchiveType":
                                    return "gz";
                            }
                            break;
                        }
                    }
                    if (PlatformDetection.IsLinux)
                    {
                        if (PlatformDetection.IsX86)
                        {
                            switch (arg)
                            {
                                case "Version":
                                    return "20140506";
                                case "FFMpegFilename":
                                    return "ffmpeg";
                                case "FFProbeFilename":
                                    return "ffprobe";
                                case "ArchiveType":
                                    return "gz";
                            }
                            break;
                        }
                        else if (PlatformDetection.IsX86_64)
                        {
                            // Linux on x86 or x86_64
                            switch (arg)
                            {
                                case "Version":
                                    return "20140505";
                                case "FFMpegFilename":
                                    return "ffmpeg";
                                case "FFProbeFilename":
                                    return "ffprobe";
                                case "ArchiveType":
                                    return "gz";
                            }
                            break;
                        }
                    }
                    // Unsupported Unix platform
                    return "";
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
                        "http://ffmpeg.zeranoe.com/builds/win32/static/ffmpeg-20140612-git-3a1c895-win32-static.7z",
                        "https://www.dropbox.com/s/lllit55bynbz6zc/ffmpeg-20140612-git-3a1c895-win32-static.7z?dl=1"
                    };

                case PlatformID.Unix:
                    if (PlatformDetection.IsMac && PlatformDetection.IsX86_64)
                    {
                        return new[]
                        {
                            "https://www.dropbox.com/s/n188rxbulqem8ry/ffmpeg-osx-20131121.gz?dl=1"
                        };
                    }

                    if (PlatformDetection.IsLinux)
                    {
                        if (PlatformDetection.IsX86)
                        {
                            return new[]
                            {
                                "http://ffmpeg.gusari.org/static/32bit/ffmpeg.static.32bit.latest.tar.gz",
                                "https://www.dropbox.com/s/k9s02pv5to6slfb/ffmpeg.static.32bit.2014-05-06.tar.gz?dl=1"
                            };
                        }

                        if (PlatformDetection.IsX86_64)
                        {
                            return new[]
                            {
                                "http://ffmpeg.gusari.org/static/64bit/ffmpeg.static.64bit.latest.tar.gz",
                                "https://www.dropbox.com/s/onuregwghywnzjo/ffmpeg.static.64bit.2014-05-05.tar.gz?dl=1"
                            };
                        }

                    }

                    //No Unix version available 
                    return new string[] { };

                default:
                    throw new ApplicationException("No ffmpeg download available for " + pid);
            }
        }
    }

    public static class PlatformDetection
    {
        public readonly static bool IsWindows;
        public readonly static bool IsMac;
        public readonly static bool IsLinux;
        public readonly static bool IsX86;
        public readonly static bool IsX86_64;
        public readonly static bool IsArm;

        static PlatformDetection()
        {
            IsWindows = Path.DirectorySeparatorChar == '\\';

            //Don't call uname on windows
            if (!IsWindows)
            {
                var uname = GetUnixName();

                IsMac = uname.sysname == "Darwin";
                IsLinux = uname.sysname == "Linux";

                var archX86 = new Regex("(i|I)[3-6]86");
                IsX86 = archX86.IsMatch(uname.machine);
                IsX86_64 = !IsX86 && uname.machine == "x86_64";
                IsArm = !IsX86 && !IsX86_64 && uname.machine.StartsWith("arm");
            }
            else
            {
                if (Environment.Is64BitOperatingSystem)
                    IsX86_64 = true;
                else
                    IsX86 = true;
            }
        }

        private static Uname GetUnixName()
        {
            var uname = new Uname();

#if __MonoCS__
                Utsname utsname;
                var callResult = Syscall.uname(out utsname);
                if (callResult == 0)
                {
                    uname.sysname= utsname.sysname;
                    uname.machine= utsname.machine;
                }
#endif
            return uname;
        }
    }

    public class Uname
    {
        public string sysname = string.Empty;
        public string machine = string.Empty;
    }
}
