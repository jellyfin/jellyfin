using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using MediaBrowser.Controller.Channels;

namespace MediaBrowser.Controller.Entities.Audio
{
    /// <summary>
    /// Class Audio
    /// </summary>
    public class Audio : BaseItem,
        IHasAlbumArtist,
        IHasArtist,
        IHasMusicGenres,
        IHasLookupInfo<SongInfo>,
        IHasMediaSources,
        IThemeMedia,
        IArchivable
    {
        public List<ChannelMediaInfo> ChannelMediaSources { get; set; }

        public int? TotalBitrate { get; set; }
        public ExtraType? ExtraType { get; set; }

        /// <summary>
        /// Gets or sets the artist.
        /// </summary>
        /// <value>The artist.</value>
        public List<string> Artists { get; set; }

        public List<string> AlbumArtists { get; set; }

        [IgnoreDataMember]
        public bool IsThemeMedia
        {
            get
            {
                return ExtraType.HasValue && ExtraType.Value == Model.Entities.ExtraType.ThemeSong;
            }
        }

        [IgnoreDataMember]
        public override bool EnableForceSaveOnDateModifiedChange
        {
            get { return true; }
        }

        public Audio()
        {
            Artists = new List<string>();
            AlbumArtists = new List<string>();
        }

        [IgnoreDataMember]
        public override bool SupportsAddingToPlaylist
        {
            get { return LocationType == LocationType.FileSystem && RunTimeTicks.HasValue; }
        }

        [IgnoreDataMember]
        protected override bool SupportsOwnedItems
        {
            get
            {
                return false;
            }
        }

        [IgnoreDataMember]
        public override Folder LatestItemsIndexContainer
        {
            get
            {
                return AlbumEntity;
            }
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

        public override bool CanDownload()
        {
            var locationType = LocationType;
            return locationType != LocationType.Remote &&
                   locationType != LocationType.Virtual;
        }

        [IgnoreDataMember]
        public List<string> AllArtists
        {
            get
            {
                var list = AlbumArtists.ToList();

                list.AddRange(Artists);

                return list;

            }
        }

        [IgnoreDataMember]
        public MusicAlbum AlbumEntity
        {
            get { return FindParent<MusicAlbum>(); }
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
                return Model.Entities.MediaType.Audio;
            }
        }

        /// <summary>
        /// Creates the name of the sort.
        /// </summary>
        /// <returns>System.String.</returns>
        protected override string CreateSortName()
        {
            return (ParentIndexNumber != null ? ParentIndexNumber.Value.ToString("0000 - ") : "")
                    + (IndexNumber != null ? IndexNumber.Value.ToString("0000 - ") : "") + Name;
        }

        public override List<string> GetUserDataKeys()
        {
            var list = base.GetUserDataKeys();

            if (ConfigurationManager.Configuration.EnableStandaloneMusicKeys)
            {
                var songKey = IndexNumber.HasValue ? IndexNumber.Value.ToString("0000") : string.Empty;


                if (ParentIndexNumber.HasValue)
                {
                    songKey = ParentIndexNumber.Value.ToString("0000") + "-" + songKey;
                }
                songKey += Name;

                if (!string.IsNullOrWhiteSpace(Album))
                {
                    songKey = Album + "-" + songKey;
                }

                var albumArtist = AlbumArtists.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(albumArtist))
                {
                    songKey = albumArtist + "-" + songKey;
                }

                list.Insert(0, songKey);
            }
            else
            {
                var parent = AlbumEntity;

                if (parent != null && IndexNumber.HasValue)
                {
                    list.InsertRange(0, parent.GetUserDataKeys().Select(i =>
                    {
                        var songKey = (ParentIndexNumber != null ? ParentIndexNumber.Value.ToString("0000 - ") : "")
                                      + IndexNumber.Value.ToString("0000 - ");

                        return i + songKey;
                    }));
                }
            }

            return list;
        }

        public override UnratedItem GetBlockUnratedType()
        {
            if (SourceType == SourceType.Library)
            {
                return UnratedItem.Music;
            }
            return base.GetBlockUnratedType();
        }

        public SongInfo GetLookupInfo()
        {
            var info = GetItemLookupInfo<SongInfo>();

            info.AlbumArtists = AlbumArtists;
            info.Album = Album;
            info.Artists = Artists;

            return info;
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

                var list = new List<MediaSourceInfo>
                {
                    GetVersionInfo(this, enablePathSubstitution)
                };

                foreach (var mediaSource in list)
                {
                    if (string.IsNullOrWhiteSpace(mediaSource.Path))
                    {
                        mediaSource.Type = MediaSourceType.Placeholder;
                    }
                }

                return list;
            }

            var result = new List<MediaSourceInfo>
            {
                GetVersionInfo(this, enablePathSubstitution)
            };

            return result;
        }

        private static MediaSourceInfo GetVersionInfo(Audio i, bool enablePathSubstituion)
        {
            var locationType = i.LocationType;

            var info = new MediaSourceInfo
            {
                Id = i.Id.ToString("N"),
                Protocol = locationType == LocationType.Remote ? MediaProtocol.Http : MediaProtocol.File,
                MediaStreams = MediaSourceManager.GetMediaStreams(i.Id).ToList(),
                Name = i.Name,
                Path = enablePathSubstituion ? GetMappedPath(i.Path, locationType) : i.Path,
                RunTimeTicks = i.RunTimeTicks,
                Container = i.Container,
                Size = i.Size
            };

            if (string.IsNullOrEmpty(info.Container))
            {
                if (!string.IsNullOrWhiteSpace(i.Path) && locationType != LocationType.Remote && locationType != LocationType.Virtual)
                {
                    info.Container = System.IO.Path.GetExtension(i.Path).TrimStart('.');
                }
            }

            var bitrate = i.TotalBitrate ??
                info.MediaStreams.Where(m => m.Type == MediaStreamType.Audio)
                .Select(m => m.BitRate ?? 0)
                .Sum();

            if (bitrate > 0)
            {
                info.Bitrate = bitrate;
            }

            return info;
        }
    }
}
