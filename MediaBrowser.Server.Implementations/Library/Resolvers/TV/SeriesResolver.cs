using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Entities;
using System;
using System.IO;

namespace MediaBrowser.Server.Implementations.Library.Resolvers.TV
{
    /// <summary>
    /// Class SeriesResolver
    /// </summary>
    public class SeriesResolver : FolderResolver<Series>
    {
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
                
                // It's a Series if any of the following conditions are met:
                // series.xml exists
                // [tvdbid= is present in the path
                // TVUtils.IsSeriesFolder returns true
                var filename = Path.GetFileName(args.Path);

                if (string.IsNullOrEmpty(filename))
                {
                    return null;
                }

                // Without these movies that have the name season in them could cause the parent folder to be resolved as a series
                if (filename.IndexOf("[tmdbid=", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    return null;
                }
                
                if (args.ContainsMetaFileByName("series.xml") || filename.IndexOf("[tvdbid=", StringComparison.OrdinalIgnoreCase) != -1 || TVUtils.IsSeriesFolder(args.Path, args.FileSystemChildren, args.DirectoryService))
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
