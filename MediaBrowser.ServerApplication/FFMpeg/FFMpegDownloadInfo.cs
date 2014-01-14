using System;
#if __MonoCS__
using System.Runtime.InteropServices;
#endif

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

                    #if __MonoCS__
                case PlatformID.Unix:
                    if (IsRunningOnMac())
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
                    else
                    {
                        // Linux
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
                        "http://ffmpeg.zeranoe.com/builds/win32/static/ffmpeg-20140105-git-70937d9-win32-static.7z",
                        "https://www.dropbox.com/s/oghurnp5zh292ry/ffmpeg-20140105-git-70937d9-win32-static.7z?dl=1"
                    };
           
                    #if __MonoCS__
                case PlatformID.Unix:
                    if (IsRunningOnMac())
                    {
                        // Mac OS X Intel 64bit
                        return new[]
                        {
                            "https://copy.com/IB0W4efS6t9A/ffall-2.1.1.tar.gz?download=1"
                        };
                    }
                    else
                    {
                        // Linux
                        return new[]
                        {
                            "http://ffmpeg.gusari.org/static/32bit/ffmpeg.static.32bit.2014-01-04.tar.gz",
                            "https://www.dropbox.com/s/b7nkg71sil812hp/ffmpeg.static.32bit.2014-01-04.tar.gz?dl=1"
                        };
                    }
                    #endif
            }

            return new string[] {};
        }

        #if __MonoCS__
        // From mono/mcs/class/Managed.Windows.Forms/System.Windows.Forms/XplatUI.cs
        [DllImport ("libc")]
        static extern int uname (IntPtr buf);

        static bool IsRunningOnMac()
        {
            IntPtr buf = IntPtr.Zero;
            try {
                buf = Marshal.AllocHGlobal (8192);
                // This is a hacktastic way of getting sysname from uname ()
                if (uname (buf) == 0) {
                    string os = Marshal.PtrToStringAnsi (buf);
                    if (os == "Darwin")
                        return true;
                }
            } catch {
            } finally {
                if (buf != IntPtr.Zero)
                    Marshal.FreeHGlobal (buf);
            }
            return false;
        }
        #endif
    }
}
