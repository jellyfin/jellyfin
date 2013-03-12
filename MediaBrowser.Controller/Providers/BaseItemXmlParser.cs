using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml;

namespace MediaBrowser.Controller.Providers
{
    /// <summary>
    /// Provides a base class for parsing metadata xml
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class BaseItemXmlParser<T>
        where T : BaseItem, new()
    {
        /// <summary>
        /// The logger
        /// </summary>
        protected ILogger Logger { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseItemXmlParser{T}" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public BaseItemXmlParser(ILogger logger)
        {
            Logger = logger;
        }

        /// <summary>
        /// Fetches metadata for an item from one xml file
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="metadataFile">The metadata file.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public void Fetch(T item, string metadataFile, CancellationToken cancellationToken)
        {
            if (item == null)
            {
                throw new ArgumentNullException();
            }

            if (string.IsNullOrEmpty(metadataFile))
            {
                throw new ArgumentNullException();
            }

            // Use XmlReader for best performance
            using (var reader = XmlReader.Create(metadataFile))
            {
                reader.MoveToContent();

                // Loop through each element
                while (reader.Read())
                {
                    cancellationToken.ThrowIfCancellationRequested();

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
        /// <param name="reader">The reader.</param>
        /// <param name="item">The item.</param>
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

                case "LocalTitle":
                    item.Name = reader.ReadElementContentAsString();
                    break;

                case "Type":
                    {
                        var type = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(type) && !type.Equals("none",StringComparison.OrdinalIgnoreCase))
                        {
                            item.DisplayMediaType = type;
                        }

                        break;
                    }
                case "SortTitle":
                    item.ForcedSortName = reader.ReadElementContentAsString();
                    break;

                case "Overview":
                case "Description":
                    item.Overview = reader.ReadElementContentAsString();
                    break;

                case "TagLine":
                    {
                        var tagline = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(tagline))
                        {
                            item.AddTagline(tagline);
                        }

                        break;
                    }

                case "TagLines":
                    {
                        FetchFromTaglinesNode(reader.ReadSubtree(), item);
                        break;
                    }

                case "ContentRating":
                case "certification":
                case "MPAARating":
                    {
                        var rating = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(rating))
                        {
                            item.OfficialRating = rating;
                        }
                        break;
                    }

                case "CustomRating":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            item.CustomRating = val;
                        }
                        break;
                    }

                case "Runtime":
                case "RunningTime":
                    {
                        var text = reader.ReadElementContentAsString();

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
                        foreach (var name in SplitNames(reader.ReadElementContentAsString()))
                        {
                            if (string.IsNullOrWhiteSpace(name))
                            {
                                continue;
                            }
                            item.AddGenre(name);
                        }
                        break;
                    }

