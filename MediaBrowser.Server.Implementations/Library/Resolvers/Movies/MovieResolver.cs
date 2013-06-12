using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MediaBrowser.Server.Implementations.Library.Resolvers.Movies
{
    /// <summary>
    /// Class MovieResolver
    /// </summary>
    public class MovieResolver : BaseVideoResolver<Video>
    {
        private IServerApplicationPaths ApplicationPaths { get; set; }

        public MovieResolver(IServerApplicationPaths appPaths)
        {
            ApplicationPaths = appPaths;
        }

        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override ResolverPriority Priority
        {
            get
            {
                // Give plugins a chance to catch iso's first
                // Also since we have to loop through child files looking for videos, 
                // see if we can avoid some of that by letting other resolvers claim folders first
                return ResolverPriority.Second;
            }
        }

        /// <summary>
        /// Resolves the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>Video.</returns>
        protected override Video Resolve(ItemResolveArgs args)
        {
            // Must be a directory
            if (args.IsDirectory)
            {
                // Avoid expensive tests against VF's and all their children by not allowing this
                if (args.Parent == null || args.Parent.IsRoot)
                {
                    return null;
                }

                // If the parent is not a boxset, the only other allowed parent type is Folder		
                if (!(args.Parent is BoxSet))
                {
                    if (args.Parent.GetType() != typeof(Folder))
                    {
                        return null;
                    }
                }

                // Optimization to avoid running all these tests against Top folders
                if (args.Parent != null && args.Parent.IsRoot)
                {
                    return null;
                }

                // Since the looping is expensive, this is an optimization to help us avoid it
                if (args.ContainsMetaFileByName("series.xml") || args.Path.IndexOf("[tvdbid", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    return null;
                }

                if (args.Path.IndexOf("[trailers]", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    return FindMovie<Trailer>(args);
                }
                if (args.Path.IndexOf("[musicvideos]", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    return FindMovie<MusicVideo>(args);
                }

                return FindMovie<Movie>(args);
            }

            return null;
        }

        /// <summary>
        /// Sets the initial item values.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="args">The args.</param>
        protected override void SetInitialItemValues(Video item, ItemResolveArgs args)
        {
            base.SetInitialItemValues(item, args);

            SetProviderIdFromPath(item);
        }

        /// <summary>
        /// Sets the provider id from path.
        /// </summary>
        /// <param name="item">The item.</param>
        private void SetProviderIdFromPath(Video item)
        {
            //we need to only look at the name of this actual item (not parents)
            var justName = Path.GetFileName(item.Path);

            var id = justName.GetAttributeValue("tmdbid");

            if (!string.IsNullOrEmpty(id))
            {
                item.SetProviderId(MetadataProviders.Tmdb, id);
            }
        }

        /// <summary>
        /// Finds a movie based on a child file system entries
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>Movie.</returns>
        private T FindMovie<T>(ItemResolveArgs args)
            where T : Video, new ()
        {
            // Optimization to avoid having to resolve every file
            bool? isKnownMovie = null;

            var movies = new List<T>();

            // Loop through each child file/folder and see if we find a video
            foreach (var child in args.FileSystemChildren)
            {
                if ((child.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    if (IsDvdDirectory(child.Name))
                    {
                        return new T
                        {
                            Path = args.Path,
                            VideoType = VideoType.Dvd
                        };
                    }
                    if (IsBluRayDirectory(child.Name))
                    {
                        return new T
                        {
                            Path = args.Path,
                            VideoType = VideoType.BluRay
                        };
                    }
                    if (IsHdDvdDirectory(child.Name))
                    {
                        return new T
                        {
                            Path = args.Path,
                            VideoType = VideoType.HdDvd
                        };
                    }

                    continue;
                }

                // Don't misidentify xbmc trailers as a movie
                if (child.Name.IndexOf(BaseItem.XbmcTrailerFileSuffix, StringComparison.OrdinalIgnoreCase) != -1)
                {
                    continue;
                }

                var childArgs = new ItemResolveArgs(ApplicationPaths)
                {
                    FileInfo = child,
                    Path = child.FullName
                };

                var item = ResolveVideo<T>(childArgs);

                if (item != null)
                {
                    // If we already know it's a movie, we can stop looping
                    if (!isKnownMovie.HasValue)
                    {
                        isKnownMovie = args.ContainsMetaFileByName("movie.xml") || args.ContainsMetaFileByName("tmdb3.json") || args.Path.IndexOf("[tmdbid", StringComparison.OrdinalIgnoreCase) != -1;
                    }

                    if (isKnownMovie.Value)
                    {
                        return item;
                    }

                    movies.Add(item);
                }
            }

            if (movies.Count > 1)
            {
                return GetMultiFileMovie(movies);
            }

            return movies.Count == 1 ? movies[0] : null;
        }


        /// <summary>
        /// Gets the multi file movie.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="movies">The movies.</param>
        /// <returns>``0.</returns>
        private T GetMultiFileMovie<T>(List<T> movies)
               where T : Video, new()
        {
            var multiPartMovies = movies.OrderBy(i => i.Path)
                .Where(i => EntityResolutionHelper.IsMultiPartFile(i.Path))
                .ToList();

            // They must all be part of the sequence
            if (multiPartMovies.Count != movies.Count)
            {
                return null;
            }

            var firstPart = multiPartMovies[0];

            firstPart.IsMultiPart = true;

            return firstPart;
        }

        /// <summary>
        /// Determines whether [is DVD directory] [the specified directory name].
        /// </summary>
        /// <param name="directoryName">Name of the directory.</param>
        /// <returns><c>true</c> if [is DVD directory] [the specified directory name]; otherwise, <c>false</c>.</returns>
        private bool IsDvdDirectory(string directoryName)
        {
            return directoryName.Equals("video_ts", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Determines whether [is hd DVD directory] [the specified directory name].
        /// </summary>
        /// <param name="directoryName">Name of the directory.</param>
        /// <returns><c>true</c> if [is hd DVD directory] [the specified directory name]; otherwise, <c>false</c>.</returns>
        private bool IsHdDvdDirectory(string directoryName)
        {
            return directoryName.Equals("hvdvd_ts", StringComparison.OrdinalIgnoreCase);
        }
        /// <summary>
        /// Determines whether [is blu ray directory] [the specified directory name].
        /// </summary>
        /// <param name="directoryName">Name of the directory.</param>
        /// <returns><c>true</c> if [is blu ray directory] [the specified directory name]; otherwise, <c>false</c>.</returns>
        private bool IsBluRayDirectory(string directoryName)
        {
            return directoryName.Equals("bdmv", StringComparison.OrdinalIgnoreCase);
        }
    }
}
