using CommonIO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.Omdb
{
    public class OmdbProvider
    {
        internal static readonly SemaphoreSlim ResourcePool = new SemaphoreSlim(1, 1);
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IFileSystem _fileSystem;
        private readonly IServerConfigurationManager _configurationManager;
        private readonly IHttpClient _httpClient;
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        public OmdbProvider(IJsonSerializer jsonSerializer, IHttpClient httpClient, IFileSystem fileSystem, IServerConfigurationManager configurationManager)
        {
            _jsonSerializer = jsonSerializer;
            _httpClient = httpClient;
            _fileSystem = fileSystem;
            _configurationManager = configurationManager;
        }

        public async Task Fetch<T>(MetadataResult<T> itemResult, string imdbId, string language, string country, CancellationToken cancellationToken)
            where T :BaseItem
        {
            if (string.IsNullOrWhiteSpace(imdbId))
            {
                throw new ArgumentNullException("imdbId");
            }

            T item = itemResult.Item;

            var result = await GetRootObject(imdbId, cancellationToken);

                // Only take the name and rating if the user's language is set to english, since Omdb has no localization
                if (string.Equals(language, "en", StringComparison.OrdinalIgnoreCase))
                {
                    item.Name = result.Title;

                    if (string.Equals(country, "us", StringComparison.OrdinalIgnoreCase))
                    {
                        item.OfficialRating = result.Rated;
                    }
                }

                int year;

                if (!string.IsNullOrEmpty(result.Year) && result.Year.Length >= 4
                    && int.TryParse(result.Year.Substring(0, 4), NumberStyles.Number, _usCulture, out year)
                    && year >= 0)
                {
                    item.ProductionYear = year;
                }

                var hasCriticRating = item as IHasCriticRating;
                if (hasCriticRating != null)
                {
                    // Seeing some bogus RT data on omdb for series, so filter it out here
                    // RT doesn't even have tv series
                    int tomatoMeter;

                    if (!string.IsNullOrEmpty(result.tomatoMeter)
                        && int.TryParse(result.tomatoMeter, NumberStyles.Integer, _usCulture, out tomatoMeter)
                        && tomatoMeter >= 0)
                    {
                        hasCriticRating.CriticRating = tomatoMeter;
                    }

                    if (!string.IsNullOrEmpty(result.tomatoConsensus)
                        && !string.Equals(result.tomatoConsensus, "No consensus yet.", StringComparison.OrdinalIgnoreCase))
                    {
                        hasCriticRating.CriticRatingSummary = WebUtility.HtmlDecode(result.tomatoConsensus);
                    }
                }

                int voteCount;

                if (!string.IsNullOrEmpty(result.imdbVotes)
                    && int.TryParse(result.imdbVotes, NumberStyles.Number, _usCulture, out voteCount)
                    && voteCount >= 0)
                {
                    item.VoteCount = voteCount;
                }

                float imdbRating;

                if (!string.IsNullOrEmpty(result.imdbRating)
                    && float.TryParse(result.imdbRating, NumberStyles.Any, _usCulture, out imdbRating)
                    && imdbRating >= 0)
                {
                    item.CommunityRating = imdbRating;
                }

                if (!string.IsNullOrEmpty(result.Website))
                {
                    item.HomePageUrl = result.Website;
                }

                if (!string.IsNullOrWhiteSpace(result.imdbID))
                {
                    item.SetProviderId(MetadataProviders.Imdb, result.imdbID);
                }

                ParseAdditionalMetadata(itemResult, result);
        }

        public async Task<bool> FetchEpisodeData<T>(MetadataResult<T> itemResult, int episodeNumber, int seasonNumber, string imdbId, string language, string country, CancellationToken cancellationToken)
            where T : BaseItem
        {
            if (string.IsNullOrWhiteSpace(imdbId))
            {
                throw new ArgumentNullException("imdbId");
            }

            T item = itemResult.Item;

            var seasonResult = await GetSeasonRootObject(imdbId, seasonNumber, cancellationToken);

            RootObject result = null;

            foreach (var episode in seasonResult.Episodes)
            {
                if (episode.Episode == episodeNumber)
                {
                    result = episode;
                    break;
                }
            }

            if (result == null)
            {
                return false;
            }


            // Only take the name and rating if the user's language is set to english, since Omdb has no localization
            if (string.Equals(language, "en", StringComparison.OrdinalIgnoreCase))
            {
                item.Name = result.Title;

                if (string.Equals(country, "us", StringComparison.OrdinalIgnoreCase))
                {
                    item.OfficialRating = result.Rated;
                }
            }

            int year;

            if (!string.IsNullOrEmpty(result.Year) && result.Year.Length >= 4
                && int.TryParse(result.Year.Substring(0, 4), NumberStyles.Number, _usCulture, out year)
                && year >= 0)
            {
                item.ProductionYear = year;
            }

            var hasCriticRating = item as IHasCriticRating;
            if (hasCriticRating != null)
            {
                // Seeing some bogus RT data on omdb for series, so filter it out here
                // RT doesn't even have tv series
                int tomatoMeter;

                if (!string.IsNullOrEmpty(result.tomatoMeter)
                    && int.TryParse(result.tomatoMeter, NumberStyles.Integer, _usCulture, out tomatoMeter)
                    && tomatoMeter >= 0)
                {
                    hasCriticRating.CriticRating = tomatoMeter;
                }

                if (!string.IsNullOrEmpty(result.tomatoConsensus)
                    && !string.Equals(result.tomatoConsensus, "No consensus yet.", StringComparison.OrdinalIgnoreCase))
                {
                    hasCriticRating.CriticRatingSummary = WebUtility.HtmlDecode(result.tomatoConsensus);
                }
            }

            int voteCount;

            if (!string.IsNullOrEmpty(result.imdbVotes)
                && int.TryParse(result.imdbVotes, NumberStyles.Number, _usCulture, out voteCount)
                && voteCount >= 0)
            {
                item.VoteCount = voteCount;
            }

            float imdbRating;

            if (!string.IsNullOrEmpty(result.imdbRating)
                && float.TryParse(result.imdbRating, NumberStyles.Any, _usCulture, out imdbRating)
                && imdbRating >= 0)
            {
                item.CommunityRating = imdbRating;
            }

            if (!string.IsNullOrEmpty(result.Website))
            {
                item.HomePageUrl = result.Website;
            }

            if (!string.IsNullOrWhiteSpace(result.imdbID))
            {
                item.SetProviderId(MetadataProviders.Imdb, result.imdbID);
            }

            ParseAdditionalMetadata(itemResult, result);

            return true;
        }

        internal async Task<RootObject> GetRootObject(string imdbId, CancellationToken cancellationToken)
        {
            var path = await EnsureItemInfo(imdbId, cancellationToken);

            string resultString;

            using (Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 131072))
            {
                using (var reader = new StreamReader(stream, new UTF8Encoding(false)))
                {
                    resultString = reader.ReadToEnd();
                    resultString = resultString.Replace("\"N/A\"", "\"\"");
                }
            }

            var result = _jsonSerializer.DeserializeFromString<RootObject>(resultString);
            return result;
        }

        internal async Task<SeasonRootObject> GetSeasonRootObject(string imdbId, int seasonId, CancellationToken cancellationToken)
        {
            var path = await EnsureSeasonInfo(imdbId, seasonId, cancellationToken);

            string resultString;

            using (Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 131072))
            {
                using (var reader = new StreamReader(stream, new UTF8Encoding(false)))
                {
                    resultString = reader.ReadToEnd();
                    resultString = resultString.Replace("\"N/A\"", "\"\"");
                }
            }

            var result = _jsonSerializer.DeserializeFromString<SeasonRootObject>(resultString);
            return result;
        }

        internal static bool IsValidSeries(Dictionary<string, string> seriesProviderIds)
        {
            string id;
            if (seriesProviderIds.TryGetValue(MetadataProviders.Imdb.ToString(), out id) && !string.IsNullOrEmpty(id))
            {
                // This check should ideally never be necessary but we're seeing some cases of this and haven't tracked them down yet.
                if (!string.IsNullOrWhiteSpace(id))
                {
                    return true;
                }
            }

            return false;
        }

        private async Task<string> EnsureItemInfo(string imdbId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(imdbId))
            {
                throw new ArgumentNullException("imdbId");
            }

            var imdbParam = imdbId.StartsWith("tt", StringComparison.OrdinalIgnoreCase) ? imdbId : "tt" + imdbId;

            var path = GetDataFilePath(imdbParam);

            var fileInfo = _fileSystem.GetFileSystemInfo(path);

            if (fileInfo.Exists)
            {
                // If it's recent or automatic updates are enabled, don't re-download
                if ((DateTime.UtcNow - _fileSystem.GetLastWriteTimeUtc(fileInfo)).TotalDays <= 3)
                {
                    return path;
                }
            }

            var url = string.Format("https://www.omdbapi.com/?i={0}&tomatoes=true", imdbParam);

            using (var stream = await _httpClient.Get(new HttpRequestOptions
            {
                Url = url,
                ResourcePool = ResourcePool,
                CancellationToken = cancellationToken

            }).ConfigureAwait(false))
            {
                var rootObject = _jsonSerializer.DeserializeFromStream<RootObject>(stream);
                _fileSystem.CreateDirectory(Path.GetDirectoryName(path));
                _jsonSerializer.SerializeToFile(rootObject, path);
            }

            return path;
        }

        private async Task<string> EnsureSeasonInfo(string seriesImdbId, int seasonId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(seriesImdbId))
            {
                throw new ArgumentNullException("imdbId");
            }

            var imdbParam = seriesImdbId.StartsWith("tt", StringComparison.OrdinalIgnoreCase) ? seriesImdbId : "tt" + seriesImdbId;

            var path = GetSeasonFilePath(imdbParam, seasonId);

            var fileInfo = _fileSystem.GetFileSystemInfo(path);

            if (fileInfo.Exists)
            {
                // If it's recent or automatic updates are enabled, don't re-download
                if ((DateTime.UtcNow - _fileSystem.GetLastWriteTimeUtc(fileInfo)).TotalDays <= 3)
                {
                    return path;
                }
            }

            var url = string.Format("https://www.omdbapi.com/?i={0}&season={1}&detail=full", imdbParam, seasonId);

            using (var stream = await _httpClient.Get(new HttpRequestOptions
            {
                Url = url,
                ResourcePool = ResourcePool,
                CancellationToken = cancellationToken

            }).ConfigureAwait(false))
            {
                var rootObject = _jsonSerializer.DeserializeFromStream<SeasonRootObject>(stream);
                _fileSystem.CreateDirectory(Path.GetDirectoryName(path));
                _jsonSerializer.SerializeToFile(rootObject, path);
            }

            return path;
        }

        internal string GetDataFilePath(string imdbId)
        {
            if (string.IsNullOrEmpty(imdbId))
            {
                throw new ArgumentNullException("imdbId");
            }

            var dataPath = Path.Combine(_configurationManager.ApplicationPaths.CachePath, "omdb");

            var filename = string.Format("{0}.json", imdbId);

            return Path.Combine(dataPath, filename);
        }

        internal string GetSeasonFilePath(string imdbId, int seasonId)
        {
            if (string.IsNullOrEmpty(imdbId))
            {
                throw new ArgumentNullException("imdbId");
            }

            var dataPath = Path.Combine(_configurationManager.ApplicationPaths.CachePath, "omdb");

            var filename = string.Format("{0}_season_{1}.json", imdbId, seasonId);

            return Path.Combine(dataPath, filename);
        }

        private void ParseAdditionalMetadata<T>(MetadataResult<T> itemResult, RootObject result)
            where T : BaseItem
        {
            T item = itemResult.Item;

            // Grab series genres because imdb data is better than tvdb. Leave movies alone
            // But only do it if english is the preferred language because this data will not be localized
            if (ShouldFetchGenres(item) &&
                !string.IsNullOrWhiteSpace(result.Genre))
            {
                item.Genres.Clear();

                foreach (var genre in result.Genre
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(i => i.Trim())
                    .Where(i => !string.IsNullOrWhiteSpace(i)))
                {
                    item.AddGenre(genre);
                }
            }

            var hasMetascore = item as IHasMetascore;
            if (hasMetascore != null)
            {
                float metascore;

                if (!string.IsNullOrEmpty(result.Metascore) && float.TryParse(result.Metascore, NumberStyles.Any, _usCulture, out metascore) && metascore >= 0)
                {
                    hasMetascore.Metascore = metascore;
                }
            }

            var hasAwards = item as IHasAwards;
            if (hasAwards != null && !string.IsNullOrEmpty(result.Awards))
            {
                hasAwards.AwardSummary = WebUtility.HtmlDecode(result.Awards);
            }

            var hasShortOverview = item as IHasShortOverview;
            if (hasShortOverview != null)
            {
                // Imdb plots are usually pretty short
                hasShortOverview.ShortOverview = result.Plot;
            }

            //if (!string.IsNullOrWhiteSpace(result.Director))
            //{
            //    var person = new PersonInfo
            //    {
            //        Name = result.Director.Trim(),
            //        Type = PersonType.Director
            //    };

            //    itemResult.AddPerson(person);
            //}

            //if (!string.IsNullOrWhiteSpace(result.Writer))
            //{
            //    var person = new PersonInfo
            //    {
            //        Name = result.Director.Trim(),
            //        Type = PersonType.Writer
            //    };

            //    itemResult.AddPerson(person);
            //}

            //if (!string.IsNullOrWhiteSpace(result.Actors))
            //{
            //    var actorList = result.Actors.Split(',');
            //    foreach (var actor in actorList)
            //    {
            //        if (!string.IsNullOrWhiteSpace(actor))
            //        {
            //            var person = new PersonInfo
            //            {
            //                Name = actor.Trim(),
            //                Type = PersonType.Actor
            //            };

            //            itemResult.AddPerson(person);
            //        }
            //    }
            //}
        }

        private bool ShouldFetchGenres(BaseItem item)
        {
            var lang = item.GetPreferredMetadataLanguage();

            // The data isn't localized and so can only be used for english users
            return string.Equals(lang, "en", StringComparison.OrdinalIgnoreCase);
        }

        internal class SeasonRootObject
        {
            public string Title { get; set; }
            public string seriesID { get; set; }
            public int Season { get; set; }
            public int? totalSeasons { get; set; }
            public RootObject[] Episodes { get; set; }
            public string Response { get; set; }
        }

        internal class RootObject
        {
            public string Title { get; set; }
            public string Year { get; set; }
            public string Rated { get; set; }
            public string Released { get; set; }
            public int Episode { get; set; }
            public string Runtime { get; set; }
            public string Genre { get; set; }
            public string Director { get; set; }
            public string Writer { get; set; }
            public string Actors { get; set; }
            public string Plot { get; set; }
            public string Poster { get; set; }
            public string imdbRating { get; set; }
            public string imdbVotes { get; set; }
            public string imdbID { get; set; }
            public string Type { get; set; }
            public string tomatoMeter { get; set; }
            public string tomatoImage { get; set; }
            public string tomatoRating { get; set; }
            public string tomatoReviews { get; set; }
            public string tomatoFresh { get; set; }
            public string tomatoRotten { get; set; }
            public string tomatoConsensus { get; set; }
            public string tomatoUserMeter { get; set; }
            public string tomatoUserRating { get; set; }
            public string tomatoUserReviews { get; set; }
            public string DVD { get; set; }
            public string BoxOffice { get; set; }
            public string Production { get; set; }
            public string Website { get; set; }
            public string Response { get; set; }

            public string Language { get; set; }
            public string Country { get; set; }
            public string Awards { get; set; }
            public string Metascore { get; set; }
        }

    }
}
