using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Emby.Server
{
    public class ApplicationPathHelper
    {
        public static string GetProgramDataPath(string applicationPath)
        {
            var useDebugPath = false;

#if DEBUG
            useDebugPath = true;
#endif

            var programDataPath = useDebugPath ?
                "programdata" :
                "programdata";

            programDataPath = programDataPath
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);

            // If it's a relative path, e.g. "..\"
            if (!Path.IsPathRooted(programDataPath))
            {
                var path = Path.GetDirectoryName(applicationPath);

                if (string.IsNullOrEmpty(path))
                {
                    throw new Exception("Unable to determine running assembly location");
                }

                programDataPath = Path.Combine(path, programDataPath);

                programDataPath = Path.GetFullPath(programDataPath);
            }

            Directory.CreateDirectory(programDataPath);

            return programDataPath;
        }
    }
}
