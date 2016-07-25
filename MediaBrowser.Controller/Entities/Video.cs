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
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;
using MediaBrowser.Controller.Channels;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Class Video
    /// </summary>
    public class Video : BaseItem,
        IHasAspectRatio,
        ISupportsPlaceHolders,
        IHasMediaSources,
        IHasShortOverview,
        IThemeMedia,
        IArchivable
    {
        [IgnoreDataMember]
        public string PrimaryVersionId { get; set; }

        public List<string> AdditionalParts { get; set; }
        public List<string> LocalAlternateVersions { get; set; }
        public List<LinkedChild> LinkedAlternateVersions { get; set; }
        public List<ChannelMediaInfo> ChannelMediaSources { get; set; }

        [IgnoreDataMember]
        public bool IsThemeMedia
        {
            get
            {
                return ExtraType.HasValue && ExtraType.Value == Model.Entities.ExtraType.ThemeVideo;
            }
        }

        [IgnoreDataMember]
        public override string PresentationUniqueKey
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(PrimaryVersionId))
                {
                    return PrimaryVersionId;
                }

                return base.PresentationUniqueKey;
            }
        }

        [IgnoreDataMember]
        public override bool EnableForceSaveOnDateModifiedChange
        {
            get { return true; }
        }

        public int? TotalBitrate { get; set; }
        public ExtraType? ExtraType { get; set; }

        /// <summary>
        /// Gets or sets the timestamp.
        /// </summary>
        /// <value>The timestamp.</value>
        public TransportStreamTimestamp? Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the subtitle paths.
        /// </summary>
        /// <value>The subtitle paths.</value>
        public List<string> SubtitleFiles { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance has subtitles.
        /// </summary>
        /// <value><c>true</c> if this instance has subtitles; otherwise, <c>false</c>.</value>
        public bool HasSubtitles { get; set; }

        public bool IsPlaceHolder { get; set; }
        public bool IsShortcut { get; set; }
        public string ShortcutPath { get; set; }

        /// <summary>
        /// Gets or sets the video bit rate.
        /// </summary>
        /// <value>The video bit rate.</value>
        public int? VideoBitRate { get; set; }

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

        public Video()
        {
            PlayableStreamFileNames = new List<string>();
            AdditionalParts = new List<string>();
            LocalAlternateVersions = new List<string>();
            Tags = new List<string>();
            SubtitleFiles = new List<string>();
            LinkedAlternateVersions = new List<LinkedChild>();
        }

        public override bool CanDownload()
        {
            if (VideoType == VideoType.HdDvd || VideoType == VideoType.Dvd ||
                VideoType == VideoType.BluRay)
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
            get { return LocationType == LocationType.FileSystem && RunTimeTicks.HasValue; }
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
                return LinkedAlternateVersions.Count + LocalAlternateVersions.Count + 1;
            }
        }

        [IgnoreDataMember]
        public bool IsStacked
        {
            get { return AdditionalParts.Count > 0; }
        }

        [IgnoreDataMember]
        public bool HasLocalAlternateVersions
        {
            get { return LocalAlternateVersions.Count > 0; }
        }

        [IgnoreDataMember]
        public bool IsArchive
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Path))
                {
                    return false;
                }
                var ext = System.IO.Path.GetExtension(Path) ?? string.Empty;

                return new[] { ".zip", ".rar", ".7z" }.Contains(ext, StringComparer.OrdinalIgnoreCase);
            }
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
            var linkedVersions = LinkedAlternateVersions
                .Select(GetLinkedChild)
                .Where(i => i != null)
                .OfType<Video>();

            return linkedVersions
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
                    return System.IO.Path.GetDirectoryName(Path);
                }

                if (!IsPlaceHolder)
                {
                    if (VideoType == VideoType.BluRay || VideoType == VideoType.Dvd ||
                        VideoType == VideoType.HdDvd)
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
                    if (VideoType == VideoType.BluRay || VideoType == VideoType.Dvd || VideoType == VideoType.HdDvd)
                    {
                        return System.IO.Path.GetFileName(Path);
                    }

                    return System.IO.Path.GetFileNameWithoutExtension(Path);
                }

                return null;
            }
        }

        internal override bool IsValidFromResolver(BaseItem newItem)
        {
            var current = this;

            var newAsVideo = newItem as Video;

            if (newAsVideo != null)
            {
                if (!current.AdditionalParts.SequenceEqual(newAsVideo.AdditionalParts, StringComparer.OrdinalIgnoreCase))
                {
                    return false;
                }
                if (!current.LocalAlternateVersions.SequenceEqual(newAsVideo.LocalAlternateVersions, StringComparer.OrdinalIgnoreCase))
                {
                    return false;
                }

                if (newAsVideo.VideoType != VideoType)
                {
                    return false;
                }
            }

            return base.IsValidFromResolver(newItem);
        }

        /// <summary>
        /// Gets the playable stream files.
        /// </summary>
        /// <param name="rootPath">The root path.</param>
        /// <returns>List{System.String}.</returns>
        public List<string> GetPlayableStreamFiles(string rootPath)
        {
            var allFiles = FileSystem.GetFilePaths(rootPath, true).ToList();

            return PlayableStreamFileNames.Select(name => allFiles.FirstOrDefault(f => string.Equals(System.IO.Path.GetFileName(f), name, StringComparison.OrdinalIgnoreCase)))
                .Where(f => !string.IsNullOrEmpty(f))
                .ToList();
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
                    .Select(i => RefreshMetadataForOwnedVideo(options, i, cancellationToken));

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
                        .Select(i => RefreshMetadataForOwnedVideo(options, i, cancellationToken));

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

        public override async Task UpdateToRepository(ItemUpdateType updateReason, CancellationToken cancellationToken)
        {
            await base.UpdateToRepository(updateReason, cancellationToken).ConfigureAwait(false);

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

                await item.UpdateToRepository(ItemUpdateType.MetadataDownload, cancellationToken).ConfigureAwait(false);
            }
        }

        public override IEnumerable<string> GetDeletePaths()
        {
            if (!IsInMixedFolder)
            {
                return new[] { ContainingFolderPath };
            }

            return base.GetDeletePaths();
        }

        public IEnumerable<MediaStream> GetMediaStreams()
        {
            var mediaSource = GetMediaSources(false)
                .FirstOrDefault();

            if (mediaSource == null)
            {
                return new List<MediaStream>();
            }

            return mediaSource.MediaStreams;
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

        public virtual IEnumerable<MediaSourceInfo> GetMediaSources(bool enablePathSubstitution)
        {
            if (SourceType == SourceType.Channel)
            {
                var sources = ChannelManager.GetStaticMediaSources(this, false, CancellationToken.None)
                           .Result.ToList();

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

        private static MediaSourceInfo GetVersionInfo(bool enablePathSubstitution, Video i, MediaSourceType type)
        {
            var mediaStreams = MediaSourceManager.GetMediaStreams(i.Id)
                .ToList();

            var locationType = i.LocationType;

            var info = new MediaSourceInfo
            {
                Id = i.Id.ToString("N"),
                IsoType = i.IsoType,
                Protocol = locationType == LocationType.Remote ? MediaProtocol.Http : MediaProtocol.File,
                MediaStreams = mediaStreams,
                Name = GetMediaSourceName(i, mediaStreams),
                Path = enablePathSubstitution ? GetMappedPath(i.Path, locationType) : i.Path,
                RunTimeTicks = i.RunTimeTicks,
                Video3DFormat = i.Video3DFormat,
                VideoType = i.VideoType,
                Container = i.Container,
                Size = i.Size,
                Timestamp = i.Timestamp,
                Type = type,
                PlayableStreamFileNames = i.PlayableStreamFileNames.ToList(),
                SupportsDirectStream = i.VideoType == VideoType.VideoFile
            };

            if (i.IsShortcut)
            {
                info.Path = i.ShortcutPath;

                if (info.Path.StartsWith("Http", StringComparison.OrdinalIgnoreCase))
                {
                    info.Protocol = MediaProtocol.Http;
                }
                else if (info.Path.StartsWith("Rtmp", StringComparison.OrdinalIgnoreCase))
                {
                    info.Protocol = MediaProtocol.Rtmp;
                }
                else if (info.Path.StartsWith("Rtsp", StringComparison.OrdinalIgnoreCase))
                {
                    info.Protocol = MediaProtocol.Rtsp;
                }
                else
                {
                    info.Protocol = MediaProtocol.File;
                }
            }

            if (string.IsNullOrEmpty(info.Container))
            {
                if (i.VideoType == VideoType.VideoFile || i.VideoType == VideoType.Iso)
                {
                    if (!string.IsNullOrWhiteSpace(i.Path) && locationType != LocationType.Remote && locationType != LocationType.Virtual)
                    {
                        info.Container = System.IO.Path.GetExtension(i.Path).TrimStart('.');
                    }
                }
            }

            try
            {
                var bitrate = i.TotalBitrate ??
                    info.MediaStreams.Where(m => m.Type != MediaStreamType.Subtitle && !string.Equals(m.Codec, "mjpeg", StringComparison.OrdinalIgnoreCase))
                    .Select(m => m.BitRate ?? 0)
                    .Sum();

                if (bitrate > 0)
                {
                    info.Bitrate = bitrate;
                }
            }
            catch (OverflowException ex)
            {
                Logger.ErrorException("Error calculating total bitrate", ex);
            }

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
            else if (video.VideoType == VideoType.HdDvd)
            {
                terms.Add("HD-DVD");
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

            return string.Join("/", terms.ToArray());
        }

    }
}
