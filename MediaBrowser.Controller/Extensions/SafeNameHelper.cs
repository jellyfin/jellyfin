using System;
using System.IO;

namespace MediaBrowser.Controller.Extensions
{
    /// <summary>
    /// Provides a shared name-length safety check for IItemByName entities
    /// (MusicArtist, Person, Studio, Genre, MusicGenre, Year) that create
    /// filesystem directories from metadata names. Names can exceed filesystem
    /// limits (e.g. concatenated performer/composer strings), causing
    /// <see cref="System.IO.PathTooLongException"/> during directory creation.
    /// </summary>
    internal static class SafeNameHelper
    {
        /// <summary>
        /// Maximum bytes for a single filename component. Linux NAME_MAX is 255;
        /// reserving headroom for filesystem overhead.
        /// </summary>
        internal const int MaxNameLength = 240;

        /// <summary>
        /// Maximum full path length with a safe margin below OS limits
        /// (PATH_MAX on Linux is 4096, MAX_PATH on Windows is 260).
        /// </summary>
        internal static int MaxFullPathLength =>
            OperatingSystem.IsWindows() ? 248 : 4069;

        /// <summary>
        /// Ensures a name does not exceed filesystem limits when combined with
        /// the given base directory. Truncates the name to <see cref="MaxNameLength"/>
        /// if necessary, and further shortens it if the resulting full path would
        /// exceed <see cref="MaxFullPathLength"/>.
        /// </summary>
        /// <param name="baseDir">The parent directory path.</param>
        /// <param name="name">The sanitized name to check.</param>
        /// <returns>A name guaranteed to produce a safe full path.</returns>
        internal static string EnsureSafeName(string baseDir, string name)
        {
            if (name.Length > MaxNameLength)
            {
                name = name[..MaxNameLength];
            }

            var fullPath = Path.Combine(baseDir, name);
            if (fullPath.Length > MaxFullPathLength)
            {
                var baseFullPath = Path.GetFullPath(baseDir);
                var available = MaxFullPathLength - baseFullPath.Length - 1;
                if (available > 0 && available < name.Length)
                {
                    name = name[..available];
                }
            }

            return name;
        }
    }
}
