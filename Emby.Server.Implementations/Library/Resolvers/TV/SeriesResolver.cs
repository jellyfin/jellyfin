#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.IO;
using Emby.Naming.TV;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Library.Resolvers.TV
{
    /// <summary>
    /// Class SeriesResolver.
    /// </summary>
    public class SeriesResolver : FolderResolver<Series>
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _logger;
        private readonly ILibraryManager _libraryManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="SeriesResolver"/> class.
        /// </summary>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="libraryManager">The library manager.</param>
        public SeriesResolver(IFileSystem fileSystem, ILogger<SeriesResolver> logger, ILibraryManager libraryManager)
        {
            _fileSystem = fileSystem;
            _logger = logger;
            _libraryManager = libraryManager;
        }

        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override ResolverPriority Priority => ResolverPriority.Second;

        /// <summary>
        /// Resolves the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>Series.</returns>
        protected override Series Resolve(ItemResolveArgs args)
        {
            if (args.IsDirectory)
            {
                if (args.HasParent<Series>() || args.HasParent<Season>())
                {
                    return null;
                }

                var collectionType = args.GetCollectionType();
                if (string.Equals(collectionType, CollectionType.TvShows, StringComparison.OrdinalIgnoreCase))
                {
                    //if (args.ContainsFileSystemEntryByName("tvshow.nfo"))
                    //{
                    //    return new Series
                    //    {
                    //        Path = args.Path,
                    //        Name = Path.GetFileName(args.Path)
                    //    };
                    //}

                    var configuredContentType = _libraryManager.GetConfiguredContentType(args.Path);
                    if (!string.Equals(configuredContentType, CollectionType.TvShows, StringComparison.OrdinalIgnoreCase))
                    {
                        return new Series
                        {
                            Path = args.Path,
                            Name = Path.GetFileName(args.Path)
                        };
                    }
                }
                else if (string.IsNullOrEmpty(collectionType))
                {
                    if (args.ContainsFileSystemEntryByName("tvshow.nfo"))
                    {
                        if (args.Parent != null && args.Parent.IsRoot)
                        {
                            // For now, return null, but if we want to allow this in the future then add some additional checks to guard against a misplaced tvshow.nfo
                            return null;
                        }

                        return new Series
                        {
                            Path = args.Path,
                            Name = Path.GetFileName(args.Path)
                        };
                    }

                    if (args.Parent != null && args.Parent.IsRoot)
                    {
                        return null;
                    }

                    if (IsSeriesFolder(args.Path, args.FileSystemChildren, args.DirectoryService, _fileSystem, _logger, _libraryManager, false))
                    {
                        return new Series
                        {
                            Path = args.Path,
                            Name = Path.GetFileName(args.Path)
                        };
                    }
                }
            }

            return null;
        }

        public static bool IsSeriesFolder(
            string path,
            IEnumerable<FileSystemMetadata> fileSystemChildren,
            IDirectoryService directoryService,
            IFileSystem fileSystem,
            ILogger logger,
            ILibraryManager libraryManager,
            bool isTvContentType)
        {
            foreach (var child in fileSystemChildren)
            {
                if (child.IsDirectory)
                {
                    if (IsSeasonFolder(child.FullName, isTvContentType, libraryManager))
                    {
                        logger.LogDebug("{Path} is a series because of season folder {Dir}.", path, child.FullName);
                        return true;
                    }
                }
                else
                {
                    string fullName = child.FullName;
                    if (libraryManager.IsVideoFile(fullName))
                    {
                        if (isTvContentType)
                        {
                            return true;
                        }

                        var namingOptions = ((LibraryManager)libraryManager).GetNamingOptions();

                        var episodeResolver = new Naming.TV.EpisodeResolver(namingOptions);

                        var episodeInfo = episodeResolver.Resolve(fullName, false, true, false, fillExtendedInfo: false);
                        if (episodeInfo != null && episodeInfo.EpisodeNumber.HasValue)
                        {
                            return true;
                        }
                    }
                }
            }

            logger.LogDebug("{Path} is not a series folder.", path);
            return false;
        }

        /// <summary>
        /// Determines whether [is place holder] [the specified path].
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns><c>true</c> if [is place holder] [the specified path]; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">path</exception>
        private static bool IsVideoPlaceHolder(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            var extension = Path.GetExtension(path);

            return string.Equals(extension, ".disc", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Determines whether [is season folder] [the specified path].
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="isTvContentType">if set to <c>true</c> [is tv content type].</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <returns><c>true</c> if [is season folder] [the specified path]; otherwise, <c>false</c>.</returns>
        private static bool IsSeasonFolder(string path, bool isTvContentType, ILibraryManager libraryManager)
        {
            var seasonNumber = SeasonPathParser.Parse(path, isTvContentType, isTvContentType).SeasonNumber;

            return seasonNumber.HasValue;
        }

        /// <summary>
        /// Sets the initial item values.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="args">The args.</param>
        protected override void SetInitialItemValues(Series item, ItemResolveArgs args)
        {
            base.SetInitialItemValues(item, args);

            SetProviderIdFromPath(item, args.Path);
        }

        /// <summary>
        /// Sets the provider id from path.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="path">The path.</param>
        private static void SetProviderIdFromPath(Series item, string path)
        {
            var justName = Path.GetFileName(path);

            var id = justName.GetAttributeValue("tvdbid");

            if (!string.IsNullOrEmpty(id))
            {
                item.SetProviderId(MetadataProviders.Tvdb, id);
            }
        }
    }
}
