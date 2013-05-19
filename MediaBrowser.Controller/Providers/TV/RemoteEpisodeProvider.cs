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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace MediaBrowser.Controller.Providers.TV
{

    /// <summary>
    /// Class RemoteEpisodeProvider
    /// </summary>
    class RemoteEpisodeProvider : BaseMetadataProvider
    {
        private readonly IProviderManager _providerManager;
        
        /// <summary>
        /// Gets the HTTP client.
        /// </summary>
        /// <value>The HTTP client.</value>
        protected IHttpClient HttpClient { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteEpisodeProvider" /> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="logManager">The log manager.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        public RemoteEpisodeProvider(IHttpClient httpClient, ILogManager logManager, IServerConfigurationManager configurationManager, IProviderManager providerManager)
            : base(logManager, configurationManager)
        {
            HttpClient = httpClient;
            _providerManager = providerManager;
        }

        /// <summary>
        /// The episode query
        /// </summary>
        private const string EpisodeQuery = "http://www.thetvdb.com/api/{0}/series/{1}/default/{2}/{3}/{4}.xml";
        /// <summary>
        /// The abs episode query
        /// </summary>
        private const string AbsEpisodeQuery = "http://www.thetvdb.com/api/{0}/series/{1}/absolute/{2}/{3}.xml";

        /// <summary>
        /// Supportses the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool Supports(BaseItem item)
        {
            return item is Episode;
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
            get { return true; }
        }

        protected override bool RefreshOnFileSystemStampChange
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
            if (HasLocalMeta(item))
            {
                return false;
            }

            return base.NeedsRefreshInternal(item, providerInfo);
        }

        /// <summary>
        /// Fetches metadata and returns true or false indicating if any work that requires persistence was done
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="force">if set to <c>true</c> [force].</param>
        /// <returns>Task{System.Boolean}.</returns>
        public override async Task<bool> FetchAsync(BaseItem item, bool force, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var episode = (Episode)item;
            if (!item.DontFetchMeta && !HasLocalMeta(episode))
            {
                var seriesId = episode.Series != null ? episode.Series.GetProviderId(MetadataProviders.Tvdb) : null;

                if (seriesId != null)
                {
                    var status = await FetchEpisodeData(episode, seriesId, cancellationToken).ConfigureAwait(false);
                    SetLastRefreshed(item, DateTime.UtcNow, status);
                    return true;
                }
                Logger.Info("Episode provider not fetching because series does not have a tvdb id: " + item.Path);
                return false;
            }
            Logger.Info("Episode provider not fetching because local meta exists or requested to ignore: " + item.Name);
            return false;
        }


        /// <summary>
        /// Fetches the episode data.
        /// </summary>
        /// <param name="episode">The episode.</param>
        /// <param name="seriesId">The series id.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.Boolean}.</returns>
        private async Task<ProviderRefreshStatus> FetchEpisodeData(Episode episode, string seriesId, CancellationToken cancellationToken)
        {
            string location = episode.Path;

            string epNum = TVUtils.EpisodeNumberFromFile(location, episode.Season != null);

            var status = ProviderRefreshStatus.Success;

            if (epNum == null)
            {
                Logger.Warn("TvDbProvider: Could not determine episode number for: " + episode.Path);
                return status;
            }

            var episodeNumber = Int32.Parse(epNum);

            episode.IndexNumber = episodeNumber;
            var usingAbsoluteData = false;

            if (string.IsNullOrEmpty(seriesId)) return status;

            var seasonNumber = "";
            if (episode.Parent is Season)
            {
                seasonNumber = episode.Parent.IndexNumber.ToString();
            }

            if (string.IsNullOrEmpty(seasonNumber))
                seasonNumber = TVUtils.SeasonNumberFromEpisodeFile(location); // try and extract the season number from the file name for S1E1, 1x04 etc.

            if (!string.IsNullOrEmpty(seasonNumber))
            {
                seasonNumber = seasonNumber.TrimStart('0');

                if (string.IsNullOrEmpty(seasonNumber))
                {
                    seasonNumber = "0"; // Specials
                }

                var url = string.Format(EpisodeQuery, TVUtils.TvdbApiKey, seriesId, seasonNumber, episodeNumber, ConfigurationManager.Configuration.PreferredMetadataLanguage);
                var doc = new XmlDocument();

                using (var result = await HttpClient.Get(new HttpRequestOptions
                {
                    Url = url,
                    ResourcePool = RemoteSeriesProvider.Current.TvDbResourcePool,
                    CancellationToken = cancellationToken,
                    EnableResponseCache = true

                }).ConfigureAwait(false))
                {
                    doc.Load(result);
                }

                //episode does not exist under this season, try absolute numbering.
                //still assuming it's numbered as 1x01
                //this is basicly just for anime.
                if (!doc.HasChildNodes && Int32.Parse(seasonNumber) == 1)
                {
                    url = string.Format(AbsEpisodeQuery, TVUtils.TvdbApiKey, seriesId, episodeNumber, ConfigurationManager.Configuration.PreferredMetadataLanguage);

                    using (var result = await HttpClient.Get(new HttpRequestOptions
                    {
                        Url = url,
                        ResourcePool = RemoteSeriesProvider.Current.TvDbResourcePool,
                        CancellationToken = cancellationToken,
                        EnableResponseCache = true

                    }).ConfigureAwait(false))
                    {
                        if (result != null) doc.Load(result);
                        usingAbsoluteData = true;
                    }
                }

                if (doc.HasChildNodes)
                {
                    var p = doc.SafeGetString("//filename");
                    if (p != null)
                    {
                        if (!Directory.Exists(episode.MetaLocation)) Directory.CreateDirectory(episode.MetaLocation);

                        try
                        {
                            episode.PrimaryImagePath = await _providerManager.DownloadAndSaveImage(episode, TVUtils.BannerUrl + p, Path.GetFileName(p), ConfigurationManager.Configuration.SaveLocalMeta, RemoteSeriesProvider.Current.TvDbResourcePool, cancellationToken);
                        }
                        catch (HttpException)
                        {
                            status = ProviderRefreshStatus.CompletedWithErrors;
                        }
                    }

                    episode.Overview = doc.SafeGetString("//Overview");
                    if (usingAbsoluteData)
                        episode.IndexNumber = doc.SafeGetInt32("//absolute_number", -1);
                    if (episode.IndexNumber < 0)
                        episode.IndexNumber = doc.SafeGetInt32("//EpisodeNumber");

                    episode.Name = doc.SafeGetString("//EpisodeName");
                    episode.CommunityRating = doc.SafeGetSingle("//Rating", -1, 10);
                    var firstAired = doc.SafeGetString("//FirstAired");
                    DateTime airDate;
                    if (DateTime.TryParse(firstAired, out airDate) && airDate.Year > 1850)
                    {
                        episode.PremiereDate = airDate.ToUniversalTime();
                        episode.ProductionYear = airDate.Year;
                    }

                    episode.People.Clear();

                    var actors = doc.SafeGetString("//GuestStars");
                    if (actors != null)
                    {
                        foreach (var person in actors.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).Select(str => new PersonInfo { Type = PersonType.GuestStar, Name = str }))
                        {
                            episode.AddPerson(person);
                        }
                    }


                    var directors = doc.SafeGetString("//Director");
                    if (directors != null)
                    {
                        foreach (var person in directors.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).Select(str => new PersonInfo { Type = PersonType.Director, Name = str }))
                        {
                            episode.AddPerson(person);
                        }
                    }


                    var writers = doc.SafeGetString("//Writer");
                    if (writers != null)
                    {
                        foreach (var person in writers.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).Select(str => new PersonInfo { Type = PersonType.Writer, Name = str }))
                        {
                            episode.AddPerson(person);
                        }
                    }

                    if (ConfigurationManager.Configuration.SaveLocalMeta)
                    {
                        if (!Directory.Exists(episode.MetaLocation)) Directory.CreateDirectory(episode.MetaLocation);
                        var ms = new MemoryStream();
                        doc.Save(ms);

                        await _providerManager.SaveToLibraryFilesystem(episode, Path.Combine(episode.MetaLocation, Path.GetFileNameWithoutExtension(episode.Path) + ".xml"), ms, cancellationToken).ConfigureAwait(false);
                    }

                    return status;
                }

            }

            return status;
        }

        /// <summary>
        /// Determines whether [has local meta] [the specified episode].
        /// </summary>
        /// <param name="episode">The episode.</param>
        /// <returns><c>true</c> if [has local meta] [the specified episode]; otherwise, <c>false</c>.</returns>
        private bool HasLocalMeta(BaseItem episode)
        {
            return (episode.Parent.ResolveArgs.ContainsMetaFileByName(Path.GetFileNameWithoutExtension(episode.Path) + ".xml"));
        }
    }
}
