using System;
#if __MonoCS__
using Mono.Unix.Native;
using System.Text.RegularExpressions;
using System.IO;
#endif

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
                            return "20140304";
                        case "FFMpegFilename":
                            return "ffmpeg.exe";
                        case "FFProbeFilename":
                            return "ffprobe.exe";
                        case "ArchiveType":
                            return "7z";
                    }
                    break;

                #if __MonoCS__
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
                                    return "20140304";
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
                                    return "20140304";
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
#endif
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
                        "http://ffmpeg.zeranoe.com/builds/win32/static/ffmpeg-20140304-git-f34cceb-win32-static.7z",
                        "https://www.dropbox.com/s/6brdetuzbld93jk/ffmpeg-20140304-git-f34cceb-win32-static.7z?dl=1"
                    };
           
                    #if __MonoCS__
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
                                "http://ffmpeg.gusari.org/static/32bit/ffmpeg.static.32bit.2014-03-04.tar.gz",
                                "https://www.dropbox.com/s/0l76mcauqqkta31/ffmpeg.static.32bit.2014-03-04.tar.gz?dl=1"
                            };
                        }

                        if (PlatformDetection.IsX86_64)
                        {
                            return new[]
                            {
                                "http://ffmpeg.gusari.org/static/64bit/ffmpeg.static.64bit.2014-03-04.tar.gz",
                                "https://www.dropbox.com/s/9wlxz440mdejuqe/ffmpeg.static.64bit.2014-03-04.tar.gz?dl=1"
                            };
                        }

                    }

                    //No Unix version available 
                    return new string[] {};
#endif
            }
            return new string[] {};
        }
    }

    #if __MonoCS__
    public static class PlatformDetection
    {
        public readonly static bool IsWindows;
        public readonly static bool IsMac;
        public readonly static bool IsLinux;
        public readonly static bool IsX86;
        public readonly static bool IsX86_64;
        public readonly static bool IsArm;

        static PlatformDetection ()
        {
            IsWindows = Path.DirectorySeparatorChar == '\\';

            //Don't call uname on windows
            if (!IsWindows)
            {
                Utsname uname;
                var callResult = Syscall.uname(out uname);
                if (callResult == 0)
                {
                    IsMac = uname.sysname == "Darwin";
                    IsLinux = !IsMac && uname.sysname == "Linux";

                    Regex archX86 = new Regex("(i|I)[3-6]86");
                    IsX86 = archX86.IsMatch(uname.machine);
                    IsX86_64 = !IsX86 && uname.machine == "x86_64";
                    IsArm = !IsX86 && !IsX86 && uname.machine.StartsWith("arm");
                }
            }
            else
            {
                if (System.Environment.Is64BitOperatingSystem)
                    IsX86_64 = true;
                else
                    IsX86 = true;
            }
        }
    }
    #endif
}
