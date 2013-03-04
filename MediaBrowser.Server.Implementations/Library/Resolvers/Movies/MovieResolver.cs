using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers.Movies;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.IO;

namespace MediaBrowser.Server.Implementations.Library.Resolvers.Movies
{
    /// <summary>
    /// Class MovieResolver
    /// </summary>
    public class MovieResolver : BaseVideoResolver<Movie>
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
        /// <returns>Movie.</returns>
        protected override Movie Resolve(ItemResolveArgs args)
        {
            // Must be a directory and under a 'Movies' VF
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

                // The movie must be a video file
                return FindMovie(args);
            }

            return null;
        }

        /// <summary>
        /// Sets the initial item values.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="args">The args.</param>
        protected override void SetInitialItemValues(Movie item, ItemResolveArgs args)
        {
            base.SetInitialItemValues(item, args);

            SetProviderIdFromPath(item);
        }

        /// <summary>
        /// Sets the provider id from path.
        /// </summary>
        /// <param name="item">The item.</param>
        private void SetProviderIdFromPath(Movie item)
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
        private Movie FindMovie(ItemResolveArgs args)
        {
            // Since the looping is expensive, this is an optimization to help us avoid it
            if (args.ContainsMetaFileByName("series.xml") || args.Path.IndexOf("[tvdbid", StringComparison.OrdinalIgnoreCase) != -1)
            {
                return null;
            }

            // Optimization to avoid having to resolve every file
            bool? isKnownMovie = null;

            var movies = new List<Movie>();

            // Loop through each child file/folder and see if we find a video
            foreach (var child in args.FileSystemChildren)
            {
                if (child.IsDirectory)
                {
                    if (IsDvdDirectory(child.cFileName))
                    {
                        return new Movie
                        {
                            Path = args.Path,
                            VideoType = VideoType.Dvd
                        };
                    }
                    if (IsBluRayDirectory(child.cFileName))
                    {
                        return new Movie
                        {
                            Path = args.Path,
                            VideoType = VideoType.BluRay
                        };
                    }
                    if (IsHdDvdDirectory(child.cFileName))
                    {
                        return new Movie
                        {
                            Path = args.Path,
                            VideoType = VideoType.HdDvd
                        };
                    }

                    continue;
                }

                var childArgs = new ItemResolveArgs(ApplicationPaths)
                {
                    FileInfo = child,
                    Path = child.Path
                };

                var item = base.Resolve(childArgs);

                if (item != null)
                {
                    // If we already know it's a movie, we can stop looping
                    if (!isKnownMovie.HasValue)
                    {
                        isKnownMovie = args.ContainsMetaFileByName("movie.xml") || args.ContainsMetaFileByName(MovieDbProvider.LOCAL_META_FILE_NAME) || args.Path.IndexOf("[tmdbid", StringComparison.OrdinalIgnoreCase) != -1;
                    }

                    if (isKnownMovie.Value)
                    {
                        return item;
                    }

                    movies.Add(item);
                }
            }

            // If there are multiple video files, return null, and let the VideoResolver catch them later as plain videos
            return movies.Count == 1 ? movies[0] : null;
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
