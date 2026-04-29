using System;
using System.Collections.Concurrent;
using System.IO;
using Emby.Naming.Audio;
using Emby.Naming.Common;
using Emby.Naming.Video;
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
        private static readonly ConcurrentDictionary<string, bool> _ghostDirectoryCache = new();

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
                if (ShouldIgnoreGhostMetadataOnlyDirectory(fileInfo.FullName))
                {
                    return true;
                }

                // Ignore extras for unsupported types
                return _namingOptions.AllExtrasTypesFolderNames.ContainsKey(fileInfo.Name)
                    && parent is not UserRootFolder;
            }

            // Don't resolve theme songs
            return Path.GetFileNameWithoutExtension(fileInfo.Name.AsSpan()).Equals(BaseItem.ThemeSongFileName, StringComparison.Ordinal)
                && AudioFileParser.IsAudioFile(fileInfo.Name, _namingOptions);
        }

        private bool ShouldIgnoreGhostMetadataOnlyDirectory(string fullPath)
        {
            return _ghostDirectoryCache.GetOrAdd(fullPath, ComputeGhostMetadataOnlyDirectory);
        }

        private bool ComputeGhostMetadataOnlyDirectory(string path)
        {
            FileSystemInfo[] entries;
            try
            {
                var info = new DirectoryInfo(path);
                if (!info.Exists)
                {
                    return false;
                }

                entries = info.GetFileSystemInfos();
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch (DirectoryNotFoundException)
            {
                return false;
            }

            var hasFile = false;
            var hasMetadataHint = false;

            for (var i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                if ((entry.Attributes & FileAttributes.Directory) != 0)
                {
                    return false;
                }

                hasFile = true;

                var name = entry.Name;
                if (VideoResolver.IsVideoFile(name, _namingOptions)
                    || VideoResolver.IsStubFile(name, _namingOptions)
                    || AudioFileParser.IsAudioFile(name, _namingOptions))
                {
                    return false;
                }

                if (!hasMetadataHint)
                {
                    var extension = Path.GetExtension(name.AsSpan());
                    if (IsMetadataHintExtension(extension))
                    {
                        hasMetadataHint = true;
                    }
                }
            }

            return hasFile && hasMetadataHint;
        }

        private static bool IsMetadataHintExtension(ReadOnlySpan<char> extension)
        {
            return extension.Equals(".nfo", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".info", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".xml", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".json", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".webp", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".gif", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".srt", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".ass", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".ssa", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".sub", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".vtt", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".txt", StringComparison.OrdinalIgnoreCase);
        }
    }
}
