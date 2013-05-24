using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Entities;
using System;

namespace MediaBrowser.Server.Implementations.Library.Resolvers.TV
{
    /// <summary>
    /// Class EpisodeResolver
    /// </summary>
    public class EpisodeResolver : BaseVideoResolver<Episode>
    {
        /// <summary>
        /// Resolves the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>Episode.</returns>
        protected override Episode Resolve(ItemResolveArgs args)
        {
            var season = args.Parent as Season;

            // If the parent is a Season or Series, then this is an Episode if the VideoResolver returns something
            if (season != null || args.Parent is Series)
            {
                Episode episode = null;

                if (args.IsDirectory)
                {
                    if (args.ContainsFileSystemEntryByName("video_ts"))
                    {
                        episode = new Episode
                        {
                            Path = args.Path,
                            VideoType = VideoType.Dvd
                        };
                    }
                    if (args.ContainsFileSystemEntryByName("bdmv"))
                    {
                        episode = new Episode
                        {
                            Path = args.Path,
                            VideoType = VideoType.BluRay
                        };
                    }
                }

                if (episode == null)
                {
                    episode = base.Resolve(args);
                }

                if (episode != null)
                {
                    episode.IndexNumber = TVUtils.GetEpisodeNumberFromFile(args.Path, season != null);
                    episode.IndexNumberEnd = TVUtils.GetEndingEpisodeNumberFromFile(args.Path);

                    if (season != null)
                    {
                        episode.ParentIndexNumber = season.IndexNumber;
                    }
                    
                    if (episode.ParentIndexNumber == null)
                    {
                        episode.ParentIndexNumber = TVUtils.GetSeasonNumberFromEpisodeFile(args.Path);
                    }
                }

                return episode;
            }

            return null;
        }

        /// <summary>
        /// Sets the initial item values.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="args">The args.</param>
        protected override void SetInitialItemValues(Episode item, ItemResolveArgs args)
        {
            base.SetInitialItemValues(item, args);

            //fill in our season and series ids
            var season = args.Parent as Season;
            if (season != null)
            {
                item.SeasonItemId = season.Id;
                var series = season.Parent as Series;
                if (series != null)
                {
                    item.SeriesItemId = series.Id;
                }
            }
            else
            {
                var series = args.Parent as Series;
                item.SeriesItemId = series != null ? series.Id : Guid.Empty;
            }
        }
    }
}
