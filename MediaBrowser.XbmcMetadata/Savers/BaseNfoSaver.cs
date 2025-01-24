#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
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
using MediaBrowser.XbmcMetadata.Configuration;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.XbmcMetadata.Savers
{
    public abstract partial class BaseNfoSaver : IMetadataFileSaver
    {
        public const string DateAddedFormat = "yyyy-MM-dd HH:mm:ss";

        public const string YouTubeWatchUrl = "https://www.youtube.com/watch?v=";

        private static readonly HashSet<string> _commonTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "plot",
            "customrating",
            "lockdata",
            "dateadded",
            "title",
            "rating",
            "year",
            "sorttitle",
            "mpaa",
            "aspectratio",
            "collectionnumber",
            "tmdbid",
            "rottentomatoesid",
            "language",
            "tvcomid",
            "tagline",
            "studio",
            "genre",
            "tag",
            "runtime",
            "actor",
            "criticrating",
            "fileinfo",
            "director",
            "writer",
            "trailer",
            "premiered",
            "releasedate",
            "outline",
            "id",
            "credits",
            "originaltitle",
            "watched",
            "playcount",
            "lastplayed",
            "art",
            "resume",
            "biography",
            "formed",
            "review",
            "style",
            "imdbid",
            "imdb_id",
            "country",
            "audiodbalbumid",
            "audiodbartistid",
            "enddate",
            "lockedfields",
            "zap2itid",
            "tvrageid",

            "musicbrainzartistid",
            "musicbrainzalbumartistid",
            "musicbrainzalbumid",
            "musicbrainzreleasegroupid",
            "tvdbid",
            "collectionitem",

            "isuserfavorite",
            "userrating",

            "countrycode"
        };

        protected BaseNfoSaver(
            IFileSystem fileSystem,
            IServerConfigurationManager configurationManager,
            ILibraryManager libraryManager,
            IUserManager userManager,
            IUserDataManager userDataManager,
            ILogger<BaseNfoSaver> logger)
        {
            Logger = logger;
            UserDataManager = userDataManager;
            UserManager = userManager;
            LibraryManager = libraryManager;
            ConfigurationManager = configurationManager;
            FileSystem = fileSystem;
        }

        protected IFileSystem FileSystem { get; }

        protected IServerConfigurationManager ConfigurationManager { get; }

        protected ILibraryManager LibraryManager { get; }

        protected IUserManager UserManager { get; }

        protected IUserDataManager UserDataManager { get; }

        protected ILogger<BaseNfoSaver> Logger { get; }

        protected ItemUpdateType MinimumUpdateType
        {
            get
            {
                if (ConfigurationManager.GetNfoConfiguration().SaveImagePathsInNfo)
                {
                    return ItemUpdateType.ImageUpdate;
                }

                return ItemUpdateType.MetadataDownload;
            }
        }

        /// <inheritdoc />
        public string Name => SaverName;

        public static string SaverName => "Nfo";

        // filters control characters but allows only properly-formed surrogate sequences
        // http://web.archive.org/web/20181230211547/https://emby.media/community/index.php?/topic/49071-nfo-not-generated-on-actualize-or-rescan-or-identify
        // Web Archive version of link since it's not really explained in the thread.
        [GeneratedRegex(@"(?<![\uD800-\uDBFF])[\uDC00-\uDFFF]|[\uD800-\uDBFF](?![\uDC00-\uDFFF])|[\x00-\x08\x0B\x0C\x0E-\x1F\x7F-\x9F\uFEFF\uFFFE\uFFFF]")]
        private static partial Regex InvalidXMLCharsRegexRegex();

        /// <inheritdoc />
        public string GetSavePath(BaseItem item)
            => GetLocalSavePath(item);

        /// <summary>
        /// Gets the save path.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><see cref="string" />.</returns>
        protected abstract string GetLocalSavePath(BaseItem item);

        /// <summary>
        /// Gets the name of the root element.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><see cref="string" />.</returns>
        protected abstract string GetRootElementName(BaseItem item);

        /// <inheritdoc />
        public abstract bool IsEnabledFor(BaseItem item, ItemUpdateType updateType);

        protected virtual IEnumerable<string> GetTagsUsed(BaseItem item)
        {
            foreach (var providerKey in item.ProviderIds.Keys)
            {
                var providerIdTagName = GetTagForProviderKey(providerKey);
                if (!_commonTags.Contains(providerIdTagName))
                {
                    yield return providerIdTagName;
                }
            }
        }

        /// <inheritdoc />
        public async Task SaveAsync(BaseItem item, CancellationToken cancellationToken)
        {
            var path = GetSavePath(item);

            using (var memoryStream = new MemoryStream())
            {
                Save(item, memoryStream, path);

                memoryStream.Position = 0;

                cancellationToken.ThrowIfCancellationRequested();

                await SaveToFileAsync(memoryStream, path).ConfigureAwait(false);
            }
        }

        private async Task SaveToFileAsync(Stream stream, string path)
        {
            var directory = Path.GetDirectoryName(path) ?? throw new ArgumentException($"Provided path ({path}) is not valid.", nameof(path));
            Directory.CreateDirectory(directory);

            // On Windows, saving the file will fail if the file is hidden or readonly
            FileSystem.SetAttributes(path, false, false);

            var fileStreamOptions = new FileStreamOptions()
            {
                Mode = FileMode.Create,
                Access = FileAccess.Write,
                Share = FileShare.None,
                PreallocationSize = stream.Length,
                Options = FileOptions.Asynchronous
            };

            var filestream = new FileStream(path, fileStreamOptions);
            await using (filestream.ConfigureAwait(false))
            {
                await stream.CopyToAsync(filestream).ConfigureAwait(false);
            }

            if (ConfigurationManager.Configuration.SaveMetadataHidden)
            {
                SetHidden(path, true);
            }
        }

        private void SetHidden(string path, bool hidden)
        {
            try
            {
                FileSystem.SetHidden(path, hidden);
            }
            catch (IOException ex)
            {
                Logger.LogError(ex, "Error setting hidden attribute on {Path}", path);
            }
        }

        private void Save(BaseItem item, Stream stream, string xmlPath)
        {
            var settings = new XmlWriterSettings
            {
                Indent = true,
                Encoding = Encoding.UTF8,
                CloseOutput = false
            };

            using (var writer = XmlWriter.Create(stream, settings))
            {
                var root = GetRootElementName(item);

                writer.WriteStartDocument(true);

                writer.WriteStartElement(root);

                var baseItem = item;

                if (baseItem is not null)
                {
                    AddCommonNodes(baseItem, writer, LibraryManager, UserManager, UserDataManager, ConfigurationManager);
                }

                WriteCustomElements(item, writer);

                if (baseItem is IHasMediaSources hasMediaSources)
                {
                    AddMediaInfo(hasMediaSources, writer);
                }

                var tagsUsed = GetTagsUsed(item).ToList();

                try
                {
                    AddCustomTags(xmlPath, tagsUsed, writer, Logger);
                }
                catch (FileNotFoundException)
                {
                }
                catch (IOException)
                {
                }
                catch (XmlException ex)
                {
                    Logger.LogError(ex, "Error reading existing nfo");
                }

                writer.WriteEndElement();

                writer.WriteEndDocument();
            }
        }

        protected abstract void WriteCustomElements(BaseItem item, XmlWriter writer);

        public static void AddMediaInfo<T>(T item, XmlWriter writer)
            where T : IHasMediaSources
        {
            writer.WriteStartElement("fileinfo");
            writer.WriteStartElement("streamdetails");

            var mediaStreams = item.GetMediaStreams();

            foreach (var stream in mediaStreams)
            {
                writer.WriteStartElement(stream.Type.ToString().ToLowerInvariant());

                if (!string.IsNullOrEmpty(stream.Codec))
                {
                    var codec = stream.Codec;

                    if ((stream.CodecTag ?? string.Empty).Contains("xvid", StringComparison.OrdinalIgnoreCase))
                    {
                        codec = "xvid";
                    }
                    else if ((stream.CodecTag ?? string.Empty).Contains("divx", StringComparison.OrdinalIgnoreCase))
                    {
                        codec = "divx";
                    }

                    writer.WriteElementString("codec", codec);
                    writer.WriteElementString("micodec", codec);
                }

                if (stream.BitRate.HasValue)
                {
                    writer.WriteElementString("bitrate", stream.BitRate.Value.ToString(CultureInfo.InvariantCulture));
                }

                if (stream.Width.HasValue)
                {
                    writer.WriteElementString("width", stream.Width.Value.ToString(CultureInfo.InvariantCulture));
                }

                if (stream.Height.HasValue)
                {
                    writer.WriteElementString("height", stream.Height.Value.ToString(CultureInfo.InvariantCulture));
                }

                if (!string.IsNullOrEmpty(stream.AspectRatio))
                {
                    writer.WriteElementString("aspect", stream.AspectRatio);
                    writer.WriteElementString("aspectratio", stream.AspectRatio);
                }

                var framerate = stream.ReferenceFrameRate;

                if (framerate.HasValue)
                {
                    writer.WriteElementString("framerate", framerate.Value.ToString(CultureInfo.InvariantCulture));
                }

                if (!string.IsNullOrEmpty(stream.Language))
                {
                    writer.WriteElementString("language", InvalidXMLCharsRegexRegex().Replace(stream.Language, string.Empty));
                }

                var scanType = stream.IsInterlaced ? "interlaced" : "progressive";
                writer.WriteElementString("scantype", scanType);

                if (stream.Channels.HasValue)
                {
                    writer.WriteElementString("channels", stream.Channels.Value.ToString(CultureInfo.InvariantCulture));
                }

                if (stream.SampleRate.HasValue)
                {
                    writer.WriteElementString("samplingrate", stream.SampleRate.Value.ToString(CultureInfo.InvariantCulture));
                }

                writer.WriteElementString("default", stream.IsDefault.ToString(CultureInfo.InvariantCulture));
                writer.WriteElementString("forced", stream.IsForced.ToString(CultureInfo.InvariantCulture));

                if (stream.Type == MediaStreamType.Video)
                {
                    var runtimeTicks = item.RunTimeTicks;
                    if (runtimeTicks.HasValue)
                    {
                        var timespan = TimeSpan.FromTicks(runtimeTicks.Value);

                        writer.WriteElementString(
                            "duration",
                            Math.Floor(timespan.TotalMinutes).ToString(CultureInfo.InvariantCulture));
                        writer.WriteElementString(
                            "durationinseconds",
                            Math.Floor(timespan.TotalSeconds).ToString(CultureInfo.InvariantCulture));
                    }

                    if (item is Video video)
                    {
                        // AddChapters(video, builder, itemRepository);

                        if (video.Video3DFormat.HasValue)
                        {
                            switch (video.Video3DFormat.Value)
                            {
                                case Video3DFormat.FullSideBySide:
                                    writer.WriteElementString("format3d", "FSBS");
                                    break;
                                case Video3DFormat.FullTopAndBottom:
                                    writer.WriteElementString("format3d", "FTAB");
                                    break;
                                case Video3DFormat.HalfSideBySide:
                                    writer.WriteElementString("format3d", "HSBS");
                                    break;
                                case Video3DFormat.HalfTopAndBottom:
                                    writer.WriteElementString("format3d", "HTAB");
                                    break;
                                case Video3DFormat.MVC:
                                    writer.WriteElementString("format3d", "MVC");
                                    break;
                            }
                        }
                    }
                }

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        /// <summary>
        /// Adds the common nodes.
        /// </summary>
        private void AddCommonNodes(
            BaseItem item,
            XmlWriter writer,
            ILibraryManager libraryManager,
            IUserManager userManager,
            IUserDataManager userDataRepo,
            IServerConfigurationManager config)
        {
            var writtenProviderIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var overview = (item.Overview ?? string.Empty)
                .StripHtml()
                .Replace("&quot;", "'", StringComparison.Ordinal);

            var options = config.GetNfoConfiguration();

            if (item is MusicArtist)
            {
                writer.WriteElementString("biography", overview);
            }
            else if (item is MusicAlbum)
            {
                writer.WriteElementString("review", overview);
            }
            else
            {
                writer.WriteElementString("plot", overview);
            }

            if (item is not Video)
            {
                writer.WriteElementString("outline", overview);
            }

            if (!string.IsNullOrWhiteSpace(item.CustomRating))
            {
                writer.WriteElementString("customrating", item.CustomRating);
            }

            writer.WriteElementString("lockdata", item.IsLocked.ToString(CultureInfo.InvariantCulture).ToLowerInvariant());

            if (item.LockedFields.Count > 0)
            {
                writer.WriteElementString("lockedfields", string.Join('|', item.LockedFields));
            }

            writer.WriteElementString("dateadded", item.DateCreated.ToString(DateAddedFormat, CultureInfo.InvariantCulture));

            writer.WriteElementString("title", item.Name ?? string.Empty);

            if (!string.IsNullOrWhiteSpace(item.OriginalTitle))
            {
                writer.WriteElementString("originaltitle", item.OriginalTitle);
            }

            var people = libraryManager.GetPeople(item);

            var directors = people
                .Where(i => i.IsType(PersonKind.Director))
                .Select(i => i.Name)
                .ToList();

            foreach (var person in directors)
            {
                writer.WriteElementString("director", person);
            }

            var writers = people
                .Where(i => i.IsType(PersonKind.Writer))
                .Select(i => i.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var person in writers)
            {
                writer.WriteElementString("writer", person);
            }

            foreach (var person in writers)
            {
                writer.WriteElementString("credits", person);
            }

            foreach (var trailer in item.RemoteTrailers)
            {
                writer.WriteElementString("trailer", GetOutputTrailerUrl(trailer.Url));
            }

            if (item.CommunityRating.HasValue)
            {
                writer.WriteElementString("rating", item.CommunityRating.Value.ToString(CultureInfo.InvariantCulture));
            }

            if (item.ProductionYear.HasValue)
            {
                writer.WriteElementString("year", item.ProductionYear.Value.ToString(CultureInfo.InvariantCulture));
            }

            var forcedSortName = item.ForcedSortName;
            if (!string.IsNullOrEmpty(forcedSortName))
            {
                writer.WriteElementString("sorttitle", forcedSortName);
            }

            if (!string.IsNullOrEmpty(item.OfficialRating))
            {
                writer.WriteElementString("mpaa", item.OfficialRating);
            }

            if (item is IHasAspectRatio hasAspectRatio
                && !string.IsNullOrEmpty(hasAspectRatio.AspectRatio))
            {
                writer.WriteElementString("aspectratio", hasAspectRatio.AspectRatio);
            }

            var tmdbCollection = item.GetProviderId(MetadataProvider.TmdbCollection);

            if (!string.IsNullOrEmpty(tmdbCollection))
            {
                writer.WriteElementString("collectionnumber", tmdbCollection);
                writtenProviderIds.Add(MetadataProvider.TmdbCollection.ToString());
            }

            var imdb = item.GetProviderId(MetadataProvider.Imdb);
            if (!string.IsNullOrEmpty(imdb))
            {
                if (item is Series)
                {
                    writer.WriteElementString("imdb_id", imdb);
                }
                else
                {
                    writer.WriteElementString("imdbid", imdb);
                }

                writtenProviderIds.Add(MetadataProvider.Imdb.ToString());
            }

            // Series xml saver already saves this
            if (item is not Series)
            {
                var tvdb = item.GetProviderId(MetadataProvider.Tvdb);
                if (!string.IsNullOrEmpty(tvdb))
                {
                    writer.WriteElementString("tvdbid", tvdb);
                    writtenProviderIds.Add(MetadataProvider.Tvdb.ToString());
                }
            }

            var tmdb = item.GetProviderId(MetadataProvider.Tmdb);
            if (!string.IsNullOrEmpty(tmdb))
            {
                writer.WriteElementString("tmdbid", tmdb);
                writtenProviderIds.Add(MetadataProvider.Tmdb.ToString());
            }

            if (!string.IsNullOrEmpty(item.PreferredMetadataLanguage))
            {
                writer.WriteElementString("language", item.PreferredMetadataLanguage);
            }

            if (!string.IsNullOrEmpty(item.PreferredMetadataCountryCode))
            {
                writer.WriteElementString("countrycode", item.PreferredMetadataCountryCode);
            }

            if (item.PremiereDate.HasValue && item is not Episode)
            {
                var formatString = options.ReleaseDateFormat;

                if (item is MusicArtist)
                {
                    writer.WriteElementString(
                        "formed",
                        item.PremiereDate.Value.ToString(formatString, CultureInfo.InvariantCulture));
                }
                else
                {
                    writer.WriteElementString(
                        "premiered",
                        item.PremiereDate.Value.ToString(formatString, CultureInfo.InvariantCulture));
                    writer.WriteElementString(
                        "releasedate",
                        item.PremiereDate.Value.ToString(formatString, CultureInfo.InvariantCulture));
                }
            }

            if (item.EndDate.HasValue)
            {
                if (item is not Episode)
                {
                    var formatString = options.ReleaseDateFormat;

                    writer.WriteElementString(
                        "enddate",
                        item.EndDate.Value.ToString(formatString, CultureInfo.InvariantCulture));
                }
            }

            if (item.CriticRating.HasValue)
            {
                writer.WriteElementString(
                    "criticrating",
                    item.CriticRating.Value.ToString(CultureInfo.InvariantCulture));
            }

            if (item is IHasDisplayOrder hasDisplayOrder)
            {
                if (!string.IsNullOrEmpty(hasDisplayOrder.DisplayOrder))
                {
                    writer.WriteElementString("displayorder", hasDisplayOrder.DisplayOrder);
                }
            }

            // Use original runtime here, actual file runtime later in MediaInfo
            var runTimeTicks = item.RunTimeTicks;

            if (runTimeTicks.HasValue)
            {
                var timespan = TimeSpan.FromTicks(runTimeTicks.Value);

                writer.WriteElementString(
                    "runtime",
                    Convert.ToInt64(timespan.TotalMinutes).ToString(CultureInfo.InvariantCulture));
            }

            if (!string.IsNullOrWhiteSpace(item.Tagline))
            {
                writer.WriteElementString("tagline", item.Tagline);
            }

            foreach (var country in item.ProductionLocations)
            {
                writer.WriteElementString("country", country);
            }

            foreach (var genre in item.Genres)
            {
                writer.WriteElementString("genre", genre);
            }

            foreach (var studio in item.Studios)
            {
                writer.WriteElementString("studio", studio);
            }

            foreach (var tag in item.Tags)
            {
                if (item is MusicAlbum || item is MusicArtist)
                {
                    writer.WriteElementString("style", tag);
                }
                else
                {
                    writer.WriteElementString("tag", tag);
                }
            }

            var externalId = item.GetProviderId(MetadataProvider.AudioDbArtist);

            if (!string.IsNullOrEmpty(externalId))
            {
                writer.WriteElementString("audiodbartistid", externalId);
                writtenProviderIds.Add(MetadataProvider.AudioDbArtist.ToString());
            }

            externalId = item.GetProviderId(MetadataProvider.AudioDbAlbum);

            if (!string.IsNullOrEmpty(externalId))
            {
                writer.WriteElementString("audiodbalbumid", externalId);
                writtenProviderIds.Add(MetadataProvider.AudioDbAlbum.ToString());
            }

            externalId = item.GetProviderId(MetadataProvider.Zap2It);

            if (!string.IsNullOrEmpty(externalId))
            {
                writer.WriteElementString("zap2itid", externalId);
                writtenProviderIds.Add(MetadataProvider.Zap2It.ToString());
            }

            externalId = item.GetProviderId(MetadataProvider.MusicBrainzAlbum);

            if (!string.IsNullOrEmpty(externalId))
            {
                writer.WriteElementString("musicbrainzalbumid", externalId);
                writtenProviderIds.Add(MetadataProvider.MusicBrainzAlbum.ToString());
            }

            externalId = item.GetProviderId(MetadataProvider.MusicBrainzAlbumArtist);

            if (!string.IsNullOrEmpty(externalId))
            {
                writer.WriteElementString("musicbrainzalbumartistid", externalId);
                writtenProviderIds.Add(MetadataProvider.MusicBrainzAlbumArtist.ToString());
            }

            externalId = item.GetProviderId(MetadataProvider.MusicBrainzArtist);

            if (!string.IsNullOrEmpty(externalId))
            {
                writer.WriteElementString("musicbrainzartistid", externalId);
                writtenProviderIds.Add(MetadataProvider.MusicBrainzArtist.ToString());
            }

            externalId = item.GetProviderId(MetadataProvider.MusicBrainzReleaseGroup);

            if (!string.IsNullOrEmpty(externalId))
            {
                writer.WriteElementString("musicbrainzreleasegroupid", externalId);
                writtenProviderIds.Add(MetadataProvider.MusicBrainzReleaseGroup.ToString());
            }

            externalId = item.GetProviderId(MetadataProvider.TvRage);
            if (!string.IsNullOrEmpty(externalId))
            {
                writer.WriteElementString("tvrageid", externalId);
                writtenProviderIds.Add(MetadataProvider.TvRage.ToString());
            }

            if (item.ProviderIds is not null)
            {
                foreach (var providerKey in item.ProviderIds.Keys)
                {
                    var providerId = item.ProviderIds[providerKey];
                    if (!string.IsNullOrEmpty(providerId) && !writtenProviderIds.Contains(providerKey))
                    {
                        try
                        {
                            var tagName = GetTagForProviderKey(providerKey);
                            Logger.LogDebug("Verifying custom provider tagname {0}", tagName);
                            XmlConvert.VerifyName(tagName);
                            Logger.LogDebug("Saving custom provider tagname {0}", tagName);

                            writer.WriteElementString(GetTagForProviderKey(providerKey), providerId);
                        }
                        catch (ArgumentException)
                        {
                            // catch invalid names without failing the entire operation
                        }
                        catch (XmlException)
                        {
                            // catch invalid names without failing the entire operation
                        }
                    }
                }
            }

            if (options.SaveImagePathsInNfo)
            {
                AddImages(item, writer, libraryManager);
            }

            AddUserData(item, writer, userManager, userDataRepo, options);

            AddActors(people, writer, libraryManager, options.SaveImagePathsInNfo);

            if (item is BoxSet folder)
            {
                AddCollectionItems(folder, writer);
            }
        }

        private void AddCollectionItems(Folder item, XmlWriter writer)
        {
            var items = item.LinkedChildren
                .Where(i => i.Type == LinkedChildType.Manual)
                .ToList();

            foreach (var link in items)
            {
                writer.WriteStartElement("collectionitem");

                if (!string.IsNullOrWhiteSpace(link.Path))
                {
                    writer.WriteElementString("path", link.Path);
                }

                if (!string.IsNullOrWhiteSpace(link.LibraryItemId))
                {
                    writer.WriteElementString("ItemId", link.LibraryItemId);
                }

                writer.WriteEndElement();
            }
        }

        /// <summary>
        /// Gets the output trailer URL.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns>System.String.</returns>
        private string GetOutputTrailerUrl(string url)
        {
            // This is what xbmc expects
            return url.Replace(YouTubeWatchUrl, "plugin://plugin.video.youtube/play/?video_id=", StringComparison.OrdinalIgnoreCase);
        }

        private void AddImages(BaseItem item, XmlWriter writer, ILibraryManager libraryManager)
        {
            writer.WriteStartElement("art");

            var image = item.GetImageInfo(ImageType.Primary, 0);

            if (image is not null)
            {
                writer.WriteElementString("poster", GetImagePathToSave(image, libraryManager));
            }

            foreach (var backdrop in item.GetImages(ImageType.Backdrop))
            {
                writer.WriteElementString("fanart", GetImagePathToSave(backdrop, libraryManager));
            }

            writer.WriteEndElement();
        }

        private void AddUserData(BaseItem item, XmlWriter writer, IUserManager userManager, IUserDataManager userDataRepo, XbmcMetadataOptions options)
        {
            var userId = options.UserId;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return;
            }

            var user = userManager.GetUserById(Guid.Parse(userId));

            if (user is null)
            {
                return;
            }

            if (item.IsFolder)
            {
                return;
            }

            var userdata = userDataRepo.GetUserData(user, item);

            writer.WriteElementString(
                "isuserfavorite",
                userdata.IsFavorite.ToString(CultureInfo.InvariantCulture).ToLowerInvariant());

            if (userdata.Rating.HasValue)
            {
                writer.WriteElementString(
                    "userrating",
                    userdata.Rating.Value.ToString(CultureInfo.InvariantCulture).ToLowerInvariant());
            }

            if (!item.IsFolder)
            {
                writer.WriteElementString(
                    "playcount",
                    userdata.PlayCount.ToString(CultureInfo.InvariantCulture));
                writer.WriteElementString(
                    "watched",
                    userdata.Played.ToString(CultureInfo.InvariantCulture).ToLowerInvariant());

                if (userdata.LastPlayedDate.HasValue)
                {
                    writer.WriteElementString(
                        "lastplayed",
                        userdata.LastPlayedDate.Value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture).ToLowerInvariant());
                }

                writer.WriteStartElement("resume");

                var runTimeTicks = item.RunTimeTicks ?? 0;

                writer.WriteElementString(
                    "position",
                    TimeSpan.FromTicks(userdata.PlaybackPositionTicks).TotalSeconds.ToString(CultureInfo.InvariantCulture));
                writer.WriteElementString(
                    "total",
                    TimeSpan.FromTicks(runTimeTicks).TotalSeconds.ToString(CultureInfo.InvariantCulture));
            }

            writer.WriteEndElement();
        }

        private void AddActors(List<PersonInfo> people, XmlWriter writer, ILibraryManager libraryManager, bool saveImagePath)
        {
            foreach (var person in people)
            {
                if (person.IsType(PersonKind.Director) || person.IsType(PersonKind.Writer))
                {
                    continue;
                }

                writer.WriteStartElement("actor");

                if (!string.IsNullOrWhiteSpace(person.Name))
                {
                    writer.WriteElementString("name", person.Name);
                }

                if (!string.IsNullOrWhiteSpace(person.Role))
                {
                    writer.WriteElementString("role", person.Role);
                }

                if (person.Type != PersonKind.Unknown)
                {
                    writer.WriteElementString("type", person.Type.ToString());
                }

                if (person.SortOrder.HasValue)
                {
                    writer.WriteElementString(
                        "sortorder",
                        person.SortOrder.Value.ToString(CultureInfo.InvariantCulture));
                }

                if (saveImagePath)
                {
                    var personEntity = libraryManager.GetPerson(person.Name);
                    var image = personEntity?.GetImageInfo(ImageType.Primary, 0);

                    if (image is not null)
                    {
                        writer.WriteElementString(
                            "thumb",
                            GetImagePathToSave(image, libraryManager));
                    }
                }

                writer.WriteEndElement();
            }
        }

        private string GetImagePathToSave(ItemImageInfo image, ILibraryManager libraryManager)
        {
            if (!image.IsLocalFile)
            {
                return image.Path;
            }

            return libraryManager.GetPathAfterNetworkSubstitution(image.Path);
        }

        private void AddCustomTags(string path, IReadOnlyCollection<string> xmlTagsUsed, XmlWriter writer, ILogger<BaseNfoSaver> logger)
        {
            var settings = new XmlReaderSettings()
            {
                ValidationType = ValidationType.None,
                CheckCharacters = false,
                IgnoreProcessingInstructions = true,
                IgnoreComments = true
            };

            using (var fileStream = File.OpenRead(path))
            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
            using (var reader = XmlReader.Create(streamReader, settings))
            {
                try
                {
                    reader.MoveToContent();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error reading existing xml tags from {Path}.", path);
                    return;
                }

                reader.Read();

                // Loop through each element
                while (!reader.EOF && reader.ReadState == ReadState.Interactive)
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        var name = reader.Name;

                        if (!_commonTags.Contains(name)
                            && !xmlTagsUsed.Contains(name, StringComparison.OrdinalIgnoreCase))
                        {
                            writer.WriteNode(reader, false);
                        }
                        else
                        {
                            reader.Skip();
                        }
                    }
                    else
                    {
                        reader.Read();
                    }
                }
            }
        }

        private string GetTagForProviderKey(string providerKey)
            => providerKey.ToLowerInvariant() + "id";
    }
}
