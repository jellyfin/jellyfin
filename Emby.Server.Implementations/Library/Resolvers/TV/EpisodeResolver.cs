#nullable disable

using System;
using System.Linq;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Controller.Resolvers;
using System.Collections.Generic;
using MediaBrowser.Model.IO;
using Emby.Naming.Video;
using MediaBrowser.Controller.Providers;

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
        /// <param name="libraryManager">The library manager.</param>
        public EpisodeResolver(ILibraryManager libraryManager)
            : base(libraryManager)
        {
        }

        /// <summary>
        /// Resolves the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>Episode.</returns>
        public override Episode Resolve(ItemResolveArgs args)
        {
            var parent = args.Parent;

            if (parent == null)
            {
                return null;
            }

            // Just in case the user decided to nest episodes.
            // Not officially supported but in some cases we can handle it.

            var season = parent as Season ?? parent.GetParents().OfType<Season>().FirstOrDefault();

            // If the parent is a Season or Series and the parent is not an extras folder, then this is an Episode if the VideoResolver returns something
            // Also handle flat tv folders
            if ((season != null ||
                 string.Equals(args.GetCollectionType(), CollectionType.TvShows, StringComparison.OrdinalIgnoreCase) ||
                 args.HasParent<Series>())
                && (parent is Series || !BaseItem.AllExtrasTypesFolderNames.Contains(parent.Name, StringComparer.OrdinalIgnoreCase)))
            {
                var episode = ResolveEpisode(args);

                if (episode != null)
                {
                    var series = parent as Series ?? parent.GetParents().OfType<Series>().FirstOrDefault();

                    if (series != null)
                    {
                        episode.SeriesId = series.Id;
                        episode.SeriesName = series.Name;
                    }

                    if (season != null)
                    {
                        episode.SeasonId = season.Id;
                        episode.SeasonName = season.Name;
                    }

                    // Assume season 1 if there's no season folder and a season number could not be determined
                    if (season == null && !episode.ParentIndexNumber.HasValue && (episode.IndexNumber.HasValue || episode.PremiereDate.HasValue))
                    {
                        episode.ParentIndexNumber = 1;
                    }
                }

                return episode;
            }

            return null;
        }

        private Episode ResolveEpisode(ItemResolveArgs args)
        {
            if (args.IsDirectory)
            {
                // Normally, ResolveVideo handles directories internally, testing if they are a BluRay or DVD directory. This strategy doesn't support that case, but it probably doesn't exist.
                var resolverResult = VideoListResolver.Resolve(args.FileSystemChildren.ToList(), LibraryManager.GetNamingOptions(), true).ToList();

                if (resolverResult.Count != 1)
                {
                    // Returning null here just means that LibraryManager.ResolvePaths will recurse into the directory. Any other videos will then get found by Resolve() above.
                    return null;
                }

                // TODO: handle owned photos
                // TODO: handle extras for the episode

                var info = resolverResult[0];
                var firstFile = info.Files[0];
                var item = new Episode
                {
                    Path = firstFile.Path,
                    ProductionYear = info.Year,
                    Name = info.Name,
                    AdditionalParts = info.Files.Skip(1).Select(i => i.Path).ToArray(), // What does this do? MovieResolver does it.
                    LocalAlternateVersions = info.AlternateVersions.Select(i => i.Path).ToArray() // What does this do? MovieResolver does it.
                };

                SetVideoType(item, firstFile);
                Set3DFormat(item, firstFile);

                return item;
            }
            else
            {
                return ResolveVideo<Episode>(args, false);
            }
        }
    }
}
