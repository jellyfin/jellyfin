using System.Runtime.InteropServices;

namespace EmbyServer
{
    public interface IOperatingSystemInformationLookup
    {
        OSPlatform GetOperatingSystem();
        string GetApplicationDataPath();
        char GetDirectorySeparatorChar();
        bool IsPathRooted(string path);
    }
}