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
        public MediaBrowser.Model.System.Architecture? CustomArchitecture { get; set; }
        public MediaBrowser.Model.System.OperatingSystem? CustomOperatingSystem { get; set; }

        public virtual MediaBrowser.Model.System.OperatingSystem OperatingSystem
        {
            get
            {
                if (CustomOperatingSystem.HasValue)
                {
                    return CustomOperatingSystem.Value;
                }

                if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return MediaBrowser.Model.System.OperatingSystem.OSX;
                }
                if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return MediaBrowser.Model.System.OperatingSystem.Windows;
                }
                if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    return MediaBrowser.Model.System.OperatingSystem.Linux;
                }

                return MediaBrowser.Model.System.OperatingSystem.Windows;
            }
        }

        public string OperatingSystemName
        {
            get
            {
                return System.Runtime.InteropServices.RuntimeInformation.OSDescription;
            }
        }

        public string OperatingSystemVersion
        {
            get
            {
                return System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
            }
        }

        public char PathSeparator
        {
            get
            {
                return Path.PathSeparator;
            }
        }

        public MediaBrowser.Model.System.Architecture SystemArchitecture
        {
            get
            {
                if (CustomArchitecture.HasValue)
                {
                    return CustomArchitecture.Value;
                }

                switch (System.Runtime.InteropServices.RuntimeInformation.OSArchitecture)
                {
                    case System.Runtime.InteropServices.Architecture.Arm:
                        return MediaBrowser.Model.System.Architecture.Arm;
                    case System.Runtime.InteropServices.Architecture.Arm64:
                        return MediaBrowser.Model.System.Architecture.Arm64;
                    case System.Runtime.InteropServices.Architecture.X64:
                        return MediaBrowser.Model.System.Architecture.X64;
                    case System.Runtime.InteropServices.Architecture.X86:
                        return MediaBrowser.Model.System.Architecture.X86;
                }
                return MediaBrowser.Model.System.Architecture.X64;
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
