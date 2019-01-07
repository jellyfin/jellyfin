using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;
using MediaBrowser.XbmcMetadata.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using MediaBrowser.Controller.Extensions;
using MediaBrowser.Model.Extensions;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Xml;

namespace MediaBrowser.XbmcMetadata.Savers
{
    public abstract class BaseNfoSaver : IMetadataFileSaver
    {
        public static readonly string YouTubeWatchUrl = "https://www.youtube.com/watch?v=";

        private static readonly CultureInfo UsCulture = new CultureInfo("en-US");

        private static readonly Dictionary<string, string> CommonTags = new[] {

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
                    "gamesdbid",

                    "musicbrainzartistid",
                    "musicbrainzalbumartistid",
                    "musicbrainzalbumid",
                    "musicbrainzreleasegroupid",
                    "tvdbid",
                    "collectionitem",

                    "isuserfavorite",
                    "userrating",

                    "countrycode"

        }.ToDictionary(i => i, StringComparer.OrdinalIgnoreCase);

        protected BaseNfoSaver(IFileSystem fileSystem, IServerConfigurationManager configurationManager, ILibraryManager libraryManager, IUserManager userManager, IUserDataManager userDataManager, ILogger logger, IXmlReaderSettingsFactory xmlReaderSettingsFactory)
        {
            Logger = logger;
            XmlReaderSettingsFactory = xmlReaderSettingsFactory;
            UserDataManager = userDataManager;
            UserManager = userManager;
            LibraryManager = libraryManager;
            ConfigurationManager = configurationManager;
            FileSystem = fileSystem;
        }

        protected IFileSystem FileSystem { get; private set; }
        protected IServerConfigurationManager ConfigurationManager { get; private set; }
        protected ILibraryManager LibraryManager { get; private set; }
        protected IUserManager UserManager { get; private set; }
        protected IUserDataManager UserDataManager { get; private set; }
        protected ILogger Logger { get; private set; }
        protected IXmlReaderSettingsFactory XmlReaderSettingsFactory { get; private set; }

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

        public string Name
        {
            get
            {
                return SaverName;
            }
        }

        public static string SaverName
        {
            get
            {
                return "Nfo";
            }
        }

        public string GetSavePath(BaseItem item)
        {
            return GetLocalSavePath(item);
        }

        /// <summary>
        /// Gets the save path.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        protected abstract string GetLocalSavePath(BaseItem item);

        /// <summary>
        /// Gets the name of the root element.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        protected abstract string GetRootElementName(BaseItem item);

        /// <summary>
        /// Determines whether [is enabled for] [the specified item].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="updateType">Type of the update.</param>
        /// <returns><c>true</c> if [is enabled for] [the specified item]; otherwise, <c>false</c>.</returns>
        public abstract bool IsEnabledFor(BaseItem item, ItemUpdateType updateType);

        protected virtual List<string> GetTagsUsed(BaseItem item)
        {
            var list = new List<string>();
            foreach (var providerKey in item.ProviderIds.Keys)
            {
                var providerIdTagName = GetTagForProviderKey(providerKey);
                if (!CommonTags.ContainsKey(providerIdTagName))
                {
                    list.Add(providerIdTagName);
                }
            }
            return list;
        }

        public void Save(BaseItem item, CancellationToken cancellationToken)
        {
            var path = GetSavePath(item);

            using (var memoryStream = new MemoryStream())
            {
                Save(item, memoryStream, path);

                memoryStream.Position = 0;

                cancellationToken.ThrowIfCancellationRequested();

                SaveToFile(memoryStream, path);
            }
        }

