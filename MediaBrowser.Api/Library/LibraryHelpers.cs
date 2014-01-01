using MediaBrowser.Common.IO;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using System;
using System.IO;
using System.Linq;

namespace MediaBrowser.Api.Library
{
    /// <summary>
    /// Class LibraryHelpers
    /// </summary>
    public static class LibraryHelpers
    {
        /// <summary>
        /// The shortcut file extension
        /// </summary>
        private const string ShortcutFileExtension = ".mblink";
        /// <summary>
        /// The shortcut file search
        /// </summary>
        private const string ShortcutFileSearch = "*" + ShortcutFileExtension;

        /// <summary>
        /// Deletes a shortcut from within a virtual folder, within either the default view or a user view
        /// </summary>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="virtualFolderName">Name of the virtual folder.</param>
        /// <param name="mediaPath">The media path.</param>
        /// <param name="user">The user.</param>
        /// <param name="appPaths">The app paths.</param>
        /// <exception cref="System.IO.DirectoryNotFoundException">The media folder does not exist</exception>
        public static void RemoveMediaPath(IFileSystem fileSystem, string virtualFolderName, string mediaPath, User user, IServerApplicationPaths appPaths)
        {
            var rootFolderPath = user != null ? user.RootFolderPath : appPaths.DefaultUserViewsPath;
            var path = Path.Combine(rootFolderPath, virtualFolderName);

            if (!Directory.Exists(path))
            {
                throw new DirectoryNotFoundException(string.Format("The media collection {0} does not exist", virtualFolderName));
            }

            var shortcut = Directory.EnumerateFiles(path, ShortcutFileSearch, SearchOption.AllDirectories).FirstOrDefault(f => fileSystem.ResolveShortcut(f).Equals(mediaPath, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(shortcut))
            {
                File.Delete(shortcut);
            }
        }

        /// <summary>
        /// Adds an additional mediaPath to an existing virtual folder, within either the default view or a user view
        /// </summary>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="virtualFolderName">Name of the virtual folder.</param>
        /// <param name="path">The path.</param>
        /// <param name="user">The user.</param>
        /// <param name="appPaths">The app paths.</param>
        /// <exception cref="System.ArgumentException">The path is not valid.</exception>
        /// <exception cref="System.IO.DirectoryNotFoundException">The path does not exist.</exception>
        public static void AddMediaPath(IFileSystem fileSystem, string virtualFolderName, string path, User user, IServerApplicationPaths appPaths)
        {
            if (!Directory.Exists(path))
            {
                throw new DirectoryNotFoundException("The path does not exist.");
            }

            var rootFolderPath = user != null ? user.RootFolderPath : appPaths.DefaultUserViewsPath;
            var virtualFolderPath = Path.Combine(rootFolderPath, virtualFolderName);

            ValidateNewMediaPath(fileSystem, rootFolderPath, path);

            var shortcutFilename = Path.GetFileNameWithoutExtension(path);

            var lnk = Path.Combine(virtualFolderPath, shortcutFilename + ShortcutFileExtension);

            while (File.Exists(lnk))
            {
                shortcutFilename += "1";
                lnk = Path.Combine(virtualFolderPath, shortcutFilename + ShortcutFileExtension);
            }

            fileSystem.CreateShortcut(lnk, path);
        }

        /// <summary>
        /// Validates that a new media path can be added
        /// </summary>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="currentViewRootFolderPath">The current view root folder path.</param>
        /// <param name="mediaPath">The media path.</param>
        /// <exception cref="System.ArgumentException">
        /// </exception>
        private static void ValidateNewMediaPath(IFileSystem fileSystem, string currentViewRootFolderPath, string mediaPath)
        {
            var pathsInCurrentVIew = Directory.EnumerateFiles(currentViewRootFolderPath, ShortcutFileSearch, SearchOption.AllDirectories)
                    .Select(fileSystem.ResolveShortcut)
                    .ToList();

            // Don't allow duplicate sub-paths within the same user library, or it will result in duplicate items
            // See comments in IsNewPathValid
            var duplicate = pathsInCurrentVIew
              .FirstOrDefault(p => !IsNewPathValid(fileSystem, mediaPath, p));

            if (!string.IsNullOrEmpty(duplicate))
            {
                throw new ArgumentException(string.Format("The path cannot be added to the library because {0} already exists.", duplicate));
            }
            
            // Make sure the current root folder doesn't already have a shortcut to the same path
            duplicate = pathsInCurrentVIew
                .FirstOrDefault(p => string.Equals(mediaPath, p, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(duplicate))
            {
                throw new ArgumentException(string.Format("The path {0} already exists in the library", mediaPath));
            }
        }

        /// <summary>
        /// Validates that a new path can be added based on an existing path
        /// </summary>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="newPath">The new path.</param>
        /// <param name="existingPath">The existing path.</param>
        /// <returns><c>true</c> if [is new path valid] [the specified new path]; otherwise, <c>false</c>.</returns>
        private static bool IsNewPathValid(IFileSystem fileSystem, string newPath, string existingPath)
        {
            // Example: D:\Movies is the existing path
            // D:\ cannot be added
            // Neither can D:\Movies\Kids
            // A D:\Movies duplicate is ok here since that will be caught later

            if (string.Equals(newPath, existingPath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // If enforceSubPathRestriction is true, validate the D:\Movies\Kids scenario
            if (fileSystem.ContainsSubPath(existingPath, newPath))
            {
                return false;
            }

            // Validate the D:\ scenario
            if (fileSystem.ContainsSubPath(newPath, existingPath))
            {
                return false;
            }

            return true;
        }
    }
}
