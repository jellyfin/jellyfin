using System;
using System.IO;
using Emby.Naming.Audio;
using Emby.Naming.Common;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.IO;

namespace Emby.Server.Implementations.Library
{
    /// <summary>
    /// Provides the core resolver ignore rules.
    /// </summary>
    public class CoreResolutionIgnoreRule : IResolverIgnoreRule
    {
        private readonly NamingOptions _namingOptions;
        private readonly IServerApplicationPaths _serverApplicationPaths;

        /// <summary>
        /// Initializes a new instance of the <see cref="CoreResolutionIgnoreRule"/> class.
        /// </summary>
        /// <param name="namingOptions">The naming options.</param>
        /// <param name="serverApplicationPaths">The server application paths.</param>
        public CoreResolutionIgnoreRule(NamingOptions namingOptions, IServerApplicationPaths serverApplicationPaths)
        {
            _namingOptions = namingOptions;
            _serverApplicationPaths = serverApplicationPaths;
        }

        /// <inheritdoc />
        public bool ShouldIgnore(FileSystemMetadata fileInfo, BaseItem? parent)
        {
            // Don't ignore application folders
            if (fileInfo.FullName.Contains(_serverApplicationPaths.RootFolderPath, StringComparison.InvariantCulture))
            {
                return false;
            }

            if (IgnorePatterns.ShouldIgnore(fileInfo.FullName))
            {
                return true;
            }

            // Don't ignore top level folders
            if (fileInfo.IsDirectory
                && (parent is AggregateFolder || (parent?.IsTopParent ?? false)))
            {
                return false;
            }

            if (parent is null)
            {
                return false;
            }

            if (fileInfo.IsDirectory)
            {
                // Ignore extras for unsupported types
                return _namingOptions.AllExtrasTypesFolderNames.ContainsKey(fileInfo.Name)
                    && parent is not UserRootFolder;
            }

            // Don't resolve theme songs
            return Path.GetFileNameWithoutExtension(fileInfo.Name.AsSpan()).Equals(BaseItem.ThemeSongFileName, StringComparison.Ordinal)
                && AudioFileParser.IsAudioFile(fileInfo.Name, _namingOptions);
        }
    }
}
