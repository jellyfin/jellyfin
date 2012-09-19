using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Entities.TV;

namespace MediaBrowser.Controller.Resolvers
{
    public static class EntityResolutionHelper
    {
        /// <summary>
        /// Any folder named in this list will be ignored - can be added to at runtime for extensibility
        /// </summary>
        public static List<string> IgnoreFolders = new List<string>()
        {
            "trailers",
            "metadata",
            "bdmv",
            "certificate",
            "backup",
            "video_ts",
            "audio_ts",
            "ps3_update",
            "ps3_vprm",
            "adv_obj",
            "hvdvd_ts"
        };
        /// <summary>
        /// Determines whether a path should be resolved or ignored entirely - called before we even look at the contents
        /// </summary>
        /// <param name="path"></param>
        /// <returns>false if the path should be ignored</returns>
        public static bool ShouldResolvePath(WIN32_FIND_DATA path)
        {
            bool resolve = true;
            // Ignore hidden files and folders
            if (path.IsHidden || path.IsSystemFile)
            {
                resolve = false;
            }

            // Ignore any folders in our list
            else if (path.IsDirectory && IgnoreFolders.Contains(Path.GetFileName(path.Path), StringComparer.OrdinalIgnoreCase))
            {
                resolve =  false;
            }

            return resolve;
        }

        /// <summary>
        /// Determines whether a path should be ignored based on its contents - called after the contents have been read
        /// </summary>
        public static bool ShouldResolvePathContents(ItemResolveEventArgs args)
        {
            bool resolve = true;
            if (args.ContainsFile(".ignore"))
            {
                // Ignore any folders containing a file called .ignore
                resolve = false;
            }

            return resolve;
        }
    }
}
