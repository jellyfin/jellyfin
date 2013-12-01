using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Xml;

namespace MediaBrowser.Providers.Savers
{
    /// <summary>
    /// Class XmlHelpers
    /// </summary>
    public static class XmlSaverHelpers
    {
        private static readonly Dictionary<string, string> CommonTags = new[] {     
               
                    "Added",
                    "AspectRatio",
                    "BirthDate",
                    "Budget",
                    "certification",
                    "Chapters",
                    "ContentRating",
                    "CustomRating",
                    "CriticRating",
                    "CriticRatingSummary",
                    "DeathDate",
                    "EndDate",
                    "Genres",
                    "Genre",
                    "GamesDbId",
                    "IMDB_ID",
                    "IMDB",
                    "IMDbId",
                    "Language",
                    "LocalTitle",
                    "LockData",
                    "LockedFields",
                    "MediaInfo",
                    "MPAARating",
                    "MusicbrainzId",
                    "MusicBrainzReleaseGroupId",
                    "Overview",
                    "Persons",
                    "PremiereDate",
                    "ProductionYear",
                    "Rating",
                    "Revenue",
                    "RottenTomatoesId",
                    "RunningTime",
                    "Runtime",
                    "SortTitle",
                    "Studios",
                    "Tags",
                    "TagLine",
                    "Taglines",
                    "TMDbCollectionId",
                    "TMDbId",
                    "Trailer",
                    "Trailers",
                    "TVcomId",
                    "TvDbId",
                    "Type",
                    "VoteCount",
                    "Website",
                    "Zap2ItId"

        }.ToDictionary(i => i, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The us culture
        /// </summary>
        private static readonly CultureInfo UsCulture = new CultureInfo("en-US");

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
                var position = xml.ToString().LastIndexOf("</", StringComparison.OrdinalIgnoreCase);
                xml.Insert(position, GetCustomTags(path, xmlTagsUsed));
            }

            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xml.ToString());

            //Add the new node to the document.
            xmlDocument.InsertBefore(xmlDocument.CreateXmlDeclaration("1.0", "UTF-8", "yes"), xmlDocument.DocumentElement);

            var parentPath = Path.GetDirectoryName(path);

            Directory.CreateDirectory(parentPath);

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

