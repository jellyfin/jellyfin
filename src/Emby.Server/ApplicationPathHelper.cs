using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Emby.Server
{
    public class ApplicationPathHelper
    {
        public static string GetProgramDataPath(string appDirectory)
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
                programDataPath = Path.Combine(appDirectory, programDataPath);

                programDataPath = Path.GetFullPath(programDataPath);
            }

            Directory.CreateDirectory(programDataPath);

            return programDataPath;
        }
    }
}
