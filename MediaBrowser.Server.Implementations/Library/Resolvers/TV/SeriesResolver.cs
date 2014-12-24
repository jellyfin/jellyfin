using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Naming.Common;
using MediaBrowser.Naming.TV;
using System;
using System.Collections.Generic;
using System.IO;

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
                // Avoid expensive tests against VF's and all their children by not allowing this
                if (args.Parent.IsRoot)
                {
                    return null;
                }

                var collectionType = args.GetCollectionType();

                // If there's a collection type and it's not tv, it can't be a series
                if (!string.Equals(collectionType, CollectionType.TvShows, StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                if (args.HasParent<Series>() || args.HasParent<Season>())
                {
                    return null;
                }

                if (IsSeriesFolder(args.Path, args.FileSystemChildren, args.DirectoryService, _fileSystem, _logger, _libraryManager))
                {
                    return new Series
                    {
                        Path = args.Path,
                        Name = Path.GetFileName(args.Path)
                    };
                }
            }

            return null;
        }

        /// <summary>
        /// Determines whether [is series folder] [the specified path].
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="fileSystemChildren">The file system children.</param>
        /// <param name="directoryService">The directory service.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <returns><c>true</c> if [is series folder] [the specified path]; otherwise, <c>false</c>.</returns>
        public static bool IsSeriesFolder(string path, IEnumerable<FileSystemInfo> fileSystemChildren, IDirectoryService directoryService, IFileSystem fileSystem, ILogger logger, ILibraryManager libraryManager)
        {
            foreach (var child in fileSystemChildren)
            {
                var attributes = child.Attributes;

                if ((attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                {
                    //logger.Debug("Igoring series file or folder marked hidden: {0}", child.FullName);
                    continue;
                }

                // Can't enforce this because files saved by Bitcasa are always marked System
                //if ((attributes & FileAttributes.System) == FileAttributes.System)
                //{
                //    logger.Debug("Igoring series subfolder marked system: {0}", child.FullName);
                //    continue;
                //}

                if ((attributes & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    if (IsSeasonFolder(child.FullName))
                    {
                        //logger.Debug("{0} is a series because of season folder {1}.", path, child.FullName);
                        return true;
                    }
                }
                else
                {
                    var fullName = child.FullName;

                    if (libraryManager.IsVideoFile(fullName) || IsVideoPlaceHolder(fullName))
                    {
                        return true;
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
        /// <returns><c>true</c> if [is season folder] [the specified path]; otherwise, <c>false</c>.</returns>
        private static bool IsSeasonFolder(string path)
        {
            var seasonNumber = new SeasonPathParser(new ExtendedNamingOptions(), new RegexProvider()).Parse(path, true).SeasonNumber;

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
