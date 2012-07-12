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
            XmlDocument doc = new XmlDocument();

            doc.Load(metadataFile);

            XmlElement titleElement = doc.DocumentElement;

            foreach (XmlNode node in titleElement.ChildNodes)
            {
                FetchDataFromXmlNode(node, item);
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

        protected virtual void FetchDataFromXmlNode(XmlNode node, T item)
        {
            switch (node.Name)
            {
                case "Added":
                    DateTime added;
                    if (DateTime.TryParse(node.InnerText ?? string.Empty, out added))
                    {
                        item.DateCreated = added;
                    }
                    break;

                case "Type":
                    {
                        item.DisplayMediaType = node.InnerText ?? string.Empty;

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
                    item.BannerImagePath = node.InnerText ?? string.Empty;
                    break;

                case "LocalTitle":
                    item.Name = node.InnerText ?? string.Empty;
                    break;

                case "SortTitle":
                    item.SortName = node.InnerText ?? string.Empty;
                    break;

                case "Overview":
                case "Description":
                    item.Overview = node.InnerText ?? string.Empty;
                    break;

                case "TagLine":
                    item.Tagline = node.InnerText ?? string.Empty;
                    break;

                case "ContentRating":
                case "MPAARating":
                    item.OfficialRating = node.InnerText ?? string.Empty;
                    break;

                case "CustomRating":
                    item.CustomRating = node.InnerText ?? string.Empty;
                    break;

                case "CustomPin":
                    item.CustomPin = node.InnerText ?? string.Empty;
                    break;

                case "Covers":
                    FetchFromCoversNode(node, item);
                    break;

                case "Genres":
                    FetchFromGenresNode(node, item);
                    break;

                case "Genre":
                    {
                        var genres = (item.Genres ?? new string[] { }).ToList();
                        genres.AddRange(GetSplitValues(node.InnerText, '|'));

                        item.Genres = genres;
                        break;
                    }

                case "AspectRatio":
                    item.AspectRatio = node.InnerText ?? string.Empty;
                    break;

                case "Rating":
                case "IMDBrating":
                    float IMDBrating = node.SafeGetSingle((float)-1, (float)10);

                    if (IMDBrating >= 0)
                    {
                        item.UserRating = IMDBrating;
                    }
                    break;

                case "Network":
                    {
                        var studios = (item.Studios ?? new string[] { }).ToList();
                        studios.AddRange(GetSplitValues(node.InnerText, '|'));

                        item.Studios = studios;
                        break;
                    }
                case "Studios":
                    FetchFromStudiosNode(node, item);
                    break;

                case "Director":
                    {
                        var list = (item.People ?? new PersonInfo[]{}).ToList();
                        list.AddRange(GetSplitValues(node.InnerText, '|').Select(v => new PersonInfo() { Name = v, PersonType = PersonType.Director }));

                        item.People = list;
                        break;
                    }
                case "Writer":
                    {
                        var list = (item.People ?? new PersonInfo[] { }).ToList();
                        list.AddRange(GetSplitValues(node.InnerText, '|').Select(v => new PersonInfo() { Name = v, PersonType = PersonType.Writer }));

                        item.People = list;
                        break;
                    }

                case "Actors":
                case "GuestStars":
                    {
                        var list = (item.People ?? new PersonInfo[] { }).ToList();
                        list.AddRange(GetSplitValues(node.InnerText, '|').Select(v => new PersonInfo() { Name = v, PersonType = PersonType.Actor }));

                        item.People = list;
                        break;
                    }

                case "Persons":
                    FetchDataFromPersonsNode(node, item);
                    break;

                case "Trailer":
                    item.TrailerUrl = node.InnerText ?? string.Empty;
                    break;

                case "ParentalRating":
                    FetchFromParentalRatingNode(node, item);
                    break;

                case "ProductionYear":
                    {
                        int ProductionYear;
                        if (int.TryParse(node.InnerText, out ProductionYear) && ProductionYear > 1850)
                        {
                            item.ProductionYear = ProductionYear;
                        }

                        break;
                    }

                case "MediaInfo":
                    FetchMediaInfo(node, item);
                    break;

                default:
                    break;
            }
        }

        protected virtual void FetchFromCoversNode(XmlNode node, T item)
        {
            string cover = node.SafeGetString("Front");

            if (!string.IsNullOrEmpty(cover))
            {
                item.PrimaryImagePath = cover;
            }
        }

        protected virtual void FetchMediaInfo(XmlNode node, T item)
        {
            var iMediaInfo = item as Video;

            if (iMediaInfo != null)
            {
                FetchMediaInfo(node, iMediaInfo);
            }
        }

        protected virtual void FetchMediaInfo(XmlNode node, Video item)
        {
            foreach (XmlNode childNode in node.ChildNodes)
            {
                switch (childNode.Name)
                {
                    case "Audio":
                        {
                            AudioStream stream = FetchMediaInfoAudio(childNode);

                            List<AudioStream> streams = item.AudioStreams.ToList();
                            streams.Add(stream);
                            item.AudioStreams = streams;

                            break;
                        }

                    case "Video":
                        FetchMediaInfoVideo(childNode, item);
                        break;

                    case "Subtitle":
                        FetchMediaInfoSubtitles(childNode, item);
                        break;

                    default:
                        break;
                }
            }
        }

        protected virtual AudioStream FetchMediaInfoAudio(XmlNode node)
        {
            AudioStream stream = new AudioStream();

            foreach (XmlNode childNode in node.ChildNodes)
            {
                switch (childNode.Name)
                {
                    case "BitRate":
                        stream.BitRate = childNode.SafeGetInt32();
                        break;

                    case "Channels":
                        stream.Channels = childNode.SafeGetInt32();
                        break;

                    case "Language":
                        stream.Language = childNode.InnerText ?? string.Empty;
                        break;

                    case "Codec":
                        {
                            string codec = childNode.InnerText ?? string.Empty;

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
                        break;
                }
            }

            return stream;
        }

        protected virtual void FetchMediaInfoVideo(XmlNode node, Video item)
        {
            foreach (XmlNode childNode in node.ChildNodes)
            {
                switch (childNode.Name)
                {
                    case "Width":
                        item.Width = childNode.SafeGetInt32();
                        break;

                    case "Height":
                        item.Height = childNode.SafeGetInt32();
                        break;

                    case "BitRate":
                        item.VideoBitRate = childNode.SafeGetInt32();
                        break;

                    case "FrameRate":
                        item.FrameRate = childNode.InnerText ?? string.Empty;
                        break;

                    case "ScanType":
                        item.ScanType = childNode.InnerText ?? string.Empty;
                        break;

                    case "Duration":
                        item.RunTime = TimeSpan.FromMinutes(childNode.SafeGetInt32());
                        break;

                    case "DurationSeconds":
                        int seconds = childNode.SafeGetInt32();
                        if (seconds > 0)
                        {
                            item.RunTime = TimeSpan.FromSeconds(seconds);
                        }
                        break;

                    case "Codec":
                        {
                            string videoCodec = childNode.InnerText ?? string.Empty;

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
                        break;
                }
            }
        }

        protected virtual void FetchMediaInfoSubtitles(XmlNode node, Video item)
        {
            List<string> subtitles = item.Subtitles.ToList();

            foreach (XmlNode childNode in node.ChildNodes)
            {
                switch (childNode.Name)
                {
                    case "Language":
                        string lang = childNode.InnerText;

                        if (!string.IsNullOrEmpty(lang))
                        {
                            subtitles.Add(lang);
                        }
                        break;

                    default:
                        break;
                }
            }

            item.Subtitles = subtitles;
        }

        protected virtual void FetchFromGenresNode(XmlNode node, T item)
        {
            List<string> list = (item.Genres ?? new string[] { }).ToList();

            foreach (XmlNode childNode in node.ChildNodes)
            {
                switch (childNode.Name)
                {
                    case "Genre":
                        string text = childNode.InnerText ?? string.Empty;

                        if (!string.IsNullOrEmpty(text))
                        {
                            list.Add(text);
                        }
                        break;

                    default:
                        break;
                }

            }
            item.Genres = list;
        }

        protected virtual void FetchDataFromPersonsNode(XmlNode node, T item)
        {
            List<PersonInfo> list = (item.People ?? new PersonInfo[] { }).ToList();

            foreach (XmlNode childNode in node.ChildNodes)
            {
                switch (childNode.Name)
                {
                    case "Person":
                        {
                            list.Add(GetPersonFromXmlNode(childNode));

                            break;
                        }

                    default:
                        break;
                }

            }

            item.People = list;
        }

        protected virtual void FetchFromStudiosNode(XmlNode node, T item)
        {
            List<string> list = (item.Studios ?? new string[] { }).ToList();

            foreach (XmlNode childNode in node.ChildNodes)
            {
                switch (childNode.Name)
                {
                    case "Studio":
                        string text = childNode.InnerText ?? string.Empty;

                        if (!string.IsNullOrEmpty(text))
                        {
                            list.Add(text);
                        }
                        break;

                    default:
                        break;
                }
            }

            item.Studios = list;
        }

        protected virtual void FetchFromParentalRatingNode(XmlNode node, T item)
        {
            foreach (XmlNode childNode in node.ChildNodes)
            {
                switch (childNode.Name)
                {
                    case "Value":
                        {
                            int ParentalRating = childNode.SafeGetInt32((int)7);

                            switch (ParentalRating)
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
                        break;
                }
            }
        }

        private PersonInfo GetPersonFromXmlNode(XmlNode node)
        {
            PersonInfo person = new PersonInfo();

            foreach (XmlNode childNode in node.ChildNodes)
            {
                switch (childNode.Name)
                {
                    case "Name":
                        person.Name = childNode.InnerText ?? string.Empty;
                        break;

                    case "Type":
                        {
                            string type = childNode.InnerText ?? string.Empty;

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
                        person.Overview = childNode.InnerText ?? string.Empty;
                        break;

                    default:
                        break;
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
