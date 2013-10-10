using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
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
    /// <summary>
    /// Class RemoteSeriesProvider
    /// </summary>
    class RemoteSeriesProvider : BaseMetadataProvider, IDisposable
    {
        /// <summary>
        /// The tv db
        /// </summary>
        internal readonly SemaphoreSlim TvDbResourcePool = new SemaphoreSlim(2, 2);

        /// <summary>
        /// Gets the current.
        /// </summary>
        /// <value>The current.</value>
        internal static RemoteSeriesProvider Current { get; private set; }

        /// <summary>
        /// The _zip client
        /// </summary>
        private readonly IZipClient _zipClient;

        /// <summary>
        /// Gets the HTTP client.
        /// </summary>
        /// <value>The HTTP client.</value>
        protected IHttpClient HttpClient { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteSeriesProvider" /> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="logManager">The log manager.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="zipClient">The zip client.</param>
        /// <exception cref="System.ArgumentNullException">httpClient</exception>
        public RemoteSeriesProvider(IHttpClient httpClient, ILogManager logManager, IServerConfigurationManager configurationManager, IZipClient zipClient)
            : base(logManager, configurationManager)
        {
            if (httpClient == null)
            {
                throw new ArgumentNullException("httpClient");
            }
            HttpClient = httpClient;
            _zipClient = zipClient;
            Current = this;
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool dispose)
        {
            if (dispose)
            {
                TvDbResourcePool.Dispose();
            }
        }

        /// <summary>
        /// The root URL
        /// </summary>
        private const string RootUrl = "http://www.thetvdb.com/api/";
        /// <summary>
        /// The series query
        /// </summary>
        private const string SeriesQuery = "GetSeries.php?seriesname={0}";
        /// <summary>
        /// The series get zip
        /// </summary>
        private const string SeriesGetZip = "http://www.thetvdb.com/api/{0}/series/{1}/all/{2}.zip";

        /// <summary>
        /// The LOCA l_ MET a_ FIL e_ NAME
        /// </summary>
        protected const string LocalMetaFileName = "series.xml";

        /// <summary>
        /// Supportses the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool Supports(BaseItem item)
        {
            return item is Series;
        }

        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override MetadataProviderPriority Priority
        {
            get { return MetadataProviderPriority.Second; }
        }

        /// <summary>
        /// Gets a value indicating whether [requires internet].
        /// </summary>
        /// <value><c>true</c> if [requires internet]; otherwise, <c>false</c>.</value>
        public override bool RequiresInternet
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets a value indicating whether [refresh on version change].
        /// </summary>
        /// <value><c>true</c> if [refresh on version change]; otherwise, <c>false</c>.</value>
        protected override bool RefreshOnVersionChange
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets the provider version.
        /// </summary>
        /// <value>The provider version.</value>
        protected override string ProviderVersion
        {
            get
            {
                return "2";
            }
        }

        public override bool EnforceDontFetchMetadata
        {
            get
            {
                // Other providers depend on the xml downloaded here
                return false;
            }
        }

        protected override bool NeedsRefreshBasedOnCompareDate(BaseItem item, BaseProviderInfo providerInfo)
        {
            var seriesId = item.GetProviderId(MetadataProviders.Tvdb);

            if (!string.IsNullOrEmpty(seriesId))
            {
                // Process images
                var path = GetSeriesDataPath(ConfigurationManager.ApplicationPaths, seriesId);

                try
                {
                    var files = new DirectoryInfo(path)
                        .EnumerateFiles("*.xml", SearchOption.TopDirectoryOnly)
                        .Select(i => i.LastWriteTimeUtc)
                        .ToList();

                    if (files.Count > 0)
                    {
                        return files.Max() > providerInfo.LastRefreshed;
                    }
                }
                catch (DirectoryNotFoundException)
                {
                    // Don't blow up
                    return true;
                }
            }
            
            return base.NeedsRefreshBasedOnCompareDate(item, providerInfo);
        }

        /// <summary>
        /// Fetches metadata and returns true or false indicating if any work that requires persistence was done
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="force">if set to <c>true</c> [force].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.Boolean}.</returns>
        public override async Task<bool> FetchAsync(BaseItem item, bool force, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var series = (Series)item;

            var seriesId = series.GetProviderId(MetadataProviders.Tvdb);

            if (string.IsNullOrEmpty(seriesId))
            {
                seriesId = await FindSeries(series.Name, cancellationToken).ConfigureAwait(false);
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (!string.IsNullOrEmpty(seriesId))
            {
                series.SetProviderId(MetadataProviders.Tvdb, seriesId);

                var seriesDataPath = GetSeriesDataPath(ConfigurationManager.ApplicationPaths, seriesId);

                await FetchSeriesData(series, seriesId, seriesDataPath, force, cancellationToken).ConfigureAwait(false);
            }

            SetLastRefreshed(item, DateTime.UtcNow);
            return true;
        }

        /// <summary>
        /// Fetches the series data.
        /// </summary>
        /// <param name="series">The series.</param>
        /// <param name="seriesId">The series id.</param>
        /// <param name="seriesDataPath">The series data path.</param>
        /// <param name="isForcedRefresh">if set to <c>true</c> [is forced refresh].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.Boolean}.</returns>
        private async Task FetchSeriesData(Series series, string seriesId, string seriesDataPath, bool isForcedRefresh, CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(seriesDataPath);

            var files = Directory.EnumerateFiles(seriesDataPath, "*.xml", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .ToList();

            var seriesXmlFilename = ConfigurationManager.Configuration.PreferredMetadataLanguage.ToLower() + ".xml";

            // Only download if not already there
            // The prescan task will take care of updates so we don't need to re-download here
            if (!files.Contains("banners.xml", StringComparer.OrdinalIgnoreCase) || !files.Contains("actors.xml", StringComparer.OrdinalIgnoreCase) || !files.Contains(seriesXmlFilename, StringComparer.OrdinalIgnoreCase))
            {
                await DownloadSeriesZip(seriesId, seriesDataPath, cancellationToken).ConfigureAwait(false);
            }

            // Examine if there's no local metadata, or save local is on (to get updates)
            if (isForcedRefresh || ConfigurationManager.Configuration.EnableTvDbUpdates || !HasLocalMeta(series))
            {
                var seriesXmlPath = Path.Combine(seriesDataPath, seriesXmlFilename);
                var actorsXmlPath = Path.Combine(seriesDataPath, "actors.xml");

                FetchSeriesInfo(series, seriesXmlPath, cancellationToken);

                if (!series.LockedFields.Contains(MetadataFields.Cast))
                {
                    series.People.Clear();

                    FetchActors(series, actorsXmlPath, cancellationToken);
                }
            }
        }

        /// <summary>
        /// Downloads the series zip.
        /// </summary>
        /// <param name="seriesId">The series id.</param>
        /// <param name="seriesDataPath">The series data path.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        internal async Task DownloadSeriesZip(string seriesId, string seriesDataPath, CancellationToken cancellationToken)
        {
            var url = string.Format(SeriesGetZip, TVUtils.TvdbApiKey, seriesId, ConfigurationManager.Configuration.PreferredMetadataLanguage);

            using (var zipStream = await HttpClient.Get(new HttpRequestOptions
            {
                Url = url,
                ResourcePool = TvDbResourcePool,
                CancellationToken = cancellationToken

            }).ConfigureAwait(false))
            {
                // Copy to memory stream because we need a seekable stream
                using (var ms = new MemoryStream())
                {
                    await zipStream.CopyToAsync(ms).ConfigureAwait(false);

                    ms.Position = 0;
                    _zipClient.ExtractAll(ms, seriesDataPath, true);
                }
            }

            foreach (var file in Directory.EnumerateFiles(seriesDataPath, "*.xml", SearchOption.AllDirectories).ToList())
            {
                await SanitizeXmlFile(file).ConfigureAwait(false);
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

            using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, StreamDefaults.DefaultFileStreamBufferSize, true))
            {
                using (var reader = new StreamReader(fileStream))
                {
                    var xml = await reader.ReadToEndAsync().ConfigureAwait(false);

                    validXml = StripInvalidXmlCharacters(xml);
                }
            }

            using (var fileStream = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.Read, StreamDefaults.DefaultFileStreamBufferSize, true))
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
            var dataPath = Path.Combine(appPaths.DataPath, "tvdb-v2");

            return dataPath;
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
                                if (!item.LockedFields.Contains(MetadataFields.Name))
                                {
                                    item.Name = (reader.ReadElementContentAsString() ?? string.Empty).Trim();
                                }
                                break;
                            }

                        case "Overview":
                            {
                                if (!item.LockedFields.Contains(MetadataFields.Overview))
                                {
                                    item.Overview = (reader.ReadElementContentAsString() ?? string.Empty).Trim();
                                }
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
                                    if (!item.LockedFields.Contains(MetadataFields.OfficialRating))
                                    {
                                        item.OfficialRating = val;
                                    }
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
                                        if (float.TryParse(val, NumberStyles.AllowDecimalPoint, UsCulture, out rval))
                                        {
                                            item.CommunityRating = rval;
                                        }
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

                                if (!string.IsNullOrWhiteSpace(val) && !item.LockedFields.Contains(MetadataFields.Runtime))
                                {
                                    int rval;

                                    // int.TryParse is local aware, so it can be probamatic, force us culture
                                    if (int.TryParse(val, NumberStyles.Integer, UsCulture, out rval))
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
                                    // Only fill this in if there's no existing genres, because Imdb data from Omdb is preferred
                                    if (!item.LockedFields.Contains(MetadataFields.Genres) && (item.Genres.Count == 0 || !string.Equals(ConfigurationManager.Configuration.PreferredMetadataLanguage, "en", StringComparison.OrdinalIgnoreCase)))
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
                                }

                                break;
                            }

                        case "Network":
                            {
                                var val = reader.ReadElementContentAsString();

                                if (!string.IsNullOrWhiteSpace(val))
                                {
                                    if (!item.LockedFields.Contains(MetadataFields.Studios))
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
                                    if (int.TryParse(val, NumberStyles.Integer, UsCulture, out rval))
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
        /// <param name="cancellationToken">The cancellation token.</param>
        private void FetchActors(Series series, string actorsXmlPath, CancellationToken cancellationToken)
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
                        cancellationToken.ThrowIfCancellationRequested();

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

        /// <summary>
        /// The us culture
        /// </summary>
        protected readonly CultureInfo UsCulture = new CultureInfo("en-US");

        /// <summary>
        /// Determines whether [has local meta] [the specified item].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if [has local meta] [the specified item]; otherwise, <c>false</c>.</returns>
        private bool HasLocalMeta(BaseItem item)
        {
            return item.ResolveArgs.ContainsMetaFileByName(LocalMetaFileName);
        }

        /// <summary>
        /// Finds the series.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.String}.</returns>
        private async Task<string> FindSeries(string name, CancellationToken cancellationToken)
        {
            var url = string.Format(RootUrl + SeriesQuery, WebUtility.UrlEncode(name));
            var doc = new XmlDocument();

            using (var results = await HttpClient.Get(new HttpRequestOptions
            {
                Url = url,
                ResourcePool = TvDbResourcePool,
                CancellationToken = cancellationToken

            }).ConfigureAwait(false))
            {
                doc.Load(results);
            }

            if (doc.HasChildNodes)
            {
                var nodes = doc.SelectNodes("//Series");
                var comparableName = GetComparableName(name);
                if (nodes != null)
                    foreach (XmlNode node in nodes)
                    {
                        var n = node.SelectSingleNode("./SeriesName");
                        if (n != null && string.Equals(GetComparableName(n.InnerText), comparableName, StringComparison.OrdinalIgnoreCase))
                        {
                            n = node.SelectSingleNode("./seriesid");
                            if (n != null)
                                return n.InnerText;
                        }
                        else
                        {
                            if (n != null)
                                Logger.Info("TVDb Provider - " + n.InnerText + " did not match " + comparableName);
                        }
                    }
            }

            // Try stripping off the year if it was supplied
            var parenthIndex = name.LastIndexOf('(');

            if (parenthIndex != -1)
            {
                var newName = name.Substring(0, parenthIndex);

                return await FindSeries(newName, cancellationToken);
            }

            Logger.Info("TVDb Provider - Could not find " + name + ". Check name on Thetvdb.org.");
            return null;
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

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }
    }
}
