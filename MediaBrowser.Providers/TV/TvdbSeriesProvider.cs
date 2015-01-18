using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Providers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace MediaBrowser.Providers.TV
{
    public class TvdbSeriesProvider : IRemoteMetadataProvider<Series, SeriesInfo>, IItemIdentityProvider<SeriesInfo, SeriesIdentity>, IHasOrder
    {
        private const string TvdbSeriesOffset = "TvdbSeriesOffset";
        private const string TvdbSeriesOffsetFormat = "{0}-{1}";

        internal readonly SemaphoreSlim TvDbResourcePool = new SemaphoreSlim(2, 2);
        internal static TvdbSeriesProvider Current { get; private set; }
        private readonly IZipClient _zipClient;
        private readonly IHttpClient _httpClient;
        private readonly IFileSystem _fileSystem;
        private readonly IServerConfigurationManager _config;
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");
        private readonly ILogger _logger;
        private readonly ISeriesOrderManager _seriesOrder;
        private readonly ILibraryManager _libraryManager;

        public TvdbSeriesProvider(IZipClient zipClient, IHttpClient httpClient, IFileSystem fileSystem, IServerConfigurationManager config, ILogger logger, ISeriesOrderManager seriesOrder, ILibraryManager libraryManager)
        {
            _zipClient = zipClient;
            _httpClient = httpClient;
            _fileSystem = fileSystem;
            _config = config;
            _logger = logger;
            _seriesOrder = seriesOrder;
            _libraryManager = libraryManager;
            Current = this;
        }

        private const string RootUrl = "http://www.thetvdb.com/api/";
        private const string SeriesQuery = "GetSeries.php?seriesname={0}";
        private const string SeriesGetZip = "http://www.thetvdb.com/api/{0}/series/{1}/all/{2}.zip";

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(SeriesInfo searchInfo, CancellationToken cancellationToken)
        {
            var seriesId = searchInfo.GetProviderId(MetadataProviders.Tvdb);

            if (string.IsNullOrEmpty(seriesId))
            {
                return await FindSeries(searchInfo.Name, cancellationToken).ConfigureAwait(false);
            }

            var metadata = await GetMetadata(searchInfo, cancellationToken).ConfigureAwait(false);

            var list = new List<RemoteSearchResult>();

            if (metadata.HasMetadata)
            {
                var res = new RemoteSearchResult
                {
                    Name = metadata.Item.Name,
                    PremiereDate = metadata.Item.PremiereDate,
                    ProductionYear = metadata.Item.ProductionYear,
                    ProviderIds = metadata.Item.ProviderIds,
                    SearchProviderName = Name
                };

                list.Add(res);
            }

            return list;
        }

        public async Task<MetadataResult<Series>> GetMetadata(SeriesInfo itemId, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Series>();

            var seriesId = itemId.GetProviderId(MetadataProviders.Tvdb);

            if (string.IsNullOrEmpty(seriesId))
            {
                seriesId = itemId.Identities
                                 .Where(id => id.Type == MetadataProviders.Tvdb.ToString())
                                 .Select(id => id.Id)
                                 .FirstOrDefault();

                if (string.IsNullOrEmpty(seriesId))
                {
                    var srch = await GetSearchResults(itemId, cancellationToken).ConfigureAwait(false);

                    var entry = srch.FirstOrDefault();

                    if (entry != null)
                    {
                        seriesId = entry.GetProviderId(MetadataProviders.Tvdb);
                    }
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (!string.IsNullOrEmpty(seriesId))
            {
                await EnsureSeriesInfo(seriesId, itemId.MetadataLanguage, cancellationToken).ConfigureAwait(false);

                result.Item = new Series();
                result.HasMetadata = true;

                FetchSeriesData(result.Item, seriesId, cancellationToken);
                await FindAnimeSeriesIndex(result.Item, itemId).ConfigureAwait(false);
            }

            return result;
        }

        private async Task FindAnimeSeriesIndex(Series series, SeriesInfo info)
        {
            var index = await _seriesOrder.FindSeriesIndex(SeriesOrderTypes.Anime, series.Name);
            if (index == null)
                return;

            var offset = info.AnimeSeriesIndex - index;
            var id = string.Format(TvdbSeriesOffsetFormat, series.GetProviderId(MetadataProviders.Tvdb), offset);
            series.SetProviderId(TvdbSeriesOffset, id);
        }

        internal static int? GetSeriesOffset(Dictionary<string, string> seriesProviderIds)
        {
            string idString;
            if (!seriesProviderIds.TryGetValue(TvdbSeriesOffset, out idString))
                return null;

            var parts = idString.Split('-');
            if (parts.Length < 2)
                return null;

            int offset;
            if (int.TryParse(parts[1], out offset))
                return offset;

            return null;
        }

        /// <summary>
        /// Fetches the series data.
        /// </summary>
        /// <param name="series">The series.</param>
        /// <param name="seriesId">The series id.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.Boolean}.</returns>
        private void FetchSeriesData(Series series, string seriesId, CancellationToken cancellationToken)
        {
            series.SetProviderId(MetadataProviders.Tvdb, seriesId);

            var seriesDataPath = GetSeriesDataPath(_config.ApplicationPaths, seriesId);

            var seriesXmlFilename = series.GetPreferredMetadataLanguage().ToLower() + ".xml";

            var seriesXmlPath = Path.Combine(seriesDataPath, seriesXmlFilename);
            var actorsXmlPath = Path.Combine(seriesDataPath, "actors.xml");

            FetchSeriesInfo(series, seriesXmlPath, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            FetchActors(series, actorsXmlPath);
        }

        /// <summary>
        /// Downloads the series zip.
        /// </summary>
        /// <param name="seriesId">The series id.</param>
        /// <param name="seriesDataPath">The series data path.</param>
        /// <param name="lastTvDbUpdateTime">The last tv database update time.</param>
        /// <param name="preferredMetadataLanguage">The preferred metadata language.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        internal async Task DownloadSeriesZip(string seriesId, string seriesDataPath, long? lastTvDbUpdateTime, string preferredMetadataLanguage, CancellationToken cancellationToken)
        {
            var url = string.Format(SeriesGetZip, TVUtils.TvdbApiKey, seriesId, preferredMetadataLanguage);

            using (var zipStream = await _httpClient.Get(new HttpRequestOptions
            {
                Url = url,
                ResourcePool = TvDbResourcePool,
                CancellationToken = cancellationToken

            }).ConfigureAwait(false))
            {
                // Delete existing files
                DeleteXmlFiles(seriesDataPath);

                // Copy to memory stream because we need a seekable stream
                using (var ms = new MemoryStream())
                {
                    await zipStream.CopyToAsync(ms).ConfigureAwait(false);

                    ms.Position = 0;
                    _zipClient.ExtractAllFromZip(ms, seriesDataPath, true);
                }
            }

            // Sanitize all files, except for extracted episode files
            foreach (var file in Directory.EnumerateFiles(seriesDataPath, "*.xml", SearchOption.AllDirectories).ToList()
                .Where(i => !Path.GetFileName(i).StartsWith("episode-", StringComparison.OrdinalIgnoreCase)))
            {
                await SanitizeXmlFile(file).ConfigureAwait(false);
            }

            await ExtractEpisodes(seriesDataPath, Path.Combine(seriesDataPath, preferredMetadataLanguage + ".xml"), lastTvDbUpdateTime).ConfigureAwait(false);
        }

        public TvdbOptions GetTvDbOptions()
        {
            return _config.GetConfiguration<TvdbOptions>("tvdb");
        }

        private readonly Task _cachedTask = Task.FromResult(true);
        internal Task EnsureSeriesInfo(string seriesId, string preferredMetadataLanguage, CancellationToken cancellationToken)
        {
            var seriesDataPath = GetSeriesDataPath(_config.ApplicationPaths, seriesId);

            Directory.CreateDirectory(seriesDataPath);

            var files = new DirectoryInfo(seriesDataPath).EnumerateFiles("*.xml", SearchOption.TopDirectoryOnly)
                .ToList();

            var seriesXmlFilename = preferredMetadataLanguage + ".xml";

            var download = false;
            var automaticUpdatesEnabled = GetTvDbOptions().EnableAutomaticUpdates;

            const int cacheDays = 2;

            var seriesFile = files.FirstOrDefault(i => string.Equals(seriesXmlFilename, i.Name, StringComparison.OrdinalIgnoreCase));
            // No need to check age if automatic updates are enabled
            if (seriesFile == null || !seriesFile.Exists || (!automaticUpdatesEnabled && (DateTime.UtcNow - _fileSystem.GetLastWriteTimeUtc(seriesFile)).TotalDays > cacheDays))
            {
                download = true;
            }

            var actorsXml = files.FirstOrDefault(i => string.Equals("actors.xml", i.Name, StringComparison.OrdinalIgnoreCase));
            // No need to check age if automatic updates are enabled
            if (actorsXml == null || !actorsXml.Exists || (!automaticUpdatesEnabled && (DateTime.UtcNow - _fileSystem.GetLastWriteTimeUtc(actorsXml)).TotalDays > cacheDays))
            {
                download = true;
            }

            var bannersXml = files.FirstOrDefault(i => string.Equals("banners.xml", i.Name, StringComparison.OrdinalIgnoreCase));
            // No need to check age if automatic updates are enabled
            if (bannersXml == null || !bannersXml.Exists || (!automaticUpdatesEnabled && (DateTime.UtcNow - _fileSystem.GetLastWriteTimeUtc(bannersXml)).TotalDays > cacheDays))
            {
                download = true;
            }

            // Only download if not already there
            // The post-scan task will take care of updates so we don't need to re-download here
            if (download)
            {
                return DownloadSeriesZip(seriesId, seriesDataPath, null, preferredMetadataLanguage, cancellationToken);
            }

            return _cachedTask;
        }

        /// <summary>
        /// Finds the series.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.String}.</returns>
        private async Task<IEnumerable<RemoteSearchResult>> FindSeries(string name, CancellationToken cancellationToken)
        {
            var results = (await FindSeriesInternal(name, cancellationToken).ConfigureAwait(false)).ToList();

            if (results.Count == 0)
            {
                var parsedName = _libraryManager.ParseName(name);
                var nameWithoutYear = parsedName.Name;

                if (!string.IsNullOrEmpty(nameWithoutYear) && !string.Equals(nameWithoutYear, name, StringComparison.OrdinalIgnoreCase))
                {
                    results = (await FindSeriesInternal(nameWithoutYear, cancellationToken).ConfigureAwait(false)).ToList();
                }
            }

            return results;
        }

        private async Task<IEnumerable<RemoteSearchResult>> FindSeriesInternal(string name, CancellationToken cancellationToken)
        {
            // TODO: Support returning more data, including image url's for the identify function

            var url = string.Format(RootUrl + SeriesQuery, WebUtility.UrlEncode(name));
            var doc = new XmlDocument();

            using (var results = await _httpClient.Get(new HttpRequestOptions
            {
                Url = url,
                ResourcePool = TvDbResourcePool,
                CancellationToken = cancellationToken

            }).ConfigureAwait(false))
            {
                doc.Load(results);
            }

            var searchResults = new List<RemoteSearchResult>();

            if (doc.HasChildNodes)
            {
                var nodes = doc.SelectNodes("//Series");
                var comparableName = GetComparableName(name);
                if (nodes != null)
                {
                    foreach (XmlNode node in nodes)
                    {
                        var titles = new List<string>();

                        var nameNode = node.SelectSingleNode("./SeriesName");
                        if (nameNode != null)
                        {
                            titles.Add(GetComparableName(nameNode.InnerText));
                        }

                        var aliasNode = node.SelectSingleNode("./AliasNames");
                        if (aliasNode != null)
                        {
                            var alias = aliasNode.InnerText.Split('|').Select(GetComparableName);
                            titles.AddRange(alias);
                        }

                        if (titles.Any(t => string.Equals(t, comparableName, StringComparison.OrdinalIgnoreCase)))
                        {
                            var id = node.SelectSingleNode("./seriesid");
                            if (id != null)
                            {
                                var searchResult = new RemoteSearchResult
                                {
                                    Name = titles.FirstOrDefault(),
                                    SearchProviderName = Name
                                };

                                searchResult.SetProviderId(MetadataProviders.Tvdb, id.InnerText);

                                searchResults.Add(searchResult);
                            }
                        }

                        foreach (var title in titles)
                        {
                            _logger.Info("TVDb Provider - " + title + " did not match " + comparableName);
                        }
                    }
                }
            }

            _logger.Info("TVDb Provider - Could not find " + name + ". Check name on Thetvdb.org.");
            return searchResults;
        }

        /// <summary>
        /// The remove
        /// </summary>
        const string remove = "\"'!`?";
        /// <summary>
        /// The spacers
        /// </summary>
        const string spacers = "/,.:;\\(){}[]+-_=–*";  // (there are not actually two - in the they are different char codes)

        /// <summary>
        /// Gets the name of the comparable.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>System.String.</returns>
        internal static string GetComparableName(string name)
        {
            name = name.ToLower();
            name = name.Normalize(NormalizationForm.FormKD);
            var sb = new StringBuilder();
            foreach (var c in name)
            {
                if ((int)c >= 0x2B0 && (int)c <= 0x0333)
                {
                    // skip char modifier and diacritics 
                }
                else if (remove.IndexOf(c) > -1)
                {
                    // skip chars we are removing
                }
                else if (spacers.IndexOf(c) > -1)
                {
                    sb.Append(" ");
                }
                else if (c == '&')
                {
                    sb.Append(" and ");
                }
                else
                {
                    sb.Append(c);
                }
            }
            name = sb.ToString();
            name = name.Replace(", the", "");
            name = name.Replace("the ", " ");
            name = name.Replace(" the ", " ");

            string prevName;
            do
            {
                prevName = name;
                name = name.Replace("  ", " ");
            } while (name.Length != prevName.Length);

            return name.Trim();
        }

        private void FetchSeriesInfo(Series item, string seriesXmlPath, CancellationToken cancellationToken)
        {
            var settings = new XmlReaderSettings
            {
                CheckCharacters = false,
                IgnoreProcessingInstructions = true,
                IgnoreComments = true,
                ValidationType = ValidationType.None
            };

            var episiodeAirDates = new List<DateTime>();

            using (var streamReader = new StreamReader(seriesXmlPath, Encoding.UTF8))
            {
                // Use XmlReader for best performance
                using (var reader = XmlReader.Create(streamReader, settings))
                {
                    reader.MoveToContent();

                    // Loop through each element
                    while (reader.Read())
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            switch (reader.Name)
                            {
                                case "Series":
                                    {
                                        using (var subtree = reader.ReadSubtree())
                                        {
                                            FetchDataFromSeriesNode(item, subtree, cancellationToken);
                                        }
                                        break;
                                    }

                                case "Episode":
                                    {
                                        using (var subtree = reader.ReadSubtree())
                                        {
                                            var date = GetFirstAiredDateFromEpisodeNode(subtree, cancellationToken);

                                            if (date.HasValue)
                                            {
                                                episiodeAirDates.Add(date.Value);
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
            }

            if (item.Status.HasValue && item.Status.Value == SeriesStatus.Ended && episiodeAirDates.Count > 0)
            {
                item.EndDate = episiodeAirDates.Max();
            }
        }

        private DateTime? GetFirstAiredDateFromEpisodeNode(XmlReader reader, CancellationToken cancellationToken)
        {
            DateTime? airDate = null;
            int? seasonNumber = null;

            reader.MoveToContent();

            // Loop through each element
            while (reader.Read())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "FirstAired":
                            {
                                var val = reader.ReadElementContentAsString();

                                if (!string.IsNullOrWhiteSpace(val))
                                {
                                    DateTime date;
                                    if (DateTime.TryParse(val, out date))
                                    {
                                        airDate = date.ToUniversalTime();
                                    }
                                }

                                break;
                            }

                        case "SeasonNumber":
                            {
                                var val = reader.ReadElementContentAsString();

                                if (!string.IsNullOrWhiteSpace(val))
                                {
                                    int rval;

                                    // int.TryParse is local aware, so it can be probamatic, force us culture
                                    if (int.TryParse(val, NumberStyles.Integer, _usCulture, out rval))
                                    {
                                        seasonNumber = rval;
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

            if (seasonNumber.HasValue && seasonNumber.Value != 0)
            {
                return airDate;
            }

            return null;
        }

        /// <summary>
        /// Fetches the actors.
        /// </summary>
        /// <param name="series">The series.</param>
        /// <param name="actorsXmlPath">The actors XML path.</param>
        private void FetchActors(Series series, string actorsXmlPath)
        {
            var settings = new XmlReaderSettings
            {
                CheckCharacters = false,
                IgnoreProcessingInstructions = true,
                IgnoreComments = true,
                ValidationType = ValidationType.None
            };

            using (var streamReader = new StreamReader(actorsXmlPath, Encoding.UTF8))
            {
                // Use XmlReader for best performance
                using (var reader = XmlReader.Create(streamReader, settings))
                {
                    reader.MoveToContent();

                    // Loop through each element
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            switch (reader.Name)
                            {
                                case "Actor":
                                    {
                                        using (var subtree = reader.ReadSubtree())
                                        {
                                            FetchDataFromActorNode(series, subtree);
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
            }
        }

        /// <summary>
        /// Fetches the data from actor node.
        /// </summary>
        /// <param name="series">The series.</param>
        /// <param name="reader">The reader.</param>
        private void FetchDataFromActorNode(Series series, XmlReader reader)
        {
            reader.MoveToContent();

            var personInfo = new PersonInfo();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Name":
                            {
                                personInfo.Name = (reader.ReadElementContentAsString() ?? string.Empty).Trim();
                                break;
                            }

                        case "Role":
                            {
                                personInfo.Role = (reader.ReadElementContentAsString() ?? string.Empty).Trim();
                                break;
                            }

                        case "SortOrder":
                            {
                                var val = reader.ReadElementContentAsString();

                                if (!string.IsNullOrWhiteSpace(val))
                                {
                                    int rval;

                                    // int.TryParse is local aware, so it can be probamatic, force us culture
                                    if (int.TryParse(val, NumberStyles.Integer, _usCulture, out rval))
                                    {
                                        personInfo.SortOrder = rval;
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

            personInfo.Type = PersonType.Actor;

            if (!string.IsNullOrEmpty(personInfo.Name))
            {
                series.AddPerson(personInfo);
            }
        }

        private void FetchDataFromSeriesNode(Series item, XmlReader reader, CancellationToken cancellationToken)
        {
            reader.MoveToContent();

            // Loop through each element
            while (reader.Read())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "SeriesName":
                            {
                                item.Name = (reader.ReadElementContentAsString() ?? string.Empty).Trim();
                                break;
                            }

                        case "Overview":
                            {
                                item.Overview = (reader.ReadElementContentAsString() ?? string.Empty).Trim();
                                break;
                            }

                        case "Airs_DayOfWeek":
                            {
                                var val = reader.ReadElementContentAsString();

                                if (!string.IsNullOrWhiteSpace(val))
                                {
                                    item.AirDays = TVUtils.GetAirDays(val);
                                }
                                break;
                            }

                        case "Airs_Time":
                            {
                                var val = reader.ReadElementContentAsString();

                                if (!string.IsNullOrWhiteSpace(val))
                                {
                                    item.AirTime = val;
                                }
                                break;
                            }

                        case "ContentRating":
                            {
                                var val = reader.ReadElementContentAsString();

                                if (!string.IsNullOrWhiteSpace(val))
                                {
                                    item.OfficialRating = val;
                                }
                                break;
                            }

                        case "Rating":
                            {
                                var val = reader.ReadElementContentAsString();

                                if (!string.IsNullOrWhiteSpace(val))
                                {
                                    // Only fill this if it doesn't already have a value, since we get it from imdb which has better data
                                    if (!item.CommunityRating.HasValue || string.IsNullOrWhiteSpace(item.GetProviderId(MetadataProviders.Imdb)))
                                    {
                                        float rval;

                                        // float.TryParse is local aware, so it can be probamatic, force us culture
                                        if (float.TryParse(val, NumberStyles.AllowDecimalPoint, _usCulture, out rval))
                                        {
                                            item.CommunityRating = rval;
                                        }
                                    }
                                }
                                break;
                            }
                        case "RatingCount":
                            {
                                var val = reader.ReadElementContentAsString();

                                if (!string.IsNullOrWhiteSpace(val))
                                {
                                    int rval;

                                    // int.TryParse is local aware, so it can be probamatic, force us culture
                                    if (int.TryParse(val, NumberStyles.Integer, _usCulture, out rval))
                                    {
                                        item.VoteCount = rval;
                                    }
                                }

                                break;
                            }

                        case "IMDB_ID":
                            {
                                var val = reader.ReadElementContentAsString();

                                if (!string.IsNullOrWhiteSpace(val))
                                {
                                    item.SetProviderId(MetadataProviders.Imdb, val);
                                }

                                break;
                            }

                        case "zap2it_id":
                            {
                                var val = reader.ReadElementContentAsString();

                                if (!string.IsNullOrWhiteSpace(val))
                                {
                                    item.SetProviderId(MetadataProviders.Zap2It, val);
                                }

                                break;
                            }

                        case "Status":
                            {
                                var val = reader.ReadElementContentAsString();

                                if (!string.IsNullOrWhiteSpace(val))
                                {
                                    SeriesStatus seriesStatus;

                                    if (Enum.TryParse(val, true, out seriesStatus))
                                        item.Status = seriesStatus;
                                }

                                break;
                            }

                        case "FirstAired":
                            {
                                var val = reader.ReadElementContentAsString();

                                if (!string.IsNullOrWhiteSpace(val))
                                {
                                    DateTime date;
                                    if (DateTime.TryParse(val, out date))
                                    {
                                        date = date.ToUniversalTime();

                                        item.PremiereDate = date;
                                        item.ProductionYear = date.Year;
                                    }
                                }

                                break;
                            }

                        case "Runtime":
                            {
                                var val = reader.ReadElementContentAsString();

                                if (!string.IsNullOrWhiteSpace(val))
                                {
                                    int rval;

                                    // int.TryParse is local aware, so it can be probamatic, force us culture
                                    if (int.TryParse(val, NumberStyles.Integer, _usCulture, out rval))
                                    {
                                        item.RunTimeTicks = TimeSpan.FromMinutes(rval).Ticks;
                                    }
                                }

                                break;
                            }

                        case "Genre":
                            {
                                var val = reader.ReadElementContentAsString();

                                if (!string.IsNullOrWhiteSpace(val))
                                {
                                    var vals = val
                                        .Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)
                                        .Select(i => i.Trim())
                                        .Where(i => !string.IsNullOrWhiteSpace(i))
                                        .ToList();

                                    if (vals.Count > 0)
                                    {
                                        item.Genres.Clear();

                                        foreach (var genre in vals)
                                        {
                                            item.AddGenre(genre);
                                        }
                                    }
                                }

                                break;
                            }

                        case "Network":
                            {
                                var val = reader.ReadElementContentAsString();

                                if (!string.IsNullOrWhiteSpace(val))
                                {
                                    var vals = val
                                        .Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)
                                        .Select(i => i.Trim())
                                        .Where(i => !string.IsNullOrWhiteSpace(i))
                                        .ToList();

                                    if (vals.Count > 0)
                                    {
                                        item.Studios.Clear();

                                        foreach (var genre in vals)
                                        {
                                            item.AddStudio(genre);
                                        }
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
        /// Extracts info for each episode into invididual xml files so that they can be easily accessed without having to step through the entire series xml
        /// </summary>
        /// <param name="seriesDataPath">The series data path.</param>
        /// <param name="xmlFile">The XML file.</param>
        /// <param name="lastTvDbUpdateTime">The last tv db update time.</param>
        /// <returns>Task.</returns>
        private async Task ExtractEpisodes(string seriesDataPath, string xmlFile, long? lastTvDbUpdateTime)
        {
            var settings = new XmlReaderSettings
            {
                CheckCharacters = false,
                IgnoreProcessingInstructions = true,
                IgnoreComments = true,
                ValidationType = ValidationType.None
            };

            using (var streamReader = new StreamReader(xmlFile, Encoding.UTF8))
            {
                // Use XmlReader for best performance
                using (var reader = XmlReader.Create(streamReader, settings))
                {
                    reader.MoveToContent();

                    // Loop through each element
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            switch (reader.Name)
                            {
                                case "Episode":
                                    {
                                        var outerXml = reader.ReadOuterXml();

                                        await SaveEpsiodeXml(seriesDataPath, outerXml, lastTvDbUpdateTime).ConfigureAwait(false);
                                        break;
                                    }

                                default:
                                    reader.Skip();
                                    break;
                            }
                        }
                    }
                }
            }
        }

        private async Task SaveEpsiodeXml(string seriesDataPath, string xml, long? lastTvDbUpdateTime)
        {
            var settings = new XmlReaderSettings
            {
                CheckCharacters = false,
                IgnoreProcessingInstructions = true,
                IgnoreComments = true,
                ValidationType = ValidationType.None
            };

            var seasonNumber = -1;
            var episodeNumber = -1;
            var absoluteNumber = -1;
            var lastUpdateString = string.Empty;

            using (var streamReader = new StringReader(xml))
            {
                // Use XmlReader for best performance
                using (var reader = XmlReader.Create(streamReader, settings))
                {
                    reader.MoveToContent();

                    // Loop through each element
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            switch (reader.Name)
                            {
                                case "lastupdated":
                                    {
                                        lastUpdateString = reader.ReadElementContentAsString();
                                        break;
                                    }

                                case "EpisodeNumber":
                                    {
                                        var val = reader.ReadElementContentAsString();
                                        if (!string.IsNullOrWhiteSpace(val))
                                        {
                                            int num;
                                            if (int.TryParse(val, NumberStyles.Integer, _usCulture, out num))
                                            {
                                                episodeNumber = num;
                                            }
                                        }
                                        break;
                                    }

                                case "absolute_number":
                                    {
                                        var val = reader.ReadElementContentAsString();
                                        if (!string.IsNullOrWhiteSpace(val))
                                        {
                                            int num;
                                            if (int.TryParse(val, NumberStyles.Integer, _usCulture, out num))
                                            {
                                                absoluteNumber = num;
                                            }
                                        }
                                        break;
                                    }

                                case "SeasonNumber":
                                    {
                                        var val = reader.ReadElementContentAsString();
                                        if (!string.IsNullOrWhiteSpace(val))
                                        {
                                            int num;
                                            if (int.TryParse(val, NumberStyles.Integer, _usCulture, out num))
                                            {
                                                seasonNumber = num;
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
            }

            var hasEpisodeChanged = true;
            if (!string.IsNullOrEmpty(lastUpdateString) && lastTvDbUpdateTime.HasValue)
            {
                long num;
                if (long.TryParse(lastUpdateString, NumberStyles.Any, _usCulture, out num))
                {
                    hasEpisodeChanged = num >= lastTvDbUpdateTime.Value;
                }
            }

            var file = Path.Combine(seriesDataPath, string.Format("episode-{0}-{1}.xml", seasonNumber, episodeNumber));

            // Only save the file if not already there, or if the episode has changed
            if (hasEpisodeChanged || !File.Exists(file))
            {
                using (var writer = XmlWriter.Create(file, new XmlWriterSettings
                {
                    Encoding = Encoding.UTF8,
                    Async = true
                }))
                {
                    await writer.WriteRawAsync(xml).ConfigureAwait(false);
                }
            }

            if (absoluteNumber != -1)
            {
                file = Path.Combine(seriesDataPath, string.Format("episode-abs-{0}.xml", absoluteNumber));

                // Only save the file if not already there, or if the episode has changed
                if (hasEpisodeChanged || !File.Exists(file))
                {
                    using (var writer = XmlWriter.Create(file, new XmlWriterSettings
                    {
                        Encoding = Encoding.UTF8,
                        Async = true
                    }))
                    {
                        await writer.WriteRawAsync(xml).ConfigureAwait(false);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the series data path.
        /// </summary>
        /// <param name="appPaths">The app paths.</param>
        /// <param name="seriesId">The series id.</param>
        /// <returns>System.String.</returns>
        internal static string GetSeriesDataPath(IApplicationPaths appPaths, string seriesId)
        {
            var seriesDataPath = Path.Combine(GetSeriesDataPath(appPaths), seriesId);

            return seriesDataPath;
        }

        /// <summary>
        /// Gets the series data path.
        /// </summary>
        /// <param name="appPaths">The app paths.</param>
        /// <returns>System.String.</returns>
        internal static string GetSeriesDataPath(IApplicationPaths appPaths)
        {
            var dataPath = Path.Combine(appPaths.CachePath, "tvdb");

            return dataPath;
        }

        private void DeleteXmlFiles(string path)
        {
            try
            {
                foreach (var file in new DirectoryInfo(path)
                    .EnumerateFiles("*.xml", SearchOption.AllDirectories)
                    .ToList())
                {
                    _fileSystem.DeleteFile(file.FullName);
                }
            }
            catch (DirectoryNotFoundException)
            {
                // No biggie
            }
        }

        /// <summary>
        /// Sanitizes the XML file.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <returns>Task.</returns>
        private async Task SanitizeXmlFile(string file)
        {
            string validXml;

            using (var fileStream = _fileSystem.GetFileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, true))
            {
                using (var reader = new StreamReader(fileStream))
                {
                    var xml = await reader.ReadToEndAsync().ConfigureAwait(false);

                    validXml = StripInvalidXmlCharacters(xml);
                }
            }

            using (var fileStream = _fileSystem.GetFileStream(file, FileMode.Create, FileAccess.Write, FileShare.Read, true))
            {
                using (var writer = new StreamWriter(fileStream))
                {
                    await writer.WriteAsync(validXml).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Strips the invalid XML characters.
        /// </summary>
        /// <param name="inString">The in string.</param>
        /// <returns>System.String.</returns>
        public static string StripInvalidXmlCharacters(string inString)
        {
            if (inString == null) return null;

            var sbOutput = new StringBuilder();
            char ch;

            for (int i = 0; i < inString.Length; i++)
            {
                ch = inString[i];
                if ((ch >= 0x0020 && ch <= 0xD7FF) ||
                    (ch >= 0xE000 && ch <= 0xFFFD) ||
                    ch == 0x0009 ||
                    ch == 0x000A ||
                    ch == 0x000D)
                {
                    sbOutput.Append(ch);
                }
            }
            return sbOutput.ToString();
        }

        public string Name
        {
            get { return "TheTVDB"; }
        }

        public async Task<SeriesIdentity> FindIdentity(SeriesInfo info)
        {
            string tvdbId;
            if (!info.ProviderIds.TryGetValue(MetadataProviders.Tvdb.ToString(), out tvdbId))
            {
                var srch = await GetSearchResults(info, CancellationToken.None).ConfigureAwait(false);

                var entry = srch.FirstOrDefault();

                if (entry != null)
                {
                    tvdbId = entry.GetProviderId(MetadataProviders.Tvdb);
                }
            }

            if (!string.IsNullOrEmpty(tvdbId))
            {
                return new SeriesIdentity { Type = MetadataProviders.Tvdb.ToString(), Id = tvdbId };
            }

            return null;
        }

        public int Order
        {
            get
            {
                // After Omdb
                return 1;
            }
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClient.GetResponse(new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = url,
                ResourcePool = TvDbResourcePool
            });
        }
    }

    public class TvdbConfigStore : IConfigurationFactory
    {
        public IEnumerable<ConfigurationStore> GetConfigurations()
        {
            return new List<ConfigurationStore>
            {
                new ConfigurationStore
                {
                     Key = "tvdb",
                     ConfigurationType = typeof(TvdbOptions)
                }
            };
        }
    }
}
