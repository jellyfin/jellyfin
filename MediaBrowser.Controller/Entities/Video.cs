using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Extensions;

using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.IO;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Extensions;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Class Video
    /// </summary>
    public class Video : BaseItem,
        IHasAspectRatio,
        ISupportsPlaceHolders,
        IHasMediaSources
    {
        [IgnoreDataMember]
        public string PrimaryVersionId { get; set; }

        public string[] AdditionalParts { get; set; }
        public string[] LocalAlternateVersions { get; set; }
        public LinkedChild[] LinkedAlternateVersions { get; set; }

        [IgnoreDataMember]
        public override bool SupportsPlayedStatus
        {
            get
            {
                return true;
            }
        }

        [IgnoreDataMember]
        public override bool SupportsInheritedParentImages
        {
            get
            {
                return true;
            }
        }

        [IgnoreDataMember]
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

        public override double? GetDefaultPrimaryImageAspectRatio()
        {
            double value = 16;
            value /= 9;

            return value;
        }

        public override string CreatePresentationUniqueKey()
        {
            if (!string.IsNullOrWhiteSpace(PrimaryVersionId))
            {
                return PrimaryVersionId;
            }

            return base.CreatePresentationUniqueKey();
        }

        [IgnoreDataMember]
        public override bool EnableRefreshOnDateModifiedChange
        {
            get
            {
                return VideoType == VideoType.VideoFile || VideoType == VideoType.Iso;
            }
        }

        [IgnoreDataMember]
        public override bool SupportsThemeMedia
        {
            get { return true; }
        }

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
        /// Gets or sets a value indicating whether this instance has subtitles.
        /// </summary>
        /// <value><c>true</c> if this instance has subtitles; otherwise, <c>false</c>.</value>
        public bool HasSubtitles { get; set; }

        public bool IsPlaceHolder { get; set; }
        public bool IsShortcut { get; set; }
        public string ShortcutPath { get; set; }

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

        public string[] GetPlayableStreamFileNames()
        {
            var videoType = VideoType;

            if (videoType == VideoType.Iso && IsoType == Model.Entities.IsoType.BluRay)
            {
                videoType = VideoType.BluRay;
            }
            else if (videoType == VideoType.Iso && IsoType == Model.Entities.IsoType.Dvd)
            {
                videoType = VideoType.Dvd;
            }
            else
            {
                return new string[] { };
            }
            return MediaEncoder.GetPlayableStreamFileNames(Path, videoType);
        }

        /// <summary>
        /// Gets or sets the aspect ratio.
        /// </summary>
        /// <value>The aspect ratio.</value>
        public string AspectRatio { get; set; }

        public Video()
        {
            AdditionalParts = EmptyStringArray;
            LocalAlternateVersions = EmptyStringArray;
            SubtitleFiles = EmptyStringArray;
            LinkedAlternateVersions = EmptyLinkedChildArray;
        }

        public override bool CanDownload()
        {
            if (VideoType == VideoType.Dvd || VideoType == VideoType.BluRay)
            {
                return false;
            }

            var locationType = LocationType;
            return locationType != LocationType.Remote &&
                   locationType != LocationType.Virtual;
        }

        [IgnoreDataMember]
        public override bool SupportsAddingToPlaylist
        {
            get { return true; }
        }

        [IgnoreDataMember]
        public int MediaSourceCount
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(PrimaryVersionId))
                {
                    var item = LibraryManager.GetItemById(PrimaryVersionId) as Video;
                    if (item != null)
                    {
                        return item.MediaSourceCount;
                    }
                }
                return LinkedAlternateVersions.Length + LocalAlternateVersions.Length + 1;
            }
        }

        [IgnoreDataMember]
        public bool IsStacked
        {
            get { return AdditionalParts.Length > 0; }
        }

        [IgnoreDataMember]
        public bool HasLocalAlternateVersions
        {
            get { return LocalAlternateVersions.Length > 0; }
        }

        public IEnumerable<Guid> GetAdditionalPartIds()
        {
            return AdditionalParts.Select(i => LibraryManager.GetNewItemId(i, typeof(Video)));
        }

        public IEnumerable<Guid> GetLocalAlternateVersionIds()
        {
            return LocalAlternateVersions.Select(i => LibraryManager.GetNewItemId(i, typeof(Video)));
        }

        [IgnoreDataMember]
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

        protected bool IsActiveRecording()
        {
            return LiveTvManager.GetActiveRecordingInfo(Path) != null;
        }

        public override bool CanDelete()
        {
            if (IsActiveRecording())
            {
                return false;
            }

            return base.CanDelete();
        }

        [IgnoreDataMember]
        public bool IsCompleteMedia
        {
            get { return !IsActiveRecording(); }
        }

        [IgnoreDataMember]
        protected virtual bool EnableDefaultVideoUserDataKeys
        {
            get
            {
                return true;
            }
        }

        public override List<string> GetUserDataKeys()
        {
            var list = base.GetUserDataKeys();

            if (EnableDefaultVideoUserDataKeys)
            {
                if (ExtraType.HasValue)
                {
                    var key = this.GetProviderId(MetadataProviders.Tmdb);
                    if (!string.IsNullOrWhiteSpace(key))
                    {
                        list.Insert(0, GetUserDataKey(key));
                    }

                    key = this.GetProviderId(MetadataProviders.Imdb);
                    if (!string.IsNullOrWhiteSpace(key))
                    {
                        list.Insert(0, GetUserDataKey(key));
                    }
                }
                else
                {
                    var key = this.GetProviderId(MetadataProviders.Imdb);
                    if (!string.IsNullOrWhiteSpace(key))
                    {
                        list.Insert(0, key);
                    }

                    key = this.GetProviderId(MetadataProviders.Tmdb);
                    if (!string.IsNullOrWhiteSpace(key))
                    {
                        list.Insert(0, key);
                    }
                }
            }

            return list;
        }

        private string GetUserDataKey(string providerId)
        {
            var key = providerId + "-" + ExtraType.ToString().ToLower();

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
                .Where(i => i != null)
                .OfType<Video>()
                .OrderBy(i => i.SortName);
        }

        /// <summary>
        /// Gets the additional parts.
        /// </summary>
        /// <returns>IEnumerable{Video}.</returns>
        public IEnumerable<Video> GetAdditionalParts()
        {
            return GetAdditionalPartIds()
                .Select(i => LibraryManager.GetItemById(i))
                .Where(i => i != null)
                .OfType<Video>()
                .OrderBy(i => i.SortName);
        }

        [IgnoreDataMember]
        public override string ContainingFolderPath
        {
            get
            {
                if (IsStacked)
                {
                    return FileSystem.GetDirectoryName(Path);
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

        [IgnoreDataMember]
        public override string FileNameWithoutExtension
        {
            get
            {
                if (LocationType == LocationType.FileSystem)
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

        internal override ItemUpdateType UpdateFromResolvedItem(BaseItem newItem)
        {
            var updateType = base.UpdateFromResolvedItem(newItem);

            var newVideo = newItem as Video;
            if (newVideo != null)
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

        public static string[] QueryPlayableStreamFiles(string rootPath, VideoType videoType)
        {
            if (videoType == VideoType.Dvd)
            {
                return FileSystem.GetFiles(rootPath, new[] { ".vob" }, false, true)
                    .OrderByDescending(i => i.Length)
                    .ThenBy(i => i.FullName)
                    .Take(1)
                    .Select(i => i.FullName)
                    .ToArray();
            }
            if (videoType == VideoType.BluRay)
            {
                return FileSystem.GetFiles(rootPath, new[] { ".m2ts" }, false, true)
                    .OrderByDescending(i => i.Length)
                    .ThenBy(i => i.FullName)
                    .Take(1)
                    .Select(i => i.FullName)
                    .ToArray();
            }
            return new string[] { };
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

        /// <summary>
        /// Gets the type of the media.
        /// </summary>
        /// <value>The type of the media.</value>
        [IgnoreDataMember]
        public override string MediaType
        {
            get
            {
                return Model.Entities.MediaType.Video;
            }
        }

        protected override async Task<bool> RefreshedOwnedItems(MetadataRefreshOptions options, List<FileSystemMetadata> fileSystemChildren, CancellationToken cancellationToken)
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
            if (LocationType == LocationType.FileSystem && GetParent() != null)
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
                if (child.ItemId.HasValue && child.ItemId.Value == Guid.Empty)
                {
                    child.ItemId = null;
                }
            }
        }

        public override void UpdateToRepository(ItemUpdateType updateReason, CancellationToken cancellationToken)
        {
            base.UpdateToRepository(updateReason, cancellationToken);

            var localAlternates = GetLocalAlternateVersionIds()
                .Select(i => LibraryManager.GetItemById(i))
                .Where(i => i != null);

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

                item.UpdateToRepository(ItemUpdateType.MetadataDownload, cancellationToken);
            }
        }

        public override IEnumerable<FileSystemMetadata> GetDeletePaths()
        {
            if (!IsInMixedFolder)
            {
                return new[] {
                    new FileSystemMetadata
                    {
                        FullName = ContainingFolderPath,
                        IsDirectory = true
                    }
                };
            }

            return base.GetDeletePaths();
        }

        public List<MediaStream> GetMediaStreams()
        {
            return MediaSourceManager.GetMediaStreams(new MediaStreamQuery
            {
                ItemId = Id
            });
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

        private List<Tuple<Video, MediaSourceType>> GetAllVideosForMediaSources()
        {
            var list = new List<Tuple<Video, MediaSourceType>>();

            list.Add(new Tuple<Video, MediaSourceType>(this, MediaSourceType.Default));
            list.AddRange(GetLinkedAlternateVersions().Select(i => new Tuple<Video, MediaSourceType>(i, MediaSourceType.Grouping)));

            if (!string.IsNullOrWhiteSpace(PrimaryVersionId))
            {
                var primary = LibraryManager.GetItemById(PrimaryVersionId) as Video;
                if (primary != null)
                {
                    var existingIds = list.Select(i => i.Item1.Id).ToList();
                    list.Add(new Tuple<Video, MediaSourceType>(primary, MediaSourceType.Grouping));
                    list.AddRange(primary.GetLinkedAlternateVersions().Where(i => !existingIds.Contains(i.Id)).Select(i => new Tuple<Video, MediaSourceType>(i, MediaSourceType.Grouping)));
                }
            }

            var localAlternates = list
                .SelectMany(i => i.Item1.GetLocalAlternateVersionIds())
                .Select(LibraryManager.GetItemById)
                .Where(i => i != null)
                .OfType<Video>()
                .ToList();

            list.AddRange(localAlternates.Select(i => new Tuple<Video, MediaSourceType>(i, MediaSourceType.Default)));

            return list;
        }

        public virtual List<MediaSourceInfo> GetMediaSources(bool enablePathSubstitution)
        {
            if (SourceType == SourceType.Channel)
            {
                var sources = ChannelManager.GetStaticMediaSources(this, CancellationToken.None)
                           .ToList();

                if (sources.Count > 0)
                {
                    return sources;
                }

                return new List<MediaSourceInfo>
                {
                    GetVersionInfo(enablePathSubstitution, this, MediaSourceType.Placeholder)
                };
            }

            var list = GetAllVideosForMediaSources();
            var result = list.Select(i => GetVersionInfo(enablePathSubstitution, i.Item1, i.Item2)).ToList();

            if (IsActiveRecording())
            {
                foreach (var mediaSource in result)
                {
                    mediaSource.Type = MediaSourceType.Placeholder;
                }
            }

            return result.OrderBy(i =>
            {
                if (i.VideoType == VideoType.VideoFile)
                {
                    return 0;
                }

                return 1;

            }).ThenBy(i => i.Video3DFormat.HasValue ? 1 : 0)
            .ThenByDescending(i =>
            {
                var stream = i.VideoStream;

                return stream == null || stream.Width == null ? 0 : stream.Width.Value;
            })
            .ToList();
        }

        private static MediaSourceInfo GetVersionInfo(bool enablePathSubstitution, Video media, MediaSourceType type)
        {
            if (media == null)
            {
                throw new ArgumentNullException("media");
            }

            var mediaStreams = MediaSourceManager.GetMediaStreams(media.Id);

            var locationType = media.LocationType;

            var info = new MediaSourceInfo
            {
                Id = media.Id.ToString("N"),
                IsoType = media.IsoType,
                Protocol = locationType == LocationType.Remote ? MediaProtocol.Http : MediaProtocol.File,
                MediaStreams = mediaStreams,
                Name = GetMediaSourceName(media, mediaStreams),
                Path = enablePathSubstitution ? GetMappedPath(media, media.Path, locationType) : media.Path,
                RunTimeTicks = media.RunTimeTicks,
                Video3DFormat = media.Video3DFormat,
                VideoType = media.VideoType,
                Container = media.Container,
                Size = media.Size,
                Timestamp = media.Timestamp,
                Type = type,
                SupportsDirectStream = media.VideoType == VideoType.VideoFile,
                IsRemote = media.IsShortcut
            };

            if (info.Protocol == MediaProtocol.File)
            {
                info.ETag = media.DateModified.Ticks.ToString(CultureInfo.InvariantCulture).GetMD5().ToString("N");
            }

            if (media.IsShortcut)
            {
                info.Path = media.ShortcutPath;

                if (!string.IsNullOrWhiteSpace(info.Path))
                {
                    if (info.Path.StartsWith("Http", StringComparison.OrdinalIgnoreCase))
                    {
                        info.Protocol = MediaProtocol.Http;
                        info.SupportsDirectStream = false;
                    }
                    else if (info.Path.StartsWith("Rtmp", StringComparison.OrdinalIgnoreCase))
                    {
                        info.Protocol = MediaProtocol.Rtmp;
                        info.SupportsDirectStream = false;
                    }
                    else if (info.Path.StartsWith("Rtsp", StringComparison.OrdinalIgnoreCase))
                    {
                        info.Protocol = MediaProtocol.Rtsp;
                        info.SupportsDirectStream = false;
                    }
                    else
                    {
                        info.Protocol = MediaProtocol.File;
                    }
                }
            }

            if (string.IsNullOrEmpty(info.Container))
            {
                if (media.VideoType == VideoType.VideoFile || media.VideoType == VideoType.Iso)
                {
                    if (!string.IsNullOrWhiteSpace(media.Path) && locationType != LocationType.Remote && locationType != LocationType.Virtual)
                    {
                        info.Container = System.IO.Path.GetExtension(media.Path).TrimStart('.');
                    }
                }
            }

            info.Bitrate = media.TotalBitrate;
            info.InferTotalBitrate();

            return info;
        }

        private static string GetMediaSourceName(Video video, List<MediaStream> mediaStreams)
        {
            var terms = new List<string>();

            var videoStream = mediaStreams.FirstOrDefault(i => i.Type == MediaStreamType.Video);
            var audioStream = mediaStreams.FirstOrDefault(i => i.Type == MediaStreamType.Audio);

            if (video.Video3DFormat.HasValue)
            {
                terms.Add("3D");
            }

            if (video.VideoType == VideoType.BluRay)
            {
                terms.Add("Bluray");
            }
            else if (video.VideoType == VideoType.Dvd)
            {
                terms.Add("DVD");
            }
            else if (video.VideoType == VideoType.Iso)
            {
                if (video.IsoType.HasValue)
                {
                    if (video.IsoType.Value == Model.Entities.IsoType.BluRay)
                    {
                        terms.Add("Bluray");
                    }
                    else if (video.IsoType.Value == Model.Entities.IsoType.Dvd)
                    {
                        terms.Add("DVD");
                    }
                }
                else
                {
                    terms.Add("ISO");
                }
            }

            if (videoStream != null)
            {
                if (videoStream.Width.HasValue)
                {
                    if (videoStream.Width.Value >= 3800)
                    {
                        terms.Add("4K");
                    }
                    else if (videoStream.Width.Value >= 1900)
                    {
                        terms.Add("1080P");
                    }
                    else if (videoStream.Width.Value >= 1270)
                    {
                        terms.Add("720P");
                    }
                    else if (videoStream.Width.Value >= 700)
                    {
                        terms.Add("480P");
                    }
                    else
                    {
                        terms.Add("SD");
                    }
                }
            }

            if (videoStream != null && !string.IsNullOrWhiteSpace(videoStream.Codec))
            {
                terms.Add(videoStream.Codec.ToUpper());
            }

            if (audioStream != null)
            {
                var audioCodec = string.Equals(audioStream.Codec, "dca", StringComparison.OrdinalIgnoreCase)
                    ? audioStream.Profile
                    : audioStream.Codec;

                if (!string.IsNullOrEmpty(audioCodec))
                {
                    terms.Add(audioCodec.ToUpper());
                }
            }

            return string.Join("/", terms.ToArray(terms.Count));
        }

    }
}
