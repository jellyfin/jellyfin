using System;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using System.Linq;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Server.Implementations.Library.Resolvers.TV
{
    /// <summary>
    /// Class EpisodeResolver
    /// </summary>
    public class EpisodeResolver : BaseVideoResolver<Episode>
    {
        public EpisodeResolver(ILibraryManager libraryManager) : base(libraryManager)
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

            if (parent == null)
            {
                return null;
            }

            var season = parent as Season;
            // Just in case the user decided to nest episodes. 
            // Not officially supported but in some cases we can handle it.
            if (season == null)
            {
                season = parent.GetParents().OfType<Season>().FirstOrDefault();
            }

            // If the parent is a Season or Series, then this is an Episode if the VideoResolver returns something
            // Also handle flat tv folders
            if (season != null || 
                string.Equals(args.GetCollectionType(), CollectionType.TvShows, StringComparison.OrdinalIgnoreCase) || 
                args.HasParent<Series>())
            {
                var episode = ResolveVideo<Episode>(args, false);

                if (episode != null)
                {
                    var series = parent as Series;
                    if (series == null)
                    {
                        series = parent.GetParents().OfType<Series>().FirstOrDefault();
                    }

                    if (series != null)
                    {
                        episode.SeriesId = series.Id;
                        episode.SeriesName = series.Name;
                        episode.SeriesSortName = series.SortName;
                    }
                    if (season != null)
                    {
                        episode.SeasonId = season.Id;
                        episode.SeasonName = season.Name;
                    }
                }

                return episode;
            }

            return null;
        }
    }
}
