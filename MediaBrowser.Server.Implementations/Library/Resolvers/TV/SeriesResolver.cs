using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
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

        public SeriesResolver(IFileSystem fileSystem, ILogger logger)
        {
            _fileSystem = fileSystem;
            _logger = logger;
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
                if (args.Parent == null || args.Parent.IsRoot)
                {
                    return null;
                }
                
                // Optimization to avoid running these tests against Seasons
                if (args.Parent is Series || args.Parent is Season || args.Parent is MusicArtist || args.Parent is MusicAlbum)
                {
                    return null;
                }

                var collectionType = args.GetCollectionType();

                // If there's a collection type and it's not tv, it can't be a series
                if (!string.IsNullOrEmpty(collectionType) &&
                    !string.Equals(collectionType, CollectionType.TvShows, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(collectionType, CollectionType.BoxSets, StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                if (TVUtils.IsSeriesFolder(args.Path, string.Equals(collectionType, CollectionType.TvShows, StringComparison.OrdinalIgnoreCase), args.FileSystemChildren, args.DirectoryService, _fileSystem, _logger))
                {
                    return new Series();
                }
            }

            return null;
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
