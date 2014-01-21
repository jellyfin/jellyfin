using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Entities;
using System.Linq;

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
            var parent = args.Parent;
            var season = parent as Season;

            // Just in case the user decided to nest episodes. 
            // Not officially supported but in some cases we can handle it.
            if (season == null)
            {
                if (parent != null)
                {
                    season = parent.Parents.OfType<Season>().FirstOrDefault();
                }
            }

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
    }
}
