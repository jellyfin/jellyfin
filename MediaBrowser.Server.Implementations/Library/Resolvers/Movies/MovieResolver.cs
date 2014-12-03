using MediaBrowser.Common.IO;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Naming.Common;
using MediaBrowser.Naming.Video;
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
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;

        public MovieResolver(ILibraryManager libraryManager, IServerApplicationPaths applicationPaths, ILogger logger, IFileSystem fileSystem) : base(libraryManager)
        {
            _applicationPaths = applicationPaths;
            _logger = logger;
            _fileSystem = fileSystem;
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
            }

            var collectionType = args.GetCollectionType();

            // Find movies with their own folders
            if (args.IsDirectory)
            {
                if (string.Equals(collectionType, CollectionType.MusicVideos, StringComparison.OrdinalIgnoreCase))
                {
                    return FindMovie<MusicVideo>(args.Path, args.Parent, args.FileSystemChildren.ToList(), args.DirectoryService, false, collectionType);
                }

                if (string.Equals(collectionType, CollectionType.HomeVideos, StringComparison.OrdinalIgnoreCase))
                {
                    return FindMovie<Video>(args.Path, args.Parent, args.FileSystemChildren.ToList(), args.DirectoryService, false, collectionType);
                }

                if (string.IsNullOrEmpty(collectionType))
                {
                    // Owned items should just use the plain video type
                    if (args.Parent == null)
                    {
                        return FindMovie<Video>(args.Path, args.Parent, args.FileSystemChildren.ToList(), args.DirectoryService, false, collectionType);
                    }

                    // Since the looping is expensive, this is an optimization to help us avoid it
                    if (args.ContainsMetaFileByName("series.xml"))
                    {
                        return null;
                    }
                    
                    return FindMovie<Movie>(args.Path, args.Parent, args.FileSystemChildren.ToList(), args.DirectoryService, true, collectionType);
                }

                if (string.Equals(collectionType, CollectionType.Movies, StringComparison.OrdinalIgnoreCase) ||
                  string.Equals(collectionType, CollectionType.BoxSets, StringComparison.OrdinalIgnoreCase))
                {
                    return FindMovie<Movie>(args.Path, args.Parent, args.FileSystemChildren.ToList(), args.DirectoryService, true, collectionType);
                }
                
                return null;
            }

            var filename = Path.GetFileName(args.Path);
            // Don't misidentify extras or trailers
            if (BaseItem.ExtraSuffixes.Any(i => filename.IndexOf(i.Key, StringComparison.OrdinalIgnoreCase) != -1))
            {
                return null;
            }

            Video item = null;

            if (string.Equals(collectionType, CollectionType.MusicVideos, StringComparison.OrdinalIgnoreCase))
            {
                item = ResolveVideo<MusicVideo>(args, true);
            }

            // To find a movie file, the collection type must be movies or boxsets
            else if (string.Equals(collectionType, CollectionType.Movies, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(collectionType, CollectionType.BoxSets, StringComparison.OrdinalIgnoreCase))
            {
                item = ResolveVideo<Movie>(args, true);
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
            var justName = item.IsInMixedFolder ? Path.GetFileName(item.Path) : Path.GetFileName(item.ContainingFolderPath);

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
        /// <param name="parent">The parent.</param>
        /// <param name="fileSystemEntries">The file system entries.</param>
        /// <param name="directoryService">The directory service.</param>
        /// <param name="supportsMultipleSources">if set to <c>true</c> [supports multiple sources].</param>
        /// <param name="collectionType">Type of the collection.</param>
        /// <returns>Movie.</returns>
        private T FindMovie<T>(string path, Folder parent, IEnumerable<FileSystemInfo> fileSystemEntries, IDirectoryService directoryService, bool supportsMultipleSources, string collectionType)
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

                    multiDiscFolders.Add(child);

                    continue;
                }

                // Don't misidentify extras or trailers as a movie
                if (BaseItem.ExtraSuffixes.Any(i => filename.IndexOf(i.Key, StringComparison.OrdinalIgnoreCase) != -1))
                {
                    continue;
                }

                var childArgs = new ItemResolveArgs(_applicationPaths, LibraryManager, directoryService)
                {
                    FileInfo = child,
                    Path = child.FullName,
                    Parent = parent,
                    CollectionType = collectionType
                };

                var item = ResolveVideo<T>(childArgs, true);

                if (item != null)
                {
                    item.IsInMixedFolder = false;
                    movies.Add(item);
                }
            }

            if (movies.Count > 1)
            {
                var multiFileResult = GetMultiFileMovie(movies);

                if (multiFileResult != null)
                {
                    return multiFileResult;
                }

                if (supportsMultipleSources)
                {
                    var result = GetMovieWithMultipleSources(movies);

                    if (result != null)
                    {
                        return result;
                    }
                }
                return null;
            }

            if (movies.Count == 1)
            {
                return movies[0];
            }

            if (multiDiscFolders.Count > 0)
            {
                return GetMultiDiscMovie<T>(multiDiscFolders, directoryService);
            }

            return null;
        }

        /// <summary>
        /// Gets the multi disc movie.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="multiDiscFolders">The folders.</param>
        /// <param name="directoryService">The directory service.</param>
        /// <returns>``0.</returns>
        private T GetMultiDiscMovie<T>(List<FileSystemInfo> multiDiscFolders, IDirectoryService directoryService)
               where T : Video, new()
        {
            var videoTypes = new List<VideoType>();

            var folderPaths = multiDiscFolders.Select(i => i.FullName).Where(i =>
            {
                var subfolders = directoryService.GetDirectories(i)
                    .Select(d => d.Name)
                    .ToList();

                if (subfolders.Any(IsDvdDirectory))
                {
                    videoTypes.Add(VideoType.Dvd);
                    return true;
                }
                if (subfolders.Any(IsBluRayDirectory))
                {
                    videoTypes.Add(VideoType.BluRay);
                    return true;
                }

                return false;

            }).OrderBy(i => i).ToList();

            // If different video types were found, don't allow this
            if (videoTypes.Distinct().Count() > 1)
            {
                return null;
            }

            if (folderPaths.Count == 0)
            {
                return null;
            }

            var resolver = new StackResolver(new ExtendedNamingOptions(), new Naming.Logging.NullLogger());

            var result = resolver.ResolveDirectories(folderPaths);

            if (result.Stacks.Count != 1)
            {
                return null;
            }
            
            return new T
            {
                Path = folderPaths[0],

                AdditionalParts = folderPaths.Skip(1).ToList(),

                VideoType = videoTypes[0],

                Name = result.Stacks[0].Name
            };
        }

        /// <summary>
        /// Gets the multi file movie.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="movies">The movies.</param>
        /// <returns>``0.</returns>
        private T GetMultiFileMovie<T>(IEnumerable<T> movies)
               where T : Video, new()
        {
            var sortedMovies = movies.OrderBy(i => i.Path).ToList();

            var firstMovie = sortedMovies[0];

            var paths = sortedMovies.Select(i => i.Path).ToList();

            var resolver = new StackResolver(new ExtendedNamingOptions(), new Naming.Logging.NullLogger());

            var result = resolver.ResolveFiles(paths);

            if (result.Stacks.Count != 1)
            {
                return null;
            }

            firstMovie.AdditionalParts = result.Stacks[0].Files.Skip(1).ToList();
            firstMovie.Name = result.Stacks[0].Name;

            // They must all be part of the sequence if we're going to consider it a multi-part movie
            return firstMovie;
        }

        private T GetMovieWithMultipleSources<T>(IEnumerable<T> movies)
               where T : Video, new()
        {
            var sortedMovies = movies.OrderBy(i => i.Path).ToList();

            // Cap this at five to help avoid incorrect matching
            if (sortedMovies.Count > 5)
            {
                return null;
            }

            var firstMovie = sortedMovies[0];

            var filenamePrefix = Path.GetFileName(Path.GetDirectoryName(firstMovie.Path));

            if (!string.IsNullOrWhiteSpace(filenamePrefix))
            {
                if (sortedMovies.All(i => _fileSystem.GetFileNameWithoutExtension(i.Path).StartsWith(filenamePrefix + " - ", StringComparison.OrdinalIgnoreCase)))
                {
                    firstMovie.LocalAlternateVersions = sortedMovies.Skip(1).Select(i => i.Path).ToList();

                    _logger.Debug("Multi-version video found: " + firstMovie.Path);

                    return firstMovie;
                }
            }

            return null;
        }
    }
}
