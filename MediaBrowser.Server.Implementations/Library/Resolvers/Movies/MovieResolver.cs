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
                if (args.Parent != null)
                {
                    if (args.Parent.IsRoot)
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
                }

                // Since the looping is expensive, this is an optimization to help us avoid it
                if (args.ContainsMetaFileByName("series.xml") || args.Path.IndexOf("[tvdbid", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    return null;
                }

                // A shortcut to help us resolve faster in some cases
                var isKnownMovie = args.ContainsMetaFileByName("movie.xml") || args.ContainsMetaFileByName("tmdb3.json") ||
                                   args.Path.IndexOf("[tmdbid", StringComparison.OrdinalIgnoreCase) != -1;

                if (args.Path.IndexOf("[trailers]", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    return FindMovie<Trailer>(args.Path, args.FileSystemChildren, isKnownMovie);
                }
                if (args.Path.IndexOf("[musicvideos]", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    return FindMovie<MusicVideo>(args.Path, args.FileSystemChildren, isKnownMovie);
                }

                return FindMovie<Movie>(args.Path, args.FileSystemChildren, isKnownMovie);
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
        /// <typeparam name="T"></typeparam>
        /// <param name="path">The path.</param>
        /// <param name="fileSystemEntries">The file system entries.</param>
        /// <param name="isKnownMovie">if set to <c>true</c> [is known movie].</param>
        /// <returns>Movie.</returns>
        private T FindMovie<T>(string path, IEnumerable<FileSystemInfo> fileSystemEntries, bool isKnownMovie)
            where T : Video, new()
        {
            var movies = new List<T>();

            var multiDiscFolders = new List<FileSystemInfo>();

            // Loop through each child file/folder and see if we find a video
            foreach (var child in fileSystemEntries)
            {
                if ((child.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    if (IsDvdDirectory(child.Name))
                    {
                        return new T
                        {
                            Path = path,
                            VideoType = VideoType.Dvd
                        };
                    }
                    if (IsBluRayDirectory(child.Name))
                    {
                        return new T
                        {
                            Path = path,
                            VideoType = VideoType.BluRay
                        };
                    }

                    if (EntityResolutionHelper.IsMultiPartFile(child.Name))
                    {
                        multiDiscFolders.Add(child);
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
                    movies.Add(item);
                }
            }

            if (movies.Count > 1)
            {
                return GetMultiFileMovie(movies);
            }

            if (movies.Count == 1)
            {
                return movies[0];
            }

            if (multiDiscFolders.Count > 0)
            {
                return GetMultiDiscMovie<T>(multiDiscFolders);
            }

            return null;
        }

        /// <summary>
        /// Gets the multi disc movie.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="folders">The folders.</param>
        /// <returns>``0.</returns>
        private T GetMultiDiscMovie<T>(List<FileSystemInfo> folders)
               where T : Video, new()
        {
            var videoType = VideoType.BluRay;

            folders = folders.Where(i =>
            {
                var subfolders = Directory.GetDirectories(i.FullName).Select(Path.GetFileName).ToList();

                if (subfolders.Any(IsDvdDirectory))
                {
                    videoType = VideoType.Dvd;
                    return true;
                }
                if (subfolders.Any(IsBluRayDirectory))
                {
                    videoType = VideoType.BluRay;
                    return true;
                }

                return false;

            }).OrderBy(i => i.FullName).ToList();

            if (folders.Count == 0)
            {
                return null;
            }

            return new T
            {
                Path = folders[0].FullName,

                IsMultiPart = true,

                VideoType = videoType
            };
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
