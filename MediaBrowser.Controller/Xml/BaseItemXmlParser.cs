using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Xml
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
                        item.DateCreated = added;
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
                                item.DisplayMediaType = VideoType.DVD.ToString();
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
                        var list = (item.Taglines ?? new string[] { }).ToList();
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
                        var genres = (item.Genres ?? new string[] { }).ToList();
                        genres.AddRange(GetSplitValues(reader.ReadElementContentAsString(), '|'));

                        item.Genres = genres;
                        break;
                    }

                case "AspectRatio":
                    item.AspectRatio = reader.ReadElementContentAsString();
                    break;

                case "Network":
                    {
                        var studios = (item.Studios ?? new string[] { }).ToList();
                        studios.AddRange(GetSplitValues(reader.ReadElementContentAsString(), '|'));

                        item.Studios = studios;
                        break;
                    }

                case "Director":
                    {
                        var list = (item.People ?? new PersonInfo[] { }).ToList();
                        list.AddRange(GetSplitValues(reader.ReadElementContentAsString(), '|').Select(v => new PersonInfo() { Name = v, Type = "Director" }));

                        item.People = list;
                        break;
                    }
                case "Writer":
                    {
                        var list = (item.People ?? new PersonInfo[] { }).ToList();
                        list.AddRange(GetSplitValues(reader.ReadElementContentAsString(), '|').Select(v => new PersonInfo() { Name = v, Type = "Writer" }));

                        item.People = list;
                        break;
                    }

                case "Actors":
                case "GuestStars":
                    {
                        var list = (item.People ?? new PersonInfo[] { }).ToList();
                        list.AddRange(GetSplitValues(reader.ReadElementContentAsString(), '|').Select(v => new PersonInfo() { Name = v, Type = "Actor" }));

                        item.People = list;
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
                            item.UserRating = val;
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
                                item.PremiereDate = airDate;
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

                                List<AudioStream> streams = (item.AudioStreams ?? new AudioStream[] { }).ToList();
                                streams.Add(stream);
                                item.AudioStreams = streams;

                                break;
                            }

                        case "Video":
                            FetchMediaInfoVideo(reader.ReadSubtree(), item);
                            break;

                        case "Subtitle":
                            FetchMediaInfoSubtitles(reader.ReadSubtree(), item);
                            break;

                        default:
                            reader.Skip();
                            break;
                    }
                }
            }
        }

        private AudioStream FetchMediaInfoAudio(XmlReader reader)
        {
            AudioStream stream = new AudioStream();

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

                        case "Forced":
                            stream.IsForced = reader.ReadElementContentAsString() == "True";
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

        private void FetchMediaInfoSubtitles(XmlReader reader, Video item)
        {
            List<string> list = (item.Subtitles ?? new string[] { }).ToList();

            reader.MoveToContent();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Language":
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

            item.Subtitles = list;
        }

        private void FetchFromTaglinesNode(XmlReader reader, T item)
        {
            List<string> list = (item.Taglines ?? new string[] { }).ToList();

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

                                if (!string.IsNullOrWhiteSpace(val))
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
            List<string> list = (item.Genres ?? new string[] { }).ToList();

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
            List<PersonInfo> list = (item.People ?? new PersonInfo[] { }).ToList();

            reader.MoveToContent();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Person":
                            {
                                list.Add(GetPersonFromXmlNode(reader.ReadSubtree()));
                                break;
                            }

                        default:
                            reader.Skip();
                            break;
                    }
                }
            }

            item.People = list;
        }

        private void FetchFromStudiosNode(XmlReader reader, T item)
        {
            List<string> list = (item.Studios ?? new string[] { }).ToList();

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
            PersonInfo person = new PersonInfo();

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
