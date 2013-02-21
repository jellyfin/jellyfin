using System;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using System.ComponentModel.Composition;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Resolvers.TV
{
    [Export(typeof(IBaseItemResolver))]
    public class EpisodeResolver : BaseVideoResolver<Episode>
    {
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
