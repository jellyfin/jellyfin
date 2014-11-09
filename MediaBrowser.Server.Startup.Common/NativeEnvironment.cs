
namespace MediaBrowser.Server.Startup.Common
{
    public class NativeEnvironment
    {
        public OperatingSystem OperatingSystem { get; set; }
        public Architecture SystemArchitecture { get; set; }
    }

    public enum OperatingSystem
    {
        Windows = 0,
        Osx = 1,
        Bsd = 2,
        Linux = 3
    }

    public enum Architecture
    {
        X86 = 0,
        X86_X64 = 1,
        Arm = 2
    }
}