        /// <summary>
        /// Adds the common nodes.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="builder">The builder.</param>
        public static void AddCommonNodes(BaseItem item, StringBuilder builder)
        {
            if (!string.IsNullOrEmpty(item.OfficialRating))
            {
                builder.Append("<ContentRating>" + SecurityElement.Escape(item.OfficialRating) + "</ContentRating>");
                builder.Append("<MPAARating>" + SecurityElement.Escape(item.OfficialRating) + "</MPAARating>");
                builder.Append("<certification>" + SecurityElement.Escape(item.OfficialRating) + "</certification>");
            }

            builder.Append("<Added>" + SecurityElement.Escape(item.DateCreated.ToLocalTime().ToString("G")) + "</Added>");

            builder.Append("<LockData>" + item.DontFetchMeta.ToString().ToLower() + "</LockData>");

            if (item.LockedFields.Count > 0)
            {
                builder.Append("<LockedFields>" + string.Join("|", item.LockedFields.Select(i => i.ToString()).ToArray()) + "</LockedFields>");
            }

            if (!string.IsNullOrEmpty(item.DisplayMediaType))
            {
                builder.Append("<Type>" + SecurityElement.Escape(item.DisplayMediaType) + "</Type>");
            }

            var hasCriticRating = item as IHasCriticRating;
            if (hasCriticRating != null)
            {
                if (hasCriticRating.CriticRating.HasValue)
                {
                    builder.Append("<CriticRating>" + SecurityElement.Escape(hasCriticRating.CriticRating.Value.ToString(UsCulture)) + "</CriticRating>");
                }

                if (!string.IsNullOrEmpty(hasCriticRating.CriticRatingSummary))
                {
                    builder.Append("<CriticRatingSummary><![CDATA[" + hasCriticRating.CriticRatingSummary + "]]></CriticRatingSummary>");
                }
            }

            if (!string.IsNullOrEmpty(item.Overview))
            {
                builder.Append("<Overview><![CDATA[" + item.Overview + "]]></Overview>");
            }

            if (!string.IsNullOrEmpty(item.CustomRating))
            {
                builder.Append("<CustomRating>" + SecurityElement.Escape(item.CustomRating) + "</CustomRating>");
            }

            if (!string.IsNullOrEmpty(item.Name) && !(item is Episode))
            {
                builder.Append("<LocalTitle>" + SecurityElement.Escape(item.Name) + "</LocalTitle>");
            }

            if (!string.IsNullOrEmpty(item.ForcedSortName))
            {
                builder.Append("<SortTitle>" + SecurityElement.Escape(item.ForcedSortName) + "</SortTitle>");
            }

            if (item.PremiereDate.HasValue)
            {
                if (item is Person)
                {
                    builder.Append("<BirthDate>" + SecurityElement.Escape(item.PremiereDate.Value.ToString("yyyy-MM-dd")) + "</BirthDate>");
                }
                else if (!(item is Episode))
                {
                    builder.Append("<PremiereDate>" + SecurityElement.Escape(item.PremiereDate.Value.ToString("yyyy-MM-dd")) + "</PremiereDate>");
                }
            }

            if (item.EndDate.HasValue)
            {
                if (item is Person)
                {
                    builder.Append("<DeathDate>" + SecurityElement.Escape(item.EndDate.Value.ToString("yyyy-MM-dd")) + "</DeathDate>");
                }
                else if (!(item is Episode))
                {
                    builder.Append("<EndDate>" + SecurityElement.Escape(item.EndDate.Value.ToString("yyyy-MM-dd")) + "</EndDate>");
                }
            }

            if (item.RemoteTrailers.Count > 0)
            {
                builder.Append("<Trailers>");

                foreach (var trailer in item.RemoteTrailers)
                {
                    builder.Append("<Trailer>" + SecurityElement.Escape(trailer.Url) + "</Trailer>");
                }

                builder.Append("</Trailers>");
            }

            if (item.Budget.HasValue)
            {
                builder.Append("<Budget>" + SecurityElement.Escape(item.Budget.Value.ToString(UsCulture)) + "</Budget>");
            }

            if (item.Revenue.HasValue)
            {
                builder.Append("<Revenue>" + SecurityElement.Escape(item.Revenue.Value.ToString(UsCulture)) + "</Revenue>");
            }

            if (item.CommunityRating.HasValue)
            {
                builder.Append("<Rating>" + SecurityElement.Escape(item.CommunityRating.Value.ToString(UsCulture)) + "</Rating>");
            }
            if (item.VoteCount.HasValue)
            {
                builder.Append("<VoteCount>" + SecurityElement.Escape(item.VoteCount.Value.ToString(UsCulture)) + "</VoteCount>");
            }

            if (item.ProductionYear.HasValue && !(item is Person))
            {
                builder.Append("<ProductionYear>" + SecurityElement.Escape(item.ProductionYear.Value.ToString(UsCulture)) + "</ProductionYear>");
            }

            if (!string.IsNullOrEmpty(item.HomePageUrl))
            {
                builder.Append("<Website>" + SecurityElement.Escape(item.HomePageUrl) + "</Website>");
            }

            var hasAspectRatio = item as IHasAspectRatio;
            if (hasAspectRatio != null)
            {
                if (!string.IsNullOrEmpty(hasAspectRatio.AspectRatio))
                {
                    builder.Append("<AspectRatio>" + SecurityElement.Escape(hasAspectRatio.AspectRatio) + "</AspectRatio>");
                }
            }

            if (!string.IsNullOrEmpty(item.Language))
            {
                builder.Append("<Language>" + SecurityElement.Escape(item.Language) + "</Language>");
            }

            // Use original runtime here, actual file runtime later in MediaInfo
            var runTimeTicks = item.OriginalRunTimeTicks ?? item.RunTimeTicks;

            if (runTimeTicks.HasValue)
            {
                var timespan = TimeSpan.FromTicks(runTimeTicks.Value);

                builder.Append("<RunningTime>" + Convert.ToInt32(timespan.TotalMinutes).ToString(UsCulture) + "</RunningTime>");
                builder.Append("<Runtime>" + Convert.ToInt32(timespan.TotalMinutes).ToString(UsCulture) + "</Runtime>");
            }

            var imdb = item.GetProviderId(MetadataProviders.Imdb);

            if (!string.IsNullOrEmpty(imdb))
            {
                builder.Append("<IMDB_ID>" + SecurityElement.Escape(imdb) + "</IMDB_ID>");
                builder.Append("<IMDB>" + SecurityElement.Escape(imdb) + "</IMDB>");
                builder.Append("<IMDbId>" + SecurityElement.Escape(imdb) + "</IMDbId>");
            }

            var tmdb = item.GetProviderId(MetadataProviders.Tmdb);

            if (!string.IsNullOrEmpty(tmdb))
            {
                builder.Append("<TMDbId>" + SecurityElement.Escape(tmdb) + "</TMDbId>");
            }

            if (!(item is Series))
            {
                var tvdb = item.GetProviderId(MetadataProviders.Tvdb);

                if (!string.IsNullOrEmpty(tvdb))
                {
                    builder.Append("<TvDbId>" + SecurityElement.Escape(tvdb) + "</TvDbId>");
                }
            }

            var tvcom = item.GetProviderId(MetadataProviders.Tvcom);

            if (!string.IsNullOrEmpty(tvcom))
            {
                builder.Append("<TVcomId>" + SecurityElement.Escape(tvcom) + "</TVcomId>");
            }

            var rt = item.GetProviderId(MetadataProviders.RottenTomatoes);

            if (!string.IsNullOrEmpty(rt))
            {
                builder.Append("<RottenTomatoesId>" + SecurityElement.Escape(rt) + "</RottenTomatoesId>");
            }

            var zap2It = item.GetProviderId(MetadataProviders.Zap2It);

            if (!string.IsNullOrEmpty(zap2It))
            {
                builder.Append("<Zap2ItId>" + SecurityElement.Escape(zap2It) + "</Zap2ItId>");
            }

            var mbz = item.GetProviderId(MetadataProviders.Musicbrainz);

            if (!string.IsNullOrEmpty(mbz))
            {
                builder.Append("<MusicbrainzId>" + SecurityElement.Escape(mbz) + "</MusicbrainzId>");
            }

            mbz = item.GetProviderId(MetadataProviders.MusicBrainzReleaseGroup);

            if (!string.IsNullOrEmpty(mbz))
            {
                builder.Append("<MusicBrainzReleaseGroupId>" + SecurityElement.Escape(mbz) + "</MusicBrainzReleaseGroupId>");
            }

            var gamesdb = item.GetProviderId(MetadataProviders.Gamesdb);

            if (!string.IsNullOrEmpty(gamesdb))
            {
                builder.Append("<GamesDbId>" + SecurityElement.Escape(gamesdb) + "</GamesDbId>");
            }

            var tmdbCollection = item.GetProviderId(MetadataProviders.TmdbCollection);

            if (!string.IsNullOrEmpty(tmdbCollection))
            {
                builder.Append("<TMDbCollectionId>" + SecurityElement.Escape(tmdbCollection) + "</TMDbCollectionId>");
            }

            if (item.Taglines.Count > 0)
            {
                builder.Append("<TagLine>" + SecurityElement.Escape(item.Taglines[0]) + "</TagLine>");

                builder.Append("<Taglines>");

                foreach (var tagline in item.Taglines)
                {
                    builder.Append("<Tagline>" + SecurityElement.Escape(tagline) + "</Tagline>");
                }

                builder.Append("</Taglines>");
            }

            if (item.Genres.Count > 0)
            {
                builder.Append("<Genres>");

                foreach (var genre in item.Genres)
                {
                    builder.Append("<Genre>" + SecurityElement.Escape(genre) + "</Genre>");
                }

                builder.Append("</Genres>");

                builder.Append("<Genre>" + SecurityElement.Escape(string.Join("|", item.Genres.ToArray())) + "</Genre>");
            }

            if (item.Studios.Count > 0)
            {
                builder.Append("<Studios>");

                foreach (var studio in item.Studios)
                {
                    builder.Append("<Studio>" + SecurityElement.Escape(studio) + "</Studio>");
                }

                builder.Append("</Studios>");
            }

            if (item.Tags.Count > 0)
            {
                builder.Append("<Tags>");

                foreach (var tag in item.Tags)
                {
                    builder.Append("<Tag>" + SecurityElement.Escape(tag) + "</Tag>");
                }

                builder.Append("</Tags>");
            }

            if (item.People.Count > 0)
            {
                builder.Append("<Persons>");

                foreach (var person in item.People)
                {
                    builder.Append("<Person>");
                    builder.Append("<Name>" + SecurityElement.Escape(person.Name) + "</Name>");
                    builder.Append("<Type>" + SecurityElement.Escape(person.Type) + "</Type>");
                    builder.Append("<Role>" + SecurityElement.Escape(person.Role) + "</Role>");

                    if (person.SortOrder.HasValue)
                    {
                        builder.Append("<SortOrder>" + SecurityElement.Escape(person.SortOrder.Value.ToString(UsCulture)) + "</SortOrder>");
                    }

                    builder.Append("</Person>");
                }

                builder.Append("</Persons>");
            }
        }

