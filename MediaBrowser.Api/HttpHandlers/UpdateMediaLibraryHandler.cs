using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MediaBrowser.Api.HttpHandlers
{
    /// <summary>
    /// Makes changes to the user's media library
    /// </summary>
    [Export(typeof(IHttpServerHandler))]
    public class UpdateMediaLibraryHandler : BaseActionHandler<Kernel>
    {
        /// <summary>
        /// Executes the action.
        /// </summary>
        /// <returns>Task.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        protected override Task ExecuteAction()
        {
            return Task.Run(() =>
            {
                var action = QueryString["action"];

                if (string.IsNullOrEmpty(action))
                {
                    throw new ArgumentNullException();
                }

                User user = null;

                if (!string.IsNullOrEmpty(QueryString["userId"]))
                {
                    user = ApiService.GetUserById(QueryString["userId"]);
                }

                if (action.Equals("AddVirtualFolder", StringComparison.OrdinalIgnoreCase))
                {
                    AddVirtualFolder(Uri.UnescapeDataString(QueryString["name"]), user);
                }

                if (action.Equals("RemoveVirtualFolder", StringComparison.OrdinalIgnoreCase))
                {
                    RemoveVirtualFolder(QueryString["name"], user);
                }

                if (action.Equals("RenameVirtualFolder", StringComparison.OrdinalIgnoreCase))
                {
                    RenameVirtualFolder(QueryString["name"], QueryString["newName"], user);
                }

                if (action.Equals("RemoveMediaPath", StringComparison.OrdinalIgnoreCase))
                {
                    RemoveMediaPath(QueryString["virtualFolderName"], QueryString["mediaPath"], user);
                }

                if (action.Equals("AddMediaPath", StringComparison.OrdinalIgnoreCase))
                {
                    AddMediaPath(QueryString["virtualFolderName"], QueryString["mediaPath"], user);
                }

                throw new ArgumentOutOfRangeException();
            });
        }

        /// <summary>
        /// Adds a virtual folder to either the default view or a user view
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="user">The user.</param>
        private void AddVirtualFolder(string name, User user)
        {
            name = FileSystem.GetValidFilename(name);

            var rootFolderPath = user != null ? user.RootFolderPath : Kernel.ApplicationPaths.DefaultUserViewsPath;
            var virtualFolderPath = Path.Combine(rootFolderPath, name);

            if (Directory.Exists(virtualFolderPath))
            {
                throw new ArgumentException("There is already a media collection with the name " + name + ".");
            }

            Directory.CreateDirectory(virtualFolderPath);
        }

        /// <summary>
        /// Adds an additional mediaPath to an existing virtual folder, within either the default view or a user view
        /// </summary>
        /// <param name="virtualFolderName">Name of the virtual folder.</param>
        /// <param name="path">The path.</param>
        /// <param name="user">The user.</param>
        private void AddMediaPath(string virtualFolderName, string path, User user)
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

            var rootFolderPath = user != null ? user.RootFolderPath : Kernel.ApplicationPaths.DefaultUserViewsPath;
            var virtualFolderPath = Path.Combine(rootFolderPath, virtualFolderName);

            ValidateNewMediaPath(rootFolderPath, path);

            var shortcutFilename = Path.GetFileNameWithoutExtension(path);

            var lnk = Path.Combine(virtualFolderPath, shortcutFilename + ".lnk");

            while (File.Exists(lnk))
            {
                shortcutFilename += "1";
                lnk = Path.Combine(virtualFolderPath, shortcutFilename + ".lnk");
            }

            FileSystem.CreateShortcut(lnk, path);
        }

        /// <summary>
        /// Validates that a new media path can be added
        /// </summary>
        /// <param name="currentViewRootFolderPath">The current view root folder path.</param>
        /// <param name="mediaPath">The media path.</param>
        private void ValidateNewMediaPath(string currentViewRootFolderPath, string mediaPath)
        {
            var duplicate = Directory.EnumerateFiles(Kernel.ApplicationPaths.RootFolderPath, "*.lnk", SearchOption.AllDirectories)
                .Select(FileSystem.ResolveShortcut)
                .FirstOrDefault(p => !IsNewPathValid(mediaPath, p));

            if (!string.IsNullOrEmpty(duplicate))
            {
                throw new ArgumentException(string.Format("The path cannot be added to the library because {0} already exists.", duplicate));
            }

            // Make sure the current root folder doesn't already have a shortcut to the same path
            duplicate = Directory.EnumerateFiles(currentViewRootFolderPath, "*.lnk", SearchOption.AllDirectories)
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
        /// <returns><c>true</c> if [is new path valid] [the specified new path]; otherwise, <c>false</c>.</returns>
        private bool IsNewPathValid(string newPath, string existingPath)
        {
            // Example: D:\Movies is the existing path
            // D:\ cannot be added
            // Neither can D:\Movies\Kids
            // A D:\Movies duplicate is ok here since that will be caught later

            if (newPath.Equals(existingPath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Validate the D:\Movies\Kids scenario
            if (newPath.StartsWith(existingPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
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

        /// <summary>
        /// Renames a virtual folder within either the default view or a user view
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="newName">The new name.</param>
        /// <param name="user">The user.</param>
        private void RenameVirtualFolder(string name, string newName, User user)
        {
            var rootFolderPath = user != null ? user.RootFolderPath : Kernel.ApplicationPaths.DefaultUserViewsPath;

            var currentPath = Path.Combine(rootFolderPath, name);
            var newPath = Path.Combine(rootFolderPath, newName);

            if (!Directory.Exists(currentPath))
            {
                throw new DirectoryNotFoundException("The media collection does not exist");
            }

            if (Directory.Exists(newPath))
            {
                throw new ArgumentException("There is already a media collection with the name " + newPath + ".");
            }

            Directory.Move(currentPath, newPath);
        }

        /// <summary>
        /// Deletes a virtual folder from either the default view or a user view
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="user">The user.</param>
        private void RemoveVirtualFolder(string name, User user)
        {
            var rootFolderPath = user != null ? user.RootFolderPath : Kernel.ApplicationPaths.DefaultUserViewsPath;
            var path = Path.Combine(rootFolderPath, name);

            if (!Directory.Exists(path))
            {
                throw new DirectoryNotFoundException("The media folder does not exist");
            }

            Directory.Delete(path, true);
        }

        /// <summary>
        /// Deletes a shortcut from within a virtual folder, within either the default view or a user view
        /// </summary>
        /// <param name="virtualFolderName">Name of the virtual folder.</param>
        /// <param name="mediaPath">The media path.</param>
        /// <param name="user">The user.</param>
        private void RemoveMediaPath(string virtualFolderName, string mediaPath, User user)
        {
            var rootFolderPath = user != null ? user.RootFolderPath : Kernel.ApplicationPaths.DefaultUserViewsPath;
            var path = Path.Combine(rootFolderPath, virtualFolderName);

            if (!Directory.Exists(path))
            {
                throw new DirectoryNotFoundException("The media folder does not exist");
            }

            var shortcut = Directory.EnumerateFiles(path, "*.lnk", SearchOption.AllDirectories).FirstOrDefault(f => FileSystem.ResolveShortcut(f).Equals(mediaPath, StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrEmpty(shortcut))
            {
                throw new DirectoryNotFoundException("The media folder does not exist");
            }
            File.Delete(shortcut);
        }
    }
}
