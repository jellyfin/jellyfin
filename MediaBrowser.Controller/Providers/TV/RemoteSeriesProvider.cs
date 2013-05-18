using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Extensions;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace MediaBrowser.Controller.Providers.TV
{
    /// <summary>
    /// Class RemoteSeriesProvider
    /// </summary>
    class RemoteSeriesProvider : BaseMetadataProvider, IDisposable
    {
        private readonly IProviderManager _providerManager;
        
        /// <summary>
        /// The tv db
        /// </summary>
        internal readonly SemaphoreSlim TvDbResourcePool = new SemaphoreSlim(5, 5);

        internal static RemoteSeriesProvider Current { get; private set; }

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
        /// <param name="providerManager">The provider manager.</param>
        /// <exception cref="System.ArgumentNullException">httpClient</exception>
        public RemoteSeriesProvider(IHttpClient httpClient, ILogManager logManager, IServerConfigurationManager configurationManager, IProviderManager providerManager)
            : base(logManager, configurationManager)
        {
            if (httpClient == null)
            {
                throw new ArgumentNullException("httpClient");
            }
            HttpClient = httpClient;
            _providerManager = providerManager;
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
        private const string rootUrl = "http://www.thetvdb.com/api/";
        /// <summary>
        /// The series query
        /// </summary>
        private const string seriesQuery = "GetSeries.php?seriesname={0}";
        /// <summary>
        /// The series get
        /// </summary>
        private const string seriesGet = "http://www.thetvdb.com/api/{0}/series/{1}/{2}.xml";
        /// <summary>
        /// The get actors
        /// </summary>
        private const string getActors = "http://www.thetvdb.com/api/{0}/series/{1}/actors.xml";

        /// <summary>
        /// The LOCA l_ MET a_ FIL e_ NAME
        /// </summary>
        protected const string LOCAL_META_FILE_NAME = "Series.xml";

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
        /// Needses the refresh internal.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="providerInfo">The provider info.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        protected override bool NeedsRefreshInternal(BaseItem item, BaseProviderInfo providerInfo)
        {
            var downloadDate = providerInfo.LastRefreshed;

            if (ConfigurationManager.Configuration.MetadataRefreshDays == -1 && downloadDate != DateTime.MinValue)
            {
                return false;
            }

            if (item.DontFetchMeta) return false;

            return !HasLocalMeta(item) && base.NeedsRefreshInternal(item, providerInfo);
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
            if (!item.DontFetchMeta && !HasLocalMeta(series))
            {
                var path = item.Path ?? "";
                var seriesId = Path.GetFileName(path).GetAttributeValue("tvdbid") ?? await GetSeriesId(series, cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();

                var status = ProviderRefreshStatus.Success;

                if (!string.IsNullOrEmpty(seriesId))
                {
                    series.SetProviderId(MetadataProviders.Tvdb, seriesId);

                    status = await FetchSeriesData(series, seriesId, cancellationToken).ConfigureAwait(false);
                }

                SetLastRefreshed(item, DateTime.UtcNow, status);
                return true;
            }
            Logger.Info("Series provider not fetching because local meta exists or requested to ignore: " + item.Name);
            return false;

        }

        /// <summary>
        /// Fetches the series data.
        /// </summary>
        /// <param name="series">The series.</param>
        /// <param name="seriesId">The series id.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.Boolean}.</returns>
        private async Task<ProviderRefreshStatus> FetchSeriesData(Series series, string seriesId, CancellationToken cancellationToken)
        {
            var status = ProviderRefreshStatus.Success;

            if (!string.IsNullOrEmpty(seriesId))
            {

                string url = string.Format(seriesGet, TVUtils.TvdbApiKey, seriesId, ConfigurationManager.Configuration.PreferredMetadataLanguage);
                var doc = new XmlDocument();

                using (var xml = await HttpClient.Get(new HttpRequestOptions
                {
                    Url = url,
                    ResourcePool = TvDbResourcePool,
                    CancellationToken = cancellationToken,
                    EnableResponseCache = true

                }).ConfigureAwait(false))
                {
                    doc.Load(xml);
                }

                if (doc.HasChildNodes)
                {
                    //kick off the actor and image fetch simultaneously
                    var actorTask = FetchActors(series, seriesId, doc, cancellationToken);
                    var imageTask = FetchImages(series, seriesId, cancellationToken);

                    series.Name = doc.SafeGetString("//SeriesName");
                    series.Overview = doc.SafeGetString("//Overview");
                    series.CommunityRating = doc.SafeGetSingle("//Rating", 0, 10);
                    series.AirDays = TVUtils.GetAirDays(doc.SafeGetString("//Airs_DayOfWeek"));
                    series.AirTime = doc.SafeGetString("//Airs_Time");

                    string n = doc.SafeGetString("//banner");
                    if (!string.IsNullOrWhiteSpace(n))
                    {
                        series.SetImage(ImageType.Banner, await _providerManager.DownloadAndSaveImage(series, TVUtils.BannerUrl + n, "banner" + Path.GetExtension(n), ConfigurationManager.Configuration.SaveLocalMeta, TvDbResourcePool, cancellationToken).ConfigureAwait(false));
                    }

                    string s = doc.SafeGetString("//Network");

                    if (!string.IsNullOrWhiteSpace(s))
                    {
                        series.Studios.Clear();

                        foreach (var studio in s.Trim().Split('|'))
                        {
                            series.AddStudio(studio);
                        }
                    }

                    series.OfficialRating = doc.SafeGetString("//ContentRating");

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

                    try
                    {
                        //wait for other tasks
                        await Task.WhenAll(actorTask, imageTask).ConfigureAwait(false);
                    }
                    catch (HttpException)
                    {
                        status = ProviderRefreshStatus.CompletedWithErrors;
                    }

                    if (ConfigurationManager.Configuration.SaveLocalMeta)
                    {
                        var ms = new MemoryStream();
                        doc.Save(ms);

                        await _providerManager.SaveToLibraryFilesystem(series, Path.Combine(series.MetaLocation, LOCAL_META_FILE_NAME), ms, cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            return status;
        }

        /// <summary>
        /// Fetches the actors.
        /// </summary>
        /// <param name="series">The series.</param>
        /// <param name="seriesId">The series id.</param>
        /// <param name="doc">The doc.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        private async Task FetchActors(Series series, string seriesId, XmlDocument doc, CancellationToken cancellationToken)
        {
            string urlActors = string.Format(getActors, TVUtils.TvdbApiKey, seriesId);
            var docActors = new XmlDocument();

            using (var actors = await HttpClient.Get(new HttpRequestOptions
            {
                Url = urlActors,
                ResourcePool = TvDbResourcePool,
                CancellationToken = cancellationToken,
                EnableResponseCache = true

            }).ConfigureAwait(false))
            {
                docActors.Load(actors);
            }

            if (docActors.HasChildNodes)
            {
                XmlNode actorsNode = null;
                if (ConfigurationManager.Configuration.SaveLocalMeta)
                {
                    //add to the main doc for saving
                    var seriesNode = doc.SelectSingleNode("//Series");
                    if (seriesNode != null)
                    {
                        actorsNode = doc.CreateNode(XmlNodeType.Element, "Persons", null);
                        seriesNode.AppendChild(actorsNode);
                    }
                }

                var xmlNodeList = docActors.SelectNodes("Actors/Actor");

                if (xmlNodeList != null)
                {
                    series.People.Clear();

                    foreach (XmlNode p in xmlNodeList)
                    {
                        string actorName = p.SafeGetString("Name");
                        string actorRole = p.SafeGetString("Role");
                        if (!string.IsNullOrWhiteSpace(actorName))
                        {
                            series.AddPerson(new PersonInfo { Type = PersonType.Actor, Name = actorName, Role = actorRole });

                            if (ConfigurationManager.Configuration.SaveLocalMeta && actorsNode != null)
                            {
                                //create in main doc
                                var personNode = doc.CreateNode(XmlNodeType.Element, "Person", null);
                                foreach (XmlNode subNode in p.ChildNodes)
                                    personNode.AppendChild(doc.ImportNode(subNode, true));
                                //need to add the type
                                var typeNode = doc.CreateNode(XmlNodeType.Element, "Type", null);
                                typeNode.InnerText = PersonType.Actor;
                                personNode.AppendChild(typeNode);
                                actorsNode.AppendChild(personNode);
                            }

                        }
                    }
                }
            }
        }

        protected readonly CultureInfo UsCulture = new CultureInfo("en-US");
        
        /// <summary>
        /// Fetches the images.
        /// </summary>
        /// <param name="series">The series.</param>
        /// <param name="seriesId">The series id.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        private async Task FetchImages(Series series, string seriesId, CancellationToken cancellationToken)
        {
            if ((!string.IsNullOrEmpty(seriesId)) && ((series.PrimaryImagePath == null) || (series.BackdropImagePaths == null)))
            {
                string url = string.Format("http://www.thetvdb.com/api/" + TVUtils.TvdbApiKey + "/series/{0}/banners.xml", seriesId);
                var images = new XmlDocument();

                using (var imgs = await HttpClient.Get(new HttpRequestOptions
                {
                    Url = url,
                    ResourcePool = TvDbResourcePool,
                    CancellationToken = cancellationToken,
                    EnableResponseCache = true

                }).ConfigureAwait(false))
                {
                    images.Load(imgs);
                }

                if (images.HasChildNodes)
                {
                    if (ConfigurationManager.Configuration.RefreshItemImages || !series.HasLocalImage("folder"))
                    {
                        var n = images.SelectSingleNode("//Banner[BannerType='poster']");
                        if (n != null)
                        {
                            n = n.SelectSingleNode("./BannerPath");
                            if (n != null)
                            {
                                series.PrimaryImagePath = await _providerManager.DownloadAndSaveImage(series, TVUtils.BannerUrl + n.InnerText, "folder" + Path.GetExtension(n.InnerText), ConfigurationManager.Configuration.SaveLocalMeta, TvDbResourcePool, cancellationToken).ConfigureAwait(false);
                            }
                        }
                    }

                    if (ConfigurationManager.Configuration.DownloadSeriesImages.Banner && (ConfigurationManager.Configuration.RefreshItemImages || !series.HasLocalImage("banner")))
                    {
                        var n = images.SelectSingleNode("//Banner[BannerType='series']");
                        if (n != null)
                        {
                            n = n.SelectSingleNode("./BannerPath");
                            if (n != null)
                            {
                                var bannerImagePath = await _providerManager.DownloadAndSaveImage(series, TVUtils.BannerUrl + n.InnerText, "banner" + Path.GetExtension(n.InnerText), ConfigurationManager.Configuration.SaveLocalMeta, TvDbResourcePool, cancellationToken);

                                series.SetImage(ImageType.Banner, bannerImagePath);
                            }
                        }
                    }

                    var bdNo = 0;
                    var xmlNodeList = images.SelectNodes("//Banner[BannerType='fanart']");
                    if (xmlNodeList != null)
                        foreach (XmlNode b in xmlNodeList)
                        {
                            series.BackdropImagePaths = new List<string>();
                            var p = b.SelectSingleNode("./BannerPath");
                            if (p != null)
                            {
                                var bdName = "backdrop" + (bdNo > 0 ? bdNo.ToString(UsCulture) : "");
                                if (ConfigurationManager.Configuration.RefreshItemImages || !series.HasLocalImage(bdName))
                                {
                                    series.BackdropImagePaths.Add(await _providerManager.DownloadAndSaveImage(series, TVUtils.BannerUrl + p.InnerText, bdName + Path.GetExtension(p.InnerText), ConfigurationManager.Configuration.SaveLocalMeta, TvDbResourcePool, cancellationToken).ConfigureAwait(false));
                                }
                                bdNo++;
                                if (bdNo >= ConfigurationManager.Configuration.MaxBackdrops) break;
                            }
                        }
                }
            }
        }

        /// <summary>
        /// Determines whether [has local meta] [the specified item].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if [has local meta] [the specified item]; otherwise, <c>false</c>.</returns>
        private bool HasLocalMeta(BaseItem item)
        {
            return item.ResolveArgs.ContainsMetaFileByName(LOCAL_META_FILE_NAME);
        }

        /// <summary>
        /// Gets the series id.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.String}.</returns>
        private async Task<string> GetSeriesId(BaseItem item, CancellationToken cancellationToken)
        {
            var seriesId = item.GetProviderId(MetadataProviders.Tvdb);
            if (string.IsNullOrEmpty(seriesId))
            {
                seriesId = await FindSeries(item.Name, cancellationToken).ConfigureAwait(false);
            }
            return seriesId;
        }


        /// <summary>
        /// Finds the series.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.String}.</returns>
        public async Task<string> FindSeries(string name, CancellationToken cancellationToken)
        {

            //nope - search for it
            string url = string.Format(rootUrl + seriesQuery, WebUtility.UrlEncode(name));
            var doc = new XmlDocument();

            try
            {
                using (var results = await HttpClient.Get(new HttpRequestOptions
                {
                    Url = url,
                    ResourcePool = TvDbResourcePool,
                    CancellationToken = cancellationToken,
                    EnableResponseCache = true

                }).ConfigureAwait(false))
                {
                    doc.Load(results);
                }
            }
            catch (HttpException)
            {
            }

            if (doc.HasChildNodes)
            {
                XmlNodeList nodes = doc.SelectNodes("//Series");
                string comparableName = GetComparableName(name);
                if (nodes != null)
                    foreach (XmlNode node in nodes)
                    {
                        var n = node.SelectSingleNode("./SeriesName");
                        if (n != null && GetComparableName(n.InnerText) == comparableName)
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

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
