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
using MediaBrowser.Model.Logging;
using MediaBrowser.XbmcMetadata.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using CommonIO;
using MediaBrowser.Model.Extensions;

namespace MediaBrowser.XbmcMetadata.Savers
{
    public abstract class BaseNfoSaver : IMetadataFileSaver
    {
        private static readonly CultureInfo UsCulture = new CultureInfo("en-US");

        private static readonly Dictionary<string, string> CommonTags = new[] {     
               
                    "plot",
                    "customrating",
                    "lockdata",
                    "type",
                    "dateadded",
                    "title",
                    "rating",
                    "year",
                    "sorttitle",
                    "mpaa",
                    "mpaadescription",
                    "aspectratio",
                    "website",
                    "collectionnumber",
                    "tmdbid",
                    "rottentomatoesid",
                    "language",
                    "tvcomid",
                    "budget",
                    "revenue",
                    "tagline",
                    "studio",
                    "genre",
                    "tag",
                    "runtime",
                    "actor",
                    "criticratingsummary",
                    "criticrating",
                    "fileinfo",
                    "director",
                    "writer",
                    "trailer",
                    "premiered",
                    "releasedate",
                    "outline",
                    "id",
                    "votes",
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
                    "plotkeyword",
                    "country",
                    "audiodbalbumid",
                    "audiodbartistid",
                    "awardsummary",
                    "enddate",
                    "lockedfields",
                    "metascore",
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

        protected BaseNfoSaver(IFileSystem fileSystem, IServerConfigurationManager configurationManager, ILibraryManager libraryManager, IUserManager userManager, IUserDataManager userDataManager, ILogger logger)
        {
            Logger = logger;
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

        public string GetSavePath(IHasMetadata item)
        {
            return GetLocalSavePath(item);
        }

        /// <summary>
        /// Gets the save path.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        protected abstract string GetLocalSavePath(IHasMetadata item);

        /// <summary>
        /// Gets the name of the root element.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        protected abstract string GetRootElementName(IHasMetadata item);

        /// <summary>
        /// Determines whether [is enabled for] [the specified item].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="updateType">Type of the update.</param>
        /// <returns><c>true</c> if [is enabled for] [the specified item]; otherwise, <c>false</c>.</returns>
        public abstract bool IsEnabledFor(IHasMetadata item, ItemUpdateType updateType);

        protected virtual List<string> GetTagsUsed()
        {
            return new List<string>();
        }

        public void Save(IHasMetadata item, CancellationToken cancellationToken)
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
            FileSystem.CreateDirectory(Path.GetDirectoryName(path));

            var file = new FileInfo(path);

            var wasHidden = false;

            // This will fail if the file is hidden
            if (file.Exists)
            {
                if ((file.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                {
                    file.Attributes &= ~FileAttributes.Hidden;

                    wasHidden = true;
                }
            }

            using (var filestream = FileSystem.GetFileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                stream.CopyTo(filestream);
            }

            if (wasHidden || ConfigurationManager.Configuration.SaveMetadataHidden)
            {
                file.Refresh();

                // Add back the attribute
                file.Attributes |= FileAttributes.Hidden;
            }
        }

        private void Save(IHasMetadata item, Stream stream, string xmlPath)
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

                var baseItem = item as BaseItem;

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

                var tagsUsed = GetTagsUsed();

                try
                {
                    AddCustomTags(xmlPath, tagsUsed, writer, Logger);
                }
                catch (FileNotFoundException)
                {

                }
                catch (DirectoryNotFoundException)
                {

                }
                catch (XmlException ex)
                {
                    Logger.ErrorException("Error reading existng nfo", ex);
                }

                writer.WriteEndElement();

                writer.WriteEndDocument();
            }
        }

        protected abstract void WriteCustomElements(IHasMetadata item, XmlWriter writer);

        public static void AddMediaInfo<T>(T item, XmlWriter writer)
         where T : IHasMediaSources
        {
            writer.WriteStartElement("fileinfo");
            writer.WriteStartElement("streamdetails");

            var mediaSource = item.GetMediaSources(false).First();

            foreach (var stream in mediaSource.MediaStreams)
            {
                writer.WriteStartElement(stream.Type.ToString().ToLower());

                if (!string.IsNullOrEmpty(stream.Codec))
                {
                    var codec = stream.Codec;

                    if ((stream.CodecTag ?? string.Empty).IndexOf("xvid", StringComparison.OrdinalIgnoreCase) != -1)
                    {
                        codec = "xvid;";
                    }
                    else if ((stream.CodecTag ?? string.Empty).IndexOf("divx", StringComparison.OrdinalIgnoreCase) != -1)
                    {
                        codec = "divx;";
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
                    writer.WriteElementString("language", stream.Language);
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
                    if (mediaSource.RunTimeTicks.HasValue)
                    {
                        var timespan = TimeSpan.FromTicks(mediaSource.RunTimeTicks.Value);

                        writer.WriteElementString("duration", Convert.ToInt32(timespan.TotalMinutes).ToString(UsCulture));
                        writer.WriteElementString("durationinseconds", Convert.ToInt32(timespan.TotalSeconds).ToString(UsCulture));
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

        public const string DateAddedFormat = "yyyy-MM-dd HH:mm:ss";

        /// <summary>
        /// Adds the common nodes.
        /// </summary>
        /// <returns>Task.</returns>
        public static void AddCommonNodes(BaseItem item, XmlWriter writer, ILibraryManager libraryManager, IUserManager userManager, IUserDataManager userDataRepo, IFileSystem fileSystem, IServerConfigurationManager config)
        {
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

            var hasShortOverview = item as IHasShortOverview;
            if (hasShortOverview != null)
            {
                var outline = (hasShortOverview.ShortOverview ?? string.Empty)
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

            if (item.LockedFields.Count > 0)
            {
                writer.WriteElementString("lockedfields", string.Join("|", item.LockedFields.Select(i => i.ToString()).ToArray()));
            }

            if (!string.IsNullOrEmpty(item.DisplayMediaType))
            {
                writer.WriteElementString("type", item.DisplayMediaType);
            }

            writer.WriteElementString("dateadded", item.DateCreated.ToLocalTime().ToString(DateAddedFormat));

            writer.WriteElementString("title", item.Name ?? string.Empty);

            var hasOriginalTitle = item as IHasOriginalTitle;
            if (hasOriginalTitle != null)
            {
                if (!string.IsNullOrEmpty(hasOriginalTitle.OriginalTitle))
                {
                    writer.WriteElementString("originaltitle", hasOriginalTitle.OriginalTitle ?? string.Empty);
                }
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

            var hasTrailer = item as IHasTrailers;
            if (hasTrailer != null)
            {
                foreach (var trailer in hasTrailer.RemoteTrailers)
                {
                    writer.WriteElementString("trailer", GetOutputTrailerUrl(trailer.Url));
                }
            }

            if (item.CommunityRating.HasValue)
            {
                writer.WriteElementString("rating", item.CommunityRating.Value.ToString(UsCulture));
            }

            if (item.ProductionYear.HasValue)
            {
                writer.WriteElementString("year", item.ProductionYear.Value.ToString(UsCulture));
            }

            if (!string.IsNullOrEmpty(item.ForcedSortName))
            {
                writer.WriteElementString("sorttitle", item.ForcedSortName);
            }

            if (!string.IsNullOrEmpty(item.OfficialRating))
            {
                writer.WriteElementString("mpaa", item.OfficialRating);
            }

            if (!string.IsNullOrEmpty(item.OfficialRatingDescription))
            {
                writer.WriteElementString("mpaadescription", item.OfficialRatingDescription);
            }

            var hasAspectRatio = item as IHasAspectRatio;
            if (hasAspectRatio != null)
            {
                if (!string.IsNullOrEmpty(hasAspectRatio.AspectRatio))
                {
                    writer.WriteElementString("aspectratio", hasAspectRatio.AspectRatio);
                }
            }

            if (!string.IsNullOrEmpty(item.HomePageUrl))
            {
                writer.WriteElementString("website", item.HomePageUrl);
            }

            var rt = item.GetProviderId(MetadataProviders.RottenTomatoes);

            if (!string.IsNullOrEmpty(rt))
            {
                writer.WriteElementString("rottentomatoesid", rt);
            }

            var tmdbCollection = item.GetProviderId(MetadataProviders.TmdbCollection);

            if (!string.IsNullOrEmpty(tmdbCollection))
            {
                writer.WriteElementString("collectionnumber", tmdbCollection);
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
            }

            // Series xml saver already saves this
            if (!(item is Series))
            {
                var tvdb = item.GetProviderId(MetadataProviders.Tvdb);
                if (!string.IsNullOrEmpty(tvdb))
                {
                    writer.WriteElementString("tvdbid", tvdb);
                }
            }

            var tmdb = item.GetProviderId(MetadataProviders.Tmdb);
            if (!string.IsNullOrEmpty(tmdb))
            {
                writer.WriteElementString("tmdbid", tmdb);
            }

            var tvcom = item.GetProviderId(MetadataProviders.Tvcom);
            if (!string.IsNullOrEmpty(tvcom))
            {
                writer.WriteElementString("tvcomid", tvcom);
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

            var hasCriticRating = item as IHasCriticRating;

            if (hasCriticRating != null)
            {
                if (hasCriticRating.CriticRating.HasValue)
                {
                    writer.WriteElementString("criticrating", hasCriticRating.CriticRating.Value.ToString(UsCulture));
                }

                if (!string.IsNullOrEmpty(hasCriticRating.CriticRatingSummary))
                {
                    writer.WriteElementString("criticratingsummary", hasCriticRating.CriticRatingSummary);
                }
            }

            var hasDisplayOrder = item as IHasDisplayOrder;

            if (hasDisplayOrder != null)
            {
                if (!string.IsNullOrEmpty(hasDisplayOrder.DisplayOrder))
                {
                    writer.WriteElementString("displayorder", hasDisplayOrder.DisplayOrder);
                }
            }

            if (item.VoteCount.HasValue)
            {
                writer.WriteElementString("votes", item.VoteCount.Value.ToString(UsCulture));
            }

            var hasBudget = item as IHasBudget;
            if (hasBudget != null)
            {
                if (hasBudget.Budget.HasValue)
                {
                    writer.WriteElementString("budget", hasBudget.Budget.Value.ToString(UsCulture));
                }

                if (hasBudget.Revenue.HasValue)
                {
                    writer.WriteElementString("revenue", hasBudget.Revenue.Value.ToString(UsCulture));
                }
            }

            var hasMetascore = item as IHasMetascore;
            if (hasMetascore != null && hasMetascore.Metascore.HasValue)
            {
                writer.WriteElementString("metascore", hasMetascore.Metascore.Value.ToString(UsCulture));
            }

            // Use original runtime here, actual file runtime later in MediaInfo
            var runTimeTicks = item.RunTimeTicks;

            if (runTimeTicks.HasValue)
            {
                var timespan = TimeSpan.FromTicks(runTimeTicks.Value);

                writer.WriteElementString("runtime", Convert.ToInt32(timespan.TotalMinutes).ToString(UsCulture));
            }

            var hasTaglines = item as IHasTaglines;
            if (hasTaglines != null)
            {
                foreach (var tagline in hasTaglines.Taglines)
                {
                    writer.WriteElementString("tagline", tagline);
                }
            }

            var hasProductionLocations = item as IHasProductionLocations;
            if (hasProductionLocations != null)
            {
                foreach (var country in hasProductionLocations.ProductionLocations)
                {
                    writer.WriteElementString("country", country);
                }
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

            foreach (var tag in item.Keywords)
            {
                writer.WriteElementString("plotkeyword", tag);
            }

            var hasAwards = item as IHasAwards;
            if (hasAwards != null && !string.IsNullOrEmpty(hasAwards.AwardSummary))
            {
                writer.WriteElementString("awardsummary", hasAwards.AwardSummary);
            }

            var externalId = item.GetProviderId(MetadataProviders.AudioDbArtist);

            if (!string.IsNullOrEmpty(externalId))
            {
                writer.WriteElementString("audiodbartistid", externalId);
            }

            externalId = item.GetProviderId(MetadataProviders.AudioDbAlbum);

            if (!string.IsNullOrEmpty(externalId))
            {
                writer.WriteElementString("audiodbalbumid", externalId);
            }

            externalId = item.GetProviderId(MetadataProviders.Zap2It);

            if (!string.IsNullOrEmpty(externalId))
            {
                writer.WriteElementString("zap2itid", externalId);
            }

            externalId = item.GetProviderId(MetadataProviders.MusicBrainzAlbum);

            if (!string.IsNullOrEmpty(externalId))
            {
                writer.WriteElementString("musicbrainzalbumid", externalId);
            }

            externalId = item.GetProviderId(MetadataProviders.MusicBrainzAlbumArtist);

            if (!string.IsNullOrEmpty(externalId))
            {
                writer.WriteElementString("musicbrainzalbumartistid", externalId);
            }

            externalId = item.GetProviderId(MetadataProviders.MusicBrainzArtist);

            if (!string.IsNullOrEmpty(externalId))
            {
                writer.WriteElementString("musicbrainzartistid", externalId);
            }

            externalId = item.GetProviderId(MetadataProviders.MusicBrainzReleaseGroup);

            if (!string.IsNullOrEmpty(externalId))
            {
                writer.WriteElementString("musicbrainzreleasegroupid", externalId);
            }

            externalId = item.GetProviderId(MetadataProviders.Gamesdb);
            if (!string.IsNullOrEmpty(externalId))
            {
                writer.WriteElementString("gamesdbid", externalId);
            }

            externalId = item.GetProviderId(MetadataProviders.TvRage);
            if (!string.IsNullOrEmpty(externalId))
            {
                writer.WriteElementString("tvrageid", externalId);
            }

            externalId = item.GetProviderId(MetadataProviders.TvMaze);
            if (!string.IsNullOrEmpty(externalId))
            {
                writer.WriteElementString("tvmazeid", externalId);
            }

            if (options.SaveImagePathsInNfo)
            {
                AddImages(item, writer, libraryManager, config);
            }

            AddUserData(item, writer, userManager, userDataRepo, options);

            AddActors(people, writer, libraryManager, fileSystem, config);

            var folder = item as BoxSet;
            if (folder != null)
            {
                AddCollectionItems(folder, writer);
            }
        }

        public static void AddChapters(Video item, XmlWriter writer, IItemRepository repository)
        {
            var chapters = repository.GetChapters(item.Id);

            foreach (var chapter in chapters)
            {
                writer.WriteStartElement("chapter");
                writer.WriteElementString("name", chapter.Name);

                var time = TimeSpan.FromTicks(chapter.StartPositionTicks);
                var ms = Convert.ToInt64(time.TotalMilliseconds);

                writer.WriteElementString("startpositionms", ms.ToString(UsCulture));
                writer.WriteEndElement();
            }
        }

        private static void AddCollectionItems(Folder item, XmlWriter writer)
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

                writer.WriteEndElement();
            }
        }

        /// <summary>
        /// Gets the output trailer URL.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns>System.String.</returns>
        private static string GetOutputTrailerUrl(string url)
        {
            // This is what xbmc expects

            return url.Replace("https://www.youtube.com/watch?v=",
                "plugin://plugin.video.youtube/?action=play_video&videoid=",
                StringComparison.OrdinalIgnoreCase);
        }

        private static void AddImages(BaseItem item, XmlWriter writer, ILibraryManager libraryManager, IServerConfigurationManager config)
        {
            writer.WriteStartElement("art");

            var image = item.GetImageInfo(ImageType.Primary, 0);

            if (image != null && image.IsLocalFile)
            {
                writer.WriteElementString("poster", GetPathToSave(image.Path, libraryManager, config));
            }

            foreach (var backdrop in item.GetImages(ImageType.Backdrop))
            {
                if (backdrop.IsLocalFile)
                {
                    writer.WriteElementString("fanart", GetPathToSave(backdrop.Path, libraryManager, config));
                }
            }

            writer.WriteEndElement();
        }

        private static void AddUserData(BaseItem item, XmlWriter writer, IUserManager userManager, IUserDataManager userDataRepo, XbmcMetadataOptions options)
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

        private static void AddActors(List<PersonInfo> people, XmlWriter writer, ILibraryManager libraryManager, IFileSystem fileSystem, IServerConfigurationManager config)
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

                try
                {
                    var personEntity = libraryManager.GetPerson(person.Name);
                    var image = personEntity.GetImageInfo(ImageType.Primary, 0);

                    if (image != null && image.IsLocalFile)
                    {
                        writer.WriteElementString("thumb", GetPathToSave(image.Path, libraryManager, config));
                    }
                }
                catch (Exception)
                {
                    // Already logged in core
                }

                writer.WriteEndElement();
            }
        }

        private static string GetPathToSave(string path, ILibraryManager libraryManager, IServerConfigurationManager config)
        {
            foreach (var map in config.Configuration.PathSubstitutions)
            {
                path = libraryManager.SubstitutePath(path, map.From, map.To);
            }

            return path;
        }

        private static bool IsPersonType(PersonInfo person, string type)
        {
            return string.Equals(person.Type, type, StringComparison.OrdinalIgnoreCase) || string.Equals(person.Role, type, StringComparison.OrdinalIgnoreCase);
        }

        private static void AddCustomTags(string path, List<string> xmlTagsUsed, XmlWriter writer, ILogger logger)
        {
            var settings = new XmlReaderSettings
            {
                CheckCharacters = false,
                IgnoreProcessingInstructions = true,
                IgnoreComments = true,
                ValidationType = ValidationType.None
            };

            using (var streamReader = GetStreamReader(path))
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
                        logger.ErrorException("Error reading existing xml tags from {0}.", ex, path);
                        return;
                    }

                    // Loop through each element
                    while (reader.Read())
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
                    }
                }
            }

        }

        public static StreamReader GetStreamReader(string path)
        {
            return new StreamReader(path, Encoding.UTF8);
        }
    }
}
