using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Jellyfin.KodiMetadata.Models;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.KodiMetadata.Providers
{
    /// <summary>
    /// The base nfo metadata provider.
    /// </summary>
    /// <typeparam name="T1">The media object for which to provide metadata.</typeparam>
    /// <typeparam name="T2">The nfo object type.</typeparam>
    public abstract class BaseNfoProvider<T1, T2> : ILocalMetadataProvider<T1>, IHasItemChangeMonitor
        where T1 : BaseItem, new()
        where T2 : BaseNfo, new()
    {
        private readonly IFileSystem _fileSystem;
        private readonly IXmlSerializer _xmlSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseNfoProvider{T1, T2}"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{TCategoryName}"/> interface.</param>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
        public BaseNfoProvider(
            ILogger<BaseNfoProvider<T1, T2>> logger,
            IFileSystem fileSystem,
            IXmlSerializer xmlSerializer)
        {
            Logger = logger;
            _fileSystem = fileSystem;
            _xmlSerializer = xmlSerializer;
        }

        /// <summary>
        /// Gets the logger.
        /// </summary>
        public ILogger<BaseNfoProvider<T1, T2>> Logger { get; }

        /// <inheritdoc/>
        public string Name => "Nfo";

        /// <inheritdoc/>
        public Task<MetadataResult<T1>> GetMetadata(ItemInfo info, IDirectoryService directoryService, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<T1>();
            result.Item = new T1();

            var file = GetXmlFile(info, directoryService);

            if (file == null)
            {
                return Task.FromResult(result);
            }

            using var fileStream = File.OpenRead(file.FullName);
            try
            {
                var nfo = _xmlSerializer.DeserializeFromStream(typeof(T2), fileStream) as T2;
                MapNfoToJellyfinObject(nfo, result);
            }
            catch (XmlException e)
            {
                Logger.LogError(e, "Error deserializing {FullName}", file.FullName);
            }

            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public bool HasChanged(BaseItem item, IDirectoryService directoryService)
        {
            var file = GetXmlFile(new ItemInfo(item), directoryService);

            if (file == null)
            {
                return false;
            }

            return file.Exists && _fileSystem.GetLastWriteTimeUtc(file) > item.DateLastSaved;
        }

        /// <summary>
        /// Maps the <see cref="BaseNfo"/> object to the jellyfin <see cref="T2"/> object.
        /// </summary>
        /// <param name="nfo">The nfo object.</param>
        /// <param name="metadataResult">The resulting metadata object.</param>
        public virtual void MapNfoToJellyfinObject(T2? nfo, MetadataResult<T1> metadataResult)
        {
            if (nfo == null)
            {
                return;
            }

            var item = new T1()
            {
                DateCreated = nfo.DateAdded.GetValueOrDefault().ToUniversalTime(),
                OriginalTitle = nfo.OriginalTitle,
                Name = nfo.LocalTitle ?? nfo.Title,
                CriticRating = nfo.CriticRating,
                SortName = nfo.SortTitle,
                Overview = nfo.Review ?? nfo.Plot ?? nfo.Biography,
                PreferredMetadataLanguage = nfo.Language,
                PreferredMetadataCountryCode = nfo.CountryCode,
                Tagline = nfo.Tagline,
                ProductionLocations = nfo.Countries,
                OfficialRating = nfo.Mpaa,
                CustomRating = nfo.CustomRating,
                RunTimeTicks = TimeSpan.FromMinutes(nfo.Runtime ?? 0).Ticks,
                IsLocked = nfo.LockData,
                Studios = new[] { nfo.Studio },
                ProductionYear = nfo.Year,
                PremiereDate = (nfo.Premiered ?? nfo.Formed ?? nfo.Aired).GetValueOrDefault().ToUniversalTime(),
                EndDate = nfo.EndDate.GetValueOrDefault().ToUniversalTime()
            };

            var trailerUrl = nfo.Trailer?.Replace("plugin://plugin.video.youtube/?action=play_video&videoid=", "https://www.youtube.com/watch?v=", StringComparison.OrdinalIgnoreCase);
            if (trailerUrl != null)
            {
                item.AddTrailerUrl(trailerUrl);
            }

            if (item is IHasDisplayOrder hasDisplayOrder && nfo.DisplayOrder != null)
            {
                hasDisplayOrder.DisplayOrder = nfo.DisplayOrder;
            }

            if (nfo.Ratings.Length != 0)
            {
                item.CriticRating = nfo.Ratings
                    .First(x => x.Name != null &&
                                x.Name.Contains("tomato", StringComparison.OrdinalIgnoreCase) &&
                                !x.Name.Contains("audience", StringComparison.OrdinalIgnoreCase))
                    .Value;

                item.CommunityRating = nfo.Ratings
                    .First(x => x.Default)
                    .Value;
            }
            else if (nfo.Rating != null)
            {
                item.CommunityRating = nfo.Rating;
            }

            foreach (var genre in nfo.Genres)
            {
                if (!string.IsNullOrWhiteSpace(genre))
                {
                    item.AddGenre(genre.Trim());
                }
            }

            foreach (var style in nfo.Styles)
            {
                if (!string.IsNullOrWhiteSpace(style))
                {
                    item.AddGenre(style.Trim());
                }
            }

            foreach (var tag in nfo.Tags)
            {
                if (!string.IsNullOrWhiteSpace(tag))
                {
                    item.AddGenre(tag.Trim());
                }
            }

            if (item is IHasAspectRatio hasAspectRatio && nfo.AspectRatio != null)
            {
                hasAspectRatio.AspectRatio = nfo.AspectRatio;
            }

            item.LockedFields = nfo.LockedFields?.Split('|').Select(i =>
            {
                if (Enum.TryParse(i, true, out MetadataField field))
                {
                    return (MetadataField?)field;
                }

                return null;
            }).OfType<MetadataField>().ToArray();

            foreach (var actor in nfo.Actors)
            {
                var person = new PersonInfo()
                {
                    Name = actor.Name?.Trim(),
                    Role = actor.Role,
                    SortOrder = actor.Order,
                    ImageUrl = actor.Thumb
                };

                switch (actor.Type)
                {
                    case PersonType.Actor:
                        person.Type = PersonType.Actor;
                        break;
                    case PersonType.Composer:
                        person.Type = PersonType.Composer;
                        break;
                    case PersonType.Conductor:
                        person.Type = PersonType.Conductor;
                        break;
                    case PersonType.Director:
                        person.Type = PersonType.Director;
                        break;
                    case PersonType.Lyricist:
                        person.Type = PersonType.Lyricist;
                        break;
                    case PersonType.Producer:
                        person.Type = PersonType.Producer;
                        break;
                    case PersonType.Writer:
                        person.Type = PersonType.Writer;
                        break;
                    case PersonType.GuestStar:
                        person.Type = PersonType.GuestStar;
                        break;
                }

                if (person.Name != null)
                {
                    metadataResult.AddPerson(person);
                }
            }

            foreach (var director in nfo.Directors)
            {
                if (!string.IsNullOrWhiteSpace(director))
                {
                    metadataResult.AddPerson(new PersonInfo()
                    {
                        Name = director.Trim(),
                        Type = PersonType.Director
                    });
                }
            }

            foreach (var credit in nfo.Credits)
            {
                if (!string.IsNullOrWhiteSpace(credit))
                {
                    metadataResult.AddPerson(new PersonInfo()
                    {
                        Name = credit.Trim(),
                        Type = PersonType.Writer
                    });
                }
            }

            foreach (var writer in nfo.Writers)
            {
                if (!string.IsNullOrWhiteSpace(writer))
                {
                    metadataResult.AddPerson(new PersonInfo()
                    {
                        Name = writer.Trim(),
                        Type = PersonType.Writer
                    });
                }
            }

            // map provider ids

            // It's okay to pass null because the method removes the providers for which the id is null
            item.SetProviderId(MetadataProvider.Imdb, nfo.ImdbId!);
            item.SetProviderId(MetadataProvider.Tmdb, nfo.TmdbId!);
            item.SetProviderId(MetadataProvider.Tvdb, nfo.TvdbId!);
            item.SetProviderId(MetadataProvider.Tvcom, nfo.TvcomId!);
            item.SetProviderId(MetadataProvider.TmdbCollection, nfo.CollectionId!);
            item.SetProviderId(MetadataProvider.MusicBrainzAlbum, nfo.MusicBrainzAlbumId!);
            item.SetProviderId(MetadataProvider.MusicBrainzAlbumArtist, nfo.MusicBrainzAlbumArtistId!);
            item.SetProviderId(MetadataProvider.MusicBrainzArtist, nfo.MusicBrainzArtistId!);
            item.SetProviderId(MetadataProvider.MusicBrainzReleaseGroup, nfo.MusicBrainzReleaseGroupId!);
            item.SetProviderId(MetadataProvider.Zap2It, nfo.Zap2ItId!);
            item.SetProviderId(MetadataProvider.TvRage, nfo.TvRageId!);
            item.SetProviderId(MetadataProvider.AudioDbArtist, nfo.AudioDbArtistId!);
            item.SetProviderId(MetadataProvider.AudioDbAlbum, nfo.AudioDbAlbumId!);
            item.SetProviderId(MetadataProvider.MusicBrainzTrack, nfo.MusicBrainzTrackId!);
            item.SetProviderId(MetadataProvider.TvMaze, nfo.TvMazeId!);

            metadataResult.Item = item;
        }

        /// <summary>
        /// Gets the xml file metadata.
        /// </summary>
        /// <param name="info">The item for which the xml file should be retrieved.</param>
        /// <param name="directoryService">The directory services.</param>
        /// <returns>The file system metadata.</returns>
        protected abstract FileSystemMetadata? GetXmlFile(ItemInfo info, IDirectoryService directoryService);
    }
}
