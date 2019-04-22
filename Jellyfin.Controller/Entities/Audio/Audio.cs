using System;
using System.Collections.Generic;
using Jellyfin.Controller.Persistence;
using Jellyfin.Controller.Providers;
using Jellyfin.Model.Configuration;
using Jellyfin.Model.Dto;
using Jellyfin.Model.Entities;
using Jellyfin.Model.Serialization;

namespace Jellyfin.Controller.Entities.Audio
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

        public Audio()
        {
            Artists = Array.Empty<string>();
            AlbumArtists = Array.Empty<string>();
        }

        public override double GetDefaultPrimaryImageAspectRatio()
        {
            return 1;
        }

        [IgnoreDataMember]
        public override bool SupportsPlayedStatus => true;

        [IgnoreDataMember]
        public override bool SupportsPeople => false;

        [IgnoreDataMember]
        public override bool SupportsAddingToPlaylist => true;

        [IgnoreDataMember]
        public override bool SupportsInheritedParentImages => true;

        [IgnoreDataMember]
        protected override bool SupportsOwnedItems => false;

        [IgnoreDataMember]
        public override Folder LatestItemsIndexContainer => AlbumEntity;

        public override bool CanDownload()
        {
            return IsFileProtocol;
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
        public MusicAlbum AlbumEntity => FindParent<MusicAlbum>();

        /// <summary>
        /// Gets the type of the media.
        /// </summary>
        /// <value>The type of the media.</value>
        [IgnoreDataMember]
        public override string MediaType => Model.Entities.MediaType.Audio;

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

            if (!string.IsNullOrEmpty(Album))
            {
                songKey = Album + "-" + songKey;
            }

            var albumArtist = AlbumArtists.Length == 0 ? null : AlbumArtists[0];
            if (!string.IsNullOrEmpty(albumArtist))
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

        protected override List<Tuple<BaseItem, MediaSourceType>> GetAllItemsForMediaSources()
        {
            var list = new List<Tuple<BaseItem, MediaSourceType>>();
            list.Add(new Tuple<BaseItem, MediaSourceType>(this, MediaSourceType.Default));
            return list;
        }
    }
}
