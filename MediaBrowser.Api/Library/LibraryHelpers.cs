using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.IO;
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
        private const string ShortcutFileExtension = ".lnk";
        private const string ShortcutFileSearch = "*.lnk";

        /// <summary>
        /// Adds the virtual folder.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="collectionType">Type of the collection.</param>
        /// <param name="user">The user.</param>
        /// <param name="appPaths">The app paths.</param>
        /// <exception cref="System.ArgumentException">There is already a media collection with the name  + name + .</exception>
        public static void AddVirtualFolder(string name, string collectionType, User user, IServerApplicationPaths appPaths)
        {
            name = FileSystem.GetValidFilename(name);

            var rootFolderPath = user != null ? user.RootFolderPath : appPaths.DefaultUserViewsPath;
            var virtualFolderPath = Path.Combine(rootFolderPath, name);

            if (Directory.Exists(virtualFolderPath))
            {
                throw new ArgumentException("There is already a media collection with the name " + name + ".");
            }

            Directory.CreateDirectory(virtualFolderPath);

            if (!string.IsNullOrEmpty(collectionType))
            {
                var path = Path.Combine(virtualFolderPath, collectionType + ".collection");

                File.Create(path);
            }
        }

        /// <summary>
        /// Removes the virtual folder.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="user">The user.</param>
        /// <param name="appPaths">The app paths.</param>
        /// <exception cref="System.IO.DirectoryNotFoundException">The media folder does not exist</exception>
        public static void RemoveVirtualFolder(string name, User user, IServerApplicationPaths appPaths)
        {
            var rootFolderPath = user != null ? user.RootFolderPath : appPaths.DefaultUserViewsPath;
            var path = Path.Combine(rootFolderPath, name);

            if (!Directory.Exists(path))
            {
                throw new DirectoryNotFoundException("The media folder does not exist");
            }

            Directory.Delete(path, true);
        }

        /// <summary>
        /// Renames the virtual folder.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="newName">The new name.</param>
        /// <param name="user">The user.</param>
        /// <param name="appPaths">The app paths.</param>
        /// <exception cref="System.IO.DirectoryNotFoundException">The media collection does not exist</exception>
        /// <exception cref="System.ArgumentException">There is already a media collection with the name  + newPath + .</exception>
        public static void RenameVirtualFolder(string name, string newName, User user, IServerApplicationPaths appPaths)
        {
            var rootFolderPath = user != null ? user.RootFolderPath : appPaths.DefaultUserViewsPath;

            var currentPath = Path.Combine(rootFolderPath, name);
            var newPath = Path.Combine(rootFolderPath, newName);

            if (!Directory.Exists(currentPath))
            {
                throw new DirectoryNotFoundException("The media collection does not exist");
            }

            if (!string.Equals(currentPath, newPath, StringComparison.OrdinalIgnoreCase) && Directory.Exists(newPath))
            {
                throw new ArgumentException("There is already a media collection with the name " + newPath + ".");
            }
            //Only make a two-phase move when changing capitalization
            if (string.Equals(currentPath, newPath, StringComparison.OrdinalIgnoreCase))
            {
                //Create an unique name
                var temporaryName = Guid.NewGuid().ToString();
                var temporaryPath = Path.Combine(rootFolderPath, temporaryName);
                Directory.Move(currentPath,temporaryPath);
                currentPath = temporaryPath;
            }

            Directory.Move(currentPath, newPath);
        }

        /// <summary>
        /// Deletes a shortcut from within a virtual folder, within either the default view or a user view
        /// </summary>
        /// <param name="virtualFolderName">Name of the virtual folder.</param>
        /// <param name="mediaPath">The media path.</param>
        /// <param name="user">The user.</param>
        /// <param name="appPaths">The app paths.</param>
        /// <exception cref="System.IO.DirectoryNotFoundException">The media folder does not exist</exception>
        public static void RemoveMediaPath(string virtualFolderName, string mediaPath, User user, IServerApplicationPaths appPaths)
        {
            var rootFolderPath = user != null ? user.RootFolderPath : appPaths.DefaultUserViewsPath;
            var path = Path.Combine(rootFolderPath, virtualFolderName);

            if (!Directory.Exists(path))
            {
                throw new DirectoryNotFoundException(string.Format("The media collection {0} does not exist", virtualFolderName));
            }

            var shortcut = Directory.EnumerateFiles(path, ShortcutFileSearch, SearchOption.AllDirectories).FirstOrDefault(f => FileSystem.ResolveShortcut(f).Equals(mediaPath, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(shortcut))
            {
                File.Delete(shortcut);
            }
        }

        /// <summary>
        /// Adds an additional mediaPath to an existing virtual folder, within either the default view or a user view
        /// </summary>
        /// <param name="virtualFolderName">Name of the virtual folder.</param>
        /// <param name="path">The path.</param>
        /// <param name="user">The user.</param>
        /// <param name="appPaths">The app paths.</param>
        /// <exception cref="System.ArgumentException">The path is not valid.</exception>
        /// <exception cref="System.IO.DirectoryNotFoundException">The path does not exist.</exception>
        public static void AddMediaPath(string virtualFolderName, string path, User user, IServerApplicationPaths appPaths)
        {
            if (!Path.IsPathRooted(path))
            {
                throw new ArgumentException("The path is not valid.");
            }

            if (!Directory.Exists(path))
            {
                throw new DirectoryNotFoundException("The path does not exist.");
            }

            // Strip off trailing slash, but not on drives
            path = path.TrimEnd(Path.DirectorySeparatorChar);
            if (path.EndsWith(":", StringComparison.OrdinalIgnoreCase))
            {
                path += Path.DirectorySeparatorChar;
            }

            var rootFolderPath = user != null ? user.RootFolderPath : appPaths.DefaultUserViewsPath;
            var virtualFolderPath = Path.Combine(rootFolderPath, virtualFolderName);

            ValidateNewMediaPath(rootFolderPath, path, appPaths);

            var shortcutFilename = Path.GetFileNameWithoutExtension(path);

            var lnk = Path.Combine(virtualFolderPath, shortcutFilename + ShortcutFileExtension);

            while (File.Exists(lnk))
            {
                shortcutFilename += "1";
                lnk = Path.Combine(virtualFolderPath, shortcutFilename + ShortcutFileExtension);
            }

            FileSystem.CreateShortcut(lnk, path);
        }

        /// <summary>
        /// Validates that a new media path can be added
        /// </summary>
        /// <param name="currentViewRootFolderPath">The current view root folder path.</param>
        /// <param name="mediaPath">The media path.</param>
        /// <param name="appPaths">The app paths.</param>
        /// <exception cref="System.ArgumentException"></exception>
        private static void ValidateNewMediaPath(string currentViewRootFolderPath, string mediaPath, IServerApplicationPaths appPaths)
        {
            var duplicate = Directory.EnumerateFiles(appPaths.RootFolderPath, ShortcutFileSearch, SearchOption.AllDirectories)
                .Select(FileSystem.ResolveShortcut)
                .FirstOrDefault(p => !IsNewPathValid(mediaPath, p, false));

            if (!string.IsNullOrEmpty(duplicate))
            {
                throw new ArgumentException(string.Format("The path cannot be added to the library because {0} already exists.", duplicate));
            }

            // Don't allow duplicate sub-paths within the same user library, or it will result in duplicate items
            // See comments in IsNewPathValid
            duplicate = Directory.EnumerateFiles(currentViewRootFolderPath, ShortcutFileSearch, SearchOption.AllDirectories)
              .Select(FileSystem.ResolveShortcut)
              .FirstOrDefault(p => !IsNewPathValid(mediaPath, p, true));

            if (!string.IsNullOrEmpty(duplicate))
            {
                throw new ArgumentException(string.Format("The path cannot be added to the library because {0} already exists.", duplicate));
            }
            
            // Make sure the current root folder doesn't already have a shortcut to the same path
            duplicate = Directory.EnumerateFiles(currentViewRootFolderPath, ShortcutFileSearch, SearchOption.AllDirectories)
                .Select(FileSystem.ResolveShortcut)
                .FirstOrDefault(p => mediaPath.Equals(p, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(duplicate))
            {
                throw new ArgumentException(string.Format("The path {0} already exists in the library", mediaPath));
            }
        }

        /// <summary>
        /// Validates that a new path can be added based on an existing path
        /// </summary>
        /// <param name="newPath">The new path.</param>
        /// <param name="existingPath">The existing path.</param>
        /// <param name="enforceSubPathRestriction">if set to <c>true</c> [enforce sub path restriction].</param>
        /// <returns><c>true</c> if [is new path valid] [the specified new path]; otherwise, <c>false</c>.</returns>
        private static bool IsNewPathValid(string newPath, string existingPath, bool enforceSubPathRestriction)
        {
            // Example: D:\Movies is the existing path
            // D:\ cannot be added
            // Neither can D:\Movies\Kids
            // A D:\Movies duplicate is ok here since that will be caught later

            if (newPath.Equals(existingPath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // If enforceSubPathRestriction is true, validate the D:\Movies\Kids scenario
            if (enforceSubPathRestriction && newPath.StartsWith(existingPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Validate the D:\ scenario
            if (existingPath.StartsWith(newPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }
    }
}
