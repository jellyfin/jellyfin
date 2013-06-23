using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;
using System;
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
    public static class XmlHelpers
    {
        /// <summary>
        /// The us culture
        /// </summary>
        private static readonly CultureInfo UsCulture = new CultureInfo("en-US");

        /// <summary>
        /// Saves the specified XML.
        /// </summary>
        /// <param name="xml">The XML.</param>
        /// <param name="path">The path.</param>
        public static void Save(StringBuilder xml, string path)
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xml.ToString());

            //Add the new node to the document.
            xmlDocument.InsertBefore(xmlDocument.CreateXmlDeclaration("1.0", "UTF-8", "yes"), xmlDocument.DocumentElement);

            using (var streamWriter = new StreamWriter(path, false, Encoding.UTF8))
            {
                xmlDocument.Save(streamWriter);
            }
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

            if (item.People.Count > 0)
            {
                builder.Append("<Persons>");

                foreach (var person in item.People)
                {
                    builder.Append("<Person>");
                    builder.Append("<Name>" + SecurityElement.Escape(person.Name) + "</Name>");
                    builder.Append("<Type>" + SecurityElement.Escape(person.Type) + "</Type>");
                    builder.Append("<Role>" + SecurityElement.Escape(person.Role) + "</Role>");
                    builder.Append("</Person>");
                }

                builder.Append("</Persons>");
            }

            if (!string.IsNullOrEmpty(item.DisplayMediaType))
            {
                builder.Append("<Type>" + SecurityElement.Escape(item.DisplayMediaType) + "</Type>");
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

            if (item.ProductionYear.HasValue)
            {
                builder.Append("<ProductionYear>" + SecurityElement.Escape(item.ProductionYear.Value.ToString(UsCulture)) + "</ProductionYear>");
            }
            
            if (!string.IsNullOrEmpty(item.HomePageUrl))
            {
                builder.Append("<Website>" + SecurityElement.Escape(item.HomePageUrl) + "</Website>");
            }

            if (!string.IsNullOrEmpty(item.AspectRatio))
            {
                builder.Append("<AspectRatio>" + SecurityElement.Escape(item.AspectRatio) + "</AspectRatio>");
            }

            if (!string.IsNullOrEmpty(item.Language))
            {
                builder.Append("<Language>" + SecurityElement.Escape(item.Language) + "</Language>");
            }

            if (item.RunTimeTicks.HasValue)
            {
                var timespan = TimeSpan.FromTicks(item.RunTimeTicks.Value);

                builder.Append("<RunningTime>" + Convert.ToInt32(timespan.TotalMinutes).ToString(UsCulture) + "</RunningTime>");
                builder.Append("<Runtime>" + Convert.ToInt32(timespan.TotalMinutes).ToString(UsCulture) + "</Runtime>");
            }
            
            if (item.Taglines.Count > 0)
            {
                builder.Append("<TagLine>" + SecurityElement.Escape(item.Taglines[0]) + "</TagLine>");

                builder.Append("<TagLines>");

                foreach (var tagline in item.Taglines)
                {
                    builder.Append("<Tagline>" + SecurityElement.Escape(tagline) + "</Tagline>");
                }

                builder.Append("</TagLines>");
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

            var tmdbCollection = item.GetProviderId(MetadataProviders.TmdbCollection);

            if (!string.IsNullOrEmpty(tmdbCollection))
            {
                builder.Append("<CollectionNumber>" + SecurityElement.Escape(tmdbCollection) + "</CollectionNumber>");
            }

            if (item.Genres.Count > 0)
            {
                builder.Append("<Genres>");

                foreach (var genre in item.Genres)
                {
                    builder.Append("<Genre>" + SecurityElement.Escape(genre) + "</Genre>");
                }

                builder.Append("</Genres>");
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

            builder.Append("<Added>" + SecurityElement.Escape(item.DateCreated.ToString(UsCulture)) + "</Added>");
        }

        /// <summary>
        /// Appends the media info.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item">The item.</param>
        /// <param name="builder">The builder.</param>
        public static void AppendMediaInfo<T>(T item, StringBuilder builder)
            where T : BaseItem, IHasMediaStreams
        {
            builder.Append("<MediaInfo>");

            foreach (var stream in item.MediaStreams)
            {
                if (stream.Type == MediaStreamType.Video)
                {
                    builder.Append("<Video>");

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

                    if (item.RunTimeTicks.HasValue)
                    {
                        var timespan = TimeSpan.FromTicks(item.RunTimeTicks.Value);

                        builder.Append("<Duration>" + Convert.ToInt32(timespan.TotalMinutes).ToString(UsCulture) + "</Duration>");
                        builder.Append("<DurationSeconds>" + Convert.ToInt32(timespan.TotalSeconds).ToString(UsCulture) + "</DurationSeconds>");
                    }

                    builder.Append("<Default>" + SecurityElement.Escape(stream.IsDefault.ToString()) + "</Default>");
                    builder.Append("<Forced>" + SecurityElement.Escape(stream.IsForced.ToString()) + "</Forced>");

                    builder.Append("</Video>");
                }
                else if (stream.Type == MediaStreamType.Audio)
                {
                    builder.Append("<Audio>");

                    if (!string.IsNullOrEmpty(stream.Codec))
                    {
                        builder.Append("<Codec>" + SecurityElement.Escape(stream.Codec) + "</Codec>");
                        builder.Append("<FFCodec>" + SecurityElement.Escape(stream.Codec) + "</FFCodec>");
                    }

                    if (stream.Channels.HasValue)
                    {
                        builder.Append("<Channels>" + stream.Channels.Value.ToString(UsCulture) + "</Channels>");
                    }

                    if (stream.BitRate.HasValue)
                    {
                        builder.Append("<BitRate>" + stream.BitRate.Value.ToString(UsCulture) + "</BitRate>");
                    }

                    if (stream.SampleRate.HasValue)
                    {
                        builder.Append("<SamplingRate>" + stream.SampleRate.Value.ToString(UsCulture) + "</SamplingRate>");
                    }

                    if (!string.IsNullOrEmpty(stream.Language))
                    {
                        builder.Append("<Language>" + SecurityElement.Escape(stream.Language) + "</Language>");
                    }

                    builder.Append("<Default>" + SecurityElement.Escape(stream.IsDefault.ToString()) + "</Default>");
                    builder.Append("<Forced>" + SecurityElement.Escape(stream.IsForced.ToString()) + "</Forced>");

                    builder.Append("</Audio>");
                }
                else if (stream.Type == MediaStreamType.Subtitle)
                {
                    builder.Append("<Subtitle>");

                    if (!string.IsNullOrEmpty(stream.Language))
                    {
                        builder.Append("<Language>" + SecurityElement.Escape(stream.Language) + "</Language>");
                    }

                    builder.Append("<Default>" + SecurityElement.Escape(stream.IsDefault.ToString()) + "</Default>");
                    builder.Append("<Forced>" + SecurityElement.Escape(stream.IsForced.ToString()) + "</Forced>");

                    builder.Append("</Subtitle>");
                }
            }

            builder.Append("</MediaInfo>");
        }
    }
}
