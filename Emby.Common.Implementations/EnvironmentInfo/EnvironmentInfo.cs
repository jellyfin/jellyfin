using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using MediaBrowser.Model.System;

namespace Emby.Common.Implementations.EnvironmentInfo
{
    public class EnvironmentInfo : IEnvironmentInfo
    {
        public MediaBrowser.Model.System.Architecture? CustomArchitecture { get; set; }

        public MediaBrowser.Model.System.OperatingSystem OperatingSystem
        {
            get
            {
#if NET46
                switch (Environment.OSVersion.Platform)
                {
                    case PlatformID.MacOSX:
                        return MediaBrowser.Model.System.OperatingSystem.OSX;
                    case PlatformID.Win32NT:
                        return MediaBrowser.Model.System.OperatingSystem.Windows;
                    case PlatformID.Unix:
                        return MediaBrowser.Model.System.OperatingSystem.Linux;
                }
#elif NETSTANDARD1_6
                if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return OperatingSystem.OSX;
                }
                if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return OperatingSystem.Windows;
                }
                if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    return OperatingSystem.Linux;
                }
#endif
                return MediaBrowser.Model.System.OperatingSystem.Windows;
            }
        }

        public string OperatingSystemName
        {
            get
            {
#if NET46
                return Environment.OSVersion.Platform.ToString();
#elif NETSTANDARD1_6
            return System.Runtime.InteropServices.RuntimeInformation.OSDescription;
#endif
                return "Operating System";
            }
        }

        public string OperatingSystemVersion
        {
            get
            {
#if NET46
                return Environment.OSVersion.Version.ToString() + " " + Environment.OSVersion.ServicePack.ToString();
#elif NETSTANDARD1_6
            return System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
#endif
                return "1.0";
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
#if NET46
                return Environment.Is64BitOperatingSystem ? MediaBrowser.Model.System.Architecture.X64 : MediaBrowser.Model.System.Architecture.X86;
#elif NETSTANDARD1_6
                switch(System.Runtime.InteropServices.RuntimeInformation.OSArchitecture)
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
#endif
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
    }
}
