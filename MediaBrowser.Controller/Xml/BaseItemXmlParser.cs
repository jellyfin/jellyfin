using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Xml
{
    public class BaseItemXmlParser<T>
        where T : BaseItem, new()
    {
        public virtual void Fetch(T item, string metadataFile)
        {
            using (XmlReader reader = XmlReader.Create(metadataFile))
            {
                reader.MoveToContent();

                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        FetchDataFromXmlNode(reader, item);
                    }
                }
            }

            // If dates weren't supplied in metadata, use values from the file
            if (item.DateCreated == DateTime.MinValue)
            {
                item.DateCreated = File.GetCreationTime(metadataFile);
            }

            if (item.DateModified == DateTime.MinValue)
            {
                item.DateModified = File.GetLastWriteTime(metadataFile);
            }
        }

        protected virtual void FetchDataFromXmlNode(XmlReader reader, T item)
        {
            switch (reader.Name)
            {
                case "Added":
                    DateTime added;
                    if (DateTime.TryParse(reader.ReadElementContentAsString() ?? string.Empty, out added))
                    {
                        item.DateCreated = added;
                    }
                    break;

                case "Type":
                    {
                        item.DisplayMediaType = reader.ReadElementContentAsString() ?? string.Empty;

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

                case "banner":
                    item.BannerImagePath = reader.ReadElementContentAsString() ?? string.Empty;
                    break;

                case "LocalTitle":
                    item.Name = reader.ReadElementContentAsString() ?? string.Empty;
                    break;

                case "SortTitle":
                    item.SortName = reader.ReadElementContentAsString() ?? string.Empty;
                    break;

                case "Overview":
                case "Description":
                    item.Overview = reader.ReadElementContentAsString() ?? string.Empty;
                    break;

                case "TagLine":
                    item.Tagline = reader.ReadElementContentAsString() ?? string.Empty;
                    break;

                case "ContentRating":
                case "MPAARating":
                    item.OfficialRating = reader.ReadElementContentAsString() ?? string.Empty;
                    break;

                case "CustomRating":
                    item.CustomRating = reader.ReadElementContentAsString() ?? string.Empty;
                    break;

                case "CustomPin":
                    item.CustomPin = reader.ReadElementContentAsString() ?? string.Empty;
                    break;

                case "Genre":
                    {
                        var genres = (item.Genres ?? new string[] { }).ToList();
                        genres.AddRange(GetSplitValues(reader.ReadElementContentAsString(), '|'));

                        item.Genres = genres;
                        break;
                    }

                case "AspectRatio":
                    item.AspectRatio = reader.ReadElementContentAsString() ?? string.Empty;
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
                        list.AddRange(GetSplitValues(reader.ReadElementContentAsString(), '|').Select(v => new PersonInfo() { Name = v, PersonType = PersonType.Director }));

                        item.People = list;
                        break;
                    }
                case "Writer":
                    {
                        var list = (item.People ?? new PersonInfo[] { }).ToList();
                        list.AddRange(GetSplitValues(reader.ReadElementContentAsString(), '|').Select(v => new PersonInfo() { Name = v, PersonType = PersonType.Writer }));

                        item.People = list;
                        break;
                    }

                case "Actors":
                case "GuestStars":
                    {
                        var list = (item.People ?? new PersonInfo[] { }).ToList();
                        list.AddRange(GetSplitValues(reader.ReadElementContentAsString(), '|').Select(v => new PersonInfo() { Name = v, PersonType = PersonType.Actor }));

                        item.People = list;
                        break;
                    }

                case "Trailer":
                    item.TrailerUrl = reader.ReadElementContentAsString() ?? string.Empty;
                    break;

                case "ProductionYear":
                    {
                        int ProductionYear;
                        if (int.TryParse(reader.ReadElementContentAsString(), out ProductionYear) && ProductionYear > 1850)
                        {
                            item.ProductionYear = ProductionYear;
                        }

                        break;
                    }

                case "Rating":
                case "IMDBrating":

                    string rating = reader.ReadElementContentAsString();

                    if (!string.IsNullOrEmpty(rating))
                    {
                        float val;

                        if (float.TryParse(rating, out val))
                        {
                            item.UserRating = val;
                        }
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
                    FetchMediaInfo(reader.ReadSubtree(), item);
                    break;

                default:
                    reader.Skip();
                    break;
            }
        }

        private void FetchMediaInfo(XmlReader reader, T item)
        {
            var video = item as Video;

            if (video != null)
            {
                FetchMediaInfo(reader, video);
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

                                List<AudioStream> streams = item.AudioStreams.ToList();
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
                        case "BitRate":
                            stream.BitRate = reader.ReadIntSafe();
                            break;

                        case "Channels":
                            stream.Channels = reader.ReadIntSafe();
                            break;

                        case "Language":
                            stream.Language = reader.ReadElementContentAsString() ?? string.Empty;
                            break;

                        case "Codec":
                            {
                                string codec = reader.ReadElementContentAsString() ?? string.Empty;

                                switch (codec.ToLower())
                                {
                                    case "dts-es":
                                    case "dts-es matrix":
                                    case "dts-es discrete":
                                        stream.AudioFormat = "DTS";
                                        stream.AudioProfile = "ES";
                                        break;
                                    case "dts-hd hra":
                                    case "dts-hd high resolution":
                                        stream.AudioFormat = "DTS";
                                        stream.AudioProfile = "HRA";
                                        break;
                                    case "dts ma":
                                    case "dts-hd ma":
                                    case "dts-hd master":
                                        stream.AudioFormat = "DTS";
                                        stream.AudioProfile = "MA";
                                        break;
                                    case "dolby digital":
                                    case "dolby digital surround ex":
                                    case "dolby surround":
                                        stream.AudioFormat = "AC-3";
                                        break;
                                    case "dolby digital plus":
                                        stream.AudioFormat = "E-AC-3";
                                        break;
                                    case "dolby truehd":
                                        stream.AudioFormat = "AC-3";
                                        stream.AudioProfile = "TrueHD";
                                        break;
                                    case "mp2":
                                        stream.AudioFormat = "MPEG Audio";
                                        stream.AudioProfile = "Layer 2";
                                        break;
                                    case "other":
                                        break;
                                    default:
                                        stream.AudioFormat = codec;
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
                            item.VideoBitRate = reader.ReadIntSafe();
                            break;

                        case "FrameRate":
                            item.FrameRate = reader.ReadElementContentAsString() ?? string.Empty;
                            break;

                        case "ScanType":
                            item.ScanType = reader.ReadElementContentAsString() ?? string.Empty;
                            break;

                        case "Duration":
                            item.RunTime = TimeSpan.FromMinutes(reader.ReadIntSafe());
                            break;

                        case "DurationSeconds":
                            int seconds = reader.ReadIntSafe();
                            if (seconds > 0)
                            {
                                item.RunTime = TimeSpan.FromSeconds(seconds);
                            }
                            break;

                        case "Codec":
                            {
                                string videoCodec = reader.ReadElementContentAsString() ?? string.Empty;

                                switch (videoCodec.ToLower())
                                {
                                    case "sorenson h.263":
                                        item.VideoCodec = "Sorenson H263";
                                        break;
                                    case "h.262":
                                        item.VideoCodec = "MPEG-2 Video";
                                        break;
                                    case "h.264":
                                        item.VideoCodec = "AVC";
                                        break;
                                    default:
                                        item.VideoCodec = videoCodec;
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

                                if (!string.IsNullOrEmpty(genre))
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

                                if (!string.IsNullOrEmpty(genre))
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
                                list.Add(GetPersonFromXmlNode(reader));
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

                                if (!string.IsNullOrEmpty(studio))
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

                                if (!string.IsNullOrEmpty(ratingString))
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
                            person.Name = reader.ReadElementContentAsString() ?? string.Empty;
                            break;

                        case "Type":
                            {
                                string type = reader.ReadElementContentAsString() ?? string.Empty;

                                if (type == "Director")
                                {
                                    person.PersonType = PersonType.Director;
                                }
                                else if (type == "Actor")
                                {
                                    person.PersonType = PersonType.Actor;
                                }
                                break;
                            }

                        case "Role":
                            person.Overview = reader.ReadElementContentAsString() ?? string.Empty;
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

            return string.IsNullOrEmpty(value) ? new string[] { } : value.Split(deliminator);
        }
    }
}