                case "AspectRatio":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            item.AspectRatio = val;
                        }
                        break;
                    }

                case "Network":
                    {
                        foreach (var name in SplitNames(reader.ReadElementContentAsString()))
                        {
                            if (string.IsNullOrWhiteSpace(name))
                            {
                                continue;
                            }
                            item.AddStudio(name);
                        }
                        break;
                    }

                case "Director":
                    {
                        foreach (var p in SplitNames(reader.ReadElementContentAsString()).Select(v => new PersonInfo { Name = v, Type = PersonType.Director }))
                        {
                            if (string.IsNullOrWhiteSpace(p.Name))
                            {
                                continue;
                            }
                            item.AddPerson(p);
                        }
                        break;
                    }
                case "Writer":
                    {
                        foreach (var p in SplitNames(reader.ReadElementContentAsString()).Select(v => new PersonInfo { Name = v, Type = PersonType.Writer }))
                        {
                            if (string.IsNullOrWhiteSpace(p.Name))
                            {
                                continue;
                            }
                            item.AddPerson(p);
                        }
                        break;
                    }

                case "Actors":
                case "GuestStars":
                    {
                        foreach (var p in SplitNames(reader.ReadElementContentAsString()).Select(v => new PersonInfo { Name = v.Trim(), Type = PersonType.Actor }))
                        {
                            if (string.IsNullOrWhiteSpace(p.Name))
                            {
                                continue;
                            }
                            item.AddPerson(p);
                        }
                        break;
                    }

                case "Trailer":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            item.AddTrailerUrl(val);
                        }
                        break;
                    }

                case "ProductionYear":
                    {
                        var val = reader.ReadElementContentAsString();

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
                    {

                        var rating = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(rating))
                        {
                            float val;

                            if (float.TryParse(rating, out val))
                            {
                                item.CommunityRating = val;
                            }
                        }
                        break;
                    }

                case "FirstAired":
                    {
                        var firstAired = reader.ReadElementContentAsString();

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
                    var tmdb = reader.ReadElementContentAsString();
                    if (!string.IsNullOrWhiteSpace(tmdb))
                    {
                        item.SetProviderId(MetadataProviders.Tmdb, tmdb);
                    }
                    break;

                case "TVcomId":
                    var TVcomId = reader.ReadElementContentAsString();
                    if (!string.IsNullOrWhiteSpace(TVcomId))
                    {
                        item.SetProviderId(MetadataProviders.Tvcom, TVcomId);
                    }
                    break;

                case "IMDB_ID":
                case "IMDB":
                case "IMDbId":
                    var IMDbId = reader.ReadElementContentAsString();
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

                default:
                    reader.Skip();
                    break;
            }
        }

        /// <summary>
        /// Fetches from taglines node.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="item">The item.</param>
        private void FetchFromTaglinesNode(XmlReader reader, T item)
        {
            reader.MoveToContent();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Tagline":
                            {
                                var val = reader.ReadElementContentAsString();

                                if (!string.IsNullOrWhiteSpace(val))
                                {
                                    item.AddTagline(val);
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

        /// <summary>
        /// Fetches from genres node.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="item">The item.</param>
        private void FetchFromGenresNode(XmlReader reader, T item)
        {
            reader.MoveToContent();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Genre":
                            {
                                var genre = reader.ReadElementContentAsString();

                                if (!string.IsNullOrWhiteSpace(genre))
                                {
                                    item.AddGenre(genre);
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

        /// <summary>
        /// Fetches the data from persons node.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="item">The item.</param>
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
                                item.AddPeople(GetPersonsFromXmlNode(reader.ReadSubtree()));
                                break;
                            }

                        default:
                            reader.Skip();
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Fetches from studios node.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="item">The item.</param>
        private void FetchFromStudiosNode(XmlReader reader, T item)
        {
            reader.MoveToContent();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Studio":
                            {
                                var studio = reader.ReadElementContentAsString();

                                if (!string.IsNullOrWhiteSpace(studio))
                                {
                                    item.AddStudio(studio);
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

        /// <summary>
        /// Fetches from parental rating node.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="item">The item.</param>
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
                                var ratingString = reader.ReadElementContentAsString();

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

        /// <summary>
        /// Gets the persons from XML node.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>IEnumerable{PersonInfo}.</returns>
        private IEnumerable<PersonInfo> GetPersonsFromXmlNode(XmlReader reader)
        {
            var names = new List<string>();
            var type = string.Empty;
            var role = string.Empty;

            reader.MoveToContent();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Name":
                            names.AddRange(SplitNames(reader.ReadElementContentAsString()));
                            break;

                        case "Type":
                            {
                                var val = reader.ReadElementContentAsString();

                                if (!string.IsNullOrWhiteSpace(val))
                                {
                                    type = val;
                                }
                                break;
                            }

                        case "Role":
                            {
                                var val = reader.ReadElementContentAsString();

                                if (!string.IsNullOrWhiteSpace(val))
                                {
                                    role = val;
                                }
                                break;
                            }

                        default:
                            reader.Skip();
                            break;
                    }
                }
            }

            return names.Select(n => new PersonInfo { Name = n, Role = role, Type = type });
        }

        /// <summary>
        /// Used to split names of comma or pipe delimeted genres and people
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>IEnumerable{System.String}.</returns>
        private IEnumerable<string> SplitNames(string value)
        {
            value = value ?? string.Empty;

            // Only split by comma if there is no pipe in the string
            // We have to be careful to not split names like Matthew, Jr.
            var separator = value.IndexOf('|') == -1 ? ',' : '|';

            value = value.Trim().Trim(separator);

            return string.IsNullOrWhiteSpace(value) ? new string[] { } : value.Split(separator, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
