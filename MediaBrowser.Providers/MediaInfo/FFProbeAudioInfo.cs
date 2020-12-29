#pragma warning disable CS1591

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

namespace MediaBrowser.Providers.MediaInfo
{
    public class FFProbeAudioInfo
    {
        private readonly IMediaEncoder _mediaEncoder;
        private readonly IItemRepository _itemRepo;
        private readonly ILibraryManager _libraryManager;
        private readonly IMediaSourceManager _mediaSourceManager;

        public FFProbeAudioInfo(
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
        /// <param name="audio">The audio.</param>
        /// <param name="mediaInfo">The media information.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        protected void Fetch(Audio audio, Model.MediaInfo.MediaInfo mediaInfo, CancellationToken cancellationToken)
        {
            var mediaStreams = mediaInfo.MediaStreams;

            audio.Container = mediaInfo.Container;
            audio.TotalBitrate = mediaInfo.Bitrate;

            audio.RunTimeTicks = mediaInfo.RunTimeTicks;
            audio.Size = mediaInfo.Size;

            // var extension = (Path.GetExtension(audio.Path) ?? string.Empty).TrimStart('.');
            // audio.Container = extension;

            FetchDataFromTags(audio, mediaInfo);

            _itemRepo.SaveMediaStreams(audio.Id, mediaStreams, cancellationToken);
        }

        /// <summary>
        /// Fetches data from the tags dictionary.
        /// </summary>
        /// <param name="audio">The audio.</param>
        /// <param name="data">The data.</param>
        private void FetchDataFromTags(Audio audio, Model.MediaInfo.MediaInfo data)
        {
            // Only set Name if title was found in the dictionary
            if (!string.IsNullOrEmpty(data.Name))
            {
                audio.Name = data.Name;
            }

            if (audio.SupportsPeople && !audio.LockedFields.Contains(MetadataField.Cast))
            {
                var people = new List<PersonInfo>();

                foreach (var person in data.People)
                {
                    PeopleHelper.AddPerson(people, new PersonInfo
                    {
                        Name = person.Name,
                        Type = person.Type,
                        Role = person.Role
                    });
                }

                _libraryManager.UpdatePeople(audio, people);
            }

            audio.Album = data.Album;
            audio.Artists = data.Artists;
            audio.AlbumArtists = data.AlbumArtists;
            audio.IndexNumber = data.IndexNumber;
            audio.ParentIndexNumber = data.ParentIndexNumber;
            audio.ProductionYear = data.ProductionYear;
            audio.PremiereDate = data.PremiereDate;

            // If we don't have a ProductionYear try and get it from PremiereDate
            if (audio.PremiereDate.HasValue && !audio.ProductionYear.HasValue)
            {
                audio.ProductionYear = audio.PremiereDate.Value.ToLocalTime().Year;
            }

            if (!audio.LockedFields.Contains(MetadataField.Genres))
            {
                audio.Genres = Array.Empty<string>();

                foreach (var genre in data.Genres)
                {
                    audio.AddGenre(genre);
                }
            }

            if (!audio.LockedFields.Contains(MetadataField.Studios))
            {
                audio.SetStudios(data.Studios);
            }

            audio.SetProviderId(MetadataProvider.MusicBrainzAlbumArtist, data.GetProviderId(MetadataProvider.MusicBrainzAlbumArtist));
            audio.SetProviderId(MetadataProvider.MusicBrainzArtist, data.GetProviderId(MetadataProvider.MusicBrainzArtist));
            audio.SetProviderId(MetadataProvider.MusicBrainzAlbum, data.GetProviderId(MetadataProvider.MusicBrainzAlbum));
            audio.SetProviderId(MetadataProvider.MusicBrainzReleaseGroup, data.GetProviderId(MetadataProvider.MusicBrainzReleaseGroup));
            audio.SetProviderId(MetadataProvider.MusicBrainzTrack, data.GetProviderId(MetadataProvider.MusicBrainzTrack));
        }
    }
}
