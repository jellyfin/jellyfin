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
        public string PrimaryVersionId { get; set; }

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
                return GetMediaSourceCount(new HashSet<Guid>());
            }
        }

        [JsonIgnore]
        public bool IsStacked => AdditionalParts.Length > 0;

        [JsonIgnore]
        public override bool HasLocalAlternateVersions => LocalAlternateVersions.Length > 0;

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

        public void SetPrimaryVersionId(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                PrimaryVersionId = null;
            }
            else
            {
                PrimaryVersionId = id;
            }

            PresentationUniqueKey = CreatePresentationUniqueKey();
        }

        public override string CreatePresentationUniqueKey()
        {
            if (!string.IsNullOrEmpty(PrimaryVersionId))
            {
                return PrimaryVersionId;
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

        public IEnumerable<Guid> GetLocalAlternateVersionIds()
        {
            return LocalAlternateVersions.Select(i => LibraryManager.GetNewItemId(i, typeof(Video)));
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

        public IEnumerable<Video> GetLinkedAlternateVersions()
        {
            return LinkedAlternateVersions
                .Select(GetLinkedChild)
                .Where(i => i is not null)
                .OfType<Video>()
                .OrderBy(i => i.SortName);
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
                if (!IsStacked)
                {
                    RefreshLinkedAlternateVersions();

                    var tasks = LocalAlternateVersions
                        .Select(i => RefreshMetadataForOwnedVideo(options, false, i, cancellationToken));

                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }
            }

            return hasChanges;
        }

        private void RefreshLinkedAlternateVersions()
        {
            foreach (var child in LinkedAlternateVersions)
            {
                // Reset the cached value
                if (child.ItemId.IsNullOrEmpty())
                {
                    child.ItemId = null;
                }
            }
        }

        /// <inheritdoc />
        public override async Task UpdateToRepositoryAsync(ItemUpdateType updateReason, CancellationToken cancellationToken)
        {
            await base.UpdateToRepositoryAsync(updateReason, cancellationToken).ConfigureAwait(false);

            var localAlternates = GetLocalAlternateVersionIds()
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

            list.AddRange(GetLinkedAlternateVersions().Select(i => ((BaseItem)i, MediaSourceType.Grouping)));

            if (!string.IsNullOrEmpty(PrimaryVersionId))
            {
                if (LibraryManager.GetItemById(PrimaryVersionId) is Video primary)
                {
                    var existingIds = list.Select(i => i.Item1.Id).ToList();
                    list.Add((primary, MediaSourceType.Grouping));
                    list.AddRange(primary.GetLinkedAlternateVersions().Where(i => !existingIds.Contains(i.Id)).Select(i => ((BaseItem)i, MediaSourceType.Grouping)));
                }
            }

            var localAlternates = list
                .SelectMany(i =>
                {
                    return i.Item1 is Video video ? video.GetLocalAlternateVersionIds() : Enumerable.Empty<Guid>();
                })
                .Select(LibraryManager.GetItemById)
                .Where(i => i is not null)
                .ToList();

            list.AddRange(localAlternates.Select(i => (i, MediaSourceType.Default)));

            return list;
        }

        private int GetMediaSourceCount(HashSet<Guid> callstack)
        {
            if (!string.IsNullOrEmpty(PrimaryVersionId))
            {
                var item = LibraryManager.GetItemById(PrimaryVersionId);
                if (item is Video video)
                {
                    if (callstack.Contains(video.Id))
                    {
                        return video.LinkedAlternateVersions.Length + video.LocalAlternateVersions.Length + 1;
                    }

                    callstack.Add(video.Id);
                    return video.GetMediaSourceCount(callstack);
                }
            }

            return LinkedAlternateVersions.Length + LocalAlternateVersions.Length + 1;
        }
    }
}
