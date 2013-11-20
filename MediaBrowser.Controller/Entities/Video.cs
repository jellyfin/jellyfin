using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Entities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Class Video
    /// </summary>
    public class Video : BaseItem, IHasMediaStreams, IHasAspectRatio
    {
        public bool IsMultiPart { get; set; }

        public List<Guid> AdditionalPartIds { get; set; }

        public Video()
        {
            MediaStreams = new List<MediaStream>();
            PlayableStreamFileNames = new List<string>();
            AdditionalPartIds = new List<Guid>();
        }

        /// <summary>
        /// Gets or sets the type of the video.
        /// </summary>
        /// <value>The type of the video.</value>
        public VideoType VideoType { get; set; }

        /// <summary>
        /// Gets or sets the type of the iso.
        /// </summary>
        /// <value>The type of the iso.</value>
        public IsoType? IsoType { get; set; }

        /// <summary>
        /// Gets or sets the video3 D format.
        /// </summary>
        /// <value>The video3 D format.</value>
        public Video3DFormat? Video3DFormat { get; set; }

        /// <summary>
        /// Gets or sets the media streams.
        /// </summary>
        /// <value>The media streams.</value>
        public List<MediaStream> MediaStreams { get; set; }

        /// <summary>
        /// If the video is a folder-rip, this will hold the file list for the largest playlist
        /// </summary>
        public List<string> PlayableStreamFileNames { get; set; }

        /// <summary>
        /// Gets the playable stream files.
        /// </summary>
        /// <returns>List{System.String}.</returns>
        public List<string> GetPlayableStreamFiles()
        {
            return GetPlayableStreamFiles(Path);
        }

        /// <summary>
        /// Gets or sets the aspect ratio.
        /// </summary>
        /// <value>The aspect ratio.</value>
        public string AspectRatio { get; set; }
        
        /// <summary>
        /// Should be overridden to return the proper folder where metadata lives
        /// </summary>
        /// <value>The meta location.</value>
        [IgnoreDataMember]
        public override string MetaLocation
        {
            get
            {
                return VideoType == VideoType.VideoFile || VideoType == VideoType.Iso || IsMultiPart ? System.IO.Path.GetDirectoryName(Path) : Path;
            }
        }

        /// <summary>
        /// Needed because the resolver stops at the movie folder and we find the video inside.
        /// </summary>
        /// <value><c>true</c> if [use parent path to create resolve args]; otherwise, <c>false</c>.</value>
        protected override bool UseParentPathToCreateResolveArgs
        {
            get
            {
                if (IsInMixedFolder)
                {
                    return false;
                }

                return VideoType == VideoType.VideoFile || VideoType == VideoType.Iso || IsMultiPart;
            }
        }

        public string MainFeaturePlaylistName { get; set; }

        /// <summary>
        /// Gets the playable stream files.
        /// </summary>
        /// <param name="rootPath">The root path.</param>
        /// <returns>List{System.String}.</returns>
        public List<string> GetPlayableStreamFiles(string rootPath)
        {
            if (PlayableStreamFileNames == null)
            {
                return null;
            }

            var allFiles = Directory.EnumerateFiles(rootPath, "*", SearchOption.AllDirectories).ToList();

            return PlayableStreamFileNames.Select(name => allFiles.FirstOrDefault(f => string.Equals(System.IO.Path.GetFileName(f), name, StringComparison.OrdinalIgnoreCase)))
                .Where(f => !string.IsNullOrEmpty(f))
                .ToList();
        }

        /// <summary>
        /// The default video stream for this video.  Use this to determine media info for this item.
        /// </summary>
        /// <value>The default video stream.</value>
        [IgnoreDataMember]
        public MediaStream DefaultVideoStream
        {
            get { return MediaStreams != null ? MediaStreams.FirstOrDefault(s => s.Type == MediaStreamType.Video) : null; }
        }

        /// <summary>
        /// Gets a value indicating whether [is3 D].
        /// </summary>
        /// <value><c>true</c> if [is3 D]; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public bool Is3D
        {
            get { return Video3DFormat.HasValue; }
        }

        public bool IsHD { get; set; }

        /// <summary>
        /// Gets the type of the media.
        /// </summary>
        /// <value>The type of the media.</value>
        public override string MediaType
        {
            get
            {
                return Model.Entities.MediaType.Video;
            }
        }

        /// <summary>
        /// Overrides the base implementation to refresh metadata for local trailers
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="forceSave">if set to <c>true</c> [is new item].</param>
        /// <param name="forceRefresh">if set to <c>true</c> [force].</param>
        /// <param name="allowSlowProviders">if set to <c>true</c> [allow slow providers].</param>
        /// <param name="resetResolveArgs">The reset resolve args.</param>
        /// <returns>true if a provider reports we changed</returns>
        public override async Task<bool> RefreshMetadata(CancellationToken cancellationToken, bool forceSave = false, bool forceRefresh = false, bool allowSlowProviders = true, bool resetResolveArgs = true)
        {
            // Kick off a task to refresh the main item
            var result = await base.RefreshMetadata(cancellationToken, forceSave, forceRefresh, allowSlowProviders, resetResolveArgs).ConfigureAwait(false);

            var additionalPartsChanged = false;

            // Must have a parent to have additional parts
            // In other words, it must be part of the Parent/Child tree
            // The additional parts won't have additional parts themselves
            if (IsMultiPart && LocationType == LocationType.FileSystem && Parent != null)
            {
                try
                {
                    additionalPartsChanged = await RefreshAdditionalParts(cancellationToken, forceSave, forceRefresh, allowSlowProviders).ConfigureAwait(false);
                }
                catch (IOException ex)
                {
                    Logger.ErrorException("Error loading additional parts for {0}.", ex, Name);
                }
            }

            return additionalPartsChanged || result;
        }

        /// <summary>
        /// Refreshes the additional parts.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="forceSave">if set to <c>true</c> [force save].</param>
        /// <param name="forceRefresh">if set to <c>true</c> [force refresh].</param>
        /// <param name="allowSlowProviders">if set to <c>true</c> [allow slow providers].</param>
        /// <returns>Task{System.Boolean}.</returns>
        private async Task<bool> RefreshAdditionalParts(CancellationToken cancellationToken, bool forceSave = false, bool forceRefresh = false, bool allowSlowProviders = true)
        {
            var newItems = LoadAdditionalParts().ToList();

            var newItemIds = newItems.Select(i => i.Id).ToList();

            var itemsChanged = !AdditionalPartIds.SequenceEqual(newItemIds);

            var tasks = newItems.Select(i => i.RefreshMetadata(cancellationToken, forceSave, forceRefresh, allowSlowProviders));

            var results = await Task.WhenAll(tasks).ConfigureAwait(false);

            AdditionalPartIds = newItemIds;

            return itemsChanged || results.Contains(true);
        }

        /// <summary>
        /// Loads the additional parts.
        /// </summary>
        /// <returns>IEnumerable{Video}.</returns>
        private IEnumerable<Video> LoadAdditionalParts()
        {
            IEnumerable<FileSystemInfo> files;

            if (VideoType == VideoType.BluRay || VideoType == VideoType.Dvd)
            {
                files = new DirectoryInfo(System.IO.Path.GetDirectoryName(Path))
                    .EnumerateDirectories()
                    .Where(i => !string.Equals(i.FullName, Path, StringComparison.OrdinalIgnoreCase) && EntityResolutionHelper.IsMultiPartFile(i.Name));
            }
            else
            {
                files = ResolveArgs.FileSystemChildren.Where(i =>
                {
                    if ((i.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                    {
                        return false;
                    }

                    return !string.Equals(i.FullName, Path, StringComparison.OrdinalIgnoreCase) && EntityResolutionHelper.IsVideoFile(i.FullName) && EntityResolutionHelper.IsMultiPartFile(i.Name);
                });
            }

            return LibraryManager.ResolvePaths<Video>(files, null).Select(video =>
            {
                // Try to retrieve it from the db. If we don't find it, use the resolved version
                var dbItem = LibraryManager.RetrieveItem(video.Id) as Video;

                if (dbItem != null)
                {
                    dbItem.ResetResolveArgs(video.ResolveArgs);
                    video = dbItem;
                }

                return video;

            }).ToList();
        }

    }
}
