using Interfaces.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Extensions;
using MediaBrowser.Naming.Video;
using MediaBrowser.Server.Implementations.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommonIO;

namespace MediaBrowser.Server.Implementations.Library.Resolvers.Movies
{
    /// <summary>
    /// Class MovieResolver
    /// </summary>
    public class MovieResolver : BaseVideoResolver<Video>, IMultiItemResolver
    {
        public MovieResolver(ILibraryManager libraryManager)
            : base(libraryManager)
        {
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
                // Also run after series resolver
                return ResolverPriority.Third;
            }
        }

        public MultiItemResolverResult ResolveMultiple(Folder parent,
            List<FileSystemMetadata> files,
            string collectionType,
            IDirectoryService directoryService)
        {
            var result = ResolveMultipleInternal(parent, files, collectionType, directoryService);

            if (result != null)
            {
                foreach (var item in result.Items)
                {
                    SetInitialItemValues((Video)item, null);
                }
            }

            return result;
        }

        private MultiItemResolverResult ResolveMultipleInternal(Folder parent,
            List<FileSystemMetadata> files,
            string collectionType,
            IDirectoryService directoryService)
        {
            if (IsInvalid(parent, collectionType))
            {
                return null;
            }

            if (string.Equals(collectionType, CollectionType.MusicVideos, StringComparison.OrdinalIgnoreCase))
            {
                return ResolveVideos<MusicVideo>(parent, files, directoryService, false);
            }

            if (string.Equals(collectionType, CollectionType.HomeVideos, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(collectionType, CollectionType.Photos, StringComparison.OrdinalIgnoreCase))
            {
                return ResolveVideos<Video>(parent, files, directoryService, false);
            }

            if (string.IsNullOrEmpty(collectionType))
            {
                // Owned items should just use the plain video type
                if (parent == null)
                {
                    return ResolveVideos<Video>(parent, files, directoryService, false);
                }

                if (parent is Series || parent.GetParents().OfType<Series>().Any())
                {
                    return null;
                }

                return ResolveVideos<Movie>(parent, files, directoryService, false);
            }

            if (string.Equals(collectionType, CollectionType.Movies, StringComparison.OrdinalIgnoreCase))
            {
                return ResolveVideos<Movie>(parent, files, directoryService, true);
            }

            return null;
        }

        private MultiItemResolverResult ResolveVideos<T>(Folder parent, IEnumerable<FileSystemMetadata> fileSystemEntries, IDirectoryService directoryService, bool suppportMultiEditions)
            where T : Video, new()
        {
            var files = new List<FileSystemMetadata>();
            var videos = new List<BaseItem>();
            var leftOver = new List<FileSystemMetadata>();

            // Loop through each child file/folder and see if we find a video
            foreach (var child in fileSystemEntries)
            {
                if ((child.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    leftOver.Add(child);
                }
                else if (IsIgnored(child.Name))
                {

                }
                else
                {
                    files.Add(child);
                }
            }

            var namingOptions = ((LibraryManager)LibraryManager).GetNamingOptions();

            var resolver = new VideoListResolver(namingOptions, new PatternsLogger());
            var resolverResult = resolver.Resolve(files.Select(i => new FileMetadata
            {
                Id = i.FullName,
                IsFolder = i.IsDirectory

            }).ToList(), suppportMultiEditions).ToList();

            var result = new MultiItemResolverResult
            {
                ExtraFiles = leftOver,
                Items = videos
            };

            var isInMixedFolder = resolverResult.Count > 1;

            foreach (var video in resolverResult)
            {
                var firstVideo = video.Files.First();

                var videoItem = new T
                {
                    Path = video.Files[0].Path,
                    IsInMixedFolder = isInMixedFolder,
                    ProductionYear = video.Year,
                    Name = video.Name,
                    AdditionalParts = video.Files.Skip(1).Select(i => i.Path).ToList(),
                    LocalAlternateVersions = video.AlternateVersions.Select(i => i.Path).ToList()
                };

                SetVideoType(videoItem, firstVideo);
                Set3DFormat(videoItem, firstVideo);

                result.Items.Add(videoItem);
            }

            result.ExtraFiles.AddRange(files.Where(i => !ContainsFile(resolverResult, i)));

            return result;
        }

        private bool ContainsFile(List<VideoInfo> result, FileSystemMetadata file)
        {
            return result.Any(i => ContainsFile(i, file));
        }

        private bool ContainsFile(VideoInfo result, FileSystemMetadata file)
        {
            return result.Files.Any(i => ContainsFile(i, file)) ||
                result.AlternateVersions.Any(i => ContainsFile(i, file)) ||
                result.Extras.Any(i => ContainsFile(i, file));
        }

        private bool ContainsFile(VideoFileInfo result, FileSystemMetadata file)
        {
            return string.Equals(result.Path, file.FullName, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Resolves the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>Video.</returns>
        protected override Video Resolve(ItemResolveArgs args)
        {
            var collectionType = args.GetCollectionType();

            if (IsInvalid(args.Parent, collectionType))
            {
                return null;
            }

            // Find movies with their own folders
            if (args.IsDirectory)
            {
                var files = args.FileSystemChildren
                    .Where(i => !LibraryManager.IgnoreFile(i, args.Parent))
                    .ToList();

                if (string.Equals(collectionType, CollectionType.MusicVideos, StringComparison.OrdinalIgnoreCase))
                {
                    return FindMovie<MusicVideo>(args.Path, args.Parent, files, args.DirectoryService, collectionType);
                }

                if (string.Equals(collectionType, CollectionType.HomeVideos, StringComparison.OrdinalIgnoreCase))
                {
                    return FindMovie<Video>(args.Path, args.Parent, files, args.DirectoryService, collectionType);
                }

                if (string.IsNullOrEmpty(collectionType))
                {
                    // Owned items should just use the plain video type
                    if (args.Parent == null)
                    {
                        return FindMovie<Video>(args.Path, args.Parent, files, args.DirectoryService, collectionType);
                    }

                    if (args.HasParent<Series>())
                    {
                        return null;
                    }

                    return FindMovie<Movie>(args.Path, args.Parent, files, args.DirectoryService, collectionType);
                }

                if (string.Equals(collectionType, CollectionType.Movies, StringComparison.OrdinalIgnoreCase))
                {
                    return FindMovie<Movie>(args.Path, args.Parent, files, args.DirectoryService, collectionType);
                }

                return null;
            }

            // Owned items will be caught by the plain video resolver
            if (args.Parent == null)
            {
                return null;
            }

            Video item = null;

            if (string.Equals(collectionType, CollectionType.MusicVideos, StringComparison.OrdinalIgnoreCase))
            {
                item = ResolveVideo<MusicVideo>(args, false);
            }

            // To find a movie file, the collection type must be movies or boxsets
            else if (string.Equals(collectionType, CollectionType.Movies, StringComparison.OrdinalIgnoreCase))
            {
                item = ResolveVideo<Movie>(args, true);
            }

            else if (string.Equals(collectionType, CollectionType.HomeVideos, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(collectionType, CollectionType.Photos, StringComparison.OrdinalIgnoreCase))
            {
                item = ResolveVideo<Video>(args, false);
            }
            else if (string.IsNullOrEmpty(collectionType))
            {
                if (args.HasParent<Series>())
                {
                    return null;
                }

                item = ResolveVideo<Video>(args, false);
            }

            if (item != null)
            {
                item.IsInMixedFolder = true;
            }

            return item;
        }

        private bool IsIgnored(string filename)
        {
            // Ignore samples
            var sampleFilename = " " + filename.Replace(".", " ", StringComparison.OrdinalIgnoreCase)
                .Replace("-", " ", StringComparison.OrdinalIgnoreCase)
                .Replace("_", " ", StringComparison.OrdinalIgnoreCase)
                .Replace("!", " ", StringComparison.OrdinalIgnoreCase);

            if (sampleFilename.IndexOf(" sample ", StringComparison.OrdinalIgnoreCase) != -1)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Sets the initial item values.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="args">The args.</param>
        protected override void SetInitialItemValues(Video item, ItemResolveArgs args)
        {
            base.SetInitialItemValues(item, args);

            SetProviderIdsFromPath(item);
        }

        /// <summary>
        /// Sets the provider id from path.
        /// </summary>
        /// <param name="item">The item.</param>
        private void SetProviderIdsFromPath(Video item)
        {
            if (item is Movie || item is MusicVideo)
            {
                //we need to only look at the name of this actual item (not parents)
                var justName = item.IsInMixedFolder ? Path.GetFileName(item.Path) : Path.GetFileName(item.ContainingFolderPath);

                if (!string.IsNullOrWhiteSpace(justName))
                {
                    // check for tmdb id
                    var tmdbid = justName.GetAttributeValue("tmdbid");

                    if (!string.IsNullOrWhiteSpace(tmdbid))
                    {
                        item.SetProviderId(MetadataProviders.Tmdb, tmdbid);
                    }
                }

                if (!string.IsNullOrWhiteSpace(item.Path))
                {
                    // check for imdb id - we use full media path, as we can assume, that this will match in any use case (wither id in parent dir or in file name)
                    var imdbid = item.Path.GetAttributeValue("imdbid");

                    if (!string.IsNullOrWhiteSpace(imdbid))
                    {
                        item.SetProviderId(MetadataProviders.Imdb, imdbid);
                    }
                }
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
        /// <param name="collectionType">Type of the collection.</param>
        /// <returns>Movie.</returns>
        private T FindMovie<T>(string path, Folder parent, List<FileSystemMetadata> fileSystemEntries, IDirectoryService directoryService, string collectionType)
            where T : Video, new()
        {
            var multiDiscFolders = new List<FileSystemMetadata>();

            // Search for a folder rip
            foreach (var child in fileSystemEntries)
            {
                var filename = child.Name;

                if ((child.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    if (IsDvdDirectory(filename))
                    {
                        var movie = new T
                        {
                            Path = path,
                            VideoType = VideoType.Dvd
                        };
                        Set3DFormat(movie);
                        return movie;
                    }
                    if (IsBluRayDirectory(filename))
                    {
                        var movie = new T
                        {
                            Path = path,
                            VideoType = VideoType.BluRay
                        };
                        Set3DFormat(movie);
                        return movie;
                    }

                    multiDiscFolders.Add(child);
                }
                else if (IsDvdFile(filename))
                {
                    var movie = new T
                    {
                        Path = path,
                        VideoType = VideoType.Dvd
                    };
                    Set3DFormat(movie);
                    return movie;
                }
            }

            var supportsMultiVersion = !string.Equals(collectionType, CollectionType.HomeVideos) &&
                                    !string.Equals(collectionType, CollectionType.Photos) &&
                                    !string.Equals(collectionType, CollectionType.MusicVideos);

            var result = ResolveVideos<T>(parent, fileSystemEntries, directoryService, supportsMultiVersion);

            if (result.Items.Count == 1)
            {
                var movie = (T)result.Items[0];
                movie.IsInMixedFolder = false;
                movie.Name = Path.GetFileName(movie.ContainingFolderPath);
                return movie;
            }

            if (result.Items.Count == 0 && multiDiscFolders.Count > 0)
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
        private T GetMultiDiscMovie<T>(List<FileSystemMetadata> multiDiscFolders, IDirectoryService directoryService)
               where T : Video, new()
        {
            var videoTypes = new List<VideoType>();

            var folderPaths = multiDiscFolders.Select(i => i.FullName).Where(i =>
            {
                var subFileEntries = directoryService.GetFileSystemEntries(i)
                    .ToList();

                var subfolders = subFileEntries
                    .Where(e => (e.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
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

                var subFiles = subFileEntries
                 .Where(e => (e.Attributes & FileAttributes.Directory) != FileAttributes.Directory)
                 .Select(d => d.Name);

                if (subFiles.Any(IsDvdFile))
                {
                    videoTypes.Add(VideoType.Dvd);
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

            var namingOptions = ((LibraryManager)LibraryManager).GetNamingOptions();
            var resolver = new StackResolver(namingOptions, new PatternsLogger());

            var result = resolver.ResolveDirectories(folderPaths);

            if (result.Stacks.Count != 1)
            {
                return null;
            }

            var returnVideo = new T
            {
                Path = folderPaths[0],

                AdditionalParts = folderPaths.Skip(1).ToList(),

                VideoType = videoTypes[0],

                Name = result.Stacks[0].Name
            };

            SetIsoType(returnVideo);

            return returnVideo;
        }

        private bool IsInvalid(Folder parent, string collectionType)
        {
            if (parent != null)
            {
                if (parent.IsRoot)
                {
                    return true;
                }
            }

            var validCollectionTypes = new[]
            {
                CollectionType.Movies,
                CollectionType.HomeVideos,
                CollectionType.MusicVideos,
                CollectionType.Movies,
                CollectionType.Photos
            };

            if (string.IsNullOrWhiteSpace(collectionType))
            {
                return false;
            }

            return !validCollectionTypes.Contains(collectionType, StringComparer.OrdinalIgnoreCase);
        }
    }
}
