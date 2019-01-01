using System;
using System.IO;
using MediaBrowser.Model.System;
using System.Runtime.InteropServices;

namespace Emby.Server.Implementations.EnvironmentInfo
{
    public class EnvironmentInfo : IEnvironmentInfo
    {
        private MediaBrowser.Model.System.OperatingSystem? _customOperatingSystem;

        public virtual MediaBrowser.Model.System.OperatingSystem OperatingSystem
        {
            get
            {
                if (_customOperatingSystem.HasValue)
                {
                    return _customOperatingSystem.Value;
                }

                switch (Environment.OSVersion.Platform)
                {
                    case PlatformID.MacOSX:
                        return MediaBrowser.Model.System.OperatingSystem.OSX;
                    case PlatformID.Win32NT:
                        return MediaBrowser.Model.System.OperatingSystem.Windows;
                    case PlatformID.Unix:
                        return MediaBrowser.Model.System.OperatingSystem.Linux;
                }

                return MediaBrowser.Model.System.OperatingSystem.Windows;
            }
            set
            {
                _customOperatingSystem = value;
            }
        }

        public string OperatingSystemName
        {
            get
            {
                return Environment.OSVersion.Platform.ToString();
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

        public Architecture SystemArchitecture { get; set; }

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
