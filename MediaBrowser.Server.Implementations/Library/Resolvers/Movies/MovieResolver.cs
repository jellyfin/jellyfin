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
        private readonly IServerApplicationPaths _applicationPaths;
        private readonly ILibraryManager _libraryManager;

        public MovieResolver(IServerApplicationPaths appPaths, ILibraryManager libraryManager)
        {
            _applicationPaths = appPaths;
            _libraryManager = libraryManager;
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
            if (args.Path.IndexOf("[tvdbid", StringComparison.OrdinalIgnoreCase) != -1)
            {
                return null;
            }

            var isDirectory = args.IsDirectory;

            if (isDirectory)
            {
                // Since the looping is expensive, this is an optimization to help us avoid it
                if (args.ContainsMetaFileByName("series.xml"))
                {
                    return null;
                }
            }

            var collectionType = args.GetCollectionType();

            // Find movies with their own folders
            if (isDirectory)
            {
                if (args.Path.IndexOf("[trailers]", StringComparison.OrdinalIgnoreCase) != -1 ||
                    string.Equals(collectionType, CollectionType.Trailers, StringComparison.OrdinalIgnoreCase))
                {
                    return FindMovie<Trailer>(args.Path, args.FileSystemChildren);
                }

                if (args.Path.IndexOf("[musicvideos]", StringComparison.OrdinalIgnoreCase) != -1 ||
                    string.Equals(collectionType, CollectionType.MusicVideos, StringComparison.OrdinalIgnoreCase))
                {
                    return FindMovie<MusicVideo>(args.Path, args.FileSystemChildren);
                }

                if (args.Path.IndexOf("[adultvideos]", StringComparison.OrdinalIgnoreCase) != -1 ||
                    string.Equals(collectionType, CollectionType.AdultVideos, StringComparison.OrdinalIgnoreCase))
                {
                    return FindMovie<AdultVideo>(args.Path, args.FileSystemChildren);
                }

                if (string.IsNullOrEmpty(collectionType) ||
                    string.Equals(collectionType, CollectionType.Movies, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(collectionType, CollectionType.BoxSets, StringComparison.OrdinalIgnoreCase))
                {
                    return FindMovie<Movie>(args.Path, args.FileSystemChildren);
                }

                return null;
            }

            var filename = Path.GetFileName(args.Path);
            // Don't misidentify xbmc trailers as a movie
            if (filename.IndexOf(BaseItem.XbmcTrailerFileSuffix, StringComparison.OrdinalIgnoreCase) != -1)
            {
                return null;
            }

            // Find movies that are mixed in the same folder
            if (args.Path.IndexOf("[trailers]", StringComparison.OrdinalIgnoreCase) != -1 ||
                string.Equals(collectionType, CollectionType.Trailers, StringComparison.OrdinalIgnoreCase))
            {
                return ResolveVideo<Trailer>(args);
            }

            Video item = null;

            if (args.Path.IndexOf("[musicvideos]", StringComparison.OrdinalIgnoreCase) != -1 ||
                string.Equals(collectionType, CollectionType.MusicVideos, StringComparison.OrdinalIgnoreCase))
            {
                item = ResolveVideo<MusicVideo>(args);
            }

            if (args.Path.IndexOf("[adultvideos]", StringComparison.OrdinalIgnoreCase) != -1 ||
                string.Equals(collectionType, CollectionType.AdultVideos, StringComparison.OrdinalIgnoreCase))
            {
                item = ResolveVideo<AdultVideo>(args);
            }

            // To find a movie file, the collection type must be movies or boxsets
            // Otherwise we'll consider it a plain video and let the video resolver handle it
            if (string.Equals(collectionType, CollectionType.Movies, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(collectionType, CollectionType.BoxSets, StringComparison.OrdinalIgnoreCase))
            {
                item = ResolveVideo<Movie>(args);
            }

            if (item != null)
            {
                item.IsInMixedFolder = true;
            }

            return item;
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
            var justName = item.IsInMixedFolder ? Path.GetFileName(item.Path) : Path.GetFileName(Path.GetDirectoryName(item.Path));

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
        /// <returns>Movie.</returns>
        private T FindMovie<T>(string path, IEnumerable<FileSystemInfo> fileSystemEntries)
            where T : Video, new()
        {
            var movies = new List<T>();

            var multiDiscFolders = new List<FileSystemInfo>();

            // Loop through each child file/folder and see if we find a video
            foreach (var child in fileSystemEntries)
            {
                var filename = child.Name;

                if ((child.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    if (IsDvdDirectory(filename))
                    {
                        return new T
                        {
                            Path = path,
                            VideoType = VideoType.Dvd
                        };
                    }
                    if (IsBluRayDirectory(filename))
                    {
                        return new T
                        {
                            Path = path,
                            VideoType = VideoType.BluRay
                        };
                    }

                    if (EntityResolutionHelper.IsMultiPartFile(filename))
                    {
                        multiDiscFolders.Add(child);
                    }

                    continue;
                }

                // Don't misidentify xbmc trailers as a movie
                if (filename.IndexOf(BaseItem.XbmcTrailerFileSuffix, StringComparison.OrdinalIgnoreCase) != -1)
                {
                    continue;
                }

                var childArgs = new ItemResolveArgs(_applicationPaths, _libraryManager)
                {
                    FileInfo = child,
                    Path = child.FullName
                };

                var item = ResolveVideo<T>(childArgs);

                if (item != null)
                {
                    item.IsInMixedFolder = false;
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

            var folderPaths = folders.Select(i => i.FullName).Where(i =>
            {
                var subfolders = Directory.GetDirectories(i).Select(Path.GetFileName).ToList();

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

            }).OrderBy(i => i).ToList();

            if (folderPaths.Count == 0)
            {
                return null;
            }

            return new T
            {
                Path = folderPaths[0],

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
            return string.Equals(directoryName, "video_ts", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Determines whether [is blu ray directory] [the specified directory name].
        /// </summary>
        /// <param name="directoryName">Name of the directory.</param>
        /// <returns><c>true</c> if [is blu ray directory] [the specified directory name]; otherwise, <c>false</c>.</returns>
        private bool IsBluRayDirectory(string directoryName)
        {
            return string.Equals(directoryName, "bdmv", StringComparison.OrdinalIgnoreCase);
        }
    }
}
