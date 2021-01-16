#pragma warning disable CA1000

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Jellyfin.KodiMetadata.Configuration;
using Jellyfin.KodiMetadata.Models;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.KodiMetadata.Savers
{
    /// <summary>
    /// The base nfo metadata saver.
    /// </summary>
    /// <typeparam name="T1">The base item to save.</typeparam>
    /// <typeparam name="T2">The nfo object type.</typeparam>
    public abstract class BaseNfoSaver<T1, T2> : IMetadataFileSaver
        where T1 : BaseItem
        where T2 : BaseNfo, new()
    {
        // filters control characters but allows only properly-formed surrogate sequences
        private const string _invalidXMLCharsRegex = @"(?<![\uD800-\uDBFF])[\uDC00-\uDFFF]|[\uD800-\uDBFF](?![\uDC00-\uDFFF])|[\x00-\x08\x0B\x0C\x0E-\x1F\x7F-\x9F\uFEFF\uFFFE\uFFFF]";

        private readonly ILogger<BaseNfoSaver<T1, T2>> _logger;
        private readonly IXmlSerializer _xmlSerializer;
        private readonly IFileSystem _fileSystem;
        private readonly IServerConfigurationManager _configurationManager;
        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;
        private readonly IUserDataManager _userDataManager;
        private readonly XbmcMetadataOptions _nfoConfiguration;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseNfoSaver{T1, T2}"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{TCategoryName}"/> interface.</param>
        /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        /// <param name="configurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
        /// <param name="userDataManager">Instance of the <see cref="IUserDataManager"/> interface.</param>
        protected BaseNfoSaver(
            ILogger<BaseNfoSaver<T1, T2>> logger,
            IXmlSerializer xmlSerializer,
            IFileSystem fileSystem,
            IServerConfigurationManager configurationManager,
            ILibraryManager libraryManager,
            IUserManager userManager,
            IUserDataManager userDataManager)
        {
            _logger = logger;
            _xmlSerializer = xmlSerializer;
            _fileSystem = fileSystem;
            _configurationManager = configurationManager;
            _libraryManager = libraryManager;
            _userManager = userManager;
            _userDataManager = userDataManager;
            _nfoConfiguration = configurationManager.GetNfoConfiguration();
        }

        /// <summary>
        /// Gets the name of the nfo saver.
        /// </summary>
        public static string SaverName => "Nfo";

        /// <inheritdoc />
        public string Name => SaverName;

        /// <summary>
        /// Gets the minimum type of update for rewriting the nfo.
        /// </summary>
        protected ItemUpdateType MinimumUpdateType
        {
            get
            {
                if (_nfoConfiguration.SaveImagePathsInNfo)
                {
                    return ItemUpdateType.ImageUpdate;
                }

                return ItemUpdateType.MetadataDownload;
            }
        }

        /// <inheritdoc />
        public void Save(BaseItem item, CancellationToken cancellationToken)
        {
            var nfo = new T2();
            if (item is T1 castedItem)
            {
                MapJellyfinToNfoObject(castedItem, nfo);
            }
            else
            {
                _logger.LogError("Error mapping Jellyfin to Nfo object: Could not cast {itemType} to {destinationType}", item.GetType(), typeof(T1));
            }

            using var memoryStream = new MemoryStream();
            _xmlSerializer.SerializeToStream(nfo, memoryStream);

            cancellationToken.ThrowIfCancellationRequested();

            // todo get currently used tags and write them again

            SaveToFile(memoryStream, GetSavePath(item));
        }

        /// <inheritdoc />
        public abstract string GetSavePath(BaseItem item);

        /// <inheritdoc />
        public abstract bool IsEnabledFor(BaseItem item, ItemUpdateType updateType);

        /// <summary>
        /// Maps the base item to the nfo object.
        /// </summary>
        /// <param name="item">The base item to map to the nfo.</param>
        /// <param name="nfo">The nfo to map to.</param>
        protected virtual void MapJellyfinToNfoObject(T1 item, T2 nfo)
        {
            if (item == null)
            {
                throw new ArgumentException("BaseItem can't be null", nameof(item));
            }

            if (nfo == null)
            {
                throw new ArgumentException("Nfo can't be null", nameof(nfo));
            }

            nfo.CustomRating = item.CustomRating;
            nfo.LockData = item.IsLocked;
            nfo.DateAdded = item.DateCreated.ToLocalTime();
            nfo.Title = item.Name;
            nfo.OriginalTitle = item.OriginalTitle;
            nfo.Rating = item.CommunityRating;
            nfo.Year = item.ProductionYear;
            nfo.SortTitle = item.ForcedSortName;
            nfo.Mpaa = item.OfficialRating;
            nfo.CriticRating = item.CriticRating;

            var overview = (item.Overview ?? string.Empty)
                .StripHtml()
                .Replace("&quot;", "'", StringComparison.Ordinal);

            if (item is MusicArtist)
            {
                nfo.Biography = overview;
            }
            else if (item is MusicAlbum)
            {
                nfo.Review = overview;
            }
            else
            {
                nfo.Plot = overview;
            }

            if (item is Video)
            {
                nfo.Outline = (item.Tagline ?? string.Empty)
                    .StripHtml()
                    .Replace("&quot;", "'", StringComparison.Ordinal);
            }
            else
            {
                nfo.Outline = overview;
            }

            // add people
            var people = _libraryManager.GetPeople(item);
            nfo.Directors = people
                .Where(i => IsPersonType(i, PersonType.Director))
                .Select(i => i.Name)
                .ToArray();
            nfo.Writers = people
                .Where(i => IsPersonType(i, PersonType.Writer))
                .Select(i => i.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            nfo.Credits = nfo.Writers;
            var actorsAndOther = people
                .Where(i => !IsPersonType(i, PersonType.Writer) && !IsPersonType(i, PersonType.Director))
                .ToList();
            List<ActorNfo> actorNfos = new List<ActorNfo>();
            foreach (var personInfo in actorsAndOther)
            {
                var personNfo = new ActorNfo
                {
                    Name = personInfo.Name,
                    Role = personInfo.Role,
                    Order = personInfo.SortOrder,
                    Type = personInfo.Type
                };

                if (_nfoConfiguration.SaveImagePathsInNfo)
                {
                    var personEntity = _libraryManager.GetPerson(personInfo.Name);
                    var image = personEntity.GetImageInfo(ImageType.Primary, 0);

                    if (image != null)
                    {
                        personNfo.Thumb = GetImagePathToSave(image);
                    }
                }

                actorNfos.Add(personNfo);
            }

            nfo.Actors = actorNfos.ToArray();

            List<string> trailers = new List<string>();
            foreach (var trailer in item.RemoteTrailers)
            {
                trailers.Add(trailer.Url.Replace("https://www.youtube.com/watch?v=", "plugin://plugin.video.youtube/?action=play_video&videoid=", StringComparison.OrdinalIgnoreCase));
            }

            nfo.Trailers = trailers.ToArray();

            if (item is IHasAspectRatio hasAspectRatio
                && !string.IsNullOrEmpty(hasAspectRatio.AspectRatio))
            {
                nfo.AspectRatio = hasAspectRatio.AspectRatio;
            }

            nfo.Language = item.PreferredMetadataLanguage;
            nfo.CountryCode = item.PreferredMetadataCountryCode;
            if (item.PremiereDate.HasValue && !(item is Episode))
            {
                if (item is MusicArtist)
                {
                    nfo.Formed = item.PremiereDate.Value.ToLocalTime();
                }
                else
                {
                    nfo.Premiered = item.PremiereDate.Value.ToLocalTime();
                    nfo.Released = item.PremiereDate.Value.ToLocalTime();
                }
            }

            if (!(item is Episode))
            {
                nfo.EndDate = item.EndDate?.ToLocalTime();
            }

            if (item is IHasDisplayOrder hasDisplayOrder)
            {
                nfo.DisplayOrder = hasDisplayOrder.DisplayOrder;
            }

            if (item is BoxSet folder)
            {
                AddCollectionItems(folder, nfo);
            }

            nfo.FileInfo = new FileInfoNfo() { StreamDetails = new StreamDetailsNfo() };

            // Video
            List<VideoStreamNfo> videoStreamNfos = new List<VideoStreamNfo>();
            foreach (var videoStream in item.GetMediaStreams().Where(i => i.Type == MediaStreamType.Video))
            {
                var codec = videoStream.Codec;
                if ((videoStream.CodecTag ?? string.Empty).IndexOf("xvid", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    codec = "xvid";
                }
                else if ((videoStream.CodecTag ?? string.Empty).IndexOf("divx", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    codec = "divx";
                }

                var videoStreamNfo = new VideoStreamNfo()
                {
                    AspectRatio = videoStream.AspectRatio,
                    Codec = codec,
                    Duration = TimeSpan.FromTicks(item.RunTimeTicks ?? 0).TotalMinutes,
                    DurationInSeconds = TimeSpan.FromTicks(item.RunTimeTicks ?? 0).TotalSeconds,
                    Height = videoStream.Height,
                    Width = videoStream.Width,
                    Bitrate = videoStream.BitRate,
                    Framerate = videoStream.AverageFrameRate ?? videoStream.RealFrameRate,
                    Scantype = videoStream.IsInterlaced ? "interlaced" : "progressive",
                };

                if (item is Video video)
                {
                    if (video.Video3DFormat.HasValue)
                    {
                        switch (video.Video3DFormat.Value)
                        {
                            case Video3DFormat.FullSideBySide:
                                videoStreamNfo.Format3D = "FSBS";
                                break;
                            case Video3DFormat.FullTopAndBottom:
                                videoStreamNfo.Format3D = "FTAB";
                                break;
                            case Video3DFormat.HalfSideBySide:
                                videoStreamNfo.Format3D = "HSBS";
                                break;
                            case Video3DFormat.HalfTopAndBottom:
                                videoStreamNfo.Format3D = "HTAB";
                                break;
                            case Video3DFormat.MVC:
                                videoStreamNfo.Format3D = "MVC";
                                break;
                        }
                    }
                }

                videoStreamNfos.Add(videoStreamNfo);
            }

            nfo.FileInfo.StreamDetails.Video = videoStreamNfos.ToArray();

            // Audio
            List<AudioStreamNfo> audioStreamNfos = new List<AudioStreamNfo>();
            foreach (var audioStream in item.GetMediaStreams().Where(i => i.Type == MediaStreamType.Audio))
            {
                var audioStreamNfo = new AudioStreamNfo()
                {
                    Channels = audioStream.Channels,
                    // http://web.archive.org/web/20181230211547/https://emby.media/community/index.php?/topic/49071-nfo-not-generated-on-actualize-or-rescan-or-identify
                    // Web Archive version of link since it's not really explained in the thread.
                    Language = Regex.Replace(audioStream.Language, _invalidXMLCharsRegex, string.Empty),
                    Codec = audioStream.Codec,
                    SamplingRate = audioStream.SampleRate,
                    Bitrate = audioStream.BitRate,
                    Default = audioStream.IsDefault,
                    Forced = audioStream.IsForced
                };
                audioStreamNfos.Add(audioStreamNfo);
            }

            nfo.FileInfo.StreamDetails.Audio = audioStreamNfos.ToArray();

            // Subtitles
            List<SubtitleStreamNfo> subtitleStreamNfos = new List<SubtitleStreamNfo>();
            foreach (var subtitleStream in item.GetMediaStreams().Where(i => i.Type == MediaStreamType.Subtitle))
            {
                var subtitleStreamNfo = new SubtitleStreamNfo()
                {
                    // http://web.archive.org/web/20181230211547/https://emby.media/community/index.php?/topic/49071-nfo-not-generated-on-actualize-or-rescan-or-identify
                    // Web Archive version of link since it's not really explained in the thread.
                    Language = Regex.Replace(subtitleStream.Language, _invalidXMLCharsRegex, string.Empty),
                    Default = subtitleStream.IsDefault,
                    Forced = subtitleStream.IsForced
                };
                subtitleStreamNfos.Add(subtitleStreamNfo);
            }

            nfo.FileInfo.StreamDetails.Subtitle = subtitleStreamNfos.ToArray();

            // Use original runtime here, actual file runtime later in MediaInfo
            if (item.RunTimeTicks != null)
            {
                nfo.Runtime = Convert.ToInt64(TimeSpan.FromTicks(item.RunTimeTicks.Value).TotalMinutes);
            }

            nfo.Tagline = item.Tagline;

            List<string> countries = new List<string>();
            foreach (var country in item.ProductionLocations)
            {
                countries.Add(country);
            }

            nfo.Countries = countries.ToArray();

            List<string> genres = new List<string>();
            foreach (var genre in item.Genres)
            {
                genres.Add(genre);
            }

            nfo.Genres = genres.ToArray();

            List<string> studios = new List<string>();
            foreach (var studio in item.Studios)
            {
                studios.Add(studio);
            }

            nfo.Studios = studios.ToArray();

            List<string> tags = new List<string>();
            foreach (var tag in item.Tags)
            {
                tags.Add(tag);
            }

            if (item is MusicAlbum || item is MusicArtist)
            {
                nfo.Styles = tags.ToArray();
            }
            else
            {
                nfo.Tags = tags.ToArray();
            }

            AddIds(item, nfo);

            AddUserData(item, nfo);
            if (_nfoConfiguration.SaveImagePathsInNfo)
            {
                AddImages(item, nfo);
            }
        }

        private void SaveToFile(Stream stream, string path)
        {
            var directory = Path.GetDirectoryName(path) ?? throw new ArgumentException($"Provided path ({path}) is not valid.", nameof(path));
            Directory.CreateDirectory(directory);

            // On Windows, savint the file will fail if the file is hidden or readonly
            _fileSystem.SetAttributes(path, false, false);

            using (var filestream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                stream.CopyTo(filestream);
            }

            if (_configurationManager.Configuration.SaveMetadataHidden)
            {
                try
                {
                    _fileSystem.SetHidden(path, true);
                }
                catch (IOException ex)
                {
                    _logger.LogError(ex, "Error setting hidden attribute on {Path}", path);
                }
            }
        }

        private bool IsPersonType(PersonInfo person, string type)
            => string.Equals(person.Type, type, StringComparison.OrdinalIgnoreCase)
               || string.Equals(person.Role, type, StringComparison.OrdinalIgnoreCase);

        private string GetImagePathToSave(ItemImageInfo image)
            => !image.IsLocalFile ? image.Path : _libraryManager.GetPathAfterNetworkSubstitution(image.Path);

        private void AddUserData(T1 item, T2 nfo)
        {
            var userId = _nfoConfiguration.UserId;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return;
            }

            var user = _userManager.GetUserById(Guid.Parse(userId));

            if (user == null)
            {
                return;
            }

            if (item.IsFolder)
            {
                return;
            }

            var userdata = _userDataManager.GetUserData(user, item);

            nfo.UserRating = (float)(userdata.Rating ?? 0);
            nfo.PlayCount = userdata.PlayCount;
            nfo.LastPlayed = userdata.LastPlayedDate?.ToLocalTime();
            nfo.ResumePosition = new ResumePositionNfo()
            {
                Position = TimeSpan.FromTicks(userdata.PlaybackPositionTicks).TotalSeconds,
                Total = TimeSpan.FromTicks(item.RunTimeTicks ?? 0).TotalSeconds
            };

            nfo.Watched = userdata.Played;
            nfo.IsUserFavorite = userdata.IsFavorite;
        }

        private void AddImages(T1 item, T2 nfo)
        {
            var artNfo = new ArtNfo();

            var primaryImage = item.GetImageInfo(ImageType.Primary, 0);
            if (primaryImage != null)
            {
                artNfo.Poster[0] = GetImagePathToSave(primaryImage);
            }

            List<string> backdrops = new List<string>();
            foreach (var backdrop in item.GetImages(ImageType.Backdrop))
            {
                backdrops.Add(GetImagePathToSave(backdrop));
            }

            artNfo.Fanart = backdrops.ToArray();
            nfo.Art = artNfo;
        }

        private void AddIds(T1 item, T2 nfo)
        {
            nfo.ImdbId = item.GetProviderId(MetadataProvider.Imdb);
            nfo.TmdbId = item.GetProviderId(MetadataProvider.Tmdb);
            nfo.AudioDbAlbumId = item.GetProviderId(MetadataProvider.AudioDbAlbum);
            nfo.MusicBrainzAlbumId = item.GetProviderId(MetadataProvider.MusicBrainzAlbum);
            nfo.MusicBrainzAlbumArtistId = item.GetProviderId(MetadataProvider.MusicBrainzAlbumArtist);
            nfo.MusicBrainzReleaseGroupId = item.GetProviderId(MetadataProvider.MusicBrainzReleaseGroup);
        }

        private void AddCollectionItems(Folder item, T2 nfo)
        {
            var items = item.LinkedChildren
                .Where(i => i.Type == LinkedChildType.Manual)
                .ToList();

            List<CollectionItemNfo> collectionItemNfos = new List<CollectionItemNfo>();
            foreach (var link in items)
            {
                collectionItemNfos.Add(new CollectionItemNfo()
                {
                    Path = link.Path,
                    ItemId = link.LibraryItemId
                });
            }

            nfo.CollectionItems = collectionItemNfos.ToArray();
        }
    }
}
