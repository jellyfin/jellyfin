using System.Linq;
using DotNet.Globbing;

namespace Emby.Server.Implementations.Library
{
    /// <summary>
    /// Glob patterns for files to ignore
    /// </summary>
    public static class IgnorePatterns
    {
        /// <summary>
        /// Files matching these glob patterns will be ignored
        /// </summary>
        public static readonly string[] Patterns = new string[]
        {
            "**/small.jpg",
            "**/albumart.jpg",
            "**/*sample*",

            // Directories
            "**/metadata/**",
            "**/ps3_update/**",
            "**/ps3_vprm/**",
            "**/extrafanart/**",
            "**/extrathumbs/**",
            "**/.actors/**",
            "**/.wd_tv/**",
            "**/lost+found/**",

            // WMC temp recording directories that will constantly be written to
            "**/TempRec/**",
            "**/TempSBE/**",

            // Synology
            "**/eaDir/**",
            "**/@eaDir/**",
            "**/#recycle/**",

            // Qnap
            "**/@Recycle/**",
            "**/.@__thumb/**",
            "**/$RECYCLE.BIN/**",
            "**/System Volume Information/**",
            "**/.grab/**",

            // Unix hidden files and directories
            "**/.*/**",

            // thumbs.db
            "**/thumbs.db",

            // bts sync files
            "**/*.bts",
            "**/*.sync",
        };

        private static readonly GlobOptions _globOptions = new GlobOptions
        {
            Evaluation = {
                CaseInsensitive = true
            }
        };

        private static readonly Glob[] _globs = Patterns.Select(p => Glob.Parse(p, _globOptions)).ToArray();

        /// <summary>
        /// Returns true if the supplied path should be ignored
        /// </summary>
        public static bool ShouldIgnore(string path)
        {
            return _globs.Any(g => g.IsMatch(path));
        }
    }
}
