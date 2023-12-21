#nullable disable

using System;
using System.Linq;
using Emby.Naming.Common;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Library.Resolvers.TV
{
    /// <summary>
    /// Class EpisodeResolver.
    /// </summary>
    public class EpisodeResolver : BaseVideoResolver<Episode>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EpisodeResolver"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="namingOptions">The naming options.</param>
        /// <param name="directoryService">The directory service.</param>
        public EpisodeResolver(ILogger<EpisodeResolver> logger, NamingOptions namingOptions, IDirectoryService directoryService)
            : base(logger, namingOptions, directoryService)
        {
        }

        /// <summary>
        /// Resolves the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>Episode.</returns>
        protected override Episode Resolve(ItemResolveArgs args)
        {
            var parent = args.Parent;

            if (parent is null)
            {
                return null;
            }

            // Just in case the user decided to nest episodes.
            // Not officially supported but in some cases we can handle it.

            var season = parent as Season ?? parent.GetParents().OfType<Season>().FirstOrDefault();

            // If the parent is a Season or Series and the parent is not an extras folder, then this is an Episode if the VideoResolver returns something
            // Also handle flat tv folders
            if (season is not null
                || args.GetCollectionType() == CollectionType.tvshows
                || args.HasParent<Series>())
            {
                var episode = ResolveVideo<Episode>(args, false);

                // Ignore extras
                if (episode is null || episode.ExtraType is not null)
                {
                    return null;
                }

                var series = parent as Series ?? parent.GetParents().OfType<Series>().FirstOrDefault();

                if (series is not null)
                {
                    episode.SeriesId = series.Id;
                    episode.SeriesName = series.Name;
                }

                if (season is not null)
                {
                    episode.SeasonId = season.Id;
                    episode.SeasonName = season.Name;
                }

                // Assume season 1 if there's no season folder and a season number could not be determined
                if (season is null && !episode.ParentIndexNumber.HasValue && (episode.IndexNumber.HasValue || episode.PremiereDate.HasValue))
                {
                    episode.ParentIndexNumber = 1;
                }

                return episode;
            }

            return null;
        }
    }
}
