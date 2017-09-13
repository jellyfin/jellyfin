using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Serialization;

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
        IHasMediaSources
    {
        /// <summary>
        /// Gets or sets the artist.
        /// </summary>
        /// <value>The artist.</value>
        [IgnoreDataMember]
        public string[] Artists { get; set; }

        [IgnoreDataMember]
        public string[] AlbumArtists { get; set; }

        [IgnoreDataMember]
        public override bool EnableRefreshOnDateModifiedChange
        {
            get { return true; }
        }

        public Audio()
        {
            Artists = EmptyStringArray;
            AlbumArtists = EmptyStringArray;
        }

        public override double? GetDefaultPrimaryImageAspectRatio()
        {
            return 1;
        }

        [IgnoreDataMember]
        public override bool SupportsPlayedStatus
        {
            get
            {
                return true;
            }
        }

        [IgnoreDataMember]
        public override bool SupportsAddingToPlaylist
        {
            get { return true; }
        }

        [IgnoreDataMember]
        public override bool SupportsInheritedParentImages
        {
            get { return true; }
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

        public override bool CanDownload()
        {
            var locationType = LocationType;
            return locationType != LocationType.Remote &&
                   locationType != LocationType.Virtual;
        }

        [IgnoreDataMember]
        public string[] AllArtists
        {
            get
            {
                var list = new string[AlbumArtists.Length + Artists.Length];

                var index = 0;
                foreach (var artist in AlbumArtists)
                {
                    list[index] = artist;
                    index++;
                }
                foreach (var artist in Artists)
                {
                    list[index] = artist;
                    index++;
                }

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

            var albumArtist = AlbumArtists.Length == 0 ? null : AlbumArtists[0];
            if (!string.IsNullOrWhiteSpace(albumArtist))
            {
                songKey = albumArtist + "-" + songKey;
            }

            list.Insert(0, songKey);

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

        public List<MediaStream> GetMediaStreams()
        {
            return MediaSourceManager.GetMediaStreams(new MediaStreamQuery
            {
                ItemId = Id
            });
        }

        public List<MediaStream> GetMediaStreams(MediaStreamType type)
        {
            return MediaSourceManager.GetMediaStreams(new MediaStreamQuery
            {
                ItemId = Id,
                Type = type
            });
        }

        public SongInfo GetLookupInfo()
        {
            var info = GetItemLookupInfo<SongInfo>();

            info.AlbumArtists = AlbumArtists;
            info.Album = Album;
            info.Artists = Artists;

            return info;
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
                MediaStreams = MediaSourceManager.GetMediaStreams(i.Id),
                Name = i.Name,
                Path = enablePathSubstituion ? GetMappedPath(i, i.Path, locationType) : i.Path,
                RunTimeTicks = i.RunTimeTicks,
                Container = i.Container,
                Size = i.Size
            };

            if (info.Protocol == MediaProtocol.File)
            {
                info.ETag = i.DateModified.Ticks.ToString(CultureInfo.InvariantCulture).GetMD5().ToString("N");
            }

            if (string.IsNullOrEmpty(info.Container))
            {
                if (!string.IsNullOrWhiteSpace(i.Path) && locationType != LocationType.Remote && locationType != LocationType.Virtual)
                {
                    info.Container = System.IO.Path.GetExtension(i.Path).TrimStart('.');
                }
            }

            info.Bitrate = i.TotalBitrate;
            info.InferTotalBitrate();

            return info;
        }
    }
}
