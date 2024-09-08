#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Emby.Naming.Common;
using Emby.Naming.Video;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Library.Resolvers.Movies
{
    /// <summary>
    /// Class MovieResolver.
    /// </summary>
    public partial class MovieResolver : BaseVideoResolver<Video>, IMultiItemResolver
    {
        private readonly IImageProcessor _imageProcessor;

        private static readonly CollectionType[] _validCollectionTypes = new[]
        {
            CollectionType.movies,
            CollectionType.homevideos,
            CollectionType.musicvideos,
            CollectionType.tvshows,
            CollectionType.photos
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="MovieResolver"/> class.
        /// </summary>
        /// <param name="imageProcessor">The image processor.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="namingOptions">The naming options.</param>
        /// <param name="directoryService">The directory service.</param>
        public MovieResolver(IImageProcessor imageProcessor, ILogger<MovieResolver> logger, NamingOptions namingOptions, IDirectoryService directoryService)
            : base(logger, namingOptions, directoryService)
        {
            _imageProcessor = imageProcessor;
        }

        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override ResolverPriority Priority => ResolverPriority.Fourth;

        [GeneratedRegex(@"\bsample\b", RegexOptions.IgnoreCase)]
        private static partial Regex IsIgnoredRegex();

        /// <inheritdoc />
        public MultiItemResolverResult ResolveMultiple(
            Folder parent,
            List<FileSystemMetadata> files,
            CollectionType? collectionType,
            IDirectoryService directoryService)
        {
            var result = ResolveMultipleInternal(parent, files, collectionType);

            if (result is not null)
            {
                foreach (var item in result.Items)
                {
                    SetInitialItemValues((Video)item, null);
                }
            }

            return result;
        }

        /// <summary>
        /// Resolves the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>Video.</returns>
        protected override Video Resolve(ItemResolveArgs args)
        {
            var collectionType = args.GetCollectionType();

            // Find movies with their own folders
            if (args.IsDirectory)
            {
                if (IsInvalid(args.Parent, collectionType))
                {
                    return null;
                }

                Video movie = null;
                var files = args.GetActualFileSystemChildren().ToList();

                if (collectionType == CollectionType.musicvideos)
                {
                    movie = FindMovie<MusicVideo>(args, args.Path, args.Parent, files, DirectoryService, collectionType, false);
                }

                if (collectionType == CollectionType.homevideos)
                {
                    movie = FindMovie<Video>(args, args.Path, args.Parent, files, DirectoryService, collectionType, false);
                }

                if (collectionType is null)
                {
                    // Owned items will be caught by the video extra resolver
                    if (args.Parent is null)
                    {
                        return null;
                    }

                    if (args.HasParent<Series>())
                    {
                        return null;
                    }

                    movie = FindMovie<Movie>(args, args.Path, args.Parent, files, DirectoryService, collectionType, true);
                }

                if (collectionType == CollectionType.movies)
                {
                    movie = FindMovie<Movie>(args, args.Path, args.Parent, files, DirectoryService, collectionType, true);
                }

                // ignore extras
                return movie?.ExtraType is null ? movie : null;
            }

            if (args.Parent is null)
            {
                return base.Resolve(args);
            }

            if (IsInvalid(args.Parent, collectionType))
            {
                return null;
            }

            Video item = null;

            if (collectionType == CollectionType.musicvideos)
            {
                item = ResolveVideo<MusicVideo>(args, false);
            }

            // To find a movie file, the collection type must be movies or boxsets
            else if (collectionType == CollectionType.movies)
            {
                item = ResolveVideo<Movie>(args, true);
            }
            else if (collectionType == CollectionType.homevideos || collectionType == CollectionType.photos)
            {
                item = ResolveVideo<Video>(args, false);
            }
            else if (collectionType is null)
            {
                if (args.HasParent<Series>())
                {
                    return null;
                }

                item = ResolveVideo<Video>(args, false);
            }

            // Ignore extras
            if (item?.ExtraType is not null)
            {
                return null;
            }

            if (item is not null)
            {
                item.IsInMixedFolder = true;
            }

            return item;
        }

        private MultiItemResolverResult ResolveMultipleInternal(
            Folder parent,
            List<FileSystemMetadata> files,
            CollectionType? collectionType)
        {
            if (IsInvalid(parent, collectionType))
            {
                return null;
            }

            if (collectionType is CollectionType.musicvideos)
            {
                return ResolveVideos<MusicVideo>(parent, files, true, collectionType, false);
            }

            if (collectionType == CollectionType.homevideos || collectionType == CollectionType.photos)
            {
                return ResolveVideos<Video>(parent, files, false, collectionType, false);
            }

            if (collectionType is null)
            {
                // Owned items should just use the plain video type
                if (parent is null)
                {
                    return ResolveVideos<Video>(parent, files, false, collectionType, false);
                }

                if (parent is Series || parent.GetParents().OfType<Series>().Any())
                {
                    return null;
                }

                return ResolveVideos<Movie>(parent, files, false, collectionType, true);
            }

            if (collectionType == CollectionType.movies)
            {
                return ResolveVideos<Movie>(parent, files, true, collectionType, true);
            }

            if (collectionType == CollectionType.tvshows)
            {
                return ResolveVideos<Episode>(parent, files, false, collectionType, true);
            }

            return null;
        }

        private MultiItemResolverResult ResolveVideos<T>(
            Folder parent,
            IEnumerable<FileSystemMetadata> fileSystemEntries,
            bool supportMultiEditions,
            CollectionType? collectionType,
            bool parseName)
            where T : Video, new()
        {
            var files = new List<FileSystemMetadata>();
            var leftOver = new List<FileSystemMetadata>();
            var hasCollectionType = collectionType is not null;

            // Loop through each child file/folder and see if we find a video
            foreach (var child in fileSystemEntries)
            {
                // This is a hack but currently no better way to resolve a sometimes ambiguous situation
                if (!hasCollectionType)
                {
                    if (string.Equals(child.Name, "tvshow.nfo", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(child.Name, "season.nfo", StringComparison.OrdinalIgnoreCase))
                    {
                        return null;
                    }
                }

                if (child.IsDirectory)
                {
                    leftOver.Add(child);
                }
                else if (!IsIgnoredRegex().IsMatch(child.Name))
                {
                    files.Add(child);
                }
            }

            var videoInfos = files
                .Select(i => VideoResolver.Resolve(i.FullName, i.IsDirectory, NamingOptions, parseName))
                .Where(f => f is not null)
                .ToList();

            var resolverResult = VideoListResolver.Resolve(videoInfos, NamingOptions, supportMultiEditions, parseName);

            var result = new MultiItemResolverResult
            {
                ExtraFiles = leftOver
            };

            var isInMixedFolder = resolverResult.Count > 1 || parent?.IsTopParent == true;

            foreach (var video in resolverResult)
            {
                var firstVideo = video.Files[0];
                var path = firstVideo.Path;
                if (video.ExtraType is not null)
                {
                    result.ExtraFiles.Add(files.Find(f => string.Equals(f.FullName, path, StringComparison.OrdinalIgnoreCase)));
                    continue;
                }

                var additionalParts = video.Files.Count > 1 ? video.Files.Skip(1).Select(i => i.Path).ToArray() : Array.Empty<string>();

                var videoItem = new T
                {
                    Path = path,
                    IsInMixedFolder = isInMixedFolder,
                    ProductionYear = video.Year,
                    Name = parseName ? video.Name : firstVideo.Name,
                    AdditionalParts = additionalParts,
                    LocalAlternateVersions = video.AlternateVersions.Select(i => i.Path).ToArray()
                };

                SetVideoType(videoItem, firstVideo);
                Set3DFormat(videoItem, firstVideo);

                result.Items.Add(videoItem);
            }

            result.ExtraFiles.AddRange(files.Where(i => !ContainsFile(resolverResult, i)));

            return result;
        }

        private static bool ContainsFile(IReadOnlyList<VideoInfo> result, FileSystemMetadata file)
        {
            for (var i = 0; i < result.Count; i++)
            {
                var current = result[i];
                for (var j = 0; j < current.Files.Count; j++)
                {
                    if (ContainsFile(current.Files[j], file))
                    {
                        return true;
                    }
                }

                for (var j = 0; j < current.AlternateVersions.Count; j++)
                {
                    if (ContainsFile(current.AlternateVersions[j], file))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool ContainsFile(VideoFileInfo result, FileSystemMetadata file)
        {
            return string.Equals(result.Path, file.FullName, StringComparison.OrdinalIgnoreCase);
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
        private static void SetProviderIdsFromPath(Video item)
        {
            if (item is Movie || item is MusicVideo)
            {
                // We need to only look at the name of this actual item (not parents)
                var justName = item.IsInMixedFolder ? Path.GetFileName(item.Path.AsSpan()) : Path.GetFileName(item.ContainingFolderPath.AsSpan());

                if (!justName.IsEmpty)
                {
                    // Check for TMDb id
                    var tmdbid = justName.GetAttributeValue("tmdbid");
                    item.TrySetProviderId(MetadataProvider.Tmdb, tmdbid);
                }

                if (!string.IsNullOrEmpty(item.Path))
                {
                    // Check for IMDb id - we use full media path, as we can assume that this will match in any use case (whether  id in parent dir or in file name)
                    var imdbid = item.Path.AsSpan().GetAttributeValue("imdbid");
                    item.TrySetProviderId(MetadataProvider.Imdb, imdbid);
                }
            }
        }

        /// <summary>
        /// Finds a movie based on a child file system entries.
        /// </summary>
        /// <returns>Movie.</returns>
        private T FindMovie<T>(ItemResolveArgs args, string path, Folder parent, List<FileSystemMetadata> fileSystemEntries, IDirectoryService directoryService, CollectionType? collectionType, bool parseName)
            where T : Video, new()
        {
            var multiDiscFolders = new List<FileSystemMetadata>();

            var libraryOptions = args.LibraryOptions;
            var supportPhotos = collectionType == CollectionType.homevideos && libraryOptions.EnablePhotos;
            var photos = new List<FileSystemMetadata>();

            // Search for a folder rip
            foreach (var child in fileSystemEntries)
            {
                var filename = child.Name;

                if (child.IsDirectory)
                {
                    if (IsDvdDirectory(child.FullName, filename, directoryService))
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
                else if (supportPhotos && PhotoResolver.IsImageFile(child.FullName, _imageProcessor))
                {
                    photos.Add(child);
                }
            }

            // TODO: Allow GetMultiDiscMovie in here
            const bool SupportsMultiVersion = true;

            var result = ResolveVideos<T>(parent, fileSystemEntries, SupportsMultiVersion, collectionType, parseName) ??
                new MultiItemResolverResult();

            var isPhotosCollection = collectionType == CollectionType.homevideos || collectionType == CollectionType.photos;
            if (!isPhotosCollection && result.Items.Count == 1)
            {
                var videoPath = result.Items[0].Path;
                var hasPhotos = photos.Any(i => !PhotoResolver.IsOwnedByResolvedMedia(videoPath, i.Name));

                if (!hasPhotos)
                {
                    var movie = (T)result.Items[0];
                    movie.IsInMixedFolder = false;
                    movie.Name = Path.GetFileName(movie.ContainingFolderPath);
                    return movie;
                }
            }
            else if (result.Items.Count == 0 && multiDiscFolders.Count > 0)
            {
                return GetMultiDiscMovie<T>(multiDiscFolders, directoryService);
            }

            return null;
        }

        /// <summary>
        /// Gets the multi disc movie.
        /// </summary>
        /// <param name="multiDiscFolders">The folders.</param>
        /// <param name="directoryService">The directory service.</param>
        /// <returns>``0.</returns>
        private T GetMultiDiscMovie<T>(List<FileSystemMetadata> multiDiscFolders, IDirectoryService directoryService)
               where T : Video, new()
        {
            var videoTypes = new List<VideoType>();

            var folderPaths = multiDiscFolders.Select(i => i.FullName).Where(i =>
            {
                var subFileEntries = directoryService.GetFileSystemEntries(i);

                var subfolders = subFileEntries
                    .Where(e => e.IsDirectory)
                    .ToList();

                if (subfolders.Any(s => IsDvdDirectory(s.FullName, s.Name, directoryService)))
                {
                    videoTypes.Add(VideoType.Dvd);
                    return true;
                }

                if (subfolders.Any(s => IsBluRayDirectory(s.Name)))
                {
                    videoTypes.Add(VideoType.BluRay);
                    return true;
                }

                var subFiles = subFileEntries
                 .Where(e => !e.IsDirectory)
                 .Select(d => d.Name);

                if (subFiles.Any(IsDvdFile))
                {
                    videoTypes.Add(VideoType.Dvd);
                    return true;
                }

                return false;
            }).Order().ToList();

            // If different video types were found, don't allow this
            if (videoTypes.Distinct().Count() > 1)
            {
                return null;
            }

            if (folderPaths.Count == 0)
            {
                return null;
            }

            var result = StackResolver.ResolveDirectories(folderPaths, NamingOptions).ToList();

            if (result.Count != 1)
            {
                return null;
            }

            int additionalPartsLen = folderPaths.Count - 1;
            var additionalParts = new string[additionalPartsLen];
            folderPaths.CopyTo(1, additionalParts, 0, additionalPartsLen);

            var returnVideo = new T
            {
                Path = folderPaths[0],
                AdditionalParts = additionalParts,
                VideoType = videoTypes[0],
                Name = result[0].Name
            };

            SetIsoType(returnVideo);

            return returnVideo;
        }

        private bool IsInvalid(Folder parent, CollectionType? collectionType)
        {
            if (parent is not null)
            {
                if (parent.IsRoot)
                {
                    return true;
                }
            }

            if (collectionType is null)
            {
                return false;
            }

            return !_validCollectionTypes.Contains(collectionType.Value);
        }
    }
}
