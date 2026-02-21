#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Class Video.
    /// </summary>
    public class Video : BaseItem,
        IHasAspectRatio,
        ISupportsPlaceHolders,
        IHasMediaSources
    {
        public Video()
        {
            AdditionalParts = Array.Empty<string>();
            LocalAlternateVersions = Array.Empty<string>();
            SubtitleFiles = Array.Empty<string>();
            AudioFiles = Array.Empty<string>();
            LinkedAlternateVersions = Array.Empty<LinkedChild>();
        }

        [JsonIgnore]
        public Guid? PrimaryVersionId { get; set; }

        public string[] AdditionalParts { get; set; }

        public string[] LocalAlternateVersions { get; set; }

        public LinkedChild[] LinkedAlternateVersions { get; set; }

        [JsonIgnore]
        public override bool SupportsPlayedStatus => true;

        [JsonIgnore]
        public override bool SupportsPeople => true;

        [JsonIgnore]
        public override bool SupportsInheritedParentImages => true;

        [JsonIgnore]
        public override bool SupportsPositionTicksResume
        {
            get
            {
                var extraType = ExtraType;
                if (extraType.HasValue)
                {
                    if (extraType.Value == Model.Entities.ExtraType.Sample)
                    {
                        return false;
                    }

                    if (extraType.Value == Model.Entities.ExtraType.ThemeVideo)
                    {
                        return false;
                    }

                    if (extraType.Value == Model.Entities.ExtraType.Trailer)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        [JsonIgnore]
        public override bool SupportsThemeMedia => true;

        /// <summary>
        /// Gets or sets the timestamp.
        /// </summary>
        /// <value>The timestamp.</value>
        public TransportStreamTimestamp? Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the subtitle paths.
        /// </summary>
        /// <value>The subtitle paths.</value>
        public string[] SubtitleFiles { get; set; }

        /// <summary>
        /// Gets or sets the audio paths.
        /// </summary>
        /// <value>The audio paths.</value>
        public string[] AudioFiles { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance has subtitles.
        /// </summary>
        /// <value><c>true</c> if this instance has subtitles; otherwise, <c>false</c>.</value>
        public bool HasSubtitles { get; set; }

        public bool IsPlaceHolder { get; set; }

        /// <summary>
        /// Gets or sets the default index of the video stream.
        /// </summary>
        /// <value>The default index of the video stream.</value>
        public int? DefaultVideoStreamIndex { get; set; }

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
        /// Gets or sets the aspect ratio.
        /// </summary>
        /// <value>The aspect ratio.</value>
        public string AspectRatio { get; set; }

        [JsonIgnore]
        public override bool SupportsAddingToPlaylist => true;

        [JsonIgnore]
        public int MediaSourceCount
        {
            get
            {
                return GetMediaSourceCount();
            }
        }

        [JsonIgnore]
        public bool IsStacked => AdditionalParts.Length > 0;

        [JsonIgnore]
        public override bool HasLocalAlternateVersions => LibraryManager.GetLocalAlternateVersionIds(this).Any();

        public static IRecordingsManager RecordingsManager { get; set; }

        [JsonIgnore]
        public override SourceType SourceType
        {
            get
            {
                if (IsActiveRecording())
                {
                    return SourceType.LiveTV;
                }

                return base.SourceType;
            }
        }

        [JsonIgnore]
        public bool IsCompleteMedia
        {
            get
            {
                if (SourceType == SourceType.Channel)
                {
                    return !Tags.Contains("livestream", StringComparison.OrdinalIgnoreCase);
                }

                return !IsActiveRecording();
            }
        }

        [JsonIgnore]
        protected virtual bool EnableDefaultVideoUserDataKeys => true;

        [JsonIgnore]
        public override string ContainingFolderPath
        {
            get
            {
                if (IsStacked)
                {
                    return System.IO.Path.GetDirectoryName(Path);
                }

                if (!IsPlaceHolder)
                {
                    if (VideoType == VideoType.BluRay || VideoType == VideoType.Dvd)
                    {
                        return Path;
                    }
                }

                return base.ContainingFolderPath;
            }
        }

        [JsonIgnore]
        public override string FileNameWithoutExtension
        {
            get
            {
                if (IsFileProtocol)
                {
                    if (VideoType == VideoType.BluRay || VideoType == VideoType.Dvd)
                    {
                        return System.IO.Path.GetFileName(Path);
                    }

                    return System.IO.Path.GetFileNameWithoutExtension(Path);
                }

                return null;
            }
        }

        /// <summary>
        /// Gets a value indicating whether [is3 D].
        /// </summary>
        /// <value><c>true</c> if [is3 D]; otherwise, <c>false</c>.</value>
        [JsonIgnore]
        public bool Is3D => Video3DFormat.HasValue;

        /// <summary>
        /// Gets the type of the media.
        /// </summary>
        /// <value>The type of the media.</value>
        [JsonIgnore]
        public override MediaType MediaType => MediaType.Video;

        private int GetMediaSourceCount(HashSet<Guid> callstack = null)
        {
            callstack ??= new();
            if (PrimaryVersionId.HasValue)
            {
                var item = LibraryManager.GetItemById(PrimaryVersionId.Value);
                if (item is Video video)
                {
                    if (callstack.Contains(video.Id))
                    {
                        // Count alternate versions using LibraryManager
                        var linkedCount = LibraryManager.GetLinkedAlternateVersions(video).Count();
                        var localCount = LibraryManager.GetLocalAlternateVersionIds(video).Count();
                        return linkedCount + localCount + 1;
                    }

                    callstack.Add(video.Id);
                    return video.GetMediaSourceCount(callstack);
                }
            }

            // Count alternate versions using LibraryManager
            var linkedVersionCount = LibraryManager.GetLinkedAlternateVersions(this).Count();
            var localVersionCount = LibraryManager.GetLocalAlternateVersionIds(this).Count();
            return linkedVersionCount + localVersionCount + 1;
        }

        public override List<string> GetUserDataKeys()
        {
            var list = base.GetUserDataKeys();

            if (EnableDefaultVideoUserDataKeys)
            {
                if (ExtraType.HasValue)
                {
                    var key = this.GetProviderId(MetadataProvider.Tmdb);
                    if (!string.IsNullOrEmpty(key))
                    {
                        list.Insert(0, GetUserDataKey(key));
                    }

                    key = this.GetProviderId(MetadataProvider.Imdb);
                    if (!string.IsNullOrEmpty(key))
                    {
                        list.Insert(0, GetUserDataKey(key));
                    }
                }
                else
                {
                    var key = this.GetProviderId(MetadataProvider.Imdb);
                    if (!string.IsNullOrEmpty(key))
                    {
                        list.Insert(0, key);
                    }

                    key = this.GetProviderId(MetadataProvider.Tmdb);
                    if (!string.IsNullOrEmpty(key))
                    {
                        list.Insert(0, key);
                    }
                }
            }

            return list;
        }

        public void SetPrimaryVersionId(Guid? id)
        {
            PrimaryVersionId = id;
            PresentationUniqueKey = CreatePresentationUniqueKey();
        }

        public override string CreatePresentationUniqueKey()
        {
            if (PrimaryVersionId.HasValue)
            {
                return PrimaryVersionId.Value.ToString("N", CultureInfo.InvariantCulture);
            }

            return base.CreatePresentationUniqueKey();
        }

        public override bool CanDownload()
        {
            if (VideoType == VideoType.Dvd || VideoType == VideoType.BluRay)
            {
                return false;
            }

            return IsFileProtocol;
        }

        protected override bool IsActiveRecording()
        {
            return RecordingsManager.GetActiveRecordingInfo(Path) is not null;
        }

        public override bool CanDelete()
        {
            if (IsActiveRecording())
            {
                return false;
            }

            return base.CanDelete();
        }

        public IEnumerable<Guid> GetAdditionalPartIds()
        {
            return AdditionalParts.Select(i => LibraryManager.GetNewItemId(i, typeof(Video)));
        }

        private string GetUserDataKey(string providerId)
        {
            var key = providerId + "-" + ExtraType.ToString().ToLowerInvariant();

            // Make sure different trailers have their own data.
            if (RunTimeTicks.HasValue)
            {
                key += "-" + RunTimeTicks.Value.ToString(CultureInfo.InvariantCulture);
            }

            return key;
        }

        /// <summary>
        /// Gets the additional parts.
        /// </summary>
        /// <returns>IEnumerable{Video}.</returns>
        public IOrderedEnumerable<Video> GetAdditionalParts()
        {
            return GetAdditionalPartIds()
                .Select(i => LibraryManager.GetItemById(i))
                .Where(i => i is not null)
                .OfType<Video>()
                .OrderBy(i => i.SortName);
        }

        internal override ItemUpdateType UpdateFromResolvedItem(BaseItem newItem)
        {
            var updateType = base.UpdateFromResolvedItem(newItem);

            if (newItem is Video newVideo)
            {
                if (!AdditionalParts.SequenceEqual(newVideo.AdditionalParts, StringComparer.Ordinal))
                {
                    AdditionalParts = newVideo.AdditionalParts;
                    updateType |= ItemUpdateType.MetadataImport;
                }

                if (!LocalAlternateVersions.SequenceEqual(newVideo.LocalAlternateVersions, StringComparer.Ordinal))
                {
                    LocalAlternateVersions = newVideo.LocalAlternateVersions;
                    updateType |= ItemUpdateType.MetadataImport;
                }

                if (VideoType != newVideo.VideoType)
                {
                    VideoType = newVideo.VideoType;
                    updateType |= ItemUpdateType.MetadataImport;
                }
            }

            return updateType;
        }

        protected override async Task<bool> RefreshedOwnedItems(MetadataRefreshOptions options, IReadOnlyList<FileSystemMetadata> fileSystemChildren, CancellationToken cancellationToken)
        {
            var hasChanges = await base.RefreshedOwnedItems(options, fileSystemChildren, cancellationToken).ConfigureAwait(false);

            // Clean up LocalAlternateVersions - remove paths that no longer exist
            if (LocalAlternateVersions.Length > 0)
            {
                var validPaths = LocalAlternateVersions.Where(FileSystem.FileExists).ToArray();
                if (validPaths.Length != LocalAlternateVersions.Length)
                {
                    LocalAlternateVersions = validPaths;
                    hasChanges = true;
                }
            }

            if (IsStacked)
            {
                var tasks = AdditionalParts
                    .Select(i => RefreshMetadataForOwnedVideo(options, true, i, cancellationToken));

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }

            // Must have a parent to have additional parts or alternate versions
            // In other words, it must be part of the Parent/Child tree
            // The additional parts won't have additional parts themselves
            if (IsFileProtocol && SupportsOwnedItems)
            {
                // Check if LinkedChildren are in sync before processing
                var existingVersionCount = LibraryManager.GetLocalAlternateVersionIds(this).Count();
                var tasks = LocalAlternateVersions
                    .Select(i => RefreshMetadataForVersions(options, false, i, cancellationToken));

                await Task.WhenAll(tasks).ConfigureAwait(false);

                if (existingVersionCount != LocalAlternateVersions.Length)
                {
                    hasChanges = true;
                }
            }

            return hasChanges;
        }

        private async Task RefreshMetadataForVersions(MetadataRefreshOptions options, bool copyTitleMetadata, string path, CancellationToken cancellationToken)
        {
            // Ensure the alternate version exists with the correct type (e.g. Movie, not Video)
            // before refreshing. This must happen here rather than in RefreshMetadataForOwnedVideo
            // because that method is also used for stacked parts which should keep their resolved type.
            var id = LibraryManager.GetNewItemId(path, GetType());
            if (LibraryManager.GetItemById(id) is not Video && FileSystem.FileExists(path))
            {
                var parentFolder = GetParent() as Folder;
                var collectionType = LibraryManager.GetContentType(this);
                var altVideo = LibraryManager.ResolveAlternateVersion(path, GetType(), parentFolder, collectionType);
                if (altVideo is not null)
                {
                    altVideo.OwnerId = Id;
                    altVideo.SetPrimaryVersionId(Id);
                    LibraryManager.CreateItem(altVideo, GetParent());
                }
            }

            await RefreshMetadataForOwnedVideo(options, copyTitleMetadata, path, cancellationToken).ConfigureAwait(false);

            // Create LinkedChild entry for this local alternate version
            // This ensures the relationship exists in the database even if the alternate version
            // was created after the primary video was first saved
            if (LibraryManager.GetItemById(id) is Video video)
            {
                LibraryManager.UpsertLinkedChild(Id, video.Id, LinkedChildType.LocalAlternateVersion);

                // Ensure PrimaryVersionId is set for existing alternate versions that may not have it
                if (!video.PrimaryVersionId.HasValue)
                {
                    video.SetPrimaryVersionId(Id);
                    await video.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private new async Task RefreshMetadataForOwnedVideo(MetadataRefreshOptions options, bool copyTitleMetadata, string path, CancellationToken cancellationToken)
        {
            var newOptions = new MetadataRefreshOptions(options)
            {
                SearchResult = null
            };

            var id = LibraryManager.GetNewItemId(path, GetType());

            // Check if the file still exists
            if (!FileSystem.FileExists(path))
            {
                // File was removed - clean up any orphaned database entry
                if (LibraryManager.GetItemById(id) is Video orphanedVideo && orphanedVideo.OwnerId.Equals(Id))
                {
                    Logger.LogInformation("Owned video file no longer exists, removing orphaned item: {Path}", path);
                    LibraryManager.DeleteItem(orphanedVideo, new DeleteOptions { DeleteFileLocation = false });
                }

                return;
            }

            if (LibraryManager.GetItemById(id) is not Video video)
            {
                var parentFolder = GetParent() as Folder;
                var collectionType = LibraryManager.GetContentType(this);
                video = LibraryManager.ResolvePath(
                    FileSystem.GetFileSystemInfo(path),
                    parentFolder,
                    collectionType: collectionType) as Video;

                if (video is null)
                {
                    return;
                }

                video.Id = id;
                video.OwnerId = Id;
                LibraryManager.CreateItem(video, parentFolder);
                newOptions.ForceSave = true;
            }

            if (video.OwnerId.IsEmpty())
            {
                video.OwnerId = Id;
            }

            await RefreshMetadataForOwnedItem(video, copyTitleMetadata, newOptions, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public override async Task UpdateToRepositoryAsync(ItemUpdateType updateReason, CancellationToken cancellationToken)
        {
            await base.UpdateToRepositoryAsync(updateReason, cancellationToken).ConfigureAwait(false);

            var localAlternates = LibraryManager.GetLocalAlternateVersionIds(this)
                .Select(i => LibraryManager.GetItemById(i))
                .Where(i => i is not null);

            foreach (var item in localAlternates)
            {
                item.ImageInfos = ImageInfos;
                item.Overview = Overview;
                item.ProductionYear = ProductionYear;
                item.PremiereDate = PremiereDate;
                item.CommunityRating = CommunityRating;
                item.OfficialRating = OfficialRating;
                item.Genres = Genres;
                item.ProviderIds = ProviderIds;

                await item.UpdateToRepositoryAsync(ItemUpdateType.MetadataDownload, cancellationToken).ConfigureAwait(false);
            }
        }

        public override IEnumerable<FileSystemMetadata> GetDeletePaths()
        {
            if (!IsInMixedFolder)
            {
                return new[]
                {
                    new FileSystemMetadata
                    {
                        FullName = ContainingFolderPath,
                        IsDirectory = true
                    }
                };
            }

            return base.GetDeletePaths();
        }

        public virtual MediaStream GetDefaultVideoStream()
        {
            if (!DefaultVideoStreamIndex.HasValue)
            {
                return null;
            }

            return MediaSourceManager.GetMediaStreams(new MediaStreamQuery
            {
                ItemId = Id,
                Index = DefaultVideoStreamIndex.Value
            }).FirstOrDefault();
        }

        protected override IEnumerable<(BaseItem Item, MediaSourceType MediaSourceType)> GetAllItemsForMediaSources()
        {
            var list = new List<(BaseItem, MediaSourceType)>
            {
                (this, MediaSourceType.Default)
            };

            list.AddRange(LibraryManager.GetLinkedAlternateVersions(this).Select(i => ((BaseItem)i, MediaSourceType.Grouping)));

            if (PrimaryVersionId.HasValue)
            {
                if (LibraryManager.GetItemById(PrimaryVersionId.Value) is Video primary)
                {
                    var existingIds = list.Select(i => i.Item1.Id).ToList();
                    list.Add((primary, MediaSourceType.Grouping));
                    list.AddRange(LibraryManager.GetLinkedAlternateVersions(primary).Where(i => !existingIds.Contains(i.Id)).Select(i => ((BaseItem)i, MediaSourceType.Grouping)));
                }
            }

            var localAlternates = list
                .SelectMany(i =>
                {
                    return i.Item1 is Video video ? LibraryManager.GetLocalAlternateVersionIds(video) : Enumerable.Empty<Guid>();
                })
                .Select(LibraryManager.GetItemById)
                .Where(i => i is not null)
                .ToList();

            list.AddRange(localAlternates.Select(i => (i, MediaSourceType.Default)));

            return list;
        }
    }
}
