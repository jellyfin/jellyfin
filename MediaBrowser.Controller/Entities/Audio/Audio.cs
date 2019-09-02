using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
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
        /// <inheritdoc />
        [IgnoreDataMember]
        public IReadOnlyList<string> Artists { get; set; }

        /// <inheritdoc />
        [IgnoreDataMember]
        public IReadOnlyList<string> AlbumArtists { get; set; }

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

            var albumArtist = AlbumArtists.FirstOrDefault();
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
