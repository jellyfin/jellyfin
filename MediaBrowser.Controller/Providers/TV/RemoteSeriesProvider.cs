using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Extensions;
using MediaBrowser.Controller.Resolvers.TV;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using System;
using System.Collections.Generic;
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
    class RemoteSeriesProvider : BaseMetadataProvider
    {
        /// <summary>
        /// Gets the HTTP client.
        /// </summary>
        /// <value>The HTTP client.</value>
        protected IHttpClient HttpClient { get; private set; }

        public RemoteSeriesProvider(IHttpClient httpClient, ILogManager logManager)
            : base(logManager)
        {
            if (httpClient == null)
            {
                throw new ArgumentNullException("httpClient");
            }
            HttpClient = httpClient;
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

            if (Kernel.Instance.Configuration.MetadataRefreshDays == -1 && downloadDate != DateTime.MinValue)
            {
                return false;
            }

            if (item.DontFetchMeta) return false;

            return !HasLocalMeta(item) && (Kernel.Instance.Configuration.MetadataRefreshDays != -1 &&
                                       DateTime.UtcNow.Subtract(downloadDate).TotalDays > Kernel.Instance.Configuration.MetadataRefreshDays);
        }

        /// <summary>
        /// Fetches metadata and returns true or false indicating if any work that requires persistence was done
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="force">if set to <c>true</c> [force].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.Boolean}.</returns>
        protected override async Task<bool> FetchAsyncInternal(BaseItem item, bool force, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var series = (Series)item;
            if (!item.DontFetchMeta && !HasLocalMeta(series))
            {
                var path = item.Path ?? "";
                var seriesId = Path.GetFileName(path).GetAttributeValue("tvdbid") ?? await GetSeriesId(series, cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();
                
                if (!string.IsNullOrEmpty(seriesId))
                {
                    series.SetProviderId(MetadataProviders.Tvdb, seriesId);
                    if (!HasCompleteMetadata(series))
                    {
                        await FetchSeriesData(series, seriesId, cancellationToken).ConfigureAwait(false);
                    }
                }
                SetLastRefreshed(item, DateTime.UtcNow);
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
        private async Task<bool> FetchSeriesData(Series series, string seriesId, CancellationToken cancellationToken)
        {
            var success = false;

            var name = series.Name;
            Logger.Debug("TvDbProvider: Fetching series data: " + name);

            if (!string.IsNullOrEmpty(seriesId))
            {

                string url = string.Format(seriesGet, TVUtils.TVDBApiKey, seriesId, Kernel.Instance.Configuration.PreferredMetadataLanguage);
                var doc = new XmlDocument();

                try
                {
                    using (var xml = await HttpClient.Get(url, Kernel.Instance.ResourcePools.TvDb, cancellationToken).ConfigureAwait(false))
                    {
                        doc.Load(xml);
                    }
                }
                catch (HttpException)
                {
                }

                if (doc.HasChildNodes)
                {
                    //kick off the actor and image fetch simultaneously
                    var actorTask = FetchActors(series, seriesId, doc, cancellationToken);
                    var imageTask = FetchImages(series, seriesId, cancellationToken);

                    success = true;

                    series.Name = doc.SafeGetString("//SeriesName");
                    series.Overview = doc.SafeGetString("//Overview");
                    series.CommunityRating = doc.SafeGetSingle("//Rating", 0, 10);
                    series.AirDays = TVUtils.GetAirDays(doc.SafeGetString("//Airs_DayOfWeek"));
                    series.AirTime = doc.SafeGetString("//Airs_Time");

                    string n = doc.SafeGetString("//banner");
                    if (!string.IsNullOrWhiteSpace(n))
                    {
                        series.SetImage(ImageType.Banner, TVUtils.BannerUrl + n);
                    }

                    string s = doc.SafeGetString("//Network");
                    if (!string.IsNullOrWhiteSpace(s))
                        series.AddStudios(new List<string>(s.Trim().Split('|')));

                    series.OfficialRating = doc.SafeGetString("//ContentRating");

                    string g = doc.SafeGetString("//Genre");

                    if (g != null)
                    {
                        string[] genres = g.Trim('|').Split('|');
                        if (g.Length > 0)
                        {
                            series.AddGenres(genres);
                        }
                    }

                    //wait for other tasks
                    await Task.WhenAll(actorTask, imageTask).ConfigureAwait(false);

                    if (Kernel.Instance.Configuration.SaveLocalMeta)
                    {
                        var ms = new MemoryStream();
                        doc.Save(ms);
                        
                        await Kernel.Instance.FileSystemManager.SaveToLibraryFilesystem(series, Path.Combine(series.MetaLocation, LOCAL_META_FILE_NAME), ms, cancellationToken).ConfigureAwait(false);
                    }
                }
            }



            return success;
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
            string urlActors = string.Format(getActors, TVUtils.TVDBApiKey, seriesId);
            var docActors = new XmlDocument();

            try
            {
                using (var actors = await HttpClient.Get(urlActors, Kernel.Instance.ResourcePools.TvDb, cancellationToken).ConfigureAwait(false))
                {
                    docActors.Load(actors);
                }
            }
            catch (HttpException)
            {
            }

            if (docActors.HasChildNodes)
            {
                XmlNode actorsNode = null;
                if (Kernel.Instance.Configuration.SaveLocalMeta)
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
                    foreach (XmlNode p in xmlNodeList)
                    {
                        string actorName = p.SafeGetString("Name");
                        string actorRole = p.SafeGetString("Role");
                        if (!string.IsNullOrWhiteSpace(actorName))
                        {
                            series.AddPerson(new PersonInfo { Type = PersonType.Actor, Name = actorName, Role = actorRole });

                            if (Kernel.Instance.Configuration.SaveLocalMeta && actorsNode != null)
                            {
                                //create in main doc
                                var personNode = doc.CreateNode(XmlNodeType.Element, "Person", null);
                                foreach (XmlNode subNode in p.ChildNodes)
                                    personNode.AppendChild(doc.ImportNode(subNode, true));
                                //need to add the type
                                var typeNode = doc.CreateNode(XmlNodeType.Element, "Type", null);
                                typeNode.InnerText = "Actor";
                                personNode.AppendChild(typeNode);
                                actorsNode.AppendChild(personNode);
                            }

                        }
                    }
            }
        }

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
                string url = string.Format("http://www.thetvdb.com/api/" + TVUtils.TVDBApiKey + "/series/{0}/banners.xml", seriesId);
                var images = new XmlDocument();

                try
                {
                    using (var imgs = await HttpClient.Get(url, Kernel.Instance.ResourcePools.TvDb, cancellationToken).ConfigureAwait(false))
                    {
                        images.Load(imgs);
                    }
                }
                catch (HttpException)
                {
                }

                if (images.HasChildNodes)
                {
                    if (Kernel.Instance.Configuration.RefreshItemImages || !series.HasLocalImage("folder"))
                    {
                        var n = images.SelectSingleNode("//Banner[BannerType='poster']");
                        if (n != null)
                        {
                            n = n.SelectSingleNode("./BannerPath");
                            if (n != null)
                            {
                                try
                                {
                                    series.PrimaryImagePath = await Kernel.Instance.ProviderManager.DownloadAndSaveImage(series, TVUtils.BannerUrl + n.InnerText, "folder" + Path.GetExtension(n.InnerText), Kernel.Instance.ResourcePools.TvDb, cancellationToken).ConfigureAwait(false);
                                }
                                catch (HttpException)
                                {
                                }
                                catch (IOException)
                                {

                                }
                            }
                        }
                    }

                    if (Kernel.Instance.Configuration.DownloadTVBanner && (Kernel.Instance.Configuration.RefreshItemImages || !series.HasLocalImage("banner")))
                    {
                        var n = images.SelectSingleNode("//Banner[BannerType='series']");
                        if (n != null)
                        {
                            n = n.SelectSingleNode("./BannerPath");
                            if (n != null)
                            {
                                try
                                {
                                    var bannerImagePath = await Kernel.Instance.ProviderManager.DownloadAndSaveImage(series, TVUtils.BannerUrl + n.InnerText, "banner" + Path.GetExtension(n.InnerText), Kernel.Instance.ResourcePools.TvDb, cancellationToken);

                                    series.SetImage(ImageType.Banner, bannerImagePath);
                                }
                                catch (HttpException)
                                {
                                }
                                catch (IOException)
                                {

                                }
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
                                var bdName = "backdrop" + (bdNo > 0 ? bdNo.ToString() : "");
                                if (Kernel.Instance.Configuration.RefreshItemImages || !series.HasLocalImage(bdName))
                                {
                                    try
                                    {
                                        series.BackdropImagePaths.Add(await Kernel.Instance.ProviderManager.DownloadAndSaveImage(series, TVUtils.BannerUrl + p.InnerText, bdName + Path.GetExtension(p.InnerText), Kernel.Instance.ResourcePools.TvDb, cancellationToken).ConfigureAwait(false));
                                    }
                                    catch (HttpException)
                                    {
                                    }
                                    catch (IOException)
                                    {
                                        
                                    }
                                }
                                bdNo++;
                                if (bdNo >= Kernel.Instance.Configuration.MaxBackdrops) break;
                            }
                        }
                }
            }
        }

        /// <summary>
        /// Determines whether [has complete metadata] [the specified series].
        /// </summary>
        /// <param name="series">The series.</param>
        /// <returns><c>true</c> if [has complete metadata] [the specified series]; otherwise, <c>false</c>.</returns>
        private bool HasCompleteMetadata(Series series)
        {
            return (series.HasImage(ImageType.Banner)) && (series.CommunityRating != null)
                                && (series.Overview != null) && (series.Name != null) && (series.People != null)
                                && (series.Genres != null) && (series.OfficialRating != null);
        }

        /// <summary>
        /// Determines whether [has local meta] [the specified item].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if [has local meta] [the specified item]; otherwise, <c>false</c>.</returns>
        private bool HasLocalMeta(BaseItem item)
        {
            //need at least the xml and folder.jpg/png
            return item.ResolveArgs.ContainsMetaFileByName(LOCAL_META_FILE_NAME) && (item.ResolveArgs.ContainsMetaFileByName("folder.jpg") ||
                item.ResolveArgs.ContainsMetaFileByName("folder.png"));
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
                using (var results = await HttpClient.Get(url, Kernel.Instance.ResourcePools.TvDb, cancellationToken).ConfigureAwait(false))
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



    }
}
