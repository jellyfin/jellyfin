using System;
using System.IO;
using MediaBrowser.Model.System;

namespace Emby.Server.Implementations.EnvironmentInfo
{
    public class EnvironmentInfo : IEnvironmentInfo
    {
        private Architecture? _customArchitecture;
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

        public Architecture SystemArchitecture
        {
            get
            {
                if (_customArchitecture.HasValue)
                {
                    return _customArchitecture.Value;
                }

                return Environment.Is64BitOperatingSystem ? MediaBrowser.Model.System.Architecture.X64 : MediaBrowser.Model.System.Architecture.X86;
            }
            set
            {
                _customArchitecture = value;
            }
        }

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