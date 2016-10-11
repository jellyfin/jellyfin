using System;
using System.Configuration;
using System.IO;

namespace MediaBrowser.Server.Startup.Common
{
    public static class ApplicationPathHelper
    {
        /// <summary>
        /// Gets the path to the application's ProgramDataFolder
        /// </summary>
        /// <returns>System.String.</returns>
        public static string GetProgramDataPath(string applicationPath)
        {
            var useDebugPath = false;

#if DEBUG
            useDebugPath = true;
#endif

            var programDataPath = useDebugPath ? 
                ConfigurationManager.AppSettings["DebugProgramDataPath"] : 
                ConfigurationManager.AppSettings["ReleaseProgramDataPath"];

            programDataPath = programDataPath.Replace("%ApplicationData%", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));

            programDataPath = programDataPath
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);

            // If it's a relative path, e.g. "..\"
            if (!Path.IsPathRooted(programDataPath))
            {
                var path = Path.GetDirectoryName(applicationPath);

                if (string.IsNullOrEmpty(path))
                {
                    throw new ApplicationException("Unable to determine running assembly location");
                }

                programDataPath = Path.Combine(path, programDataPath);

                programDataPath = Path.GetFullPath(programDataPath);
            }

            Directory.CreateDirectory(programDataPath);

            return programDataPath;
        }
    }
}
