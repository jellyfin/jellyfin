using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Xml;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.XbmcMetadata.Configuration;

namespace MediaBrowser.XbmcMetadata.Savers
{
    public static class XmlSaverHelpers
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
                    "collectionitem"

        }.ToDictionary(i => i, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Saves the specified XML.
        /// </summary>
        /// <param name="xml">The XML.</param>
        /// <param name="path">The path.</param>
        /// <param name="xmlTagsUsed">The XML tags used.</param>
        public static void Save(StringBuilder xml, string path, List<string> xmlTagsUsed)
        {
            if (File.Exists(path))
            {
                var tags = xmlTagsUsed.ToList();

                var position = xml.ToString().LastIndexOf("</", StringComparison.OrdinalIgnoreCase);
                xml.Insert(position, GetCustomTags(path, tags));
            }

            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xml.ToString());

            //Add the new node to the document.
            xmlDocument.InsertBefore(xmlDocument.CreateXmlDeclaration("1.0", "UTF-8", "yes"), xmlDocument.DocumentElement);

            Directory.CreateDirectory(Path.GetDirectoryName(path));

            var wasHidden = false;

            var file = new FileInfo(path);

            // This will fail if the file is hidden
            if (file.Exists)
            {
                if ((file.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                {
                    file.Attributes &= ~FileAttributes.Hidden;

                    wasHidden = true;
                }
            }

            using (var filestream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                using (var streamWriter = new StreamWriter(filestream, Encoding.UTF8))
                {
                    xmlDocument.Save(streamWriter);
                }
            }

            if (wasHidden)
            {
                file.Refresh();

                // Add back the attribute
                file.Attributes |= FileAttributes.Hidden;
            }
        }

        /// <summary>
        /// Gets the custom tags.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="xmlTagsUsed">The XML tags used.</param>
        /// <returns>System.String.</returns>
        private static string GetCustomTags(string path, List<string> xmlTagsUsed)
        {
            var settings = new XmlReaderSettings
            {
                CheckCharacters = false,
                IgnoreProcessingInstructions = true,
                IgnoreComments = true,
                ValidationType = ValidationType.None
            };

            var builder = new StringBuilder();

            using (var streamReader = new StreamReader(path, Encoding.UTF8))
            {
                // Use XmlReader for best performance
                using (var reader = XmlReader.Create(streamReader, settings))
                {
                    reader.MoveToContent();

                    // Loop through each element
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            var name = reader.Name;

                            if (!CommonTags.ContainsKey(name) && !xmlTagsUsed.Contains(name, StringComparer.OrdinalIgnoreCase))
                            {
                                builder.AppendLine(reader.ReadOuterXml());
                            }
                            else
                            {
                                reader.Skip();
                            }
                        }
                    }
                }
            }

            return builder.ToString();
        }

        public static void AddMediaInfo<T>(T item, StringBuilder builder)
            where T : BaseItem, IHasMediaSources
        {
            builder.Append("<fileinfo>");
            builder.Append("<streamdetails>");

            foreach (var stream in item.GetMediaSources(false).First().MediaStreams)
            {
                builder.Append("<" + stream.Type.ToString().ToLower() + ">");

                if (!string.IsNullOrEmpty(stream.Codec))
                {
                    builder.Append("<codec>" + SecurityElement.Escape(stream.Codec) + "</codec>");
                    builder.Append("<micodec>" + SecurityElement.Escape(stream.Codec) + "</micodec>");
                }

                if (stream.BitRate.HasValue)
                {
                    builder.Append("<bitrate>" + stream.BitRate.Value.ToString(UsCulture) + "</bitrate>");
                }

                if (stream.Width.HasValue)
                {
                    builder.Append("<width>" + stream.Width.Value.ToString(UsCulture) + "</width>");
                }

                if (stream.Height.HasValue)
                {
                    builder.Append("<height>" + stream.Height.Value.ToString(UsCulture) + "</height>");
                }

                if (!string.IsNullOrEmpty(stream.AspectRatio))
                {
                    builder.Append("<aspect>" + SecurityElement.Escape(stream.AspectRatio) + "</aspect>");
                    builder.Append("<aspectratio>" + SecurityElement.Escape(stream.AspectRatio) + "</aspectratio>");
                }

                var framerate = stream.AverageFrameRate ?? stream.RealFrameRate;

                if (framerate.HasValue)
                {
                    builder.Append("<framerate>" + framerate.Value.ToString(UsCulture) + "</framerate>");
                }

                if (!string.IsNullOrEmpty(stream.Language))
                {
                    builder.Append("<language>" + SecurityElement.Escape(stream.Language) + "</language>");
                }

                var scanType = stream.IsInterlaced ? "interlaced" : "progressive";
                if (!string.IsNullOrEmpty(scanType))
                {
                    builder.Append("<scantype>" + SecurityElement.Escape(scanType) + "</scantype>");
                }

                if (stream.Channels.HasValue)
                {
                    builder.Append("<channels>" + stream.Channels.Value.ToString(UsCulture) + "</channels>");
                }

                if (stream.SampleRate.HasValue)
                {
                    builder.Append("<samplingrate>" + stream.SampleRate.Value.ToString(UsCulture) + "</samplingrate>");
                }

                builder.Append("<default>" + SecurityElement.Escape(stream.IsDefault.ToString()) + "</default>");
                builder.Append("<forced>" + SecurityElement.Escape(stream.IsForced.ToString()) + "</forced>");

                if (stream.Type == MediaStreamType.Video)
                {
                    if (item.RunTimeTicks.HasValue)
                    {
                        var timespan = TimeSpan.FromTicks(item.RunTimeTicks.Value);

                        builder.Append("<duration>" + Convert.ToInt32(timespan.TotalMinutes).ToString(UsCulture) + "</duration>");
                        builder.Append("<durationinseconds>" + Convert.ToInt32(timespan.TotalSeconds).ToString(UsCulture) + "</durationinseconds>");
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
                                    builder.Append("<format3d>FSBS</format3d>");
                                    break;
                                case Video3DFormat.FullTopAndBottom:
                                    builder.Append("<format3d>FTAB</format3d>");
                                    break;
                                case Video3DFormat.HalfSideBySide:
                                    builder.Append("<format3d>HSBS</format3d>");
                                    break;
                                case Video3DFormat.HalfTopAndBottom:
                                    builder.Append("<format3d>HTAB</format3d>");
                                    break;
                            }
                        }
                    }
                }

                builder.Append("</" + stream.Type.ToString().ToLower() + ">");
            }

            builder.Append("</streamdetails>");
            builder.Append("</fileinfo>");
        }

        /// <summary>
        /// Adds the common nodes.
        /// </summary>
        /// <returns>Task.</returns>
        public static void AddCommonNodes(BaseItem item, StringBuilder builder, ILibraryManager libraryManager, IUserManager userManager, IUserDataManager userDataRepo, IFileSystem fileSystem, IServerConfigurationManager config)
        {
            var overview = (item.Overview ?? string.Empty)
                .StripHtml()
                .Replace("&quot;", "'");

            var options = config.GetNfoConfiguration();

            if (item is MusicArtist)
            {
                builder.Append("<biography><![CDATA[" + overview + "]]></biography>");
            }
            else if (item is MusicAlbum)
            {
                builder.Append("<review><![CDATA[" + overview + "]]></review>");
            }
            else
            {
                builder.Append("<plot><![CDATA[" + overview + "]]></plot>");
            }

            var hasShortOverview = item as IHasShortOverview;
            if (hasShortOverview != null)
            {
                var outline = (hasShortOverview.ShortOverview ?? string.Empty)
                    .StripHtml()
                    .Replace("&quot;", "'");

                builder.Append("<outline><![CDATA[" + outline + "]]></outline>");
            }
            else
            {
                builder.Append("<outline><![CDATA[" + overview + "]]></outline>");
            }

            builder.Append("<customrating>" + SecurityElement.Escape(item.CustomRating ?? string.Empty) + "</customrating>");
            builder.Append("<lockdata>" + item.IsLocked.ToString().ToLower() + "</lockdata>");

            if (item.LockedFields.Count > 0)
            {
                builder.Append("<lockedfields>" + string.Join("|", item.LockedFields.Select(i => i.ToString()).ToArray()) + "</lockedfields>");
            }
            
            if (!string.IsNullOrEmpty(item.DisplayMediaType))
            {
                builder.Append("<type>" + SecurityElement.Escape(item.DisplayMediaType) + "</type>");
            }

            builder.Append("<dateadded>" + SecurityElement.Escape(item.DateCreated.ToString("yyyy-MM-dd HH:mm:ss")) + "</dateadded>");

            builder.Append("<title>" + SecurityElement.Escape(item.Name ?? string.Empty) + "</title>");
            builder.Append("<originaltitle>" + SecurityElement.Escape(item.Name ?? string.Empty) + "</originaltitle>");

            var directors = item.People
                .Where(i => IsPersonType(i, PersonType.Director))
                .Select(i => i.Name)
                .ToList();

            foreach (var person in directors)
            {
                builder.Append("<director>" + SecurityElement.Escape(person) + "</director>");
            }

            var writers = item.People
                .Where(i => IsPersonType(i, PersonType.Director))
                .Select(i => i.Name)
                .ToList();

            foreach (var person in writers)
            {
                builder.Append("<writer>" + SecurityElement.Escape(person) + "</writer>");
            }

            var credits = writers.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            if (credits.Count > 0)
            {
                builder.Append("<credits>" + SecurityElement.Escape(string.Join(" / ", credits.ToArray())) + "</credits>");
            }

            var hasTrailer = item as IHasTrailers;
            if (hasTrailer != null)
            {
                foreach (var trailer in hasTrailer.RemoteTrailers)
                {
                    builder.Append("<trailer>" + SecurityElement.Escape(GetOutputTrailerUrl(trailer.Url)) + "</trailer>");
                }
            }

            if (item.CommunityRating.HasValue)
            {
                builder.Append("<rating>" + SecurityElement.Escape(item.CommunityRating.Value.ToString(UsCulture)) + "</rating>");
            }

            if (item.ProductionYear.HasValue)
            {
                builder.Append("<year>" + SecurityElement.Escape(item.ProductionYear.Value.ToString(UsCulture)) + "</year>");
            }

            if (!string.IsNullOrEmpty(item.ForcedSortName))
            {
                builder.Append("<sorttitle>" + SecurityElement.Escape(item.ForcedSortName) + "</sorttitle>");
            }

            if (!string.IsNullOrEmpty(item.OfficialRating))
            {
                builder.Append("<mpaa>" + SecurityElement.Escape(item.OfficialRating) + "</mpaa>");
            }

            if (!string.IsNullOrEmpty(item.OfficialRatingDescription))
            {
                builder.Append("<mpaadescription>" + SecurityElement.Escape(item.OfficialRatingDescription) + "</mpaadescription>");
            }

            var hasAspectRatio = item as IHasAspectRatio;
            if (hasAspectRatio != null)
            {
                if (!string.IsNullOrEmpty(hasAspectRatio.AspectRatio))
                {
                    builder.Append("<aspectratio>" + SecurityElement.Escape(hasAspectRatio.AspectRatio) + "</aspectratio>");
                }
            }

            if (!string.IsNullOrEmpty(item.HomePageUrl))
            {
                builder.Append("<website>" + SecurityElement.Escape(item.HomePageUrl) + "</website>");
            }

            var rt = item.GetProviderId(MetadataProviders.RottenTomatoes);

            if (!string.IsNullOrEmpty(rt))
            {
                builder.Append("<rottentomatoesid>" + SecurityElement.Escape(rt) + "</rottentomatoesid>");
            }

            var tmdbCollection = item.GetProviderId(MetadataProviders.TmdbCollection);

            if (!string.IsNullOrEmpty(tmdbCollection))
            {
                builder.Append("<collectionnumber>" + SecurityElement.Escape(tmdbCollection) + "</collectionnumber>");
            }

            var imdb = item.GetProviderId(MetadataProviders.Imdb);
            if (!string.IsNullOrEmpty(imdb))
            {
                if (item is Series)
                {
                    builder.Append("<imdb_id>" + SecurityElement.Escape(imdb) + "</imdb_id>");
                }
                else
                {
                    builder.Append("<imdbid>" + SecurityElement.Escape(imdb) + "</imdbid>");
                }
            }

            // Series xml saver already saves this
            if (!(item is Series))
            {
                var tvdb = item.GetProviderId(MetadataProviders.Tvdb);
                if (!string.IsNullOrEmpty(tvdb))
                {
                    builder.Append("<tvdbid>" + SecurityElement.Escape(tvdb) + "</tvdbid>");
                }
            }

            var tmdb = item.GetProviderId(MetadataProviders.Tmdb);
            if (!string.IsNullOrEmpty(tmdb))
            {
                builder.Append("<tmdbid>" + SecurityElement.Escape(tmdb) + "</tmdbid>");
            }

            var tvcom = item.GetProviderId(MetadataProviders.Tvcom);
            if (!string.IsNullOrEmpty(tvcom))
            {
                builder.Append("<tvcomid>" + SecurityElement.Escape(tvcom) + "</tvcomid>");
            }

            var hasLanguage = item as IHasPreferredMetadataLanguage;
            if (hasLanguage != null)
            {
                if (!string.IsNullOrEmpty(hasLanguage.PreferredMetadataLanguage))
                {
                    builder.Append("<language>" + SecurityElement.Escape(hasLanguage.PreferredMetadataLanguage) + "</language>");
                }
            }

            if (item.PremiereDate.HasValue && !(item is Episode))
            {
                var formatString = options.ReleaseDateFormat;

                if (item is MusicArtist)
                {
                    builder.Append("<formed>" + SecurityElement.Escape(item.PremiereDate.Value.ToString(formatString)) + "</formed>");
                }
                else
                {
                    builder.Append("<premiered>" + SecurityElement.Escape(item.PremiereDate.Value.ToString(formatString)) + "</premiered>");
                    builder.Append("<releasedate>" + SecurityElement.Escape(item.PremiereDate.Value.ToString(formatString)) + "</releasedate>");
                }
            }

            if (item.EndDate.HasValue)
            {
                if (!(item is Episode))
                {
                    var formatString = options.ReleaseDateFormat;

                    builder.Append("<enddate>" + SecurityElement.Escape(item.EndDate.Value.ToString(formatString)) + "</enddate>");
                }
            }

            var hasCriticRating = item as IHasCriticRating;

            if (hasCriticRating != null)
            {
                if (hasCriticRating.CriticRating.HasValue)
                {
                    builder.Append("<criticrating>" + SecurityElement.Escape(hasCriticRating.CriticRating.Value.ToString(UsCulture)) + "</criticrating>");
                }

                if (!string.IsNullOrEmpty(hasCriticRating.CriticRatingSummary))
                {
                    builder.Append("<criticratingsummary><![CDATA[" + hasCriticRating.CriticRatingSummary + "]]></criticratingsummary>");
                }
            }

            var hasDisplayOrder = item as IHasDisplayOrder;

            if (hasDisplayOrder != null)
            {
                if (!string.IsNullOrEmpty(hasDisplayOrder.DisplayOrder))
                {
                    builder.Append("<displayorder>" + SecurityElement.Escape(hasDisplayOrder.DisplayOrder) + "</displayorder>");
                }
            }

            if (item.VoteCount.HasValue)
            {
                builder.Append("<votes>" + SecurityElement.Escape(item.VoteCount.Value.ToString(UsCulture)) + "</votes>");
            }

            var hasBudget = item as IHasBudget;
            if (hasBudget != null)
            {
                if (hasBudget.Budget.HasValue)
                {
                    builder.Append("<budget>" + SecurityElement.Escape(hasBudget.Budget.Value.ToString(UsCulture)) + "</budget>");
                }

                if (hasBudget.Revenue.HasValue)
                {
                    builder.Append("<revenue>" + SecurityElement.Escape(hasBudget.Revenue.Value.ToString(UsCulture)) + "</revenue>");
                }
            }

            var hasMetascore = item as IHasMetascore;
            if (hasMetascore != null && hasMetascore.Metascore.HasValue)
            {
                builder.Append("<metascore>" + SecurityElement.Escape(hasMetascore.Metascore.Value.ToString(UsCulture)) + "</metascore>");
            }

            // Use original runtime here, actual file runtime later in MediaInfo
            var runTimeTicks = item.RunTimeTicks;

            if (runTimeTicks.HasValue)
            {
                var timespan = TimeSpan.FromTicks(runTimeTicks.Value);

                builder.Append("<runtime>" + Convert.ToInt32(timespan.TotalMinutes).ToString(UsCulture) + "</runtime>");
            }

            var hasTaglines = item as IHasTaglines;
            if (hasTaglines != null)
            {
                foreach (var tagline in hasTaglines.Taglines)
                {
                    builder.Append("<tagline>" + SecurityElement.Escape(tagline) + "</tagline>");
                }
            }

            var hasProductionLocations = item as IHasProductionLocations;
            if (hasProductionLocations != null)
            {
                foreach (var country in hasProductionLocations.ProductionLocations)
                {
                    builder.Append("<country>" + SecurityElement.Escape(country) + "</country>");
                }
            }

            foreach (var genre in item.Genres)
            {
                builder.Append("<genre>" + SecurityElement.Escape(genre) + "</genre>");
            }

            foreach (var studio in item.Studios)
            {
                builder.Append("<studio>" + SecurityElement.Escape(studio) + "</studio>");
            }

            var hasTags = item as IHasTags;
            if (hasTags != null)
            {
                foreach (var tag in hasTags.Tags)
                {
                    if (item is MusicAlbum || item is MusicArtist)
                    {
                        builder.Append("<style>" + SecurityElement.Escape(tag) + "</style>");
                    }
                    else
                    {
                        builder.Append("<tag>" + SecurityElement.Escape(tag) + "</tag>");
                    }
                }
            }

            var hasKeywords = item as IHasKeywords;
            if (hasKeywords != null)
            {
                foreach (var tag in hasKeywords.Keywords)
                {
                    builder.Append("<plotkeyword>" + SecurityElement.Escape(tag) + "</plotkeyword>");
                }
            }

            var hasAwards = item as IHasAwards;
            if (hasAwards != null && !string.IsNullOrEmpty(hasAwards.AwardSummary))
            {
                builder.Append("<awardsummary>" + SecurityElement.Escape(hasAwards.AwardSummary) + "</awardsummary>");
            }

            var externalId = item.GetProviderId(MetadataProviders.AudioDbArtist);

            if (!string.IsNullOrEmpty(externalId))
            {
                builder.Append("<audiodbartistid>" + SecurityElement.Escape(externalId) + "</audiodbartistid>");
            }

            externalId = item.GetProviderId(MetadataProviders.AudioDbAlbum);

            if (!string.IsNullOrEmpty(externalId))
            {
                builder.Append("<audiodbalbumid>" + SecurityElement.Escape(externalId) + "</audiodbalbumid>");
            }

            externalId = item.GetProviderId(MetadataProviders.Zap2It);

            if (!string.IsNullOrEmpty(externalId))
            {
                builder.Append("<zap2itid>" + SecurityElement.Escape(externalId) + "</zap2itid>");
            }

            externalId = item.GetProviderId(MetadataProviders.MusicBrainzAlbum);

            if (!string.IsNullOrEmpty(externalId))
            {
                builder.Append("<musicbrainzalbumid>" + SecurityElement.Escape(externalId) + "</musicbrainzalbumid>");
            }

            externalId = item.GetProviderId(MetadataProviders.MusicBrainzAlbumArtist);

            if (!string.IsNullOrEmpty(externalId))
            {
                builder.Append("<musicbrainzalbumartistid>" + SecurityElement.Escape(externalId) + "</musicbrainzalbumartistid>");
            }

            externalId = item.GetProviderId(MetadataProviders.MusicBrainzArtist);

            if (!string.IsNullOrEmpty(externalId))
            {
                builder.Append("<musicbrainzartistid>" + SecurityElement.Escape(externalId) + "</musicbrainzartistid>");
            }

            externalId = item.GetProviderId(MetadataProviders.MusicBrainzReleaseGroup);

            if (!string.IsNullOrEmpty(externalId))
            {
                builder.Append("<musicbrainzreleasegroupid>" + SecurityElement.Escape(externalId) + "</musicbrainzreleasegroupid>");
            }

            externalId = item.GetProviderId(MetadataProviders.Gamesdb);
            if (!string.IsNullOrEmpty(externalId))
            {
                builder.Append("<gamesdbid>" + SecurityElement.Escape(externalId) + "</gamesdbid>");
            }

            externalId = item.GetProviderId(MetadataProviders.TvRage);
            if (!string.IsNullOrEmpty(externalId))
            {
                builder.Append("<tvrageid>" + SecurityElement.Escape(externalId) + "</tvrageid>");
            }

            if (options.SaveImagePathsInNfo)
            {
                AddImages(item, builder, fileSystem, config);
            }

            AddUserData(item, builder, userManager, userDataRepo, options);

            AddActors(item, builder, libraryManager, fileSystem, config);

            var folder = item as BoxSet;
            if (folder != null)
            {
                AddCollectionItems(folder, builder);
            }
        }

        public static void AddChapters(Video item, StringBuilder builder, IItemRepository repository)
        {
            var chapters = repository.GetChapters(item.Id);

            foreach (var chapter in chapters)
            {
                builder.Append("<chapter>");
                builder.Append("<name>" + SecurityElement.Escape(chapter.Name) + "</name>");

                var time = TimeSpan.FromTicks(chapter.StartPositionTicks);
                var ms = Convert.ToInt64(time.TotalMilliseconds);

                builder.Append("<startpositionms>" + SecurityElement.Escape(ms.ToString(UsCulture)) + "</startpositionms>");
                builder.Append("</chapter>");
            }
        }

        public static void AddCollectionItems(Folder item, StringBuilder builder)
        {
            var items = item.LinkedChildren
                .Where(i => i.Type == LinkedChildType.Manual && !string.IsNullOrWhiteSpace(i.ItemName))
                .ToList();

            foreach (var link in items)
            {
                builder.Append("<collectionitem>");

                builder.Append("<name>" + SecurityElement.Escape(link.ItemName) + "</name>");
                builder.Append("<type>" + SecurityElement.Escape(link.ItemType) + "</type>");

                if (link.ItemYear.HasValue)
                {
                    builder.Append("<year>" + SecurityElement.Escape(link.ItemYear.Value.ToString(UsCulture)) + "</year>");
                }

                builder.Append("</collectionitem>");
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

            return url.Replace("http://www.youtube.com/watch?v=",
                "plugin://plugin.video.youtube/?action=play_video&videoid=",
                StringComparison.OrdinalIgnoreCase);
        }

        private static void AddImages(BaseItem item, StringBuilder builder, IFileSystem fileSystem, IServerConfigurationManager config)
        {
            builder.Append("<art>");

            var poster = item.PrimaryImagePath;

            if (!string.IsNullOrEmpty(poster))
            {
                builder.Append("<poster>" + SecurityElement.Escape(GetPathToSave(item.PrimaryImagePath, fileSystem, config)) + "</poster>");
            }

            foreach (var backdrop in item.GetImages(ImageType.Backdrop))
            {
                builder.Append("<fanart>" + SecurityElement.Escape(GetPathToSave(backdrop.Path, fileSystem, config)) + "</fanart>");
            }

            builder.Append("</art>");
        }

        private static void AddUserData(BaseItem item, StringBuilder builder, IUserManager userManager, IUserDataManager userDataRepo, XbmcMetadataOptions options)
        {
            var userId = options.UserId;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return;
            }

            var user = userManager.GetUserById(new Guid(userId));

            if (user == null)
            {
                return;
            }

            if (item.IsFolder)
            {
                return;
            }

            var userdata = userDataRepo.GetUserData(user.Id, item.GetUserDataKey());

            builder.Append("<playcount>" + userdata.PlayCount.ToString(UsCulture) + "</playcount>");
            builder.Append("<watched>" + userdata.Played.ToString().ToLower() + "</watched>");

            if (userdata.LastPlayedDate.HasValue)
            {
                builder.Append("<lastplayed>" + SecurityElement.Escape(userdata.LastPlayedDate.Value.ToString("yyyy-MM-dd HH:mm:ss")) + "</lastplayed>");
            }

            builder.Append("<resume>");

            var runTimeTicks = item.RunTimeTicks ?? 0;

            builder.Append("<position>" + TimeSpan.FromTicks(userdata.PlaybackPositionTicks).TotalSeconds.ToString(UsCulture) + "</position>");
            builder.Append("<total>" + TimeSpan.FromTicks(runTimeTicks).TotalSeconds.ToString(UsCulture) + "</total>");

            builder.Append("</resume>");
        }

        public static void AddActors(BaseItem item, StringBuilder builder, ILibraryManager libraryManager, IFileSystem fileSystem, IServerConfigurationManager config)
        {
            var actors = item.People
                .Where(i => !IsPersonType(i, PersonType.Director) && !IsPersonType(i, PersonType.Writer))
                .ToList();

            foreach (var person in actors)
            {
                builder.Append("<actor>");
                builder.Append("<name>" + SecurityElement.Escape(person.Name ?? string.Empty) + "</name>");
                builder.Append("<role>" + SecurityElement.Escape(person.Role ?? string.Empty) + "</role>");
                builder.Append("<type>" + SecurityElement.Escape(person.Type ?? string.Empty) + "</type>");

                try
                {
                    var personEntity = libraryManager.GetPerson(person.Name);

                    if (!string.IsNullOrEmpty(personEntity.PrimaryImagePath))
                    {
                        builder.Append("<thumb>" + SecurityElement.Escape(GetPathToSave(personEntity.PrimaryImagePath, fileSystem, config)) + "</thumb>");
                    }
                }
                catch (Exception)
                {
                    // Already logged in core
                }

                builder.Append("</actor>");
            }
        }

        private static bool IsPersonType(PersonInfo person, string type)
        {
            return string.Equals(person.Type, type, StringComparison.OrdinalIgnoreCase) || string.Equals(person.Role, type, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetPathToSave(string path, IFileSystem fileSystem, IServerConfigurationManager config)
        {
            foreach (var map in config.Configuration.PathSubstitutions)
            {
                path = fileSystem.SubstitutePath(path, map.From, map.To);
            }

            return path;
        }

        public static string ReplaceString(string str, string oldValue, string newValue, StringComparison comparison)
        {
            var sb = new StringBuilder();

            int previousIndex = 0;
            int index = str.IndexOf(oldValue, comparison);
            while (index != -1)
            {
                sb.Append(str.Substring(previousIndex, index - previousIndex));
                sb.Append(newValue);
                index += oldValue.Length;

                previousIndex = index;
                index = str.IndexOf(oldValue, index, comparison);
            }
            sb.Append(str.Substring(previousIndex));

            return sb.ToString();
        }
    }
}