        public static void AddChapters(Video item, StringBuilder builder, IItemRepository repository)
        {
            var chapters = repository.GetChapters(item.Id);

            builder.Append("<Chapters>");

            foreach (var chapter in chapters)
            {
                builder.Append("<Chapter>");
                builder.Append("<Name>" + SecurityElement.Escape(chapter.Name) + "</Name>");

                var time = TimeSpan.FromTicks(chapter.StartPositionTicks);
                var ms = Convert.ToInt64(time.TotalMilliseconds);

                builder.Append("<StartPositionMs>" + SecurityElement.Escape(ms.ToString(UsCulture)) + "</StartPositionMs>");
                builder.Append("</Chapter>");
            }

            builder.Append("</Chapters>");
        }

        /// <summary>
        /// Appends the media info.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item">The item.</param>
        /// <param name="builder">The builder.</param>
        public static void AddMediaInfo<T>(T item, StringBuilder builder, IItemRepository itemRepository)
            where T : BaseItem, IHasMediaStreams
        {
            var video = item as Video;

            builder.Append("<MediaInfo>");

            foreach (var stream in item.MediaStreams)
            {
                builder.Append("<" + stream.Type + ">");

                if (!string.IsNullOrEmpty(stream.Codec))
                {
                    builder.Append("<Codec>" + SecurityElement.Escape(stream.Codec) + "</Codec>");
                    builder.Append("<FFCodec>" + SecurityElement.Escape(stream.Codec) + "</FFCodec>");
                }

                if (stream.BitRate.HasValue)
                {
                    builder.Append("<BitRate>" + stream.BitRate.Value.ToString(UsCulture) + "</BitRate>");
                }

                if (stream.Width.HasValue)
                {
                    builder.Append("<Width>" + stream.Width.Value.ToString(UsCulture) + "</Width>");
                }

                if (stream.Height.HasValue)
                {
                    builder.Append("<Height>" + stream.Height.Value.ToString(UsCulture) + "</Height>");
                }

                if (!string.IsNullOrEmpty(stream.AspectRatio))
                {
                    builder.Append("<AspectRatio>" + SecurityElement.Escape(stream.AspectRatio) + "</AspectRatio>");
                }

                var framerate = stream.AverageFrameRate ?? stream.RealFrameRate;

                if (framerate.HasValue)
                {
                    builder.Append("<FrameRate>" + framerate.Value.ToString(UsCulture) + "</FrameRate>");
                }

                if (!string.IsNullOrEmpty(stream.Language))
                {
                    builder.Append("<Language>" + SecurityElement.Escape(stream.Language) + "</Language>");
                }

                if (!string.IsNullOrEmpty(stream.ScanType))
                {
                    builder.Append("<ScanType>" + SecurityElement.Escape(stream.ScanType) + "</ScanType>");
                }

                if (stream.Channels.HasValue)
                {
                    builder.Append("<Channels>" + stream.Channels.Value.ToString(UsCulture) + "</Channels>");
                }

                if (stream.SampleRate.HasValue)
                {
                    builder.Append("<SamplingRate>" + stream.SampleRate.Value.ToString(UsCulture) + "</SamplingRate>");
                }

                builder.Append("<Default>" + SecurityElement.Escape(stream.IsDefault.ToString()) + "</Default>");
                builder.Append("<Forced>" + SecurityElement.Escape(stream.IsForced.ToString()) + "</Forced>");

                if (stream.Type == MediaStreamType.Video)
                {
                    if (item.RunTimeTicks.HasValue)
                    {
                        var timespan = TimeSpan.FromTicks(item.RunTimeTicks.Value);

                        builder.Append("<Duration>" + Convert.ToInt64(timespan.TotalMinutes).ToString(UsCulture) + "</Duration>");
                        builder.Append("<DurationSeconds>" + Convert.ToInt64(timespan.TotalSeconds).ToString(UsCulture) + "</DurationSeconds>");
                    }

                    if (video != null && video.Video3DFormat.HasValue)
                    {
                        switch (video.Video3DFormat.Value)
                        {
                            case Video3DFormat.FullSideBySide:
                                builder.Append("<Format3D>FSBS</Format3D>");
                                break;
                            case Video3DFormat.FullTopAndBottom:
                                builder.Append("<Format3D>FTAB</Format3D>");
                                break;
                            case Video3DFormat.HalfSideBySide:
                                builder.Append("<Format3D>HSBS</Format3D>");
                                break;
                            case Video3DFormat.HalfTopAndBottom:
                                builder.Append("<Format3D>HTAB</Format3D>");
                                break;
                        }
                    }
                }

                builder.Append("</" + stream.Type + ">");
            }

            builder.Append("</MediaInfo>");

            if (video != null)
            {
                //AddChapters(video, builder, itemRepository);
            }
        }
    }
}
