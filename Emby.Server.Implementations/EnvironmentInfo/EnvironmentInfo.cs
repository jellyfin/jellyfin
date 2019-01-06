using System;
using System.IO;
using MediaBrowser.Model.System;
using System.Runtime.InteropServices;

namespace Emby.Server.Implementations.EnvironmentInfo
{
    // TODO: Rework @bond
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

        public string OperatingSystemVersion
        {
            get
            {
                return Environment.OSVersion.Version.ToString() + " " + Environment.OSVersion.ServicePack.ToString();
            }
        }

        public char PathSeparator
        {
            get
            {
                return Path.PathSeparator;
            }
        }

        public Architecture SystemArchitecture { get { return RuntimeInformation.OSArchitecture; } }

        public string GetEnvironmentVariable(string name)
        {
            return Environment.GetEnvironmentVariable(name);
        }

        public string StackTrace
        {
            get { return Environment.StackTrace; }
        }

        public void SetProcessEnvironmentVariable(string name, string value)
        {
            Environment.SetEnvironmentVariable(name, value);
        }
    }
}
