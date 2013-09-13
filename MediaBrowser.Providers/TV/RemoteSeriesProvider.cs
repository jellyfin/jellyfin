using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Providers.Extensions;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

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
                return "1";
            }
        }

        protected override DateTime CompareDate(BaseItem item)
        {
            var seriesId = item.GetProviderId(MetadataProviders.Tvdb);

            if (!string.IsNullOrEmpty(seriesId))
            {
                // Process images
                var path = GetSeriesDataPath(ConfigurationManager.ApplicationPaths, seriesId);

                var files = new DirectoryInfo(path)
                    .EnumerateFiles("*.xml", SearchOption.TopDirectoryOnly)
                    .Select(i => i.LastWriteTimeUtc)
                    .ToArray();

                if (files.Length > 0)
                {
                    return files.Max();
                }
            }

            return base.CompareDate(item);
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
            var files = Directory.EnumerateFiles(seriesDataPath, "*.xml", SearchOption.TopDirectoryOnly).Select(Path.GetFileName).ToArray();

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

                var seriesDoc = new XmlDocument();
                seriesDoc.Load(seriesXmlPath);

                FetchMainInfo(series, seriesDoc);

                if (!series.LockedFields.Contains(MetadataFields.Cast))
                {
                    var actorsDoc = new XmlDocument();
                    actorsDoc.Load(actorsXmlPath);

                    FetchActors(series, actorsDoc, seriesDoc);
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

            if (!Directory.Exists(seriesDataPath))
            {
                Directory.CreateDirectory(seriesDataPath);
            }

            return seriesDataPath;
        }

        /// <summary>
        /// Gets the series data path.
        /// </summary>
        /// <param name="appPaths">The app paths.</param>
        /// <returns>System.String.</returns>
        internal static string GetSeriesDataPath(IApplicationPaths appPaths)
        {
            var dataPath = Path.Combine(appPaths.DataPath, "tvdb");

            if (!Directory.Exists(dataPath))
            {
                Directory.CreateDirectory(dataPath);
            }

            return dataPath;
        }

        /// <summary>
        /// Fetches the main info.
        /// </summary>
        /// <param name="series">The series.</param>
        /// <param name="doc">The doc.</param>
        private void FetchMainInfo(Series series, XmlDocument doc)
        {
            if (!series.LockedFields.Contains(MetadataFields.Name))
            {
                series.Name = doc.SafeGetString("//SeriesName");
            }
            if (!series.LockedFields.Contains(MetadataFields.Overview))
            {
                series.Overview = doc.SafeGetString("//Overview");
            }

            var imdbId = doc.SafeGetString("//IMDB_ID");

            if (!string.IsNullOrWhiteSpace(imdbId))
            {
                series.SetProviderId(MetadataProviders.Imdb, imdbId);
            }

            var zap2ItId = doc.SafeGetString("//zap2it_id");

            if (!string.IsNullOrWhiteSpace(zap2ItId))
            {
                series.SetProviderId(MetadataProviders.Zap2It, zap2ItId);
            }
            
            // Only fill this if it doesn't already have a value, since we get it from imdb which has better data
            if (!series.CommunityRating.HasValue || string.IsNullOrWhiteSpace(series.GetProviderId(MetadataProviders.Imdb)))
            {
                series.CommunityRating = doc.SafeGetSingle("//Rating", 0, 10);
            }

            series.AirDays = TVUtils.GetAirDays(doc.SafeGetString("//Airs_DayOfWeek"));
            series.AirTime = doc.SafeGetString("//Airs_Time");
            SeriesStatus seriesStatus;
            if(Enum.TryParse(doc.SafeGetString("//Status"), true, out seriesStatus))
                series.Status = seriesStatus;
            series.PremiereDate = doc.SafeGetDateTime("//FirstAired");
            if (series.PremiereDate.HasValue)
                series.ProductionYear = series.PremiereDate.Value.Year;

            if (!series.LockedFields.Contains(MetadataFields.Runtime))
            {
                series.RunTimeTicks = TimeSpan.FromMinutes(doc.SafeGetInt32("//Runtime")).Ticks;
            }

            if (!series.LockedFields.Contains(MetadataFields.Studios))
            {
                string s = doc.SafeGetString("//Network");

                if (!string.IsNullOrWhiteSpace(s))
                {
                    series.Studios.Clear();

                    foreach (var studio in s.Trim().Split('|'))
                    {
                        series.AddStudio(studio);
                    }
                }
            }
            series.OfficialRating = doc.SafeGetString("//ContentRating");
            if (!series.LockedFields.Contains(MetadataFields.Genres))
            {
                string g = doc.SafeGetString("//Genre");

                if (g != null)
                {
                    string[] genres = g.Trim('|').Split('|');
                    if (g.Length > 0)
                    {
                        series.Genres.Clear();

                        foreach (var genre in genres)
                        {
                            series.AddGenre(genre);
                        }
                    }
                }
            }
            if (series.Status == SeriesStatus.Ended) {
                
                var document = XDocument.Load(new XmlNodeReader(doc));
                var dates = document.Descendants("Episode").Where(x => {
                                                                      var seasonNumber = x.Element("SeasonNumber");
                                                                      var firstAired = x.Element("FirstAired");
                                                                      return firstAired != null && seasonNumber != null && (!string.IsNullOrEmpty(seasonNumber.Value) && seasonNumber.Value != "0") && !string.IsNullOrEmpty(firstAired.Value);
                                                                  }).Select(x => {
                                                                                DateTime? date = null;
                                                                                DateTime tempDate;
                                                                                var firstAired = x.Element("FirstAired");
                                                                                if (firstAired != null && DateTime.TryParse(firstAired.Value, out tempDate)) 
                                                                                {
                                                                                    date = tempDate;
                                                                                }
                                                                                return date;
                                                                            }).ToList();
                if(dates.Any(x=>x.HasValue))
                    series.EndDate = dates.Where(x => x.HasValue).Max();
            }
        }

        /// <summary>
        /// Fetches the actors.
        /// </summary>
        /// <param name="series">The series.</param>
        /// <param name="actorsDoc">The actors doc.</param>
        /// <param name="seriesDoc">The seriesDoc.</param>
        /// <returns>Task.</returns>
        private void FetchActors(Series series, XmlDocument actorsDoc, XmlDocument seriesDoc)
        {
            var xmlNodeList = actorsDoc.SelectNodes("Actors/Actor");

            if (xmlNodeList != null)
            {
                series.People.Clear();

                foreach (XmlNode p in xmlNodeList)
                {
                    string actorName = p.SafeGetString("Name");
                    string actorRole = p.SafeGetString("Role");

                    if (!string.IsNullOrWhiteSpace(actorName))
                    {
                        // Sometimes tvdb actors have leading spaces
                        series.AddPerson(new PersonInfo { Type = PersonType.Actor, Name = actorName.Trim(), Role = actorRole });
                    }
                }
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
