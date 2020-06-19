using System;
using System.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.IO;

namespace Emby.Server.Implementations.Library
{
    /// <summary>
    /// Provides the core resolver ignore rules
    /// </summary>
    public class CoreResolutionIgnoreRule : IResolverIgnoreRule
    {
        private readonly ILibraryManager _libraryManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="CoreResolutionIgnoreRule"/> class.
        /// </summary>
        /// <param name="libraryManager">The library manager.</param>
        public CoreResolutionIgnoreRule(ILibraryManager libraryManager)
        {
            _libraryManager = libraryManager;
        }

        /// <inheritdoc />
        public bool ShouldIgnore(FileSystemMetadata fileInfo, BaseItem parent)
        {
            // Don't ignore top level folders
            if (fileInfo.IsDirectory && parent is AggregateFolder)
            {
                return false;
            }

            if (IgnorePatterns.ShouldIgnore(fileInfo.FullName))
            {
                return true;
            }

            var filename = fileInfo.Name;

            if (fileInfo.IsDirectory)
            {
                if (parent != null)
                {
                    // Ignore trailer folders but allow it at the collection level
                    if (string.Equals(filename, BaseItem.TrailerFolderName, StringComparison.OrdinalIgnoreCase)
                        && !(parent is AggregateFolder)
                        && !(parent is UserRootFolder))
                    {
                        return true;
                    }

                    if (string.Equals(filename, BaseItem.ThemeVideosFolderName, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    if (string.Equals(filename, BaseItem.ThemeSongsFolderName, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            else
            {
                if (parent != null)
                {
                    // Don't resolve these into audio files
                    if (string.Equals(Path.GetFileNameWithoutExtension(filename), BaseItem.ThemeSongFilename)
                        && _libraryManager.IsAudioFile(filename))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
