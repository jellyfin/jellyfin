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
        public async Task Fetch(T item, string metadataFile)
        {
            // Use XmlReader for best performance
            using (XmlReader reader = XmlReader.Create(metadataFile, new XmlReaderSettings() { Async = true }))
            {
                await reader.MoveToContentAsync();

                // Loop through each element
                while (await reader.ReadAsync())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        await FetchDataFromXmlNode(reader, item);
                    }
                }
            }

            // If dates weren't supplied in metadata, use values from the xml file
            if (item.DateCreated == DateTime.MinValue)
            {
                item.DateCreated = File.GetCreationTime(metadataFile);
            }

            if (item.DateModified == DateTime.MinValue)
            {
                item.DateModified = File.GetLastWriteTime(metadataFile);
            }
        }

        /// <summary>
        /// Fetches metadata from one Xml Element
        /// </summary>
        protected async virtual Task FetchDataFromXmlNode(XmlReader reader, T item)
        {
            switch (reader.Name)
            {
                // DateCreated
                case "Added":
                    DateTime added;
                    if (DateTime.TryParse(await reader.ReadElementContentAsStringAsync() ?? string.Empty, out added))
                    {
                        item.DateCreated = added;
                    }
                    break;

                // DisplayMediaType
                case "Type":
                    {
                        item.DisplayMediaType = await reader.ReadElementContentAsStringAsync();

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
                    item.BannerImagePath = await reader.ReadElementContentAsStringAsync();
                    break;

                case "LocalTitle":
                    item.Name = await reader.ReadElementContentAsStringAsync();
                    break;

                case "SortTitle":
                    item.SortName = await reader.ReadElementContentAsStringAsync();
                    break;

                case "Overview":
                case "Description":
                    item.Overview = await reader.ReadElementContentAsStringAsync();
                    break;

                case "TagLine":
                    {
                        var list = (item.Taglines ?? new string[] { }).ToList();
                        var tagline = await reader.ReadElementContentAsStringAsync();

                        if (!list.Contains(tagline))
                        {
                            list.Add(tagline);
                        }

                        item.Taglines = list;
                        break;
                    }

                case "TagLines":
                    {
                        await FetchFromTaglinesNode(reader.ReadSubtree(), item);
                        break;
                    }

                case "ContentRating":
                case "MPAARating":
                    item.OfficialRating = await reader.ReadElementContentAsStringAsync();
                    break;

                case "CustomRating":
                    item.CustomRating = await reader.ReadElementContentAsStringAsync();
                    break;

                case "Runtime":
                case "RunningTime":
                    {
                        string text = await reader.ReadElementContentAsStringAsync();

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
                        genres.AddRange(GetSplitValues(await reader.ReadElementContentAsStringAsync(), '|'));

                        item.Genres = genres;
                        break;
                    }

                case "AspectRatio":
                    item.AspectRatio = await reader.ReadElementContentAsStringAsync();
                    break;

                case "Network":
                    {
                        var studios = (item.Studios ?? new string[] { }).ToList();
                        studios.AddRange(GetSplitValues(await reader.ReadElementContentAsStringAsync(), '|'));

                        item.Studios = studios;
                        break;
                    }

                case "Director":
                    {
                        var list = (item.People ?? new PersonInfo[] { }).ToList();
                        list.AddRange(GetSplitValues(await reader.ReadElementContentAsStringAsync(), '|').Select(v => new PersonInfo() { Name = v, Type = "Director" }));

                        item.People = list;
                        break;
                    }
                case "Writer":
                    {
                        var list = (item.People ?? new PersonInfo[] { }).ToList();
                        list.AddRange(GetSplitValues(await reader.ReadElementContentAsStringAsync(), '|').Select(v => new PersonInfo() { Name = v, Type = "Writer" }));

                        item.People = list;
                        break;
                    }

                case "Actors":
                case "GuestStars":
                    {
                        var list = (item.People ?? new PersonInfo[] { }).ToList();
                        list.AddRange(GetSplitValues(await reader.ReadElementContentAsStringAsync(), '|').Select(v => new PersonInfo() { Name = v, Type = "Actor" }));

                        item.People = list;
                        break;
                    }

                case "Trailer":
                    item.TrailerUrl = await reader.ReadElementContentAsStringAsync();
                    break;

                case "ProductionYear":
                    {
                        string val = await reader.ReadElementContentAsStringAsync();

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

                    string rating = await reader.ReadElementContentAsStringAsync();

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
                        string firstAired = await reader.ReadElementContentAsStringAsync();

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
                    string tmdb = await reader.ReadElementContentAsStringAsync();
                    if (!string.IsNullOrWhiteSpace(tmdb))
                    {
                        item.SetProviderId(MetadataProviders.Tmdb, tmdb);
                    }
                    break;

                case "TVcomId":
                    string TVcomId = await reader.ReadElementContentAsStringAsync();
                    if (!string.IsNullOrWhiteSpace(TVcomId))
                    {
                        item.SetProviderId(MetadataProviders.Tvcom, TVcomId);
                    }
                    break;

                case "IMDB_ID":
                case "IMDB":
                case "IMDbId":
                    string IMDbId = await reader.ReadElementContentAsStringAsync();
                    if (!string.IsNullOrWhiteSpace(IMDbId))
                    {
                        item.SetProviderId(MetadataProviders.Imdb, IMDbId);
                    }
                    break;

                case "Genres":
                    await FetchFromGenresNode(reader.ReadSubtree(), item);
                    break;

                case "Persons":
                    await FetchDataFromPersonsNode(reader.ReadSubtree(), item);
                    break;

                case "ParentalRating":
                    await FetchFromParentalRatingNode(reader.ReadSubtree(), item);
                    break;

                case "Studios":
                    await FetchFromStudiosNode(reader.ReadSubtree(), item);
                    break;

                case "MediaInfo":
                    {
                        var video = item as Video;

                        if (video != null)
                        {
                            await FetchMediaInfo(reader.ReadSubtree(), video);
                        }
                        break;
                    }

                default:
                    await reader.SkipAsync();
                    break;
            }
        }

        private async Task FetchMediaInfo(XmlReader reader, Video item)
        {
            await reader.MoveToContentAsync();

            while (await reader.ReadAsync())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Audio":
                            {
                                AudioStream stream = await FetchMediaInfoAudio(reader.ReadSubtree());

                                List<AudioStream> streams = (item.AudioStreams ?? new AudioStream[] { }).ToList();
                                streams.Add(stream);
                                item.AudioStreams = streams;

                                break;
                            }

                        case "Video":
                            await FetchMediaInfoVideo(reader.ReadSubtree(), item);
                            break;

                        case "Subtitle":
                            await FetchMediaInfoSubtitles(reader.ReadSubtree(), item);
                            break;

                        default:
                            await reader.SkipAsync();
                            break;
                    }
                }
            }
        }

        private async Task<AudioStream> FetchMediaInfoAudio(XmlReader reader)
        {
            AudioStream stream = new AudioStream();

            await reader.MoveToContentAsync();

            while (await reader.ReadAsync())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Default":
                            stream.IsDefault = await reader.ReadElementContentAsStringAsync() == "True";
                            break;

                        case "Forced":
                            stream.IsForced = await reader.ReadElementContentAsStringAsync() == "True";
                            break;

                        case "BitRate":
                            stream.BitRate = await reader.ReadIntSafe();
                            break;

                        case "Channels":
                            stream.Channels = await reader.ReadIntSafe();
                            break;

                        case "Language":
                            stream.Language = await reader.ReadElementContentAsStringAsync();
                            break;

                        case "Codec":
                            {
                                string codec = await reader.ReadElementContentAsStringAsync();

                                switch (codec.ToLower())
                                {
                                    case "dts-es":
                                    case "dts-es matrix":
                                    case "dts-es discrete":
                                        stream.Format = "DTS";
                                        stream.Profile = "ES";
                                        break;
                                    case "dts-hd hra":
                                    case "dts-hd high resolution":
                                        stream.Format = "DTS";
                                        stream.Profile = "HRA";
                                        break;
                                    case "dts ma":
                                    case "dts-hd ma":
                                    case "dts-hd master":
                                        stream.Format = "DTS";
                                        stream.Profile = "MA";
                                        break;
                                    case "dolby digital":
                                    case "dolby digital surround ex":
                                    case "dolby surround":
                                        stream.Format = "AC-3";
                                        break;
                                    case "dolby digital plus":
                                        stream.Format = "E-AC-3";
                                        break;
                                    case "dolby truehd":
                                        stream.Format = "AC-3";
                                        stream.Profile = "TrueHD";
                                        break;
                                    case "mp2":
                                        stream.Format = "MPEG Audio";
                                        stream.Profile = "Layer 2";
                                        break;
                                    case "other":
                                        break;
                                    default:
                                        stream.Format = codec;
                                        break;
                                }

                                break;
                            }

                        default:
                            await reader.SkipAsync();
                            break;
                    }
                }
            }

            return stream;
        }

        private async Task FetchMediaInfoVideo(XmlReader reader, Video item)
        {
            await reader.MoveToContentAsync();

            while (await reader.ReadAsync())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Width":
                            item.Width = await reader.ReadIntSafe();
                            break;

                        case "Height":
                            item.Height = await reader.ReadIntSafe();
                            break;

                        case "BitRate":
                            item.BitRate = await reader.ReadIntSafe();
                            break;

                        case "FrameRate":
                            item.FrameRate = await reader.ReadElementContentAsStringAsync();
                            break;

                        case "ScanType":
                            item.ScanType = await reader.ReadElementContentAsStringAsync();
                            break;

                        case "Duration":
                            item.RunTimeTicks = TimeSpan.FromMinutes(await reader.ReadIntSafe()).Ticks;
                            break;

                        case "DurationSeconds":
                            int seconds = await reader.ReadIntSafe();
                            if (seconds > 0)
                            {
                                item.RunTimeTicks = TimeSpan.FromSeconds(seconds).Ticks;
                            }
                            break;

                        case "Codec":
                            {
                                string videoCodec = await reader.ReadElementContentAsStringAsync();

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
                            await reader.SkipAsync();
                            break;
                    }
                }
            }
        }

        private async Task FetchMediaInfoSubtitles(XmlReader reader, Video item)
        {
            List<string> list = (item.Subtitles ?? new string[] { }).ToList();

            await reader.MoveToContentAsync();

            while (await reader.ReadAsync())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Language":
                            {
                                string genre = await reader.ReadElementContentAsStringAsync();

                                if (!string.IsNullOrWhiteSpace(genre))
                                {
                                    list.Add(genre);
                                }
                                break;
                            }

                        default:
                            await reader.SkipAsync();
                            break;
                    }
                }
            }

            item.Subtitles = list;
        }

        private async Task FetchFromTaglinesNode(XmlReader reader, T item)
        {
            List<string> list = (item.Taglines ?? new string[] { }).ToList();

            await reader.MoveToContentAsync();

            while (await reader.ReadAsync())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Tagline":
                            {
                                string val = await reader.ReadElementContentAsStringAsync();

                                if (!string.IsNullOrWhiteSpace(val))
                                {
                                    list.Add(val);
                                }
                                break;
                            }

                        default:
                            await reader.SkipAsync();
                            break;
                    }
                }
            }

            item.Taglines = list;
        }

        private async Task FetchFromGenresNode(XmlReader reader, T item)
        {
            List<string> list = (item.Genres ?? new string[] { }).ToList();

            await reader.MoveToContentAsync();

            while (await reader.ReadAsync())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Genre":
                            {
                                string genre = await reader.ReadElementContentAsStringAsync();

                                if (!string.IsNullOrWhiteSpace(genre))
                                {
                                    list.Add(genre);
                                }
                                break;
                            }

                        default:
                            await reader.SkipAsync();
                            break;
                    }
                }
            }

            item.Genres = list;
        }

        private async Task FetchDataFromPersonsNode(XmlReader reader, T item)
        {
            List<PersonInfo> list = (item.People ?? new PersonInfo[] { }).ToList();

            await reader.MoveToContentAsync();

            while (await reader.ReadAsync())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Person":
                            {
                                list.Add(await GetPersonFromXmlNode(reader.ReadSubtree()));
                                break;
                            }

                        default:
                            await reader.SkipAsync();
                            break;
                    }
                }
            }

            item.People = list;
        }

        private async Task FetchFromStudiosNode(XmlReader reader, T item)
        {
            List<string> list = (item.Studios ?? new string[] { }).ToList();

            await reader.MoveToContentAsync();

            while (await reader.ReadAsync())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Studio":
                            {
                                string studio = await reader.ReadElementContentAsStringAsync();

                                if (!string.IsNullOrWhiteSpace(studio))
                                {
                                    list.Add(studio);
                                }
                                break;
                            }

                        default:
                            await reader.SkipAsync();
                            break;
                    }
                }
            }

            item.Studios = list;
        }

        private async Task FetchFromParentalRatingNode(XmlReader reader, T item)
        {
            await reader.MoveToContentAsync();

            while (await reader.ReadAsync())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Value":
                            {
                                string ratingString = await reader.ReadElementContentAsStringAsync();

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
                            await reader.SkipAsync();
                            break;
                    }
                }
            }
        }

        private async Task<PersonInfo> GetPersonFromXmlNode(XmlReader reader)
        {
            PersonInfo person = new PersonInfo();

            await reader.MoveToContentAsync();

            while (await reader.ReadAsync())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Name":
                            person.Name = await reader.ReadElementContentAsStringAsync();
                            break;

                        case "Type":
                            person.Type = await reader.ReadElementContentAsStringAsync();
                            break;

                        case "Role":
                            person.Overview = await reader.ReadElementContentAsStringAsync();
                            break;

                        default:
                            await reader.SkipAsync();
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
