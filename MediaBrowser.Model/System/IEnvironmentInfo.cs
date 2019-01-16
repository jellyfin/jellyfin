using System.Runtime.InteropServices;

namespace MediaBrowser.Model.System
{
    public interface IEnvironmentInfo
    {
        MediaBrowser.Model.System.OperatingSystem OperatingSystem { get; }
        string OperatingSystemName { get; }
        string OperatingSystemVersion { get; }
        Architecture SystemArchitecture { get; }
    }

    public enum OperatingSystem
    {
        Windows,
        Linux,
        OSX,
        BSD,
        Android
    }
}
