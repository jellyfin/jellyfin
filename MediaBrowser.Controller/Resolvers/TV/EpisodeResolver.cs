using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using System;

namespace MediaBrowser.Controller.Resolvers.TV
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
            // If the parent is a Season or Series, then this is an Episode if the VideoResolver returns something
            if (args.Parent is Season || args.Parent is Series)
            {
                if (args.IsDirectory)
                {
                    if (args.ContainsFileSystemEntryByName("video_ts"))
                    {
                        return new Episode
                            {
                                Path = args.Path,
                                VideoType = VideoType.Dvd
                            };
                    }
                    if (args.ContainsFileSystemEntryByName("bdmv"))
                    {
                        return new Episode
                            {
                                Path = args.Path,
                                VideoType = VideoType.BluRay
                            };
                    }
                }

                return base.Resolve(args);
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
