using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.LocalMetadata.Parsers
{
    /// <summary>
    /// Provides a base class for parsing metadata xml
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class BaseItemXmlParser<T>
        where T : BaseItem
    {
        /// <summary>
        /// The logger
        /// </summary>
        protected ILogger Logger { get; private set; }
        protected IProviderManager ProviderManager { get; private set; }

        private Dictionary<string, string> _validProviderIds;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseItemXmlParser{T}" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public BaseItemXmlParser(ILogger logger, IProviderManager providerManager)
        {
            Logger = logger;
            ProviderManager = providerManager;
        }

        /// <summary>
        /// Fetches metadata for an item from one xml file
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="metadataFile">The metadata file.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public void Fetch(MetadataResult<T> item, string metadataFile, CancellationToken cancellationToken)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (string.IsNullOrEmpty(metadataFile))
            {
                throw new ArgumentException("The metadata file was empty or null.", nameof(metadataFile));
            }

            var settings = new XmlReaderSettings()
            {
                ValidationType = ValidationType.None,
                CheckCharacters = false,
                IgnoreProcessingInstructions = true,
                IgnoreComments = true
            };

            _validProviderIds = _validProviderIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var idInfos = ProviderManager.GetExternalIdInfos(item.Item);

            foreach (var info in idInfos)
            {
                var id = info.Key + "Id";
                if (!_validProviderIds.ContainsKey(id))
                {
                    _validProviderIds.Add(id, info.Key);
                }
            }

            //Additional Mappings
            _validProviderIds.Add("IMDB", "Imdb");

            //Fetch(item, metadataFile, settings, Encoding.GetEncoding("ISO-8859-1"), cancellationToken);
            Fetch(item, metadataFile, settings, Encoding.UTF8, cancellationToken);
        }

        /// <summary>
        /// Fetches the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="metadataFile">The metadata file.</param>
        /// <param name="settings">The settings.</param>
        /// <param name="encoding">The encoding.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private void Fetch(MetadataResult<T> item, string metadataFile, XmlReaderSettings settings, Encoding encoding, CancellationToken cancellationToken)
        {
            item.ResetPeople();

            using (var fileStream = File.OpenRead(metadataFile))
            using (var streamReader = new StreamReader(fileStream, encoding))
            using (var reader = XmlReader.Create(streamReader, settings))
            {
                reader.MoveToContent();
                reader.Read();

                // Loop through each element
                while (!reader.EOF && reader.ReadState == ReadState.Interactive)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        FetchDataFromXmlNode(reader, item);
                    }
                    else
                    {
                        reader.Read();
                    }
                }
            }
        }

        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        /// <summary>
        /// Fetches metadata from one Xml Element
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="itemResult">The item result.</param>
        protected virtual void FetchDataFromXmlNode(XmlReader reader, MetadataResult<T> itemResult)
        {
            var item = itemResult.Item;

            switch (reader.Name)
            {
                // DateCreated
                case "Added":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            if (DateTime.TryParse(val, out var added))
                            {
                                item.DateCreated = added.ToUniversalTime();
                            }
                            else
                            {
                                Logger.LogWarning("Invalid Added value found: " + val);
                            }
                        }
                        break;
                    }

                case "OriginalTitle":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrEmpty(val))
                        {
                            item.OriginalTitle = val;
                        }
                        break;
                    }

                case "LocalTitle":
                    item.Name = reader.ReadElementContentAsString();
                    break;

                case "CriticRating":
                    {
                        var text = reader.ReadElementContentAsString();

                        if (!string.IsNullOrEmpty(text))
                        {
                            if (float.TryParse(text, NumberStyles.Any, _usCulture, out var value))
                            {
                                item.CriticRating = value;
                            }
                        }

                        break;
                    }

                case "SortTitle":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            item.ForcedSortName = val;
                        }
                        break;
                    }

                case "Overview":
                case "Description":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            item.Overview = val;
                        }

                        break;
                    }

                case "Language":
                    {
                        var val = reader.ReadElementContentAsString();

                        item.PreferredMetadataLanguage = val;

                        break;
                    }

                case "CountryCode":
                    {
                        var val = reader.ReadElementContentAsString();

                        item.PreferredMetadataCountryCode = val;

                        break;
                    }

                case "PlaceOfBirth":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            var person = item as Person;
                            if (person != null)
                            {
                                person.ProductionLocations = new string[] { val };
                            }
                        }

                        break;
                    }

                case "LockedFields":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            item.LockedFields = val.Split('|').Select(i =>
                            {
                                if (Enum.TryParse(i, true, out MetadataFields field))
                                {
                                    return (MetadataFields?)field;
                                }

                                return null;

                            }).Where(i => i.HasValue).Select(i => i.Value).ToArray();
                        }

                        break;
                    }

                case "TagLines":
                    {
                        if (!reader.IsEmptyElement)
                        {
                            using (var subtree = reader.ReadSubtree())
                            {
                                FetchFromTaglinesNode(subtree, item);
                            }
                        }
                        else
                        {
                            reader.Read();
                        }
                        break;
                    }

                case "Countries":
                    {
                        if (!reader.IsEmptyElement)
                        {
                            using (var subtree = reader.ReadSubtree())
                            {
                                FetchFromCountriesNode(subtree, item);
                            }
                        }
                        else
                        {
                            reader.Read();
                        }
                        break;
                    }

                case "ContentRating":
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

                case "RunningTime":
                    {
                        var text = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            if (int.TryParse(text.Split(' ')[0], NumberStyles.Integer, _usCulture, out var runtime))
                            {
                                item.RunTimeTicks = TimeSpan.FromMinutes(runtime).Ticks;
                            }
                        }
                        break;
                    }

                case "AspectRatio":
                    {
                        var val = reader.ReadElementContentAsString();

                        var hasAspectRatio = item as IHasAspectRatio;
                        if (!string.IsNullOrWhiteSpace(val) && hasAspectRatio != null)
                        {
                            hasAspectRatio.AspectRatio = val;
                        }
                        break;
                    }

                case "LockData":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            item.IsLocked = string.Equals("true", val, StringComparison.OrdinalIgnoreCase);
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
                        foreach (var p in SplitNames(reader.ReadElementContentAsString()).Select(v => new PersonInfo { Name = v.Trim(), Type = PersonType.Director }))
                        {
                            if (string.IsNullOrWhiteSpace(p.Name))
                            {
                                continue;
                            }
                            itemResult.AddPerson(p);
                        }
                        break;
                    }
                case "Writer":
                    {
                        foreach (var p in SplitNames(reader.ReadElementContentAsString()).Select(v => new PersonInfo { Name = v.Trim(), Type = PersonType.Writer }))
                        {
                            if (string.IsNullOrWhiteSpace(p.Name))
                            {
                                continue;
                            }
                            itemResult.AddPerson(p);
                        }
                        break;
                    }

                case "Actors":
                    {

                        var actors = reader.ReadInnerXml();

                        if (actors.Contains("<"))
                        {
                            // This is one of the mis-named "Actors" full nodes created by MB2
                            // Create a reader and pass it to the persons node processor
                            FetchDataFromPersonsNode(XmlReader.Create(new StringReader("<Persons>" + actors + "</Persons>")), itemResult);
                        }
                        else
                        {
                            // Old-style piped string
                            foreach (var p in SplitNames(actors).Select(v => new PersonInfo { Name = v.Trim(), Type = PersonType.Actor }))
                            {
                                if (string.IsNullOrWhiteSpace(p.Name))
                                {
                                    continue;
                                }
                                itemResult.AddPerson(p);
                            }
                        }
                        break;
                    }

                case "GuestStars":
                    {
                        foreach (var p in SplitNames(reader.ReadElementContentAsString()).Select(v => new PersonInfo { Name = v.Trim(), Type = PersonType.GuestStar }))
                        {
                            if (string.IsNullOrWhiteSpace(p.Name))
                            {
                                continue;
                            }
                            itemResult.AddPerson(p);
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

                case "DisplayOrder":
                    {
                        var val = reader.ReadElementContentAsString();

                        var hasDisplayOrder = item as IHasDisplayOrder;
                        if (hasDisplayOrder != null)
                        {
                            if (!string.IsNullOrWhiteSpace(val))
                            {
                                hasDisplayOrder.DisplayOrder = val;
                            }
                        }
                        break;
                    }

                case "Trailers":
                    {
                        if (!reader.IsEmptyElement)
                        {
                            using (var subtree = reader.ReadSubtree())
                            {
                                FetchDataFromTrailersNode(subtree, item);
                            }
                        }
                        else
                        {
                            reader.Read();
                        }
                        break;
                    }

                case "ProductionYear":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            if (int.TryParse(val, out var productionYear) && productionYear > 1850)
                            {
                                item.ProductionYear = productionYear;
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
                            // All external meta is saving this as '.' for decimal I believe...but just to be sure
                            if (float.TryParse(rating.Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var val))
                            {
                                item.CommunityRating = val;
                            }
                        }
                        break;
                    }

                case "BirthDate":
                case "PremiereDate":
                case "FirstAired":
                    {
                        var firstAired = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(firstAired))
                        {
                            if (DateTime.TryParseExact(firstAired, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var airDate) && airDate.Year > 1850)
                            {
                                item.PremiereDate = airDate.ToUniversalTime();
                                item.ProductionYear = airDate.Year;
                            }
                        }

                        break;
                    }

                case "DeathDate":
                case "EndDate":
                    {
                        var firstAired = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(firstAired))
                        {
                            if (DateTime.TryParseExact(firstAired, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var airDate) && airDate.Year > 1850)
                            {
                                item.EndDate = airDate.ToUniversalTime();
                            }
                        }

                        break;
                    }

                case "CollectionNumber":
                    var tmdbCollection = reader.ReadElementContentAsString();
                    if (!string.IsNullOrWhiteSpace(tmdbCollection))
                    {
                        item.SetProviderId(MetadataProviders.TmdbCollection, tmdbCollection);
                    }
                    break;

                case "Genres":
                    {
                        if (!reader.IsEmptyElement)
                        {
                            using (var subtree = reader.ReadSubtree())
                            {
                                FetchFromGenresNode(subtree, item);
                            }
                        }
                        else
                        {
                            reader.Read();
                        }
                        break;
                    }

                case "Tags":
                    {
                        if (!reader.IsEmptyElement)
                        {
                            using (var subtree = reader.ReadSubtree())
                            {
                                FetchFromTagsNode(subtree, item);
                            }
                        }
                        else
                        {
                            reader.Read();
                        }
                        break;
                    }

                case "Persons":
                    {
                        if (!reader.IsEmptyElement)
                        {
                            using (var subtree = reader.ReadSubtree())
                            {
                                FetchDataFromPersonsNode(subtree, itemResult);
                            }
                        }
                        else
                        {
                            reader.Read();
                        }
                        break;
                    }

                case "Studios":
                    {
                        if (!reader.IsEmptyElement)
                        {
                            using (var subtree = reader.ReadSubtree())
                            {
                                FetchFromStudiosNode(subtree, item);
                            }
                        }
                        else
                        {
                            reader.Read();
                        }
                        break;
                    }

                case "Shares":
                    {
                        if (!reader.IsEmptyElement)
                        {
                            using (var subtree = reader.ReadSubtree())
                            {
                                var hasShares = item as IHasShares;
                                if (hasShares != null)
                                {
                                    FetchFromSharesNode(subtree, hasShares);
                                }
                            }
                        }
                        else
                        {
                            reader.Read();
                        }
                        break;
                    }

                case "Format3D":
                    {
                        var val = reader.ReadElementContentAsString();

                        var video = item as Video;

                        if (video != null)
                        {
                            if (string.Equals("HSBS", val, StringComparison.OrdinalIgnoreCase))
                            {
                                video.Video3DFormat = Video3DFormat.HalfSideBySide;
                            }
                            else if (string.Equals("HTAB", val, StringComparison.OrdinalIgnoreCase))
                            {
                                video.Video3DFormat = Video3DFormat.HalfTopAndBottom;
                            }
                            else if (string.Equals("FTAB", val, StringComparison.OrdinalIgnoreCase))
                            {
                                video.Video3DFormat = Video3DFormat.FullTopAndBottom;
                            }
                            else if (string.Equals("FSBS", val, StringComparison.OrdinalIgnoreCase))
                            {
                                video.Video3DFormat = Video3DFormat.FullSideBySide;
                            }
                            else if (string.Equals("MVC", val, StringComparison.OrdinalIgnoreCase))
                            {
                                video.Video3DFormat = Video3DFormat.MVC;
                            }
                        }
                        break;
                    }

                default:
                    {
                        string readerName = reader.Name;
                        if (_validProviderIds.TryGetValue(readerName, out string providerIdValue))
                        {
                            var id = reader.ReadElementContentAsString();
                            if (!string.IsNullOrWhiteSpace(id))
                            {
                                item.SetProviderId(providerIdValue, id);
                            }
                        }
                        else
                        {
                            reader.Skip();
                        }

                        break;

                    }
            }
        }
        private void FetchFromSharesNode(XmlReader reader, IHasShares item)
        {
            var list = new List<Share>();

            reader.MoveToContent();
            reader.Read();

            // Loop through each element
            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Share":
                            {
                                if (reader.IsEmptyElement)
                                {
                                    reader.Read();
                                    continue;
                                }

                                using (var subReader = reader.ReadSubtree())
                                {
                                    var child = GetShare(subReader);

                                    if (child != null)
                                    {
                                        list.Add(child);
                                    }
                                }

                                break;
                            }
                        default:
                            {
                                reader.Skip();
                                break;
                            }
                    }
                }
                else
                {
                    reader.Read();
                }
            }

            item.Shares = list.ToArray();
        }

        private Share GetShareFromNode(XmlReader reader)
        {
            var share = new Share();

            reader.MoveToContent();
            reader.Read();

            // Loop through each element
            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "UserId":
                            {
                                share.UserId = reader.ReadElementContentAsString();
                                break;
                            }

                        case "CanEdit":
                            {
                                share.CanEdit = string.Equals(reader.ReadElementContentAsString(), true.ToString(), StringComparison.OrdinalIgnoreCase);
                                break;
                            }

                        default:
                            reader.Skip();
                            break;
                    }
                }
                else
                {
                    reader.Read();
                }
            }

            return share;
        }

        private void FetchFromCountriesNode(XmlReader reader, T item)
        {
            reader.MoveToContent();
            reader.Read();

            // Loop through each element
            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Country":
                            {
                                var val = reader.ReadElementContentAsString();

                                if (!string.IsNullOrWhiteSpace(val))
                                {
                                }
                                break;
                            }

                        default:
                            reader.Skip();
                            break;
                    }
                }
                else
                {
                    reader.Read();
                }
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
            reader.Read();

            // Loop through each element
            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
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
                                    item.Tagline = val;
                                }
                                break;
                            }
                        default:
                            reader.Skip();
                            break;
                    }
                }
                else
                {
                    reader.Read();
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
            reader.Read();

            // Loop through each element
            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
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
                else
                {
                    reader.Read();
                }
            }
        }

        private void FetchFromTagsNode(XmlReader reader, BaseItem item)
        {
            reader.MoveToContent();
            reader.Read();

            var tags = new List<string>();

            // Loop through each element
            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Tag":
                            {
                                var tag = reader.ReadElementContentAsString();

                                if (!string.IsNullOrWhiteSpace(tag))
                                {
                                    tags.Add(tag);
                                }
                                break;
                            }

                        default:
                            reader.Skip();
                            break;
                    }
                }
                else
                {
                    reader.Read();
                }
            }

            item.Tags = tags.Distinct(StringComparer.Ordinal).ToArray();
        }

        /// <summary>
        /// Fetches the data from persons node.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="item">The item.</param>
        private void FetchDataFromPersonsNode(XmlReader reader, MetadataResult<T> item)
        {
            reader.MoveToContent();
            reader.Read();

            // Loop through each element
            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Person":
                        case "Actor":
                            {
                                if (reader.IsEmptyElement)
                                {
                                    reader.Read();
                                    continue;
                                }
                                using (var subtree = reader.ReadSubtree())
                                {
                                    foreach (var person in GetPersonsFromXmlNode(subtree))
                                    {
                                        if (string.IsNullOrWhiteSpace(person.Name))
                                        {
                                            continue;
                                        }
                                        item.AddPerson(person);
                                    }
                                }
                                break;
                            }

                        default:
                            reader.Skip();
                            break;
                    }
                }
                else
                {
                    reader.Read();
                }
            }
        }

        private void FetchDataFromTrailersNode(XmlReader reader, T item)
        {
            reader.MoveToContent();
            reader.Read();

            // Loop through each element
            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Trailer":
                            {
                                var val = reader.ReadElementContentAsString();

                                if (!string.IsNullOrWhiteSpace(val))
                                {
                                    item.AddTrailerUrl(val);
                                }
                                break;
                            }

                        default:
                            reader.Skip();
                            break;
                    }
                }
                else
                {
                    reader.Read();
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
            reader.Read();

            // Loop through each element
            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
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
                else
                {
                    reader.Read();
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
            var name = string.Empty;
            var type = PersonType.Actor;  // If type is not specified assume actor
            var role = string.Empty;
            int? sortOrder = null;

            reader.MoveToContent();
            reader.Read();

            // Loop through each element
            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Name":
                            name = reader.ReadElementContentAsString() ?? string.Empty;
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
                        case "SortOrder":
                            {
                                var val = reader.ReadElementContentAsString();

                                if (!string.IsNullOrWhiteSpace(val))
                                {
                                    if (int.TryParse(val, NumberStyles.Integer, _usCulture, out var intVal))
                                    {
                                        sortOrder = intVal;
                                    }
                                }
                                break;
                            }

                        default:
                            reader.Skip();
                            break;
                    }
                }
                else
                {
                    reader.Read();
                }
            }

            var personInfo = new PersonInfo
            {
                Name = name.Trim(),
                Role = role,
                Type = type,
                SortOrder = sortOrder
            };

            return new[] { personInfo };
        }

        protected LinkedChild GetLinkedChild(XmlReader reader)
        {
            var linkedItem = new LinkedChild
            {
                Type = LinkedChildType.Manual
            };

            reader.MoveToContent();
            reader.Read();

            // Loop through each element
            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Path":
                            {
                                linkedItem.Path = reader.ReadElementContentAsString();
                                break;
                            }
                        case "ItemId":
                            {
                                linkedItem.LibraryItemId = reader.ReadElementContentAsString();
                                break;
                            }

                        default:
                            reader.Skip();
                            break;
                    }
                }
                else
                {
                    reader.Read();
                }
            }

            // This is valid
            if (!string.IsNullOrWhiteSpace(linkedItem.Path) || !string.IsNullOrWhiteSpace(linkedItem.LibraryItemId))
            {
                return linkedItem;
            }

            return null;
        }

        protected Share GetShare(XmlReader reader)
        {
            var item = new Share();

            reader.MoveToContent();
            reader.Read();

            // Loop through each element
            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "UserId":
                            {
                                item.UserId = reader.ReadElementContentAsString();
                                break;
                            }

                        case "CanEdit":
                            {
                                item.CanEdit = string.Equals(reader.ReadElementContentAsString(), "true", StringComparison.OrdinalIgnoreCase);
                                break;
                            }
                        default:
                            {
                                reader.Skip();
                                break;
                            }
                    }
                }
                else
                {
                    reader.Read();
                }
            }

            // This is valid
            if (!string.IsNullOrWhiteSpace(item.UserId))
            {
                return item;
            }

            return null;
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
            var separator = value.IndexOf('|') == -1 && value.IndexOf(';') == -1 ? new[] { ',' } : new[] { '|', ';' };

            value = value.Trim().Trim(separator);

            return string.IsNullOrWhiteSpace(value) ? Array.Empty<string>() : Split(value, separator, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Provides an additional overload for string.split
        /// </summary>
        /// <param name="val">The val.</param>
        /// <param name="separators">The separators.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String[][].</returns>
        private string[] Split(string val, char[] separators, StringSplitOptions options)
        {
            return val.Split(separators, options);
        }

    }
}
