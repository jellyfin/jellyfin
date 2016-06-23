using MediaBrowser.Model.System;

namespace MediaBrowser.Server.Startup.Common
{
    public class NativeEnvironment
    {
        public OperatingSystem OperatingSystem { get; set; }
        public Architecture SystemArchitecture { get; set; }
        public string OperatingSystemVersionString { get; set; }
    }

    public enum OperatingSystem
    {
        Windows = 0,
        Osx = 1,
        Bsd = 2,
        Linux = 3
    }
}
