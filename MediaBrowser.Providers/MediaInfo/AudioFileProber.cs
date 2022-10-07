using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using TagLib;

namespace MediaBrowser.Providers.MediaInfo
{
    /// <summary>
    /// Probes audio files for metadata.
    /// </summary>
    public class AudioFileProber
    {
        private readonly IMediaEncoder _mediaEncoder;
        private readonly IItemRepository _itemRepo;
        private readonly ILibraryManager _libraryManager;
        private readonly IMediaSourceManager _mediaSourceManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioFileProber"/> class.
        /// </summary>
        /// <param name="mediaSourceManager">Instance of the <see cref="IMediaSourceManager"/> interface.</param>
        /// <param name="mediaEncoder">Instance of the <see cref="IMediaEncoder"/> interface.</param>
        /// <param name="itemRepo">Instance of the <see cref="IItemRepository"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        public AudioFileProber(
            IMediaSourceManager mediaSourceManager,
            IMediaEncoder mediaEncoder,
            IItemRepository itemRepo,
            ILibraryManager libraryManager)
        {
            _mediaEncoder = mediaEncoder;
            _itemRepo = itemRepo;
            _libraryManager = libraryManager;
            _mediaSourceManager = mediaSourceManager;
        }

        /// <summary>
        /// Probes the specified item for metadata.
        /// </summary>
        /// <param name="item">The item to probe.</param>
        /// <param name="options">The <see cref="MetadataRefreshOptions"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <typeparam name="T">The type of item to resolve.</typeparam>
        /// <returns>A <see cref="Task"/> probing the item for metadata.</returns>
        public async Task<ItemUpdateType> Probe<T>(
            T item,
            MetadataRefreshOptions options,
            CancellationToken cancellationToken)
            where T : Audio
        {
            var path = item.Path;
            var protocol = item.PathProtocol ?? MediaProtocol.File;

            if (!item.IsShortcut || options.EnableRemoteContentProbe)
            {
                if (item.IsShortcut)
                {
                    path = item.ShortcutPath;
                    protocol = _mediaSourceManager.GetPathProtocol(path);
                }

                var result = await _mediaEncoder.GetMediaInfo(
                    new MediaInfoRequest
                    {
                        MediaType = DlnaProfileType.Audio,
                        MediaSource = new MediaSourceInfo
                        {
                            Path = path,
                            Protocol = protocol
                        }
                    },
                    cancellationToken).ConfigureAwait(false);

                cancellationToken.ThrowIfCancellationRequested();

                Fetch(item, result, cancellationToken);
            }

            return ItemUpdateType.MetadataImport;
        }

        /// <summary>
        /// Fetches the specified audio.
        /// </summary>
        /// <param name="audio">The <see cref="Audio"/>.</param>
        /// <param name="mediaInfo">The <see cref="Model.MediaInfo.MediaInfo"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        protected void Fetch(Audio audio, Model.MediaInfo.MediaInfo mediaInfo, CancellationToken cancellationToken)
        {
            audio.Container = mediaInfo.Container;
            audio.TotalBitrate = mediaInfo.Bitrate;

            audio.RunTimeTicks = mediaInfo.RunTimeTicks;
            audio.Size = mediaInfo.Size;

            FetchDataFromTags(audio);

            _itemRepo.SaveMediaStreams(audio.Id, mediaInfo.MediaStreams, cancellationToken);
        }

        /// <summary>
        /// Fetches data from the tags.
        /// </summary>
        /// <param name="audio">The <see cref="Audio"/>.</param>
        private void FetchDataFromTags(Audio audio)
        {
            var file = TagLib.File.Create(audio.Path);
            var tagTypes = file.TagTypesOnDisk;
            Tag? tags = null;

            if (tagTypes.HasFlag(TagTypes.Id3v2))
            {
                tags = file.GetTag(TagTypes.Id3v2);
            }
            else if (tagTypes.HasFlag(TagTypes.Ape))
            {
                tags = file.GetTag(TagTypes.Ape);
            }
            else if (tagTypes.HasFlag(TagTypes.FlacMetadata))
            {
                tags = file.GetTag(TagTypes.FlacMetadata);
            }
            else if (tagTypes.HasFlag(TagTypes.Apple))
            {
                tags = file.GetTag(TagTypes.Apple);
            }
            else if (tagTypes.HasFlag(TagTypes.Xiph))
            {
                tags = file.GetTag(TagTypes.Xiph);
            }
            else if (tagTypes.HasFlag(TagTypes.AudibleMetadata))
            {
                tags = file.GetTag(TagTypes.AudibleMetadata);
            }
            else if (tagTypes.HasFlag(TagTypes.Id3v1))
            {
                tags = file.GetTag(TagTypes.Id3v1);
            }

            if (tags != null)
            {
                if (audio.SupportsPeople && !audio.LockedFields.Contains(MetadataField.Cast))
                {
                    var people = new List<PersonInfo>();
                    var albumArtists = tags.AlbumArtists;
                    foreach (var albumArtist in albumArtists)
                    {
                        PeopleHelper.AddPerson(people, new PersonInfo
                        {
                            Name = albumArtist,
                            Type = "AlbumArtist"
                        });
                    }

                    var performers = tags.Performers;
                    foreach (var performer in performers)
                    {
                        PeopleHelper.AddPerson(people, new PersonInfo
                        {
                            Name = performer,
                            Type = "Artist"
                        });
                    }

                    foreach (var composer in tags.Composers)
                    {
                        PeopleHelper.AddPerson(people, new PersonInfo
                        {
                            Name = composer,
                            Type = "Composer"
                        });
                    }

                    _libraryManager.UpdatePeople(audio, people);
                    audio.Artists = performers;
                    audio.AlbumArtists = albumArtists;
                }

                audio.Name = tags.Title;
                audio.Album = tags.Album;
                audio.IndexNumber = Convert.ToInt32(tags.Track);
                audio.ParentIndexNumber = Convert.ToInt32(tags.Disc);
                if (tags.Year != 0)
                {
                    var year = Convert.ToInt32(tags.Year);
                    audio.ProductionYear = year;
                    audio.PremiereDate = new DateTime(year, 01, 01);
                }

                if (!audio.LockedFields.Contains(MetadataField.Genres))
                {
                    audio.Genres = tags.Genres.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
                }

                audio.SetProviderId(MetadataProvider.MusicBrainzArtist, tags.MusicBrainzArtistId);
                audio.SetProviderId(MetadataProvider.MusicBrainzAlbumArtist, tags.MusicBrainzReleaseArtistId);
                audio.SetProviderId(MetadataProvider.MusicBrainzAlbum, tags.MusicBrainzReleaseId);
                audio.SetProviderId(MetadataProvider.MusicBrainzReleaseGroup, tags.MusicBrainzReleaseGroupId);
                audio.SetProviderId(MetadataProvider.MusicBrainzTrack, tags.MusicBrainzTrackId);
            }
        }
    }
}
