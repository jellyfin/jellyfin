using System;
using System.IO;
using System.Runtime.InteropServices;

namespace EmbyServer
{
    public class OperatingSystemInformationLookup : IOperatingSystemInformationLookup

    {
        public OSPlatform GetOperatingSystem()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return OSPlatform.Windows;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return OSPlatform.Linux;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return OSPlatform.OSX;
            }
            
            throw new InvalidProgramException("Your operating system isn't supported.");
        }

        public string GetApplicationDataPath()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        }

        public char GetDirectorySeparatorChar()
        {
            return Path.DirectorySeparatorChar;
        }

        public bool IsPathRooted(string path)
        {
            return Path.IsPathRooted(path);
        }
    }
}