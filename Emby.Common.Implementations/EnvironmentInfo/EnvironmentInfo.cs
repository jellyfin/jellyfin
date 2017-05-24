using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using MediaBrowser.Model.System;

namespace Emby.Common.Implementations.EnvironmentInfo
{
    public class EnvironmentInfo : IEnvironmentInfo
    {
        public Architecture? CustomArchitecture { get; set; }
        public MediaBrowser.Model.System.OperatingSystem? CustomOperatingSystem { get; set; }

        public virtual MediaBrowser.Model.System.OperatingSystem OperatingSystem
        {
            get
            {
                if (CustomOperatingSystem.HasValue)
                {
                    return CustomOperatingSystem.Value;
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
                if (CustomArchitecture.HasValue)
                {
                    return CustomArchitecture.Value;
                }

                return Environment.Is64BitOperatingSystem ? MediaBrowser.Model.System.Architecture.X64 : MediaBrowser.Model.System.Architecture.X86;
            }
        }

        public string GetEnvironmentVariable(string name)
        {
            return Environment.GetEnvironmentVariable(name);
        }

        public virtual string GetUserId()
        {
            return null;
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