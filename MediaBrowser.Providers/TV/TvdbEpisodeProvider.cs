using MediaBrowser.Common.IO;
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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace MediaBrowser.Providers.TV
{

    /// <summary>
    /// Class RemoteEpisodeProvider
    /// </summary>
    class TvdbEpisodeProvider : BaseMetadataProvider
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
        private readonly IFileSystem _fileSystem;

        internal static TvdbEpisodeProvider Current;

        /// <summary>
        /// Initializes a new instance of the <see cref="TvdbEpisodeProvider" /> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="logManager">The log manager.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="providerManager">The provider manager.</param>
        public TvdbEpisodeProvider(IHttpClient httpClient, ILogManager logManager, IServerConfigurationManager configurationManager, IProviderManager providerManager, IFileSystem fileSystem)
            : base(logManager, configurationManager)
        {
            HttpClient = httpClient;
            _providerManager = providerManager;
            _fileSystem = fileSystem;
            Current = this;
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
                return "5";
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
            var locationType = item.LocationType;

            // Always use tvdb updates for non-file system episodes
            if (locationType != LocationType.Remote && locationType != LocationType.Virtual)
            {
                // Don't proceed if there's local metadata
                if (!ConfigurationManager.Configuration.EnableTvDbUpdates && HasLocalMeta(item))
                {
                    return false;
                }
            }

            return base.NeedsRefreshInternal(item, providerInfo);
        }

        protected override bool NeedsRefreshBasedOnCompareDate(BaseItem item, BaseProviderInfo providerInfo)
        {
            var episode = (Episode)item;

            var seriesId = episode.Series != null ? episode.Series.GetProviderId(MetadataProviders.Tvdb) : null;

            if (!string.IsNullOrEmpty(seriesId))
            {
                // Process images
                var seriesDataPath = TvdbSeriesProvider.GetSeriesDataPath(ConfigurationManager.ApplicationPaths, seriesId);

                var files = GetEpisodeXmlFiles(episode, seriesDataPath);

                if (files.Count > 0)
                {
                    return files.Select(i => _fileSystem.GetLastWriteTimeUtc(i)).Max() > providerInfo.LastRefreshed;
                }
            }
            
            return false;
        }

        /// <summary>
        /// Gets the episode XML files.
        /// </summary>
        /// <param name="episode">The episode.</param>
        /// <param name="seriesDataPath">The series data path.</param>
        /// <returns>List{FileInfo}.</returns>
        internal List<FileInfo> GetEpisodeXmlFiles(Episode episode, string seriesDataPath)
        {
            var files = new List<FileInfo>();

            if (episode.IndexNumber == null)
            {
                return files;
            }

            var episodeNumber = episode.IndexNumber.Value;
            var seasonNumber = episode.ParentIndexNumber;

            if (seasonNumber == null)
            {
                return files;
            }

            var file = Path.Combine(seriesDataPath, string.Format("episode-{0}-{1}.xml", seasonNumber.Value, episodeNumber));

            var fileInfo = new FileInfo(file);
            var usingAbsoluteData = false;

            if (fileInfo.Exists)
            {
                files.Add(fileInfo);
            }
            else
            {
                file = Path.Combine(seriesDataPath, string.Format("episode-abs-{0}.xml", episodeNumber));
                fileInfo = new FileInfo(file);
                if (fileInfo.Exists)
                {
                    files.Add(fileInfo);
                    usingAbsoluteData = true;
                }
            }

            var end = episode.IndexNumberEnd ?? episodeNumber;
            episodeNumber++;

            while (episodeNumber <= end)
            {
                if (usingAbsoluteData)
                {
                    file = Path.Combine(seriesDataPath, string.Format("episode-abs-{0}.xml", episodeNumber));
                }
                else
                {
                    file = Path.Combine(seriesDataPath, string.Format("episode-{0}-{1}.xml", seasonNumber.Value, episodeNumber));
                }

                fileInfo = new FileInfo(file);
                if (fileInfo.Exists)
                {
                    files.Add(fileInfo);
                }
                else
                {
                    break;
                }

                episodeNumber++;
            }

            return files;
        }

        /// <summary>
        /// Fetches metadata and returns true or false indicating if any work that requires persistence was done
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="force">if set to <c>true</c> [force].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.Boolean}.</returns>
        public override async Task<bool> FetchAsync(BaseItem item, bool force, BaseProviderInfo providerInfo, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var status = ProviderRefreshStatus.Success;

            var episode = (Episode)item;

            var seriesId = episode.Series != null ? episode.Series.GetProviderId(MetadataProviders.Tvdb) : null;

            if (!string.IsNullOrEmpty(seriesId))
            {
                var seriesDataPath = TvdbSeriesProvider.GetSeriesDataPath(ConfigurationManager.ApplicationPaths, seriesId);

                try
                {
                    status = await FetchEpisodeData(episode, seriesDataPath, cancellationToken).ConfigureAwait(false);
                }
                catch (FileNotFoundException)
                {
                    // Don't fail the provider because this will just keep on going and going.
                }
            }

            SetLastRefreshed(item, DateTime.UtcNow, providerInfo, status);
            return true;
        }


        /// <summary>
        /// Fetches the episode data.
        /// </summary>
        /// <param name="episode">The episode.</param>
        /// <param name="seriesDataPath">The series data path.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.Boolean}.</returns>
        private async Task<ProviderRefreshStatus> FetchEpisodeData(Episode episode, string seriesDataPath, CancellationToken cancellationToken)
        {
            var status = ProviderRefreshStatus.Success;

            if (episode.IndexNumber == null)
            {
                return status;
            }

            var episodeNumber = episode.IndexNumber.Value;
            var seasonNumber = episode.ParentIndexNumber;

            if (seasonNumber == null)
            {
                return status;
            }

            var file = Path.Combine(seriesDataPath, string.Format("episode-{0}-{1}.xml", seasonNumber.Value, episodeNumber));
            var success = false;
            var usingAbsoluteData = false;

            try
            {
                status = await FetchMainEpisodeInfo(episode, file, cancellationToken).ConfigureAwait(false);

                success = true;
            }
            catch (FileNotFoundException)
            {
                // Could be using absolute numbering
                if (seasonNumber.Value != 1)
                {
                    throw;
                }
            }

            if (!success)
            {
                file = Path.Combine(seriesDataPath, string.Format("episode-abs-{0}.xml", episodeNumber));

                status = await FetchMainEpisodeInfo(episode, file, cancellationToken).ConfigureAwait(false);
                usingAbsoluteData = true;
            }

            var end = episode.IndexNumberEnd ?? episodeNumber;
            episodeNumber++;

            while (episodeNumber <= end)
            {
                if (usingAbsoluteData)
                {
                    file = Path.Combine(seriesDataPath, string.Format("episode-abs-{0}.xml", episodeNumber));
                }
                else
                {
                    file = Path.Combine(seriesDataPath, string.Format("episode-{0}-{1}.xml", seasonNumber.Value, episodeNumber));
                }

                try
                {
                    FetchAdditionalPartInfo(episode, file, cancellationToken);
                }
                catch (FileNotFoundException)
                {
                    break;
                }

                episodeNumber++;
            }

            return status;
        }

        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        private async Task<ProviderRefreshStatus> FetchMainEpisodeInfo(Episode item, string xmlFile, CancellationToken cancellationToken)
        {
            var status = ProviderRefreshStatus.Success;

            using (var streamReader = new StreamReader(xmlFile, Encoding.UTF8))
            {
                if (!item.LockedFields.Contains(MetadataFields.Cast))
                {
                    item.People.Clear();
                }

                // Use XmlReader for best performance
                using (var reader = XmlReader.Create(streamReader, new XmlReaderSettings
                {
                    CheckCharacters = false,
                    IgnoreProcessingInstructions = true,
                    IgnoreComments = true,
                    ValidationType = ValidationType.None
                }))
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
                                case "id":
                                    {
                                        var val = reader.ReadElementContentAsString();
                                        if (!string.IsNullOrWhiteSpace(val))
                                        {
                                            item.SetProviderId(MetadataProviders.Tvdb, val);
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

                                case "airsbefore_episode":
                                    {
                                        var val = reader.ReadElementContentAsString();

                                        if (!string.IsNullOrWhiteSpace(val))
                                        {
                                            int rval;

                                            // int.TryParse is local aware, so it can be probamatic, force us culture
                                            if (int.TryParse(val, NumberStyles.Integer, _usCulture, out rval))
                                            {
                                                item.AirsBeforeEpisodeNumber = rval;
                                            }
                                        }

                                        break;
                                    }

                                case "airsafter_season":
                                    {
                                        var val = reader.ReadElementContentAsString();

                                        if (!string.IsNullOrWhiteSpace(val))
                                        {
                                            int rval;

                                            // int.TryParse is local aware, so it can be probamatic, force us culture
                                            if (int.TryParse(val, NumberStyles.Integer, _usCulture, out rval))
                                            {
                                                item.AirsAfterSeasonNumber = rval;
                                            }
                                        }

                                        break;
                                    }

                                case "airsbefore_season":
                                    {
                                        var val = reader.ReadElementContentAsString();

                                        if (!string.IsNullOrWhiteSpace(val))
                                        {
                                            int rval;

                                            // int.TryParse is local aware, so it can be probamatic, force us culture
                                            if (int.TryParse(val, NumberStyles.Integer, _usCulture, out rval))
                                            {
                                                item.AirsBeforeSeasonNumber = rval;
                                            }
                                        }
                                        
                                        break;
                                    }

                                case "EpisodeName":
                                    {
                                        if (!item.LockedFields.Contains(MetadataFields.Name))
                                        {
                                            var val = reader.ReadElementContentAsString();
                                            if (!string.IsNullOrWhiteSpace(val))
                                            {
                                                item.Name = val;
                                            }
                                        }
                                        break;
                                    }

                                case "filename":
                                    {
                                        if (string.IsNullOrEmpty(item.PrimaryImagePath))
                                        {
                                            var val = reader.ReadElementContentAsString();
                                            if (!string.IsNullOrWhiteSpace(val))
                                            {
                                                try
                                                {
                                                    var url = TVUtils.BannerUrl + val;

                                                    await _providerManager.SaveImage(item, url, TvdbSeriesProvider.Current.TvDbResourcePool, ImageType.Primary, null, cancellationToken).ConfigureAwait(false);
                                                }
                                                catch (HttpException)
                                                {
                                                    status = ProviderRefreshStatus.CompletedWithErrors;
                                                }
                                            }
                                        }
                                        break;
                                    }

                                case "Overview":
                                    {
                                        if (!item.LockedFields.Contains(MetadataFields.Overview))
                                        {
                                            var val = reader.ReadElementContentAsString();
                                            if (!string.IsNullOrWhiteSpace(val))
                                            {
                                                item.Overview = val;
                                            }
                                        }
                                        break;
                                    }
                                case "Rating":
                                    {
                                        var val = reader.ReadElementContentAsString();

                                        if (!string.IsNullOrWhiteSpace(val))
                                        {
                                            float rval;

                                            // float.TryParse is local aware, so it can be probamatic, force us culture
                                            if (float.TryParse(val, NumberStyles.AllowDecimalPoint, _usCulture, out rval))
                                            {
                                                item.CommunityRating = rval;
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

                                case "Director":
                                    {
                                        var val = reader.ReadElementContentAsString();

                                        if (!string.IsNullOrWhiteSpace(val))
                                        {
                                            if (!item.LockedFields.Contains(MetadataFields.Cast))
                                            {
                                                AddPeople(item, val, PersonType.Director);
                                            }
                                        }

                                        break;
                                    }
                                case "GuestStars":
                                    {
                                        var val = reader.ReadElementContentAsString();

                                        if (!string.IsNullOrWhiteSpace(val))
                                        {
                                            if (!item.LockedFields.Contains(MetadataFields.Cast))
                                            {
                                                AddGuestStars(item, val);
                                            }
                                        }

                                        break;
                                    }
                                case "Writer":
                                    {
                                        var val = reader.ReadElementContentAsString();

                                        if (!string.IsNullOrWhiteSpace(val))
                                        {
                                            if (!item.LockedFields.Contains(MetadataFields.Cast))
                                            {
                                                AddPeople(item, val, PersonType.Writer);
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

            return status;
        }

        private void AddPeople(BaseItem item, string val, string personType)
        {
            // Sometimes tvdb actors have leading spaces
            foreach (var person in val.Split(new[] { '|', ',' }, StringSplitOptions.RemoveEmptyEntries)
                                            .Where(i => !string.IsNullOrWhiteSpace(i))
                                            .Select(str => new PersonInfo { Type = personType, Name = str.Trim() }))
            {
                item.AddPerson(person);
            }
        }

        private void AddGuestStars(BaseItem item, string val)
        {
            // Sometimes tvdb actors have leading spaces
            //Regex Info:
            //The first block are the posible delimitators (open-parentheses should be there cause if dont the next block will fail)
            //The second block Allow the delimitators to be part of the text if they're inside parentheses
            var persons = Regex.Matches(val, @"(?<delimitators>([^|,(])|(?<ignoreinParentheses>\([^)]*\)*))+")
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
                item.AddPerson(person);
            }
        }

        private void FetchAdditionalPartInfo(Episode item, string xmlFile, CancellationToken cancellationToken)
        {
            using (var streamReader = new StreamReader(xmlFile, Encoding.UTF8))
            {
                // Use XmlReader for best performance
                using (var reader = XmlReader.Create(streamReader, new XmlReaderSettings
                {
                    CheckCharacters = false,
                    IgnoreProcessingInstructions = true,
                    IgnoreComments = true,
                    ValidationType = ValidationType.None
                }))
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
                                case "EpisodeName":
                                    {
                                        if (!item.LockedFields.Contains(MetadataFields.Name))
                                        {
                                            var val = reader.ReadElementContentAsString();
                                            if (!string.IsNullOrWhiteSpace(val))
                                            {
                                                item.Name += ", " + val;
                                            }
                                        }
                                        break;
                                    }

                                case "Overview":
                                    {
                                        if (!item.LockedFields.Contains(MetadataFields.Overview))
                                        {
                                            var val = reader.ReadElementContentAsString();
                                            if (!string.IsNullOrWhiteSpace(val))
                                            {
                                                item.Overview += Environment.NewLine + Environment.NewLine + val;
                                            }
                                        }
                                        break;
                                    }
                                case "Director":
                                    {
                                        var val = reader.ReadElementContentAsString();

                                        if (!string.IsNullOrWhiteSpace(val))
                                        {
                                            if (!item.LockedFields.Contains(MetadataFields.Cast))
                                            {
                                                AddPeople(item, val, PersonType.Director);
                                            }
                                        }

                                        break;
                                    }
                                case "GuestStars":
                                    {
                                        var val = reader.ReadElementContentAsString();

                                        if (!string.IsNullOrWhiteSpace(val))
                                        {
                                            if (!item.LockedFields.Contains(MetadataFields.Cast))
                                            {
                                                AddGuestStars(item, val);
                                            }
                                        }

                                        break;
                                    }
                                case "Writer":
                                    {
                                        var val = reader.ReadElementContentAsString();

                                        if (!string.IsNullOrWhiteSpace(val))
                                        {
                                            if (!item.LockedFields.Contains(MetadataFields.Cast))
                                            {
                                                AddPeople(item, val, PersonType.Writer);
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
