using MediaBrowser.Common.IO;
using MediaBrowser.Common.Logging;
using MediaBrowser.Common.Win32;
using MediaBrowser.Controller.Library;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Allows some code sharing between entities that support special features
    /// </summary>
    public interface ISupportsSpecialFeatures
    {
        /// <summary>
        /// Gets the path.
        /// </summary>
        /// <value>The path.</value>
        string Path { get; }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        string Name { get; }

        /// <summary>
        /// Gets the resolve args.
        /// </summary>
        /// <value>The resolve args.</value>
        ItemResolveArgs ResolveArgs { get; }

        /// <summary>
        /// Gets the special features.
        /// </summary>
        /// <value>The special features.</value>
        List<Video> SpecialFeatures { get; }
    }

    /// <summary>
    /// Class SpecialFeatures
    /// </summary>
    public static class SpecialFeatures
    {
        /// <summary>
        /// Loads special features from the file system
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>List{Video}.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public static IEnumerable<Video> LoadSpecialFeatures(ISupportsSpecialFeatures entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException();
            }

            WIN32_FIND_DATA? folder;

            try
            {
                folder = entity.ResolveArgs.GetFileSystemEntryByName("specials");
            }
            catch (IOException ex)
            {
                Logger.LogException("Error getting ResolveArgs for {0}", ex, entity.Path);
                return new List<Video> { };
            }

            // Path doesn't exist. No biggie
            if (folder == null)
            {
                return new List<Video> {};
            }

            IEnumerable<WIN32_FIND_DATA> files;

            try
            {
                files = FileSystem.GetFiles(folder.Value.Path);
            }
            catch (IOException ex)
            {
                Logger.LogException("Error loading trailers for {0}", ex, entity.Name);
                return new List<Video> { };
            }

            return Kernel.Instance.LibraryManager.GetItems<Video>(files, null).Select(video =>
            {
                // Try to retrieve it from the db. If we don't find it, use the resolved version
                var dbItem = Kernel.Instance.ItemRepository.RetrieveItem(video.Id) as Video;

                if (dbItem != null)
                {
                    dbItem.ResolveArgs = video.ResolveArgs;
                    video = dbItem;
                }

                return video;
            });
        }
    }
}
