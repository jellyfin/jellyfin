#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.XbmcMetadata.Configuration;
using MediaBrowser.XbmcMetadata.Savers;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.XbmcMetadata.Parsers
{
    public class BaseNfoParser<T>
        where T : BaseItem
    {
        private readonly IConfigurationManager _config;
        private Dictionary<string, string> _validProviderIds;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseNfoParser{T}" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="config">the configuration manager.</param>
        /// <param name="providerManager">The provider manager.</param>
        public BaseNfoParser(ILogger logger, IConfigurationManager config, IProviderManager providerManager)
        {
            Logger = logger;
            _config = config;
            ProviderManager = providerManager;
        }

        protected CultureInfo UsCulture { get; } = new CultureInfo("en-US");

        /// <summary>
        /// Gets the logger.
        /// </summary>
        protected ILogger Logger { get; }

        protected IProviderManager ProviderManager { get; }

        protected virtual bool SupportsUrlAfterClosingXmlTag => false;

        protected virtual string MovieDbParserSearchString => "themoviedb.org/movie/";

        /// <summary>
        /// Fetches metadata for an item from one xml file.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="metadataFile">The metadata file.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ArgumentNullException"><c>item</c> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><c>metadataFile</c> is <c>null</c> or empty.</exception>
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

            // Additional Mappings
            _validProviderIds.Add("collectionnumber", "TmdbCollection");
            _validProviderIds.Add("tmdbcolid", "TmdbCollection");
            _validProviderIds.Add("imdb_id", "Imdb");

            Fetch(item, metadataFile, GetXmlReaderSettings(), cancellationToken);
        }

        /// <summary>
        /// Fetches the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="metadataFile">The metadata file.</param>
        /// <param name="settings">The settings.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        protected virtual void Fetch(MetadataResult<T> item, string metadataFile, XmlReaderSettings settings, CancellationToken cancellationToken)
        {
            if (!SupportsUrlAfterClosingXmlTag)
            {
                using (var fileStream = File.OpenRead(metadataFile))
                using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
                using (var reader = XmlReader.Create(streamReader, settings))
                {
                    item.ResetPeople();

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

                return;
            }

            using (var fileStream = File.OpenRead(metadataFile))
            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
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

                // These are not going to be valid xml so no sense in causing the provider to fail and spamming the log with exceptions
                try
                {
                    using (var stringReader = new StringReader(xml))
                    using (var reader = XmlReader.Create(stringReader, settings))
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
                catch (XmlException)
                {
                }
            }
        }

        protected void ParseProviderLinks(T item, string xml)
        {
            // Look for a match for the Regex pattern "tt" followed by 7 or 8 digits
            var m = Regex.Match(xml, "tt([0-9]{7,8})", RegexOptions.IgnoreCase);
            if (m.Success)
            {
                item.SetProviderId(MetadataProviders.Imdb, m.Value);
            }

            // Support Tmdb
            // https://www.themoviedb.org/movie/30287-fallo
            var srch = MovieDbParserSearchString;
            var index = xml.IndexOf(srch, StringComparison.OrdinalIgnoreCase);

            if (index != -1)
            {
                var tmdbId = xml.Substring(index + srch.Length).TrimEnd('/').Split('-')[0];
                if (!string.IsNullOrWhiteSpace(tmdbId) && int.TryParse(tmdbId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                {
                    item.SetProviderId(MetadataProviders.Tmdb, value.ToString(UsCulture));
                }
            }

            if (item is Series)
            {
                srch = "thetvdb.com/?tab=series&id=";

                index = xml.IndexOf(srch, StringComparison.OrdinalIgnoreCase);

                if (index != -1)
                {
                    var tvdbId = xml.Substring(index + srch.Length).TrimEnd('/');
                    if (!string.IsNullOrWhiteSpace(tvdbId) && int.TryParse(tvdbId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                    {
                        item.SetProviderId(MetadataProviders.Tvdb, value.ToString(UsCulture));
                    }
                }
            }
        }

        protected virtual void FetchDataFromXmlNode(XmlReader reader, MetadataResult<T> itemResult)
        {
            var item = itemResult.Item;

            switch (reader.Name)
            {
                // DateCreated
                case "dateadded":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            if (DateTime.TryParseExact(val, BaseNfoSaver.DateAddedFormat, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var added))
                            {
                                item.DateCreated = added.ToUniversalTime();
                            }
                            else if (DateTime.TryParse(val, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out added))
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

                case "originaltitle":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrEmpty(val))
                        {
                            item.OriginalTitle = val;
                        }

                        break;
                    }

                case "title":
                case "localtitle":
                    item.Name = reader.ReadElementContentAsString();
                    break;

                case "criticrating":
                    {
                        var text = reader.ReadElementContentAsString();

                        if (!string.IsNullOrEmpty(text))
                        {
                            if (float.TryParse(text, NumberStyles.Any, UsCulture, out var value))
                            {
                                item.CriticRating = value;
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

                case "lockedfields":
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
                                .ToArray();
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
                            if (int.TryParse(text.Split(' ')[0], NumberStyles.Integer, UsCulture, out var runtime))
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
                            item.AddStudio(val);
                        }

                        break;
                    }

                case "director":
                    {
                        var val = reader.ReadElementContentAsString();
                        foreach (var p in SplitNames(val).Select(v => new PersonInfo { Name = v.Trim(), Type = PersonType.Director }))
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
                        var val = reader.ReadElementContentAsString();
                        foreach (var p in SplitNames(val).Select(v => new PersonInfo { Name = v.Trim(), Type = PersonType.Writer }))
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
                        if (!reader.IsEmptyElement)
                        {
                            using (var subtree = reader.ReadSubtree())
                            {
                                var person = GetPersonFromXmlNode(subtree);

                                if (!string.IsNullOrWhiteSpace(person.Name))
                                {
                                    itemResult.AddPerson(person);
                                }
                            }
                        }
                        else
                        {
                            reader.Read();
                        }

                        break;
                    }

                case "trailer":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            val = val.Replace("plugin://plugin.video.youtube/?action=play_video&videoid=", BaseNfoSaver.YouTubeWatchUrl, StringComparison.OrdinalIgnoreCase);

                            item.AddTrailerUrl(val);
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
                            if (int.TryParse(val, out var productionYear) && productionYear > 1850)
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
                            // All external meta is saving this as '.' for decimal I believe...but just to be sure
                            if (float.TryParse(rating.Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var val))
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
                            if (DateTime.TryParseExact(val, formatString, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var date) && date.Year > 1850)
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
                            if (DateTime.TryParseExact(val, formatString, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var date) && date.Year > 1850)
                            {
                                item.EndDate = date.ToUniversalTime();
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

                case "fileinfo":
                    {
                        if (!reader.IsEmptyElement)
                        {
                            using (var subtree = reader.ReadSubtree())
                            {
                                FetchFromFileInfoNode(subtree, item);
                            }
                        }
                        else
                        {
                            reader.Read();
                        }

                        break;
                    }

                default:
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

        private void FetchFromFileInfoNode(XmlReader reader, T item)
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
                        case "streamdetails":
                            {
                                if (reader.IsEmptyElement)
                                {
                                    reader.Read();
                                    continue;
                                }

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
                else
                {
                    reader.Read();
                }
            }
        }

        private void FetchFromStreamDetailsNode(XmlReader reader, T item)
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
                        case "video":
                            {
                                if (reader.IsEmptyElement)
                                {
                                    reader.Read();
                                    continue;
                                }

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
                else
                {
                    reader.Read();
                }
            }
        }

        private void FetchFromVideoNode(XmlReader reader, T item)
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
                        case "format3d":
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
        private PersonInfo GetPersonFromXmlNode(XmlReader reader)
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
                        case "name":
                            name = reader.ReadElementContentAsString() ?? string.Empty;
                            break;

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
                                    if (int.TryParse(val, NumberStyles.Integer, UsCulture, out var intVal))
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

            return new PersonInfo
            {
                Name = name.Trim(),
                Role = role,
                Type = type,
                SortOrder = sortOrder
            };
        }

        internal XmlReaderSettings GetXmlReaderSettings()
            => new XmlReaderSettings()
            {
                ValidationType = ValidationType.None,
                CheckCharacters = false,
                IgnoreProcessingInstructions = true,
                IgnoreComments = true
            };

        /// <summary>
        /// Used to split names of comma or pipe delimeted genres and people.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>IEnumerable{System.String}.</returns>
        private IEnumerable<string> SplitNames(string value)
        {
            value = value ?? string.Empty;

            // Only split by comma if there is no pipe in the string
            // We have to be careful to not split names like Matthew, Jr.
            var separator = value.IndexOf('|', StringComparison.Ordinal) == -1 && value.IndexOf(';', StringComparison.Ordinal) == -1
                ? new[] { ',' }
                : new[] { '|', ';' };

            value = value.Trim().Trim(separator);

            return string.IsNullOrWhiteSpace(value) ? Array.Empty<string>() : value.Split(separator, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