        private void SaveToFile(Stream stream, string path)
        {
            FileSystem.CreateDirectory(FileSystem.GetDirectoryName(path));
            // On Windows, savint the file will fail if the file is hidden or readonly
            FileSystem.SetAttributes(path, false, false);

            using (var filestream = FileSystem.GetFileStream(path, FileOpenMode.Create, FileAccessMode.Write, FileShareMode.Read))
            {
                stream.CopyTo(filestream);
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
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error setting hidden attribute on {path}", path);
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

            using (XmlWriter writer = XmlWriter.Create(stream, settings))
            {
                var root = GetRootElementName(item);

                writer.WriteStartDocument(true);

                writer.WriteStartElement(root);

                var baseItem = item;

                if (baseItem != null)
                {
                    AddCommonNodes(baseItem, writer, LibraryManager, UserManager, UserDataManager, FileSystem, ConfigurationManager);
                }

                WriteCustomElements(item, writer);

                var hasMediaSources = baseItem as IHasMediaSources;

                if (hasMediaSources != null)
                {
                    AddMediaInfo(hasMediaSources, writer);
                }

                var tagsUsed = GetTagsUsed(item);

                try
                {
                    AddCustomTags(xmlPath, tagsUsed, writer, Logger, FileSystem);
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
                writer.WriteStartElement(stream.Type.ToString().ToLower());

                if (!string.IsNullOrEmpty(stream.Codec))
                {
                    var codec = stream.Codec;

                    if ((stream.CodecTag ?? string.Empty).IndexOf("xvid", StringComparison.OrdinalIgnoreCase) != -1)
                    {
                        codec = "xvid";
                    }
                    else if ((stream.CodecTag ?? string.Empty).IndexOf("divx", StringComparison.OrdinalIgnoreCase) != -1)
                    {
                        codec = "divx";
                    }

                    writer.WriteElementString("codec", codec);
                    writer.WriteElementString("micodec", codec);
                }

                if (stream.BitRate.HasValue)
                {
                    writer.WriteElementString("bitrate", stream.BitRate.Value.ToString(UsCulture));
                }

                if (stream.Width.HasValue)
                {
                    writer.WriteElementString("width", stream.Width.Value.ToString(UsCulture));
                }

                if (stream.Height.HasValue)
                {
                    writer.WriteElementString("height", stream.Height.Value.ToString(UsCulture));
                }

                if (!string.IsNullOrEmpty(stream.AspectRatio))
                {
                    writer.WriteElementString("aspect", stream.AspectRatio);
                    writer.WriteElementString("aspectratio", stream.AspectRatio);
                }

                var framerate = stream.AverageFrameRate ?? stream.RealFrameRate;

                if (framerate.HasValue)
                {
                    writer.WriteElementString("framerate", framerate.Value.ToString(UsCulture));
                }

                if (!string.IsNullOrEmpty(stream.Language))
                {
                    // http://web.archive.org/web/20181230211547/https://emby.media/community/index.php?/topic/49071-nfo-not-generated-on-actualize-or-rescan-or-identify
                    // Web Archive version of link since it's not really explained in the thread.
                    writer.WriteElementString("language", RemoveInvalidXMLChars(stream.Language));
                }

                var scanType = stream.IsInterlaced ? "interlaced" : "progressive";
                if (!string.IsNullOrEmpty(scanType))
                {
                    writer.WriteElementString("scantype", scanType);
                }

                if (stream.Channels.HasValue)
                {
                    writer.WriteElementString("channels", stream.Channels.Value.ToString(UsCulture));
                }

                if (stream.SampleRate.HasValue)
                {
                    writer.WriteElementString("samplingrate", stream.SampleRate.Value.ToString(UsCulture));
                }

                writer.WriteElementString("default", stream.IsDefault.ToString());
                writer.WriteElementString("forced", stream.IsForced.ToString());

                if (stream.Type == MediaStreamType.Video)
                {
                    var runtimeTicks = item.RunTimeTicks;
                    if (runtimeTicks.HasValue)
                    {
                        var timespan = TimeSpan.FromTicks(runtimeTicks.Value);

                        writer.WriteElementString("duration", Math.Floor(timespan.TotalMinutes).ToString(UsCulture));
                        writer.WriteElementString("durationinseconds", Math.Floor(timespan.TotalSeconds).ToString(UsCulture));
                    }

                    var video = item as Video;

                    if (video != null)
                    {
                        //AddChapters(video, builder, itemRepository);

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

        // filters control characters but allows only properly-formed surrogate sequences
        private static Regex _invalidXMLChars = new Regex(
            @"(?<![\uD800-\uDBFF])[\uDC00-\uDFFF]|[\uD800-\uDBFF](?![\uDC00-\uDFFF])|[\x00-\x08\x0B\x0C\x0E-\x1F\x7F-\x9F\uFEFF\uFFFE\uFFFF]");

        /// <summary>
        /// removes any unusual unicode characters that can't be encoded into XML
        /// </summary>
        public static string RemoveInvalidXMLChars(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return _invalidXMLChars.Replace(text, string.Empty);
        }

        public const string DateAddedFormat = "yyyy-MM-dd HH:mm:ss";

        /// <summary>
        /// Adds the common nodes.
        /// </summary>
        /// <returns>Task.</returns>
        private void AddCommonNodes(BaseItem item, XmlWriter writer, ILibraryManager libraryManager, IUserManager userManager, IUserDataManager userDataRepo, IFileSystem fileSystem, IServerConfigurationManager config)
        {
            var writtenProviderIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var overview = (item.Overview ?? string.Empty)
                .StripHtml()
                .Replace("&quot;", "'");

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

            if (item is Video)
            {
                var outline = (item.Tagline ?? string.Empty)
                    .StripHtml()
                    .Replace("&quot;", "'");

                writer.WriteElementString("outline", outline);
            }
            else
            {
                writer.WriteElementString("outline", overview);
            }

            if (!string.IsNullOrWhiteSpace(item.CustomRating))
            {
                writer.WriteElementString("customrating", item.CustomRating);
            }

            writer.WriteElementString("lockdata", item.IsLocked.ToString().ToLower());

            if (item.LockedFields.Length > 0)
            {
                writer.WriteElementString("lockedfields", string.Join("|", item.LockedFields));
            }

            writer.WriteElementString("dateadded", item.DateCreated.ToLocalTime().ToString(DateAddedFormat));

            writer.WriteElementString("title", item.Name ?? string.Empty);

            if (!string.IsNullOrWhiteSpace(item.OriginalTitle))
            {
                writer.WriteElementString("originaltitle", item.OriginalTitle);
            }

            var people = libraryManager.GetPeople(item);

            var directors = people
                .Where(i => IsPersonType(i, PersonType.Director))
                .Select(i => i.Name)
                .ToList();

            foreach (var person in directors)
            {
                writer.WriteElementString("director", person);
            }

            var writers = people
                .Where(i => IsPersonType(i, PersonType.Writer))
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
                writer.WriteElementString("rating", item.CommunityRating.Value.ToString(UsCulture));
            }

            if (item.ProductionYear.HasValue)
            {
                writer.WriteElementString("year", item.ProductionYear.Value.ToString(UsCulture));
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

            var hasAspectRatio = item as IHasAspectRatio;
            if (hasAspectRatio != null)
            {
                if (!string.IsNullOrEmpty(hasAspectRatio.AspectRatio))
                {
                    writer.WriteElementString("aspectratio", hasAspectRatio.AspectRatio);
                }
            }

            var tmdbCollection = item.GetProviderId(MetadataProviders.TmdbCollection);

            if (!string.IsNullOrEmpty(tmdbCollection))
            {
                writer.WriteElementString("collectionnumber", tmdbCollection);
                writtenProviderIds.Add(MetadataProviders.TmdbCollection.ToString());
            }

            var imdb = item.GetProviderId(MetadataProviders.Imdb);
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
                writtenProviderIds.Add(MetadataProviders.Imdb.ToString());
            }

            // Series xml saver already saves this
            if (!(item is Series))
            {
                var tvdb = item.GetProviderId(MetadataProviders.Tvdb);
                if (!string.IsNullOrEmpty(tvdb))
                {
                    writer.WriteElementString("tvdbid", tvdb);
                    writtenProviderIds.Add(MetadataProviders.Tvdb.ToString());
                }
            }

            var tmdb = item.GetProviderId(MetadataProviders.Tmdb);
            if (!string.IsNullOrEmpty(tmdb))
            {
                writer.WriteElementString("tmdbid", tmdb);
                writtenProviderIds.Add(MetadataProviders.Tmdb.ToString());
            }

            if (!string.IsNullOrEmpty(item.PreferredMetadataLanguage))
            {
                writer.WriteElementString("language", item.PreferredMetadataLanguage);
            }
            if (!string.IsNullOrEmpty(item.PreferredMetadataCountryCode))
            {
                writer.WriteElementString("countrycode", item.PreferredMetadataCountryCode);
            }

            if (item.PremiereDate.HasValue && !(item is Episode))
            {
                var formatString = options.ReleaseDateFormat;

                if (item is MusicArtist)
                {
                    writer.WriteElementString("formed", item.PremiereDate.Value.ToLocalTime().ToString(formatString));
                }
                else
                {
                    writer.WriteElementString("premiered", item.PremiereDate.Value.ToLocalTime().ToString(formatString));
                    writer.WriteElementString("releasedate", item.PremiereDate.Value.ToLocalTime().ToString(formatString));
                }
            }

            if (item.EndDate.HasValue)
            {
                if (!(item is Episode))
                {
                    var formatString = options.ReleaseDateFormat;

                    writer.WriteElementString("enddate", item.EndDate.Value.ToLocalTime().ToString(formatString));
                }
            }

            if (item.CriticRating.HasValue)
            {
                writer.WriteElementString("criticrating", item.CriticRating.Value.ToString(UsCulture));
            }

            var hasDisplayOrder = item as IHasDisplayOrder;

            if (hasDisplayOrder != null)
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

                writer.WriteElementString("runtime", Convert.ToInt64(timespan.TotalMinutes).ToString(UsCulture));
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

            var externalId = item.GetProviderId(MetadataProviders.AudioDbArtist);

            if (!string.IsNullOrEmpty(externalId))
            {
                writer.WriteElementString("audiodbartistid", externalId);
                writtenProviderIds.Add(MetadataProviders.AudioDbArtist.ToString());
            }

            externalId = item.GetProviderId(MetadataProviders.AudioDbAlbum);

            if (!string.IsNullOrEmpty(externalId))
            {
                writer.WriteElementString("audiodbalbumid", externalId);
                writtenProviderIds.Add(MetadataProviders.AudioDbAlbum.ToString());
            }

            externalId = item.GetProviderId(MetadataProviders.Zap2It);

            if (!string.IsNullOrEmpty(externalId))
            {
                writer.WriteElementString("zap2itid", externalId);
                writtenProviderIds.Add(MetadataProviders.Zap2It.ToString());
            }

            externalId = item.GetProviderId(MetadataProviders.MusicBrainzAlbum);

            if (!string.IsNullOrEmpty(externalId))
            {
                writer.WriteElementString("musicbrainzalbumid", externalId);
                writtenProviderIds.Add(MetadataProviders.MusicBrainzAlbum.ToString());
            }

            externalId = item.GetProviderId(MetadataProviders.MusicBrainzAlbumArtist);

            if (!string.IsNullOrEmpty(externalId))
            {
                writer.WriteElementString("musicbrainzalbumartistid", externalId);
                writtenProviderIds.Add(MetadataProviders.MusicBrainzAlbumArtist.ToString());
            }

            externalId = item.GetProviderId(MetadataProviders.MusicBrainzArtist);

            if (!string.IsNullOrEmpty(externalId))
            {
                writer.WriteElementString("musicbrainzartistid", externalId);
                writtenProviderIds.Add(MetadataProviders.MusicBrainzArtist.ToString());
            }

            externalId = item.GetProviderId(MetadataProviders.MusicBrainzReleaseGroup);

            if (!string.IsNullOrEmpty(externalId))
            {
                writer.WriteElementString("musicbrainzreleasegroupid", externalId);
                writtenProviderIds.Add(MetadataProviders.MusicBrainzReleaseGroup.ToString());
            }

            externalId = item.GetProviderId(MetadataProviders.Gamesdb);
            if (!string.IsNullOrEmpty(externalId))
            {
                writer.WriteElementString("gamesdbid", externalId);
                writtenProviderIds.Add(MetadataProviders.Gamesdb.ToString());
            }

            externalId = item.GetProviderId(MetadataProviders.TvRage);
            if (!string.IsNullOrEmpty(externalId))
            {
                writer.WriteElementString("tvrageid", externalId);
                writtenProviderIds.Add(MetadataProviders.TvRage.ToString());
            }

            if (item.ProviderIds != null)
            {
                foreach (var providerKey in item.ProviderIds.Keys)
                {
                    var providerId = item.ProviderIds[providerKey];
                    if (!string.IsNullOrEmpty(providerId) && !writtenProviderIds.Contains(providerKey))
                    {
                        try
                        {
                            var tagName = GetTagForProviderKey(providerKey);
                            //logger.LogDebug("Verifying custom provider tagname {0}", tagName);
                            XmlConvert.VerifyName(tagName);
                            //logger.LogDebug("Saving custom provider tagname {0}", tagName);

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
                AddImages(item, writer, libraryManager, config);
            }

            AddUserData(item, writer, userManager, userDataRepo, options);

            AddActors(people, writer, libraryManager, fileSystem, config, options.SaveImagePathsInNfo);

            var folder = item as BoxSet;
            if (folder != null)
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
            return url.Replace(YouTubeWatchUrl, "plugin://plugin.video.youtube/?action=play_video&videoid=", StringComparison.OrdinalIgnoreCase);
        }

        private void AddImages(BaseItem item, XmlWriter writer, ILibraryManager libraryManager, IServerConfigurationManager config)
        {
            writer.WriteStartElement("art");

            var image = item.GetImageInfo(ImageType.Primary, 0);

            if (image != null)
            {
                writer.WriteElementString("poster", GetImagePathToSave(image, libraryManager, config));
            }

            foreach (var backdrop in item.GetImages(ImageType.Backdrop))
            {
                writer.WriteElementString("fanart", GetImagePathToSave(backdrop, libraryManager, config));
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

            var user = userManager.GetUserById(userId);

            if (user == null)
            {
                return;
            }

            if (item.IsFolder)
            {
                return;
            }

            var userdata = userDataRepo.GetUserData(user, item);

            writer.WriteElementString("isuserfavorite", userdata.IsFavorite.ToString().ToLower());

            if (userdata.Rating.HasValue)
            {
                writer.WriteElementString("userrating", userdata.Rating.Value.ToString(CultureInfo.InvariantCulture).ToLower());
            }

            if (!item.IsFolder)
            {
                writer.WriteElementString("playcount", userdata.PlayCount.ToString(UsCulture));
                writer.WriteElementString("watched", userdata.Played.ToString().ToLower());

                if (userdata.LastPlayedDate.HasValue)
                {
                    writer.WriteElementString("lastplayed", userdata.LastPlayedDate.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss").ToLower());
                }

                writer.WriteStartElement("resume");

                var runTimeTicks = item.RunTimeTicks ?? 0;

                writer.WriteElementString("position", TimeSpan.FromTicks(userdata.PlaybackPositionTicks).TotalSeconds.ToString(UsCulture));
                writer.WriteElementString("total", TimeSpan.FromTicks(runTimeTicks).TotalSeconds.ToString(UsCulture));
            }

            writer.WriteEndElement();
        }

        private void AddActors(List<PersonInfo> people, XmlWriter writer, ILibraryManager libraryManager, IFileSystem fileSystem, IServerConfigurationManager config, bool saveImagePath)
        {
            var actors = people
                .Where(i => !IsPersonType(i, PersonType.Director) && !IsPersonType(i, PersonType.Writer))
                .ToList();

            foreach (var person in actors)
            {
                writer.WriteStartElement("actor");

                if (!string.IsNullOrWhiteSpace(person.Name))
                {
                    writer.WriteElementString("name", person.Name);
                }

                if (!string.IsNullOrWhiteSpace(person.Role))
                {
                    writer.WriteElementString("role", person.Role);
                }

                if (!string.IsNullOrWhiteSpace(person.Type))
                {
                    writer.WriteElementString("type", person.Type);
                }

                if (person.SortOrder.HasValue)
                {
                    writer.WriteElementString("sortorder", person.SortOrder.Value.ToString(UsCulture));
                }

                if (saveImagePath)
                {
                    try
                    {
                        var personEntity = libraryManager.GetPerson(person.Name);
                        var image = personEntity.GetImageInfo(ImageType.Primary, 0);

                        if (image != null)
                        {
                            writer.WriteElementString("thumb", GetImagePathToSave(image, libraryManager, config));
                        }
                    }
                    catch (Exception)
                    {
                        // Already logged in core
                    }
                }

                writer.WriteEndElement();
            }
        }

        private string GetImagePathToSave(ItemImageInfo image, ILibraryManager libraryManager, IServerConfigurationManager config)
        {
            if (!image.IsLocalFile)
            {
                return image.Path;
            }

            return libraryManager.GetPathAfterNetworkSubstitution(image.Path);
        }

        private bool IsPersonType(PersonInfo person, string type)
        {
            return string.Equals(person.Type, type, StringComparison.OrdinalIgnoreCase) || string.Equals(person.Role, type, StringComparison.OrdinalIgnoreCase);
        }

        private void AddCustomTags(string path, List<string> xmlTagsUsed, XmlWriter writer, ILogger logger, IFileSystem fileSystem)
        {
            var settings = XmlReaderSettingsFactory.Create(false);

            settings.CheckCharacters = false;
            settings.IgnoreProcessingInstructions = true;
            settings.IgnoreComments = true;

            using (var fileStream = fileSystem.OpenRead(path))
            {
                using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
                {
                    // Use XmlReader for best performance
                    using (var reader = XmlReader.Create(streamReader, settings))
                    {
                        try
                        {
                            reader.MoveToContent();
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Error reading existing xml tags from {path}.", path);
                            return;
                        }

                        reader.Read();

                        // Loop through each element
                        while (!reader.EOF && reader.ReadState == ReadState.Interactive)
                        {
                            if (reader.NodeType == XmlNodeType.Element)
                            {
                                var name = reader.Name;

                                if (!CommonTags.ContainsKey(name) && !xmlTagsUsed.Contains(name, StringComparer.OrdinalIgnoreCase))
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
            }
        }

        private string GetTagForProviderKey(string providerKey)
        {
            return providerKey.ToLower() + "id";
        }
    }
}
