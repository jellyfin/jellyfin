using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Extensions;
using MediaBrowser.Model.Logging;
using MediaBrowser.XbmcMetadata.Configuration;
using MediaBrowser.XbmcMetadata.Savers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;

namespace MediaBrowser.XbmcMetadata.Parsers
{
    public class BaseNfoParser<T>
        where T : BaseItem
    {
        /// <summary>
        /// The logger
        /// </summary>
        protected ILogger Logger { get; private set; }
        protected IProviderManager ProviderManager { get; private set; }

        private readonly CultureInfo _usCulture = new CultureInfo("en-US");
        private readonly IConfigurationManager _config;
        private Dictionary<string, string> _validProviderIds;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseNfoParser{T}" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="config">The configuration.</param>
        public BaseNfoParser(ILogger logger, IConfigurationManager config, IProviderManager providerManager)
        {
            Logger = logger;
            _config = config;
            ProviderManager = providerManager;
        }

        /// <summary>
        /// Fetches metadata for an item from one xml file
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="metadataFile">The metadata file.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="System.ArgumentNullException">
        /// </exception>
        public void Fetch(MetadataResult<T> item, string metadataFile, CancellationToken cancellationToken)
        {
            if (item == null)
            {
                throw new ArgumentNullException();
            }

            if (string.IsNullOrEmpty(metadataFile))
            {
                throw new ArgumentNullException();
            }

            var settings = new XmlReaderSettings
            {
                CheckCharacters = false,
                IgnoreProcessingInstructions = true,
                IgnoreComments = true,
                ValidationType = ValidationType.None
            };

            _validProviderIds = _validProviderIds = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

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
            _validProviderIds.Add("collectionnumber", "TmdbCollection");
            _validProviderIds.Add("tmdbcolid", "TmdbCollection");
            _validProviderIds.Add("imdb_id", "Imdb");

            Fetch(item, metadataFile, settings, cancellationToken);
        }

        protected virtual bool SupportsUrlAfterClosingXmlTag
        {
            get { return false; }
        }

        /// <summary>
        /// Fetches the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="metadataFile">The metadata file.</param>
        /// <param name="settings">The settings.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private void Fetch(MetadataResult<T> item, string metadataFile, XmlReaderSettings settings, CancellationToken cancellationToken)
        {
            if (!SupportsUrlAfterClosingXmlTag)
            {
                using (var streamReader = BaseNfoSaver.GetStreamReader(metadataFile))
                {
                    // Use XmlReader for best performance
                    using (var reader = XmlReader.Create(streamReader, settings))
                    {
                        item.ResetPeople();

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
                return;
            }

            using (var streamReader = BaseNfoSaver.GetStreamReader(metadataFile))
            {
                item.ResetPeople();

                // Need to handle a url after the xml data
                // http://kodi.wiki/view/NFO_files/movies

                var xml = streamReader.ReadToEnd();

                // Find last closing Tag
                // Need to do this in two steps to account for random > characters after the closing xml
                var index = xml.LastIndexOf(@"</", StringComparison.Ordinal);

                // If closing tag exists, move to end of Tag
                if (index != -1)
                {
                    index = xml.IndexOf('>', index);
                }

                if (index != -1)
                {
                    var endingXml = xml.Substring(index);

                    ParseProviderLinks(item.Item, endingXml);

                    // If the file is just an imdb url, don't go any further
                    if (index == 0)
                    {
                        return;
                    }

                    xml = xml.Substring(0, index + 1);
                }
                else
                {
                    // If the file is just an Imdb url, handle that

                    ParseProviderLinks(item.Item, xml);

                    return;
                }

                using (var ms = new MemoryStream())
                {
                    var bytes = Encoding.UTF8.GetBytes(xml);

                    ms.Write(bytes, 0, bytes.Length);
                    ms.Position = 0;

                    // These are not going to be valid xml so no sense in causing the provider to fail and spamming the log with exceptions
                    try
                    {
                        // Use XmlReader for best performance
                        using (var reader = XmlReader.Create(ms, settings))
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
                    catch (XmlException)
                    {

                    }
                }
            }
        }

        private void ParseProviderLinks(T item, string xml)
        {
            //Look for a match for the Regex pattern "tt" followed by 7 digits
            Match m = Regex.Match(xml, @"tt([0-9]{7})", RegexOptions.IgnoreCase);
            if (m.Success)
            {
                item.SetProviderId(MetadataProviders.Imdb, m.Value);
            }

            // Support Tmdb
            // http://www.themoviedb.org/movie/36557
            var srch = "themoviedb.org/movie/";
            var index = xml.IndexOf(srch, StringComparison.OrdinalIgnoreCase);

            if (index != -1)
            {
                var tmdbId = xml.Substring(index + srch.Length).TrimEnd('/');
                int value;
                if (!string.IsNullOrWhiteSpace(tmdbId) && int.TryParse(tmdbId, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
                {
                    item.SetProviderId(MetadataProviders.Tmdb, tmdbId);
                }
            }
        }

        protected virtual void FetchDataFromXmlNode(XmlReader reader, MetadataResult<T> itemResult)
        {
            var item = itemResult.Item;

            var userDataUserId = _config.GetNfoConfiguration().UserId;

            switch (reader.Name)
            {
                // DateCreated
                case "dateadded":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            DateTime added;
                            if (DateTime.TryParseExact(val, BaseNfoSaver.DateAddedFormat, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out added))
                            {
                                item.DateCreated = added.ToUniversalTime();
                            }
                            else if (DateTime.TryParse(val, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out added))
                            {
                                item.DateCreated = added.ToUniversalTime();
                            }
                            else
                            {
                                Logger.Warn("Invalid Added value found: " + val);
                            }
                        }
                        break;
                    }

                case "originaltitle":
                    {
                        var val = reader.ReadElementContentAsString();

                        var hasOriginalTitle = item as IHasOriginalTitle;
                        if (hasOriginalTitle != null)
                        {
                            if (!string.IsNullOrEmpty(hasOriginalTitle.OriginalTitle))
                            {
                                hasOriginalTitle.OriginalTitle = val;
                            }
                        }
                        break;
                    }

                case "type":
                    item.DisplayMediaType = reader.ReadElementContentAsString();
                    break;

                case "title":
                case "localtitle":
                    item.Name = reader.ReadElementContentAsString();
                    break;

                case "criticrating":
                    {
                        var text = reader.ReadElementContentAsString();

                        if (!string.IsNullOrEmpty(text))
                        {
                            float value;
                            if (float.TryParse(text, NumberStyles.Any, _usCulture, out value))
                            {
                                item.CriticRating = value;
                            }
                        }

                        break;
                    }

                case "budget":
                    {
                        var text = reader.ReadElementContentAsString();
                        var hasBudget = item as IHasBudget;
                        if (hasBudget != null)
                        {
                            double value;
                            if (double.TryParse(text, NumberStyles.Any, _usCulture, out value))
                            {
                                hasBudget.Budget = value;
                            }
                        }

                        break;
                    }

                case "revenue":
                    {
                        var text = reader.ReadElementContentAsString();
                        var hasBudget = item as IHasBudget;
                        if (hasBudget != null)
                        {
                            double value;
                            if (double.TryParse(text, NumberStyles.Any, _usCulture, out value))
                            {
                                hasBudget.Revenue = value;
                            }
                        }

                        break;
                    }

                case "metascore":
                    {
                        var text = reader.ReadElementContentAsString();
                        var hasMetascore = item as IHasMetascore;
                        if (hasMetascore != null)
                        {
                            float value;
                            if (float.TryParse(text, NumberStyles.Any, _usCulture, out value))
                            {
                                hasMetascore.Metascore = value;
                            }
                        }

                        break;
                    }

                case "awardsummary":
                    {
                        var text = reader.ReadElementContentAsString();
                        var hasAwards = item as IHasAwards;
                        if (hasAwards != null)
                        {
                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                hasAwards.AwardSummary = text;
                            }
                        }

                        break;
                    }

                case "sorttitle":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            item.ForcedSortName = val;
                        }
                        break;
                    }

                case "outline":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            item.ShortOverview = val;
                        }
                        break;
                    }

                case "biography":
                case "plot":
                case "review":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            item.Overview = val;
                        }

