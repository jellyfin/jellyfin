#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.IO;
using Emby.Naming.Common;
using Emby.Naming.TV;
using Emby.Naming.Video;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Library.Resolvers.TV
{
    /// <summary>
    /// Class SeriesResolver.
    /// </summary>
    public class SeriesResolver : GenericFolderResolver<Series>
    {
        private readonly ILogger<SeriesResolver> _logger;
        private readonly NamingOptions _namingOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="SeriesResolver"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="namingOptions">The naming options.</param>
        public SeriesResolver(ILogger<SeriesResolver> logger,  NamingOptions namingOptions)
        {
            _logger = logger;
            _namingOptions = namingOptions;
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

                var seriesInfo = Naming.TV.SeriesResolver.Resolve(_namingOptions, args.Path);

                var collectionType = args.GetCollectionType();
                if (collectionType == CollectionType.tvshows)
                {
                    // TODO refactor into separate class or something, this is copied from LibraryManager.GetConfiguredContentType
                    var configuredContentType = args.GetConfiguredContentType();
                    if (configuredContentType != CollectionType.tvshows)
                    {
                        return new Series
                        {
                            Path = args.Path,
                            Name = seriesInfo.Name
                        };
                    }
                }
                else if (collectionType is null)
                {
                    if (args.ContainsFileSystemEntryByName("tvshow.nfo"))
                    {
                        if (args.Parent is not null && args.Parent.IsRoot)
                        {
                            // For now, return null, but if we want to allow this in the future then add some additional checks to guard against a misplaced tvshow.nfo
                            return null;
                        }

                        return new Series
                        {
                            Path = args.Path,
                            Name = seriesInfo.Name
                        };
                    }

                    if (args.Parent is not null && args.Parent.IsRoot)
                    {
                        return null;
                    }

                    if (IsSeriesFolder(args.Path, args.FileSystemChildren, false))
                    {
                        return new Series
                        {
                            Path = args.Path,
                            Name = seriesInfo.Name
                        };
                    }
                }
            }

            return null;
        }

        private bool IsSeriesFolder(
            string path,
            IEnumerable<FileSystemMetadata> fileSystemChildren,
            bool isTvContentType)
        {
            foreach (var child in fileSystemChildren)
            {
                if (child.IsDirectory)
                {
                    if (IsSeasonFolder(child.FullName, isTvContentType))
                    {
                        _logger.LogDebug("{Path} is a series because of season folder {Dir}.", path, child.FullName);
                        return true;
                    }
                }
                else
                {
                    string fullName = child.FullName;
                    if (VideoResolver.IsVideoFile(path, _namingOptions))
                    {
                        if (isTvContentType)
                        {
                            return true;
                        }

                        var namingOptions = _namingOptions;

                        var episodeResolver = new Naming.TV.EpisodeResolver(namingOptions);

                        var episodeInfo = episodeResolver.Resolve(fullName, false, true, false, fillExtendedInfo: false);
                        if (episodeInfo is not null && episodeInfo.EpisodeNumber.HasValue)
                        {
                            return true;
                        }
                    }
                }
            }

            _logger.LogDebug("{Path} is not a series folder.", path);
            return false;
        }

        /// <summary>
        /// Determines whether [is season folder] [the specified path].
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="isTvContentType">if set to <c>true</c> [is tv content type].</param>
        /// <returns><c>true</c> if [is season folder] [the specified path]; otherwise, <c>false</c>.</returns>
        private static bool IsSeasonFolder(string path, bool isTvContentType)
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
            var justName = Path.GetFileName(path.AsSpan());

            var imdbId = justName.GetAttributeValue("imdbid");
            item.TrySetProviderId(MetadataProvider.Imdb, imdbId);

            var tvdbId = justName.GetAttributeValue("tvdbid");
            item.TrySetProviderId(MetadataProvider.Tvdb, tvdbId);

            var tvmazeId = justName.GetAttributeValue("tvmazeid");
            item.TrySetProviderId(MetadataProvider.TvMaze, tvmazeId);

            var tmdbId = justName.GetAttributeValue("tmdbid");
            item.TrySetProviderId(MetadataProvider.Tmdb, tmdbId);

            var anidbId = justName.GetAttributeValue("anidbid");
            item.TrySetProviderId("AniDB", anidbId);

            var aniListId = justName.GetAttributeValue("anilistid");
            item.TrySetProviderId("AniList", aniListId);

            var aniSearchId = justName.GetAttributeValue("anisearchid");
            item.TrySetProviderId("AniSearch", aniSearchId);
        }
    }
}
