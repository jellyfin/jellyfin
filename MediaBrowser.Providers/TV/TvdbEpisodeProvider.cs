using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
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
    class TvdbEpisodeProvider : IRemoteMetadataProvider<Episode, EpisodeInfo>, IItemIdentityProvider<EpisodeInfo, EpisodeIdentity>, IHasChangeMonitor
    {
        internal static TvdbEpisodeProvider Current;
        private readonly IFileSystem _fileSystem;
        private readonly IServerConfigurationManager _config;
        private readonly IHttpClient _httpClient;

        public TvdbEpisodeProvider(IFileSystem fileSystem, IServerConfigurationManager config, IHttpClient httpClient)
        {
            _fileSystem = fileSystem;
            _config = config;
            _httpClient = httpClient;
            Current = this;
        }

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(EpisodeInfo searchInfo, CancellationToken cancellationToken)
        {
            var list = new List<RemoteSearchResult>();

            var identity = searchInfo.Identities.FirstOrDefault(id => id.Type == MetadataProviders.Tvdb.ToString()) ?? await FindIdentity(searchInfo).ConfigureAwait(false);

            if (identity != null)
            {
                await TvdbSeriesProvider.Current.EnsureSeriesInfo(identity.SeriesId, searchInfo.MetadataLanguage,
                        cancellationToken).ConfigureAwait(false);

                var seriesDataPath = TvdbSeriesProvider.GetSeriesDataPath(_config.ApplicationPaths, identity.SeriesId);

                try
                {
                    var item = FetchEpisodeData(searchInfo, identity, seriesDataPath, searchInfo.SeriesProviderIds, cancellationToken);

                    if (item != null)
                    {
                        list.Add(new RemoteSearchResult
                        {
                            IndexNumber = item.IndexNumber,
                            Name = item.Name,
                            ParentIndexNumber = item.ParentIndexNumber,
                            PremiereDate = item.PremiereDate,
                            ProductionYear = item.ProductionYear,
                            ProviderIds = item.ProviderIds,
                            SearchProviderName = Name,
                            IndexNumberEnd = item.IndexNumberEnd
                        });
                    }
                }
                catch (FileNotFoundException)
                {
                    // Don't fail the provider because this will just keep on going and going.
                }
                catch (DirectoryNotFoundException)
                {
                    // Don't fail the provider because this will just keep on going and going.
                }
            }

            return list;
        }

        public string Name
        {
            get { return "TheTVDB"; }
        }

        public async Task<MetadataResult<Episode>> GetMetadata(EpisodeInfo searchInfo, CancellationToken cancellationToken)
        {
            var identity = searchInfo.Identities.FirstOrDefault(id => id.Type == MetadataProviders.Tvdb.ToString()) ?? await FindIdentity(searchInfo).ConfigureAwait(false);

            var result = new MetadataResult<Episode>();

            if (identity != null)
            {
                var seriesDataPath = TvdbSeriesProvider.GetSeriesDataPath(_config.ApplicationPaths, identity.SeriesId);

                try
                {
                    result.Item = FetchEpisodeData(searchInfo, identity, seriesDataPath, searchInfo.SeriesProviderIds, cancellationToken);
                    result.HasMetadata = result.Item != null;
                }
                catch (FileNotFoundException)
                {
                    // Don't fail the provider because this will just keep on going and going.
                }
                catch (DirectoryNotFoundException)
                {
                    // Don't fail the provider because this will just keep on going and going.
                }
            }

            return result;
        }

        public bool HasChanged(IHasMetadata item, IDirectoryService directoryService, DateTime date)
        {
            // Only enable for virtual items
            if (item.LocationType != LocationType.Virtual)
            {
                return false;
            }

            var episode = (Episode)item;
            var series = episode.Series;

            var seriesId = series != null ? series.GetProviderId(MetadataProviders.Tvdb) : null;

            if (!string.IsNullOrEmpty(seriesId))
            {
                // Process images
                var seriesDataPath = TvdbSeriesProvider.GetSeriesDataPath(_config.ApplicationPaths, seriesId);

                var files = GetEpisodeXmlFiles(episode.ParentIndexNumber, episode.IndexNumber, episode.IndexNumberEnd, seriesDataPath);

                return files.Any(i => _fileSystem.GetLastWriteTimeUtc(i) > date);
            }

            return false;
        }

        /// <summary>
        /// Gets the episode XML files.
        /// </summary>
        /// <param name="seasonNumber">The season number.</param>
        /// <param name="episodeNumber">The episode number.</param>
        /// <param name="endingEpisodeNumber">The ending episode number.</param>
        /// <param name="seriesDataPath">The series data path.</param>
        /// <returns>List{FileInfo}.</returns>
        internal List<FileInfo> GetEpisodeXmlFiles(int? seasonNumber, int? episodeNumber, int? endingEpisodeNumber, string seriesDataPath)
        {
            var files = new List<FileInfo>();

            if (episodeNumber == null)
            {
                return files;
            }

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

            var end = endingEpisodeNumber ?? episodeNumber;
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
        /// Fetches the episode data.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="identity">The identity.</param>
        /// <param name="seriesDataPath">The series data path.</param>
        /// <param name="seriesProviderIds">The series provider ids.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.Boolean}.</returns>
        private Episode FetchEpisodeData(EpisodeInfo id, EpisodeIdentity identity, string seriesDataPath, Dictionary<string, string> seriesProviderIds, CancellationToken cancellationToken)
        {
            var episodeNumber = identity.IndexNumber;
            var seasonOffset = TvdbSeriesProvider.GetSeriesOffset(seriesProviderIds) ?? 0;
            var seasonNumber = identity.SeasonIndex + seasonOffset;
            
            string file;
            var success = false;
            var usingAbsoluteData = false;

            var episode = new Episode
            {
                IndexNumber = id.IndexNumber,
                ParentIndexNumber = id.ParentIndexNumber,
                IndexNumberEnd = id.IndexNumberEnd
            };

            try
            {
                if (seasonNumber != null)
                {
                    file = Path.Combine(seriesDataPath, string.Format("episode-{0}-{1}.xml", seasonNumber.Value, episodeNumber));
                    FetchMainEpisodeInfo(episode, file, cancellationToken);

                    success = true;
                }
            }
            catch (FileNotFoundException)
            {
                // Could be using absolute numbering
                if (seasonNumber.HasValue && seasonNumber.Value != 1)
                {
                    throw;
                }
            }

            if (!success)
            {
                file = Path.Combine(seriesDataPath, string.Format("episode-abs-{0}.xml", episodeNumber));

                FetchMainEpisodeInfo(episode, file, cancellationToken);
                usingAbsoluteData = true;
                success = true;
            }

            var end = identity.IndexNumberEnd ?? episodeNumber;
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
                catch (DirectoryNotFoundException)
                {
                    break;
                }

                episodeNumber++;
            }

            return success ? episode : null;
        }

        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        private void FetchMainEpisodeInfo(Episode item, string xmlFile, CancellationToken cancellationToken)
        {
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

                                case "DVD_episodenumber":
                                    {
                                        var val = reader.ReadElementContentAsString();

                                        if (!string.IsNullOrWhiteSpace(val))
                                        {
                                            float num;

                                            if (float.TryParse(val, NumberStyles.Any, _usCulture, out num))
                                            {
                                                item.DvdEpisodeNumber = num;
                                            }
                                        }

                                        break;
                                    }

                                case "DVD_season":
                                    {
                                        var val = reader.ReadElementContentAsString();

                                        if (!string.IsNullOrWhiteSpace(val))
                                        {
                                            float num;

                                            if (float.TryParse(val, NumberStyles.Any, _usCulture, out num))
                                            {
                                                item.DvdSeasonNumber = Convert.ToInt32(num);
                                            }
                                        }

                                        break;
                                    }

                                case "absolute_number":
                                    {
                                        var val = reader.ReadElementContentAsString();

                                        if (!string.IsNullOrWhiteSpace(val))
                                        {
                                            int rval;

                                            // int.TryParse is local aware, so it can be probamatic, force us culture
                                            if (int.TryParse(val, NumberStyles.Integer, _usCulture, out rval))
                                            {
                                                item.AbsoluteEpisodeNumber = rval;
                                            }
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
                if (!string.IsNullOrWhiteSpace(person.Name))
                {
                    item.AddPerson(person);
                }
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

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClient.GetResponse(new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = url,
                ResourcePool = TvdbSeriesProvider.Current.TvDbResourcePool
            });
        }

        public Task<EpisodeIdentity> FindIdentity(EpisodeInfo info)
        {
            string seriesTvdbId;
            info.SeriesProviderIds.TryGetValue(MetadataProviders.Tvdb.ToString(), out seriesTvdbId);

            if (string.IsNullOrEmpty(seriesTvdbId) || info.IndexNumber == null)
            {
                return Task.FromResult<EpisodeIdentity>(null);
            }
            
            var id = new EpisodeIdentity
            {
                Type = MetadataProviders.Tvdb.ToString(),
                SeriesId = seriesTvdbId,
                SeasonIndex = info.ParentIndexNumber,
                IndexNumber = info.IndexNumber.Value,
                IndexNumberEnd = info.IndexNumberEnd
            };

            return Task.FromResult(id);
        }

        public int Order { get { return 0; } }
    }
}