                        break;
                    }

                case "criticratingsummary":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            item.CriticRatingSummary = val;
                        }

                        break;
                    }

                case "language":
                    {
                        var val = reader.ReadElementContentAsString();

                        item.PreferredMetadataLanguage = val;

                        break;
                    }

                case "countrycode":
                    {
                        var val = reader.ReadElementContentAsString();

                        item.PreferredMetadataCountryCode = val;

                        break;
                    }

                case "website":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            item.HomePageUrl = val;
                        }

                        break;
                    }

                case "lockedfields":
                    {
                        var fields = new List<MetadataFields>();

                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            var list = val.Split('|').Select(i =>
                            {
                                MetadataFields field;

                                if (Enum.TryParse<MetadataFields>(i, true, out field))
                                {
                                    return (MetadataFields?)field;
                                }

                                return null;

                            }).Where(i => i.HasValue).Select(i => i.Value);

                            fields.AddRange(list);
                        }

                        item.LockedFields = fields;

                        break;
                    }

                case "tagline":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            item.Tagline = val;
                        }
                        break;
                    }

                case "country":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            item.ProductionLocations = val.Split('/')
                                .Select(i => i.Trim())
                                .Where(i => !string.IsNullOrWhiteSpace(i))
                                .ToList();
                        }
                        break;
                    }

                case "mpaa":
                    {
                        var rating = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(rating))
                        {
                            item.OfficialRating = rating;
                        }
                        break;
                    }

                case "mpaadescription":
                    {
                        var rating = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(rating))
                        {
                            item.OfficialRatingDescription = rating;
                        }
                        break;
                    }

                case "customrating":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            item.CustomRating = val;
                        }
                        break;
                    }

                case "runtime":
                    {
                        var text = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            int runtime;
                            if (int.TryParse(text.Split(' ')[0], NumberStyles.Integer, _usCulture, out runtime))
                            {
                                item.RunTimeTicks = TimeSpan.FromMinutes(runtime).Ticks;
                            }
                        }
                        break;
                    }

                case "aspectratio":
                    {
                        var val = reader.ReadElementContentAsString();

                        var hasAspectRatio = item as IHasAspectRatio;
                        if (!string.IsNullOrWhiteSpace(val) && hasAspectRatio != null)
                        {
                            hasAspectRatio.AspectRatio = val;
                        }
                        break;
                    }

                case "lockdata":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            item.IsLocked = string.Equals("true", val, StringComparison.OrdinalIgnoreCase);
                        }
                        break;
                    }

                case "studio":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            var parts = val.Split('/')
                                .Select(i => i.Trim())
                                .Where(i => !string.IsNullOrWhiteSpace(i));

                            foreach (var p in parts)
                            {
                                item.AddStudio(p);
                            }
                        }
                        break;
                    }

                case "director":
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
                case "credits":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            var parts = val.Split('/').Select(i => i.Trim())
                                .Where(i => !string.IsNullOrEmpty(i));

                            foreach (var p in parts.Select(v => new PersonInfo { Name = v.Trim(), Type = PersonType.Writer }))
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

                case "writer":
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

                case "actor":
                    {
                        using (var subtree = reader.ReadSubtree())
                        {
                            var person = GetPersonFromXmlNode(subtree);

                            if (!string.IsNullOrWhiteSpace(person.Name))
                            {
                                itemResult.AddPerson(person);
                            }
                        }
                        break;
                    }

                case "trailer":
                    {
                        var val = reader.ReadElementContentAsString();

                        var hasTrailer = item as IHasTrailers;
                        if (hasTrailer != null)
                        {
                            if (!string.IsNullOrWhiteSpace(val))
                            {
                                val = val.Replace("plugin://plugin.video.youtube/?action=play_video&videoid=", "https://www.youtube.com/watch?v=", StringComparison.OrdinalIgnoreCase);

                                hasTrailer.AddTrailerUrl(val, false);
                            }
                        }
                        break;
                    }

                case "displayorder":
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

                case "year":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            int productionYear;
                            if (int.TryParse(val, out productionYear) && productionYear > 1850)
                            {
                                item.ProductionYear = productionYear;
                            }
                        }

                        break;
                    }

                case "rating":
                    {

                        var rating = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(rating))
                        {
                            float val;
                            // All external meta is saving this as '.' for decimal I believe...but just to be sure
                            if (float.TryParse(rating.Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out val))
                            {
                                item.CommunityRating = val;
                            }
                        }
                        break;
                    }

                case "aired":
                case "formed":
                case "premiered":
                case "releasedate":
                    {
                        var formatString = _config.GetNfoConfiguration().ReleaseDateFormat;

                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            DateTime date;

                            if (DateTime.TryParseExact(val, formatString, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out date) && date.Year > 1850)
                            {
                                item.PremiereDate = date.ToUniversalTime();
                                item.ProductionYear = date.Year;
                            }
                        }

                        break;
                    }

                case "enddate":
                    {
                        var formatString = _config.GetNfoConfiguration().ReleaseDateFormat;

                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            DateTime date;

                            if (DateTime.TryParseExact(val, formatString, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out date) && date.Year > 1850)
                            {
                                item.EndDate = date.ToUniversalTime();
                            }
                        }

                        break;
                    }

                case "votes":
                    {
                        var val = reader.ReadElementContentAsString();
                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            int num;

                            if (int.TryParse(val, NumberStyles.Integer, _usCulture, out num))
                            {
                                item.VoteCount = num;
                            }
                        }
                        break;
                    }

                case "genre":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            var parts = val.Split('/')
                                .Select(i => i.Trim())
                                .Where(i => !string.IsNullOrWhiteSpace(i));

                            foreach (var p in parts)
                            {
                                item.AddGenre(p);
                            }
                        }
                        break;
                    }

                case "style":
                case "tag":
                    {
                        var val = reader.ReadElementContentAsString();
                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            item.AddTag(val);
                        }
                        break;
                    }

                case "plotkeyword":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            item.AddKeyword(val);
                        }
                        break;
                    }

                case "fileinfo":
                    {
                        using (var subtree = reader.ReadSubtree())
                        {
                            FetchFromFileInfoNode(subtree, item);
                        }
                        break;
                    }

                case "watched":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val) && !string.IsNullOrWhiteSpace(userDataUserId))
                        {
                            bool parsedValue;
                            if (bool.TryParse(val, out parsedValue))
                            {
                                var userData = GetOrAdd(itemResult, userDataUserId);

                                userData.Played = parsedValue;
                            }
                        }
                        break;
                    }

                case "playcount":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val) && !string.IsNullOrWhiteSpace(userDataUserId))
                        {
                            int parsedValue;
                            if (int.TryParse(val, NumberStyles.Integer, _usCulture, out parsedValue))
                            {
                                var userData = GetOrAdd(itemResult, userDataUserId);

                                userData.PlayCount = parsedValue;

                                if (parsedValue > 0)
                                {
                                    userData.Played = true;
                                }
                            }
                        }
                        break;
                    }

                case "lastplayed":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val) && !string.IsNullOrWhiteSpace(userDataUserId))
                        {
                            DateTime parsedValue;
                            if (DateTime.TryParseExact(val, "yyyy-MM-dd HH:mm:ss", _usCulture, DateTimeStyles.AssumeLocal, out parsedValue))
                            {
                                var userData = GetOrAdd(itemResult, userDataUserId);

                                userData.LastPlayedDate = parsedValue.ToUniversalTime();
                            }
                        }
                        break;
                    }

                case "resume":
                    {
                        using (var subtree = reader.ReadSubtree())
                        {
                            if (!string.IsNullOrWhiteSpace(userDataUserId))
                            {
                                var userData = GetOrAdd(itemResult, userDataUserId);

                                FetchFromResumeNode(subtree, item, userData);
                            }
                        }
                        break;
                    }

                case "isuserfavorite":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val) && !string.IsNullOrWhiteSpace(userDataUserId))
                        {
                            bool parsedValue;
                            if (bool.TryParse(val, out parsedValue))
                            {
                                var userData = GetOrAdd(itemResult, userDataUserId);

                                userData.IsFavorite = parsedValue;
                            }
                        }
                        break;
                    }

                case "userrating":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val) && !string.IsNullOrWhiteSpace(userDataUserId))
                        {
                            double parsedValue;
                            if (double.TryParse(val, NumberStyles.Any, _usCulture, out parsedValue))
                            {
                                var userData = GetOrAdd(itemResult, userDataUserId);

                                userData.Rating = parsedValue;
                            }
                        }
                        break;
                    }

                default:
                    reader.Skip();
                    break;
            }
        }

        private UserItemData GetOrAdd(MetadataResult<T> result, string userId)
        {
            return result.GetOrAddUserData(userId);
        }

        private void FetchFromResumeNode(XmlReader reader, T item, UserItemData userData)
        {
            reader.MoveToContent();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "position":
                            {
                                var val = reader.ReadElementContentAsString();

                                if (!string.IsNullOrWhiteSpace(val))
                                {
                                    double parsedValue;
                                    if (double.TryParse(val, NumberStyles.Any, _usCulture, out parsedValue))
                                    {
                                        userData.PlaybackPositionTicks = TimeSpan.FromSeconds(parsedValue).Ticks;
                                    }
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

        private void FetchFromFileInfoNode(XmlReader reader, T item)
        {
            reader.MoveToContent();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "streamdetails":
                            {
                                using (var subtree = reader.ReadSubtree())
                                {
                                    FetchFromStreamDetailsNode(subtree, item);
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

        private void FetchFromStreamDetailsNode(XmlReader reader, T item)
        {
            reader.MoveToContent();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "video":
                            {
                                using (var subtree = reader.ReadSubtree())
                                {
                                    FetchFromVideoNode(subtree, item);
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

        private void FetchFromVideoNode(XmlReader reader, T item)
        {
            reader.MoveToContent();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "format3d":
                            {
                                var video = item as Video;

                                if (video != null)
                                {
                                    var val = reader.ReadElementContentAsString();

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
        private PersonInfo GetPersonFromXmlNode(XmlReader reader)
        {
            var name = string.Empty;
            var type = PersonType.Actor;  // If type is not specified assume actor
            var role = string.Empty;
            int? sortOrder = null;

            reader.MoveToContent();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "name":
                            name = reader.ReadElementContentAsString() ?? string.Empty;
                            break;

                        case "type":
                            {
                                var val = reader.ReadElementContentAsString();

                                if (!string.IsNullOrWhiteSpace(val))
                                {
                                    type = val;
                                }
                                break;
                            }

                        case "role":
                            {
                                var val = reader.ReadElementContentAsString();

                                if (!string.IsNullOrWhiteSpace(val))
                                {
                                    role = val;
                                }
                                break;
                            }
                        case "sortorder":
                            {
                                var val = reader.ReadElementContentAsString();

                                if (!string.IsNullOrWhiteSpace(val))
                                {
                                    int intVal;
                                    if (int.TryParse(val, NumberStyles.Integer, _usCulture, out intVal))
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
            }

            return new PersonInfo
            {
                Name = name.Trim(),
                Role = role,
                Type = type,
                SortOrder = sortOrder
            };
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

            return string.IsNullOrWhiteSpace(value) ? new string[] { } : Split(value, separator, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Provides an additional overload for string.split
        /// </summary>
        /// <param name="val">The val.</param>
        /// <param name="separators">The separators.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String[][].</returns>
        private static string[] Split(string val, char[] separators, StringSplitOptions options)
        {
            return val.Split(separators, options);
        }
    }
}
