using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Naming.Common;
using MediaBrowser.Naming.TV;
using MediaBrowser.Server.Implementations.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommonIO;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Configuration;

namespace MediaBrowser.Server.Implementations.Library.Resolvers.TV
{
    /// <summary>
    /// Class SeriesResolver
    /// </summary>
    public class SeriesResolver : FolderResolver<Series>
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _logger;
        private readonly ILibraryManager _libraryManager;

        public SeriesResolver(IFileSystem fileSystem, ILogger logger, ILibraryManager libraryManager)
        {
            _fileSystem = fileSystem;
            _logger = logger;
            _libraryManager = libraryManager;
        }

        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override ResolverPriority Priority
        {
            get
            {
                return ResolverPriority.Second;
            }
        }

        /// <summary>
        /// Resolves the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>Series.</returns>
        protected override Series Resolve(ItemResolveArgs args)
        {
            if (args.IsDirectory)
            {
                var collectionType = args.GetCollectionType();
                if (string.Equals(collectionType, CollectionType.TvShows, StringComparison.OrdinalIgnoreCase))
                {
                    if (args.HasParent<Series>())
                    {
                        return null;
                    }

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
                else
                {
                    if (string.IsNullOrWhiteSpace(collectionType))
                    {
                        if (args.HasParent<Series>())
                        {
                            return null;
                        }

                        if (args.Parent.IsRoot)
                        {
                            return null;
                        }
                        if (IsSeriesFolder(args.Path, args.FileSystemChildren, args.DirectoryService, _fileSystem, _logger, _libraryManager, args.GetLibraryOptions(), false))
                        {
                            return new Series
                            {
                                Path = args.Path,
                                Name = Path.GetFileName(args.Path)
                            };
                        }
                    }
                }
            }

            return null;
        }

        public static bool IsSeriesFolder(string path,
            IEnumerable<FileSystemMetadata> fileSystemChildren,
            IDirectoryService directoryService,
            IFileSystem fileSystem,
            ILogger logger,
            ILibraryManager libraryManager,
            LibraryOptions libraryOptions,
            bool isTvContentType)
        {
            foreach (var child in fileSystemChildren)
            {
                var attributes = child.Attributes;

                //if ((attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                //{
                //    //logger.Debug("Igoring series file or folder marked hidden: {0}", child.FullName);
                //    continue;
                //}

                // Can't enforce this because files saved by Bitcasa are always marked System
                //if ((attributes & FileAttributes.System) == FileAttributes.System)
                //{
                //    logger.Debug("Igoring series subfolder marked system: {0}", child.FullName);
                //    continue;
                //}

                if ((attributes & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    if (IsSeasonFolder(child.FullName, isTvContentType, libraryManager))
                    {
                        //logger.Debug("{0} is a series because of season folder {1}.", path, child.FullName);
                        return true;
                    }
                }
                else
                {
                    string fullName = child.FullName;
                    if (libraryManager.IsVideoFile(fullName, libraryOptions))
                    {
                        if (isTvContentType)
                        {
                            return true;
                        }

                        var namingOptions = ((LibraryManager)libraryManager).GetNamingOptions();

                        // In mixed folders we need to be conservative and avoid expressions that may result in false positives (e.g. movies with numbers in the title)
                        if (!isTvContentType)
                        {
                            namingOptions.EpisodeExpressions = namingOptions.EpisodeExpressions
                                .Where(i => i.IsNamed && !i.IsOptimistic)
                                .ToList();
                        }

                        var episodeResolver = new Naming.TV.EpisodeResolver(namingOptions, new PatternsLogger());
                        var episodeInfo = episodeResolver.Resolve(fullName, false, false);
                        if (episodeInfo != null && episodeInfo.EpisodeNumber.HasValue)
                        {
                            return true;
                        }
                    }
                }
            }

            logger.Debug("{0} is not a series folder.", path);
            return false;
        }

        /// <summary>
        /// Determines whether [is place holder] [the specified path].
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns><c>true</c> if [is place holder] [the specified path]; otherwise, <c>false</c>.</returns>
        /// <exception cref="System.ArgumentNullException">path</exception>
        private static bool IsVideoPlaceHolder(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
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
            var namingOptions = ((LibraryManager)libraryManager).GetNamingOptions();

            var seasonNumber = new SeasonPathParser(namingOptions, new RegexProvider()).Parse(path, isTvContentType, isTvContentType).SeasonNumber;

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
        private void SetProviderIdFromPath(Series item, string path)
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
