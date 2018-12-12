using System;
using System.IO;
using System.Runtime.InteropServices;
using EmbyServer;

namespace MediaBrowser.Server.Mono
{
    public class ApplicationPathHelper
    {
        private readonly string _debugProgramDataPath;
        private readonly string _releaseProgramDataPath;
        private readonly IOperatingSystemInformationLookup _osInfoLookup;
        private readonly bool _debug;

        public ApplicationPathHelper(
            string debugProgramDataPath,
            string releaseProgramDataPath,
            IOperatingSystemInformationLookup osInfoLookup,
            bool debug)
        {
            _debugProgramDataPath = debugProgramDataPath;
            _releaseProgramDataPath = releaseProgramDataPath;
            _osInfoLookup = osInfoLookup;
            _debug = debug;
        }
       
        /// <summary>
        /// Gets the path to the application's ProgramDataFolder
        /// </summary>
        /// <returns>System.String.</returns>
        public string GetProgramDataPath(string applicationPath)
        {
            var programDataPath = _debug ? _debugProgramDataPath : _releaseProgramDataPath;

            var isWindows = _osInfoLookup.GetOperatingSystem() == OSPlatform.Windows;
            var windowsAppDataPath = _osInfoLookup.GetApplicationDataPath();
            // GetApplicationDataPath() will return $HOME/... on *nix, so hard code an alternative here.
            const string unixAppDataPath = "/var/lib";
            
            programDataPath = isWindows
                ? programDataPath.Replace("%ApplicationData%", windowsAppDataPath)
                : programDataPath.Replace("%ApplicationData%", unixAppDataPath);

            programDataPath = programDataPath
                // Replace empty directories caused by trailing slashes
                .Replace("//", "/")
                .Replace("\\\\", "\\")
                // Correct directory separator
                .Replace('/', _osInfoLookup.GetDirectorySeparatorChar())
                .Replace('\\', _osInfoLookup.GetDirectorySeparatorChar());

            // If it's a relative path, e.g. "..\"
            if (!_osInfoLookup.IsPathRooted(programDataPath))
            {
                var path = Path.GetDirectoryName(applicationPath);

                if (string.IsNullOrEmpty(path))
                {
                    throw new ApplicationException("Unable to determine running assembly location");
                }

                programDataPath = Path.Combine(path, programDataPath);
                programDataPath = Path.GetFullPath(programDataPath);
            }

            return programDataPath;
        }
    }
}
