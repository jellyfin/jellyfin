using System;
using System.Runtime.InteropServices;
using MediaBrowser.Model.System;

namespace Emby.Server.Implementations.EnvironmentInfo
{
    public class EnvironmentInfo : IEnvironmentInfo
    {
        public EnvironmentInfo(MediaBrowser.Model.System.OperatingSystem operatingSystem)
        {
            OperatingSystem = operatingSystem;
        }

        public MediaBrowser.Model.System.OperatingSystem OperatingSystem { get; private set; }

        public string OperatingSystemName
        {
            get
            {
                switch (OperatingSystem)
                {
                    case MediaBrowser.Model.System.OperatingSystem.Android: return "Android";
                    case MediaBrowser.Model.System.OperatingSystem.BSD: return "BSD";
                    case MediaBrowser.Model.System.OperatingSystem.Linux: return "Linux";
                    case MediaBrowser.Model.System.OperatingSystem.OSX: return "macOS";
                    case MediaBrowser.Model.System.OperatingSystem.Windows: return "Windows";
                    default: throw new Exception($"Unknown OS {OperatingSystem}");
                }
            }
        }

        public string OperatingSystemVersion => Environment.OSVersion.Version.ToString() + " " + Environment.OSVersion.ServicePack.ToString();

        public Architecture SystemArchitecture => RuntimeInformation.OSArchitecture;
    }
}
