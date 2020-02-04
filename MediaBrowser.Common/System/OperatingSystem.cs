#pragma warning disable CS1591
#pragma warning disable SA1600

using System;
using System.Runtime.InteropServices;
using System.Threading;
using MediaBrowser.Model.System;

namespace MediaBrowser.Common.System
{
    public static class OperatingSystem
    {
        // We can't use Interlocked.CompareExchange for enums
        private static int _id = int.MaxValue;

        public static OperatingSystemId Id
        {
            get
            {
                if (_id == int.MaxValue)
                {
                    Interlocked.CompareExchange(ref _id, (int)GetId(), int.MaxValue);
                }

                return (OperatingSystemId)_id;
            }
        }

        public static string Name
        {
            get
            {
                switch (Id)
                {
                    case OperatingSystemId.BSD: return "BSD";
                    case OperatingSystemId.Linux: return "Linux";
                    case OperatingSystemId.Darwin: return "macOS";
                    case OperatingSystemId.Windows: return "Windows";
                    default: throw new Exception($"Unknown OS {Id}");
                }
            }
        }

        private static OperatingSystemId GetId()
        {
            switch (Environment.OSVersion.Platform)
            {
                // On .NET Core `MacOSX` got replaced by `Unix`, this case should never be hit.
                case PlatformID.MacOSX:
                    return OperatingSystemId.Darwin;
                case PlatformID.Win32NT:
                    return OperatingSystemId.Windows;
                case PlatformID.Unix:
                default:
                    {
                        string osDescription = RuntimeInformation.OSDescription;
                        if (osDescription.IndexOf("linux", StringComparison.OrdinalIgnoreCase) != -1)
                        {
                            return OperatingSystemId.Linux;
                        }
                        else if (osDescription.IndexOf("darwin", StringComparison.OrdinalIgnoreCase) != -1)
                        {
                            return OperatingSystemId.Darwin;
                        }
                        else if (osDescription.IndexOf("bsd", StringComparison.OrdinalIgnoreCase) != -1)
                        {
                            return OperatingSystemId.BSD;
                        }

                        throw new Exception($"Can't resolve OS with description: '{osDescription}'");
                    }
            }
        }
    }
}
