using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using MediaBrowser.Providers.Extensions;

namespace MediaBrowser.Providers.TV
{

    /// <summary>
    /// Class RemoteEpisodeProvider
    /// </summary>
    class RemoteEpisodeProvider : BaseMetadataProvider
    {
        /// <summary>
        /// The _provider manager
        /// </summary>
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
        /// <param name="providerManager">The provider manager.</param>
        public RemoteEpisodeProvider(IHttpClient httpClient, ILogManager logManager, IServerConfigurationManager configurationManager, IProviderManager providerManager)
            : base(logManager, configurationManager)
        {
            HttpClient = httpClient;
            _providerManager = providerManager;
        }

        /// <summary>
        /// Supportses the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool Supports(BaseItem item)
        {
            return item is Episode;
        }

        public override ItemUpdateType ItemUpdateType
        {
            get
            {
                return ItemUpdateType.ImageUpdate | ItemUpdateType.MetadataDownload;
            }
        }

        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override MetadataProviderPriority Priority
        {
            get { return MetadataProviderPriority.Third; }
        }

        /// <summary>
        /// Gets a value indicating whether [requires internet].
        /// </summary>
        /// <value><c>true</c> if [requires internet]; otherwise, <c>false</c>.</value>
        public override bool RequiresInternet
        {
            get { return true; }
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

        /// <summary>
        /// Needses the refresh internal.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="providerInfo">The provider info.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        protected override bool NeedsRefreshInternal(BaseItem item, BaseProviderInfo providerInfo)
        {
            // Don't proceed if there's local metadata
            if (HasLocalMeta(item) && !ConfigurationManager.Configuration.EnableTvDbUpdates)
            {
                return false;
            }

            return base.NeedsRefreshInternal(item, providerInfo);
        }

        protected override DateTime CompareDate(BaseItem item)
        {
            var episode = (Episode)item;

            var seriesId = episode.Series != null ? episode.Series.GetProviderId(MetadataProviders.Tvdb) : null;

            if (!string.IsNullOrEmpty(seriesId))
            {
                // Process images
                var seriesXmlPath = Path.Combine(RemoteSeriesProvider.GetSeriesDataPath(ConfigurationManager.ApplicationPaths, seriesId), ConfigurationManager.Configuration.PreferredMetadataLanguage.ToLower() + ".xml");

                var seriesXmlFileInfo = new FileInfo(seriesXmlPath);

                if (seriesXmlFileInfo.Exists)
                {
                    return seriesXmlFileInfo.LastWriteTimeUtc;
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

            var episode = (Episode)item;

            var seriesId = episode.Series != null ? episode.Series.GetProviderId(MetadataProviders.Tvdb) : null;

            if (!string.IsNullOrEmpty(seriesId))
            {
                var seriesXmlPath = Path.Combine(RemoteSeriesProvider.GetSeriesDataPath(ConfigurationManager.ApplicationPaths, seriesId), ConfigurationManager.Configuration.PreferredMetadataLanguage.ToLower() + ".xml");

                var seriesXmlFileInfo = new FileInfo(seriesXmlPath);

                var status = ProviderRefreshStatus.Success;

                if (seriesXmlFileInfo.Exists)
                {
                    var xmlDoc = new XmlDocument();
                    xmlDoc.Load(seriesXmlPath);

                    status = await FetchEpisodeData(xmlDoc, episode, seriesId, cancellationToken).ConfigureAwait(false);
                }

                BaseProviderInfo data;
                if (!item.ProviderData.TryGetValue(Id, out data))
                {
                    data = new BaseProviderInfo();
                    item.ProviderData[Id] = data;
                }

                SetLastRefreshed(item, DateTime.UtcNow, status);
                return true;
            }

            Logger.Info("Episode provider not fetching because series does not have a tvdb id: " + item.Path);
            return false;
        }


        /// <summary>
        /// Fetches the episode data.
        /// </summary>
        /// <param name="seriesXml">The series XML.</param>
        /// <param name="episode">The episode.</param>
        /// <param name="seriesId">The series id.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.Boolean}.</returns>
        private async Task<ProviderRefreshStatus> FetchEpisodeData(XmlDocument seriesXml, Episode episode, string seriesId, CancellationToken cancellationToken)
        {
            var status = ProviderRefreshStatus.Success;

            if (episode.IndexNumber == null)
            {
                return status;
            }

            var seasonNumber = episode.ParentIndexNumber ?? TVUtils.GetSeasonNumberFromEpisodeFile(episode.Path);

            if (seasonNumber == null)
            {
                return status;
            }

            var usingAbsoluteData = false;

            var episodeNode = seriesXml.SelectSingleNode("//Episode[EpisodeNumber='" + episode.IndexNumber.Value + "'][SeasonNumber='" + seasonNumber.Value + "']");

            if (episodeNode == null)
            {
                if (seasonNumber.Value == 1)
                {
                    episodeNode = seriesXml.SelectSingleNode("//Episode[absolute_number='" + episode.IndexNumber.Value + "']");
                    usingAbsoluteData = true;
                }
            }

            // If still null, nothing we can do
            if (episodeNode == null)
            {
                return status;
            }
            IEnumerable<XmlDocument> extraEpisodesNode = new XmlDocument[] { };

            if (episode.IndexNumberEnd.HasValue)
            {
                var seriesXDocument = XDocument.Load(new XmlNodeReader(seriesXml));
                if (usingAbsoluteData)
                {
                    extraEpisodesNode =
                        seriesXDocument.Descendants("Episode")
                                       .Where(
                                           x =>
                                           int.Parse(x.Element("absolute_number").Value) > episode.IndexNumber &&
                                           int.Parse(x.Element("absolute_number").Value) <= episode.IndexNumberEnd.Value).OrderBy(x => x.Element("absolute_number").Value).Select(x => x.ToXmlDocument());
                }
                else
                {
                    var all =
                        seriesXDocument.Descendants("Episode").Where(x => int.Parse(x.Element("SeasonNumber").Value) == seasonNumber.Value);

                    var xElements = all.Where(x => int.Parse(x.Element("EpisodeNumber").Value) > episode.IndexNumber && int.Parse(x.Element("EpisodeNumber").Value) <= episode.IndexNumberEnd.Value);
                    extraEpisodesNode = xElements.OrderBy(x => x.Element("EpisodeNumber").Value).Select(x => x.ToXmlDocument());
                }

            }
            var doc = new XmlDocument();
            doc.LoadXml(episodeNode.OuterXml);

            if (!episode.HasImage(ImageType.Primary))
            {
                var p = doc.SafeGetString("//filename");
                if (p != null)
                {
                    try
                    {
                        var url = TVUtils.BannerUrl + p;

                        await _providerManager.SaveImage(episode, url, RemoteSeriesProvider.Current.TvDbResourcePool, ImageType.Primary, null, cancellationToken)
                          .ConfigureAwait(false);
                    }
                    catch (HttpException)
                    {
                        status = ProviderRefreshStatus.CompletedWithErrors;
                    }
                }
            }
            if (!episode.LockedFields.Contains(MetadataFields.Overview))
            {
                var extraOverview = extraEpisodesNode.Aggregate("", (current, xmlDocument) => current + ("\r\n\r\n" + xmlDocument.SafeGetString("//Overview")));
                episode.Overview = doc.SafeGetString("//Overview") + extraOverview;
            }
            if (usingAbsoluteData)
                episode.IndexNumber = doc.SafeGetInt32("//absolute_number", -1);
            if (episode.IndexNumber < 0)
                episode.IndexNumber = doc.SafeGetInt32("//EpisodeNumber");
            if (!episode.LockedFields.Contains(MetadataFields.Name))
            {
                var extraNames = extraEpisodesNode.Aggregate("", (current, xmlDocument) => current + (", " + xmlDocument.SafeGetString("//EpisodeName")));
                episode.Name = doc.SafeGetString("//EpisodeName") + extraNames;
            }
            episode.CommunityRating = doc.SafeGetSingle("//Rating", -1, 10);
            var firstAired = doc.SafeGetString("//FirstAired");
            DateTime airDate;
            if (DateTime.TryParse(firstAired, out airDate) && airDate.Year > 1850)
            {
                episode.PremiereDate = airDate.ToUniversalTime();
                episode.ProductionYear = airDate.Year;
            }
            if (!episode.LockedFields.Contains(MetadataFields.Cast))
            {
                episode.People.Clear();

                var actors = doc.SafeGetString("//GuestStars");
                if (actors != null)
                {
                    // Sometimes tvdb actors have leading spaces
                    //Regex Info:
                    //The first block are the posible delimitators (open-parentheses should be there cause if dont the next block will fail)
                    //The second block Allow the delimitators to be part of the text if they're inside parentheses
                    var persons = Regex.Matches(actors, @"(?<delimitators>([^|,(])|(?<ignoreinParentheses>\([^)]*\)*))+")
                        .Cast<Match>()
                        .Select(m => m.Value)
                        .Where(i => !string.IsNullOrWhiteSpace(i) && !string.IsNullOrEmpty(i));

                    foreach (var person in persons.Select(str =>
                    {
                        var nameGroup = str.Split(new[] { '(' }, 2, StringSplitOptions.RemoveEmptyEntries);
                        var name = nameGroup[0].Trim();
                        var roles = nameGroup.Count() > 1 ? nameGroup[1].Trim() : null;
                        if (roles != null)
                            roles = roles.EndsWith(")") ? roles.Substring(0, roles.Length - 1) : roles;
                        return new PersonInfo { Type = PersonType.GuestStar, Name = name, Role = roles };
                    }))
                    {
                        episode.AddPerson(person);
                    }
                }
                foreach (var xmlDocument in extraEpisodesNode)
                {
                    var extraActors = xmlDocument.SafeGetString("//GuestStars");
                    if (extraActors == null) continue;
                    // Sometimes tvdb actors have leading spaces
                    var persons = Regex.Matches(extraActors, @"(?<delimitators>([^|,(])|(?<ignoreinParentheses>\([^)]*\)*))+")
                        .Cast<Match>()
                        .Select(m => m.Value)
                        .Where(i => !string.IsNullOrWhiteSpace(i) && !string.IsNullOrEmpty(i));

                    foreach (var person in persons.Select(str =>
                    {
                        var nameGroup = str.Split(new[] { '(' }, 2, StringSplitOptions.RemoveEmptyEntries);
                        var name = nameGroup[0].Trim();
                        var roles = nameGroup.Count() > 1 ? nameGroup[1].Trim() : null;
                        if (roles != null)
                            roles = roles.EndsWith(")") ? roles.Substring(0, roles.Length - 1) : roles;
                        return new PersonInfo { Type = PersonType.GuestStar, Name = name, Role = roles };
                    }))
                    {
                        episode.AddPerson(person);
                    }
                }

                var directors = doc.SafeGetString("//Director");
                if (directors != null)
                {
                    // Sometimes tvdb actors have leading spaces
                    foreach (var person in directors.Split(new[] { '|', ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                    .Where(i => !string.IsNullOrWhiteSpace(i))
                                                    .Select(str => new PersonInfo { Type = PersonType.Director, Name = str.Trim() }))
                    {
                        episode.AddPerson(person);
                    }
                }


                var writers = doc.SafeGetString("//Writer");
                if (writers != null)
                {
                    // Sometimes tvdb actors have leading spaces
                    foreach (var person in writers.Split(new[] { '|', ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                  .Where(i => !string.IsNullOrWhiteSpace(i))
                                                  .Select(str => new PersonInfo { Type = PersonType.Writer, Name = str.Trim() }))
                    {
                        episode.AddPerson(person);
                    }
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
