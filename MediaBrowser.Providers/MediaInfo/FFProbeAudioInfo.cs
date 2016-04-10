using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Serialization;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.MediaInfo
{
    class FFProbeAudioInfo
    {
        private readonly IMediaEncoder _mediaEncoder;
        private readonly IItemRepository _itemRepo;
        private readonly IApplicationPaths _appPaths;
        private readonly IJsonSerializer _json;
        private readonly ILibraryManager _libraryManager;

        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        public FFProbeAudioInfo(IMediaEncoder mediaEncoder, IItemRepository itemRepo, IApplicationPaths appPaths, IJsonSerializer json, ILibraryManager libraryManager)
        {
            _mediaEncoder = mediaEncoder;
            _itemRepo = itemRepo;
            _appPaths = appPaths;
            _json = json;
            _libraryManager = libraryManager;
        }

        public async Task<ItemUpdateType> Probe<T>(T item, CancellationToken cancellationToken)
            where T : Audio
        {
            if (item.IsArchive)
            {
                var ext = Path.GetExtension(item.Path) ?? string.Empty;
                item.Container = ext.TrimStart('.');
                return ItemUpdateType.MetadataImport;
            }

            var result = await GetMediaInfo(item, cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            await Fetch(item, cancellationToken, result).ConfigureAwait(false);

            return ItemUpdateType.MetadataImport;
        }

        private const string SchemaVersion = "3";

        private async Task<Model.MediaInfo.MediaInfo> GetMediaInfo(BaseItem item, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            //var idString = item.Id.ToString("N");
            //var cachePath = Path.Combine(_appPaths.CachePath,
            //    "ffprobe-audio",
            //    idString.Substring(0, 2), idString, "v" + SchemaVersion + _mediaEncoder.Version + item.DateModified.Ticks.ToString(_usCulture) + ".json");

            //try
            //{
            //    return _json.DeserializeFromFile<Model.MediaInfo.MediaInfo>(cachePath);
            //}
            //catch (FileNotFoundException)
            //{

            //}
            //catch (DirectoryNotFoundException)
            //{
            //}

            var result = await _mediaEncoder.GetMediaInfo(new MediaInfoRequest
            {
                InputPath = item.Path,
                MediaType = DlnaProfileType.Audio,
                Protocol = MediaProtocol.File

            }, cancellationToken).ConfigureAwait(false);

            //Directory.CreateDirectory(Path.GetDirectoryName(cachePath));
            //_json.SerializeToFile(result, cachePath);

            return result;
        }

        /// <summary>
        /// Fetches the specified audio.
        /// </summary>
        /// <param name="audio">The audio.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="mediaInfo">The media information.</param>
        /// <returns>Task.</returns>
        protected async Task Fetch(Audio audio, CancellationToken cancellationToken, Model.MediaInfo.MediaInfo mediaInfo)
        {
            var mediaStreams = mediaInfo.MediaStreams;

            //audio.FormatName = mediaInfo.Container;
            audio.TotalBitrate = mediaInfo.Bitrate;

            audio.RunTimeTicks = mediaInfo.RunTimeTicks;
            audio.Size = mediaInfo.Size;

            var extension = (Path.GetExtension(audio.Path) ?? string.Empty).TrimStart('.');
            audio.Container = extension;

            await FetchDataFromTags(audio, mediaInfo).ConfigureAwait(false);

            await _itemRepo.SaveMediaStreams(audio.Id, mediaStreams, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Fetches data from the tags dictionary
        /// </summary>
        /// <param name="audio">The audio.</param>
        /// <param name="data">The data.</param>
        private async Task FetchDataFromTags(Audio audio, Model.MediaInfo.MediaInfo data)
        {
            // Only set Name if title was found in the dictionary
            if (!string.IsNullOrEmpty(data.Name))
            {
                audio.Name = data.Name;
            }

            if (!audio.LockedFields.Contains(MetadataFields.Cast))
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

                await _libraryManager.UpdatePeople(audio, people).ConfigureAwait(false);
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

            if (!audio.LockedFields.Contains(MetadataFields.Genres))
            {
                audio.Genres.Clear();

                foreach (var genre in data.Genres)
                {
                    audio.AddGenre(genre);
                }
            }

            if (!audio.LockedFields.Contains(MetadataFields.Studios))
            {
                audio.Studios.Clear();

                foreach (var studio in data.Studios)
                {
                    audio.AddStudio(studio);
                }
            }

            audio.SetProviderId(MetadataProviders.MusicBrainzAlbumArtist, data.GetProviderId(MetadataProviders.MusicBrainzAlbumArtist));
            audio.SetProviderId(MetadataProviders.MusicBrainzArtist, data.GetProviderId(MetadataProviders.MusicBrainzArtist));
            audio.SetProviderId(MetadataProviders.MusicBrainzAlbum, data.GetProviderId(MetadataProviders.MusicBrainzAlbum));
            audio.SetProviderId(MetadataProviders.MusicBrainzReleaseGroup, data.GetProviderId(MetadataProviders.MusicBrainzReleaseGroup));
            audio.SetProviderId(MetadataProviders.MusicBrainzTrack, data.GetProviderId(MetadataProviders.MusicBrainzTrack));
        }
    }
}
