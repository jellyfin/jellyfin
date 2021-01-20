using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Jellyfin.NfoMetadata.Models;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.NfoMetadata.Providers
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
                result.HasMetadata = true;
            }
            catch (XmlException e)
            {
                Logger.LogError(e, "Error deserializing {FullName}", file.FullName);
                result.HasMetadata = false;
            }
            catch (InvalidOperationException e)
            {
                Logger.LogError(e, "Nfo file {FullName} doesn't have the right XML root tag.", file.FullName);
                result.HasMetadata = false;
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
        /// Maps the <see cref="BaseNfo"/> object to the jellyfin object.
        /// </summary>
        /// <param name="nfo">The nfo object.</param>
        /// <param name="metadataResult">The resulting metadata object.</param>
        public virtual void MapNfoToJellyfinObject(T2? nfo, MetadataResult<T1> metadataResult)
        {
            if (nfo == null)
            {
                throw new ArgumentException("Nfo can't be null", nameof(nfo));
            }

            if (metadataResult.Item == null)
            {
                throw new ArgumentException("Item can't be null.", nameof(metadataResult));
            }

            var item = new T1()
            {
                DateCreated = nfo.DateAdded.GetValueOrDefault(),
                OriginalTitle = nfo.OriginalTitle,
                Name = nfo.LocalTitle ?? nfo.Title ?? nfo.Name,
                CriticRating = nfo.CriticRating,
                SortName = nfo.SortTitle ?? nfo.SortName,
                Overview = nfo.Review ?? nfo.Plot ?? nfo.Biography,
                PreferredMetadataLanguage = nfo.Language,
                PreferredMetadataCountryCode = nfo.CountryCode,
                Tagline = nfo.Tagline,
                OfficialRating = nfo.Mpaa,
                CustomRating = nfo.CustomRating,
                RunTimeTicks = TimeSpan.FromMinutes(nfo.Runtime ?? 0).Ticks,
                IsLocked = nfo.LockData,
                Studios = nfo.Studios,
                ProductionYear = nfo.Year,
                PremiereDate = (nfo.Premiered ?? nfo.Formed ?? nfo.Aired).GetValueOrDefault(),
                EndDate = nfo.EndDate.GetValueOrDefault()
            };

            if (item is Video video)
            {
                if (nfo.FileInfo?.StreamDetails?.Video != null)
                {
                    foreach (var videoNfo in nfo.FileInfo.StreamDetails.Video)
                    {
                        switch (videoNfo.Format3D)
                        {
                            case "HSBS":
                                video.Video3DFormat = Video3DFormat.HalfSideBySide;
                                break;
                            case "HTAG":
                                video.Video3DFormat = Video3DFormat.HalfTopAndBottom;
                                break;
                            case "FTAB":
                                video.Video3DFormat = Video3DFormat.FullTopAndBottom;
                                break;
                            case "FSBS":
                                video.Video3DFormat = Video3DFormat.FullSideBySide;
                                break;
                            case "MVC":
                                video.Video3DFormat = Video3DFormat.MVC;
                                break;
                        }

                        video.Height = nfo.FileInfo.StreamDetails.Video[0].Height ?? 0;
                        video.Width = nfo.FileInfo.StreamDetails.Video[0].Width ?? 0;
                        int seconds = Convert.ToInt32(videoNfo.DurationInSeconds ?? nfo.Runtime ?? 0);
                        video.RunTimeTicks = new TimeSpan(0, 0, seconds).Ticks;
                    }
                }

                // Subtitles
                if (nfo.FileInfo?.StreamDetails?.Subtitle != null && nfo.FileInfo.StreamDetails.Subtitle.Length != 0)
                {
                    video.HasSubtitles = true;
                }
            }

            if (item is IHasAspectRatio hasAspectRatio && (nfo.AspectRatio != null || nfo.FileInfo?.StreamDetails?.Video?[0].AspectRatio != null))
            {
                hasAspectRatio.AspectRatio = nfo.FileInfo?.StreamDetails?.Video?[0].AspectRatio ?? nfo.AspectRatio;
            }

            foreach (var trailer in nfo.Trailers ?? Array.Empty<string>())
            {
                var trailerUrl = trailer.Replace("plugin://plugin.video.youtube/?action=play_video&videoid=", "https://www.youtube.com/watch?v=", StringComparison.OrdinalIgnoreCase);
                if (!string.IsNullOrWhiteSpace(trailerUrl))
                {
                    item.AddTrailerUrl(trailerUrl);
                }
            }

            if (item is IHasDisplayOrder hasDisplayOrder && nfo.DisplayOrder != null)
            {
                hasDisplayOrder.DisplayOrder = nfo.DisplayOrder;
            }

            if (nfo.Ratings != null && nfo.Ratings.Length != 0)
            {
                item.CriticRating = nfo.Ratings
                    .FirstOrDefault(x => x.Name != null &&
                                x.Name.Contains("tomato", StringComparison.OrdinalIgnoreCase) &&
                                !x.Name.Contains("audience", StringComparison.OrdinalIgnoreCase))?
                    .Value;

                item.CommunityRating = nfo.Ratings
                    .FirstOrDefault(x => x.Default)?
                    .Value;
            }
            else if (nfo.Rating != null)
            {
                item.CommunityRating = nfo.Rating;
            }

            foreach (var style in nfo.Styles ?? Array.Empty<string>())
            {
                if (!string.IsNullOrWhiteSpace(style))
                {
                    item.AddTag(style.Trim());
                }
            }

            foreach (var tag in nfo.Tags ?? Array.Empty<string>())
            {
                if (!string.IsNullOrWhiteSpace(tag))
                {
                    item.AddTag(tag.Trim());
                }
            }

            item.LockedFields = nfo.LockedFields?.Split('|').Select(i =>
            {
                if (Enum.TryParse(i, true, out MetadataField field))
                {
                    return (MetadataField?)field;
                }

                return null;
            }).OfType<MetadataField>().ToArray() ?? Array.Empty<MetadataField>();

            foreach (var actor in nfo.Actors ?? Array.Empty<ActorNfo>())
            {
                var person = new PersonInfo()
                {
                    Name = actor.Name?.Trim(),
                    Role = actor.Role,
                    SortOrder = actor.Order == null ? 0 : actor.Order.Value,
                    ImageUrl = actor.Thumb
                };

                switch (actor.Type)
                {
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
                    // no type --> actor
                    default:
                        person.Type = PersonType.Actor;
                        break;
                }

                if (person.Name != null)
                {
                    metadataResult.AddPerson(person);
                }
            }

            foreach (var director in nfo.Directors ?? Array.Empty<string>())
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

            foreach (var writer in nfo.Writers ?? Array.Empty<string>())
            {
                if (!string.IsNullOrWhiteSpace(writer))
                {
                    metadataResult.AddPerson(new PersonInfo() { Name = writer.Trim(), Type = PersonType.Writer });
                }
            }

            // split genres, credits and countries at '/' if only one element is present and contains '/'
            if (nfo.Countries != null
                && nfo.Countries.Length == 1
                && nfo.Countries[0].IndexOf('/', StringComparison.Ordinal) != -1)
            {
                item.ProductionLocations = nfo.Countries[1].Split('/', StringSplitOptions.RemoveEmptyEntries)
                    .Select(i => i.Trim())
                    .ToArray();
            }
            else
            {
                item.ProductionLocations = nfo.Countries ?? Array.Empty<string>();
            }

            string[] genreArray;
            if (nfo.Genres != null
                && nfo.Genres.Length == 1
                && nfo.Genres[0].IndexOf('/', StringComparison.Ordinal) != -1)
            {
                genreArray = nfo.Genres[1].Split('/', StringSplitOptions.RemoveEmptyEntries)
                    .Select(i => i.Trim())
                    .ToArray();
            }
            else
            {
                genreArray = nfo.Genres ?? Array.Empty<string>();
            }

            foreach (var genre in genreArray)
            {
                if (!string.IsNullOrWhiteSpace(genre))
                {
                    item.AddGenre(genre.Trim());
                }
            }

            string[] creditArray;
            if (nfo.Credits != null
                && nfo.Credits.Length == 1
                && nfo.Credits[0].IndexOf('/', StringComparison.Ordinal) != -1)
            {
                creditArray = nfo.Credits[1].Split('/', StringSplitOptions.RemoveEmptyEntries)
                    .Select(i => i.Trim())
                    .ToArray();
            }
            else
            {
                creditArray = nfo.Credits ?? Array.Empty<string>();
            }

            foreach (var credit in creditArray)
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

            // map provider ids
            // It's okay to pass null because the method removes the providers for which the id is null
            string? imdbId = nfo.ImdbId ?? nfo.UniqueIds?
                .FirstOrDefault(x => x.Type != null && x.Type.Equals("imdb", StringComparison.OrdinalIgnoreCase))
                ?.Id;
            string? tmdbId = nfo.ImdbId ?? nfo.UniqueIds?
                .FirstOrDefault(x => x.Type != null && x.Type.Equals("tmdb", StringComparison.OrdinalIgnoreCase))
                ?.Id;
            string? tvdbId = nfo.ImdbId ?? nfo.UniqueIds?
                .FirstOrDefault(x => x.Type != null && x.Type.Equals("tvdb", StringComparison.OrdinalIgnoreCase))
                ?.Id;
            string? tvcomId = nfo.ImdbId ?? nfo.UniqueIds?
                .FirstOrDefault(x => x.Type != null && x.Type.Equals("tvcom", StringComparison.OrdinalIgnoreCase))
                ?.Id;

            item.SetProviderId(MetadataProvider.Imdb, imdbId!);
            item.SetProviderId(MetadataProvider.Tmdb, tmdbId!);
            item.SetProviderId(MetadataProvider.Tvdb, tvdbId!);
            item.SetProviderId(MetadataProvider.Tvcom, tvcomId!);
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
