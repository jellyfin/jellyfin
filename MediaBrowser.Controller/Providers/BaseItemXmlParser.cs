using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Xml;
using MediaBrowser.Model.Entities;
using MediaBrowser.Common.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace MediaBrowser.Controller.Providers
{
    /// <summary>
    /// Provides a base class for parsing metadata xml
    /// </summary>
    public class BaseItemXmlParser<T>
        where T : BaseItem, new()
    {
        /// <summary>
        /// Fetches metadata for an item from one xml file
        /// </summary>
        public void Fetch(T item, string metadataFile)
        {
            // Use XmlReader for best performance
            using (XmlReader reader = XmlReader.Create(metadataFile))
            {
                reader.MoveToContent();

                // Loop through each element
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        FetchDataFromXmlNode(reader, item);
                    }
                }
            }
        }

        /// <summary>
        /// Fetches metadata from one Xml Element
        /// </summary>
        protected virtual void FetchDataFromXmlNode(XmlReader reader, T item)
        {
            switch (reader.Name)
            {
                // DateCreated
                case "Added":
                    DateTime added;
                    if (DateTime.TryParse(reader.ReadElementContentAsString() ?? string.Empty, out added))
                    {
                        item.DateCreated = added.ToUniversalTime();
                    }
                    break;

                // DisplayMediaType
                case "Type":
                    {
                        item.DisplayMediaType = reader.ReadElementContentAsString();

                        switch (item.DisplayMediaType.ToLower())
                        {
                            case "blu-ray":
                                item.DisplayMediaType = VideoType.BluRay.ToString();
                                break;
                            case "dvd":
                                item.DisplayMediaType = VideoType.Dvd.ToString();
                                break;
                            case "":
                                item.DisplayMediaType = null;
                                break;
                        }

                        break;
                    }

                // TODO: Do we still need this?
                case "banner":
                    item.BannerImagePath = reader.ReadElementContentAsString();
                    break;

                case "LocalTitle":
                    item.Name = reader.ReadElementContentAsString();
                    break;

                case "SortTitle":
                    item.SortName = reader.ReadElementContentAsString();
                    break;

                case "Overview":
                case "Description":
                    item.Overview = reader.ReadElementContentAsString();
                    break;

                case "TagLine":
                    {
                        var list = item.Taglines ?? new List<string>();
                        var tagline = reader.ReadElementContentAsString();

                        if (!list.Contains(tagline))
                        {
                            list.Add(tagline);
                        }

                        item.Taglines = list;
                        break;
                    }

                case "TagLines":
                    {
                        FetchFromTaglinesNode(reader.ReadSubtree(), item);
                        break;
                    }

                case "ContentRating":
                case "MPAARating":
                    item.OfficialRating = reader.ReadElementContentAsString();
                    break;

                case "CustomRating":
                    item.CustomRating = reader.ReadElementContentAsString();
                    break;

                case "CustomPin":
                    item.CustomPin = reader.ReadElementContentAsString();
                    break;

                case "Runtime":
                case "RunningTime":
                    {
                        string text = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            int runtime;
                            if (int.TryParse(text.Split(' ')[0], out runtime))
                            {
                                item.RunTimeTicks = TimeSpan.FromMinutes(runtime).Ticks;
                            }
                        }
                        break;
                    }

                case "Genre":
                    {
                        var list = item.Genres ?? new List<string>();
                        list.AddRange(GetSplitValues(reader.ReadElementContentAsString(), '|'));

                        item.Genres = list;
                        break;
                    }

                case "AspectRatio":
                    item.AspectRatio = reader.ReadElementContentAsString();
                    break;

                case "Network":
                    {
                        var list = item.Studios ?? new List<string>();
                        list.AddRange(GetSplitValues(reader.ReadElementContentAsString(), '|'));

                        item.Studios = list;
                        break;
                    }

                case "Director":
                    {
                        foreach (PersonInfo p in GetSplitValues(reader.ReadElementContentAsString(), '|').Select(v => new PersonInfo { Name = v, Type = "Director" }))
                        {
                            item.AddPerson(p);
                        }
                        break;
                    }
                case "Writer":
                    {
                        foreach (PersonInfo p in GetSplitValues(reader.ReadElementContentAsString(), '|').Select(v => new PersonInfo { Name = v, Type = "Writer" }))
                        {
                            item.AddPerson(p);
                        }
                        break;
                    }

                case "Actors":
                case "GuestStars":
                    {
                        foreach (PersonInfo p in GetSplitValues(reader.ReadElementContentAsString(), '|').Select(v => new PersonInfo { Name = v, Type = "Actor" }))
                        {
                            item.AddPerson(p);
                        }
                        break;
                    }

                case "Trailer":
                    item.TrailerUrl = reader.ReadElementContentAsString();
                    break;

                case "ProductionYear":
                    {
                        string val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            int ProductionYear;
                            if (int.TryParse(val, out ProductionYear) && ProductionYear > 1850)
                            {
                                item.ProductionYear = ProductionYear;
                            }
                        }

                        break;
                    }

                case "Rating":
                case "IMDBrating":

                    string rating = reader.ReadElementContentAsString();

                    if (!string.IsNullOrWhiteSpace(rating))
                    {
                        float val;

                        if (float.TryParse(rating, out val))
                        {
                            item.CommunityRating = val;
                        }
                    }
                    break;

                case "FirstAired":
                    {
                        string firstAired = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(firstAired))
                        {
                            DateTime airDate;

                            if (DateTime.TryParse(firstAired, out airDate) && airDate.Year > 1850)
                            {
                                item.PremiereDate = airDate.ToUniversalTime();
                                item.ProductionYear = airDate.Year;
                            }
                        }

                        break;
                    }

                case "TMDbId":
                    string tmdb = reader.ReadElementContentAsString();
                    if (!string.IsNullOrWhiteSpace(tmdb))
                    {
                        item.SetProviderId(MetadataProviders.Tmdb, tmdb);
                    }
                    break;

                case "TVcomId":
                    string TVcomId = reader.ReadElementContentAsString();
                    if (!string.IsNullOrWhiteSpace(TVcomId))
                    {
                        item.SetProviderId(MetadataProviders.Tvcom, TVcomId);
                    }
                    break;

                case "IMDB_ID":
                case "IMDB":
                case "IMDbId":
                    string IMDbId = reader.ReadElementContentAsString();
                    if (!string.IsNullOrWhiteSpace(IMDbId))
                    {
                        item.SetProviderId(MetadataProviders.Imdb, IMDbId);
                    }
                    break;

                case "Genres":
                    FetchFromGenresNode(reader.ReadSubtree(), item);
                    break;

                case "Persons":
                    FetchDataFromPersonsNode(reader.ReadSubtree(), item);
                    break;

                case "ParentalRating":
                    FetchFromParentalRatingNode(reader.ReadSubtree(), item);
                    break;

                case "Studios":
                    FetchFromStudiosNode(reader.ReadSubtree(), item);
                    break;

                case "MediaInfo":
                    {
                        var video = item as Video;

                        if (video != null)
                        {
                            FetchMediaInfo(reader.ReadSubtree(), video);
                        }
                        break;
                    }

                default:
                    reader.Skip();
                    break;
            }
        }

        private void FetchMediaInfo(XmlReader reader, Video item)
        {
            reader.MoveToContent();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Audio":
                            {
                                AudioStream stream = FetchMediaInfoAudio(reader.ReadSubtree());

                                List<AudioStream> streams = item.AudioStreams ?? new List<AudioStream>();
                                streams.Add(stream);
                                item.AudioStreams = streams;

                                break;
                            }

                        case "Video":
                            FetchMediaInfoVideo(reader.ReadSubtree(), item);
                            break;

                        case "Subtitle":
                            {
                                SubtitleStream stream = FetchMediaInfoSubtitles(reader.ReadSubtree());

                                List<SubtitleStream> streams = item.Subtitles ?? new List<SubtitleStream>();
                                streams.Add(stream);
                                item.Subtitles = streams;

                                break;
                            }

                        default:
                            reader.Skip();
                            break;
                    }
                }
            }
        }

        private AudioStream FetchMediaInfoAudio(XmlReader reader)
        {
            var stream = new AudioStream();

            reader.MoveToContent();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Default":
                            stream.IsDefault = reader.ReadElementContentAsString() == "True";
                            break;

                        case "SamplingRate":
                            stream.SampleRate = reader.ReadIntSafe();
                            break;

                        case "BitRate":
                            stream.BitRate = reader.ReadIntSafe();
                            break;

                        case "Channels":
                            stream.Channels = reader.ReadIntSafe();
                            break;

                        case "Language":
                            stream.Language = reader.ReadElementContentAsString();
                            break;

                        case "Codec":
                            stream.Codec = reader.ReadElementContentAsString();
                            break;

                        default:
                            reader.Skip();
                            break;
                    }
                }
            }

            return stream;
        }

        private void FetchMediaInfoVideo(XmlReader reader, Video item)
        {
            reader.MoveToContent();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Width":
                            item.Width = reader.ReadIntSafe();
                            break;

                        case "Height":
                            item.Height = reader.ReadIntSafe();
                            break;

                        case "BitRate":
                            item.BitRate = reader.ReadIntSafe();
                            break;

                        case "FrameRate":
                            item.FrameRate = reader.ReadFloatSafe();
                            break;

                        case "ScanType":
                            item.ScanType = reader.ReadElementContentAsString();
                            break;

                        case "Duration":
                            item.RunTimeTicks = TimeSpan.FromMinutes(reader.ReadIntSafe()).Ticks;
                            break;

                        case "DurationSeconds":
                            int seconds = reader.ReadIntSafe();
                            if (seconds > 0)
                            {
                                item.RunTimeTicks = TimeSpan.FromSeconds(seconds).Ticks;
                            }
                            break;

                        case "Codec":
                            {
                                string videoCodec = reader.ReadElementContentAsString();

                                switch (videoCodec.ToLower())
                                {
                                    case "sorenson h.263":
                                        item.Codec = "Sorenson H263";
                                        break;
                                    case "h.262":
                                        item.Codec = "MPEG-2 Video";
                                        break;
                                    case "h.264":
                                        item.Codec = "AVC";
                                        break;
                                    default:
                                        item.Codec = videoCodec;
                                        break;
                                }

                                break;
                            }

                        default:
                            reader.Skip();
                            break;
                    }
                }
            }
        }

        private SubtitleStream FetchMediaInfoSubtitles(XmlReader reader)
        {
            var stream = new SubtitleStream();

            reader.MoveToContent();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Language":
                            stream.Language = reader.ReadElementContentAsString();
                            break;

                        case "Default":
                            stream.IsDefault = reader.ReadElementContentAsString() == "True";
                            break;

                        case "Forced":
                            stream.IsForced = reader.ReadElementContentAsString() == "True";
                            break;

                        default:
                            reader.Skip();
                            break;
                    }
                }
            }

            return stream;
        }

        private void FetchFromTaglinesNode(XmlReader reader, T item)
        {
            var list = item.Taglines ?? new List<string>();

            reader.MoveToContent();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Tagline":
                            {
                                string val = reader.ReadElementContentAsString();

                                if (!string.IsNullOrWhiteSpace(val) && !list.Contains(val))
                                {
                                    list.Add(val);
                                }
                                break;
                            }

                        default:
                            reader.Skip();
                            break;
                    }
                }
            }

            item.Taglines = list;
        }

        private void FetchFromGenresNode(XmlReader reader, T item)
        {
            var list = item.Genres ?? new List<string>();

            reader.MoveToContent();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Genre":
                            {
                                string genre = reader.ReadElementContentAsString();

                                if (!string.IsNullOrWhiteSpace(genre))
                                {
                                    list.Add(genre);
                                }
                                break;
                            }

                        default:
                            reader.Skip();
                            break;
                    }
                }
            }

            item.Genres = list;
        }

        private void FetchDataFromPersonsNode(XmlReader reader, T item)
        {
            reader.MoveToContent();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Person":
                            {
                                item.AddPerson(GetPersonFromXmlNode(reader.ReadSubtree()));
                                break;
                            }

                        default:
                            reader.Skip();
                            break;
                    }
                }
            }
        }

        private void FetchFromStudiosNode(XmlReader reader, T item)
        {
            var list = item.Studios ?? new List<string>();

            reader.MoveToContent();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Studio":
                            {
                                string studio = reader.ReadElementContentAsString();

                                if (!string.IsNullOrWhiteSpace(studio))
                                {
                                    list.Add(studio);
                                }
                                break;
                            }

                        default:
                            reader.Skip();
                            break;
                    }
                }
            }

            item.Studios = list;
        }

        private void FetchFromParentalRatingNode(XmlReader reader, T item)
        {
            reader.MoveToContent();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Value":
                            {
                                string ratingString = reader.ReadElementContentAsString();

                                int rating = 7;

                                if (!string.IsNullOrWhiteSpace(ratingString))
                                {
                                    int.TryParse(ratingString, out rating);
                                }

                                switch (rating)
                                {
                                    case -1:
                                        item.OfficialRating = "NR";
                                        break;
                                    case 0:
                                        item.OfficialRating = "UR";
                                        break;
                                    case 1:
                                        item.OfficialRating = "G";
                                        break;
                                    case 3:
                                        item.OfficialRating = "PG";
                                        break;
                                    case 4:
                                        item.OfficialRating = "PG-13";
                                        break;
                                    case 5:
                                        item.OfficialRating = "NC-17";
                                        break;
                                    case 6:
                                        item.OfficialRating = "R";
                                        break;
                                    default:
                                        break;
                                }
                                break;
                            }

                        default:
                            reader.Skip();
                            break;
                    }
                }
            }
        }

        private PersonInfo GetPersonFromXmlNode(XmlReader reader)
        {
            var person = new PersonInfo();

            reader.MoveToContent();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Name":
                            person.Name = reader.ReadElementContentAsString();
                            break;

                        case "Type":
                            person.Type = reader.ReadElementContentAsString();
                            break;

                        case "Role":
                            person.Overview = reader.ReadElementContentAsString();
                            break;

                        default:
                            reader.Skip();
                            break;
                    }
                }
            }

            return person;
        }

        protected IEnumerable<string> GetSplitValues(string value, char deliminator)
        {
            value = (value ?? string.Empty).Trim(deliminator);

            return string.IsNullOrWhiteSpace(value) ? new string[] { } : value.Split(deliminator);
        }
    }
}
