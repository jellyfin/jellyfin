using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Providers.Movies
{
    /// <summary>
    /// Class TmdbPersonProvider
    /// </summary>
    public class TmdbPersonProvider : BaseMetadataProvider
    {
        /// <summary>
        /// The meta file name
        /// </summary>
        protected const string MetaFileName = "MBPerson.json";

        protected readonly IProviderManager ProviderManager;
        
        public TmdbPersonProvider(IHttpClient httpClient, IJsonSerializer jsonSerializer, ILogManager logManager, IServerConfigurationManager configurationManager, IProviderManager providerManager)
            : base(logManager, configurationManager)
        {
            if (jsonSerializer == null)
            {
                throw new ArgumentNullException("jsonSerializer");
            }
            if (httpClient == null)
            {
                throw new ArgumentNullException("httpClient");
            }
            HttpClient = httpClient;
            JsonSerializer = jsonSerializer;
            ProviderManager = providerManager;
        }

        /// <summary>
        /// Gets the json serializer.
        /// </summary>
        /// <value>The json serializer.</value>
        protected IJsonSerializer JsonSerializer { get; private set; }

        /// <summary>
        /// Gets the HTTP client.
        /// </summary>
        /// <value>The HTTP client.</value>
        protected IHttpClient HttpClient { get; private set; }

        /// <summary>
        /// Supportses the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool Supports(BaseItem item)
        {
            return item is Person;
        }

        /// <summary>
        /// Needses the refresh internal.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="providerInfo">The provider info.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        protected override bool NeedsRefreshInternal(BaseItem item, BaseProviderInfo providerInfo)
        {
            //we fetch if either info or image needed and haven't already tried recently
            return (string.IsNullOrEmpty(item.PrimaryImagePath) || !item.ResolveArgs.ContainsMetaFileByName(MetaFileName))
                && DateTime.Today.Subtract(providerInfo.LastRefreshed).TotalDays > ConfigurationManager.Configuration.MetadataRefreshDays;
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

            var person = (Person)item;
            var tasks = new List<Task>();

            var id = person.GetProviderId(MetadataProviders.Tmdb);

            // We don't already have an Id, need to fetch it
            if (string.IsNullOrEmpty(id))
            {
                id = await GetTmdbId(item, cancellationToken).ConfigureAwait(false);
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (!string.IsNullOrEmpty(id))
            {
                //get info only if not already saved
                if (!item.ResolveArgs.ContainsMetaFileByName(MetaFileName))
                {
                    tasks.Add(FetchInfo(person, id, cancellationToken));
                }

                //get image only if not already there
                if (string.IsNullOrEmpty(item.PrimaryImagePath))
                {
                    tasks.Add(FetchImages(person, id, cancellationToken));
                }

                //and wait for them to complete
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            else
            {
                Logger.Debug("TmdbPersonProvider Unable to obtain id for " + item.Name);
            }

            SetLastRefreshed(item, DateTime.UtcNow);
            return true;
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
        /// Gets the TMDB id.
        /// </summary>
        /// <param name="person">The person.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.String}.</returns>
        private async Task<string> GetTmdbId(BaseItem person, CancellationToken cancellationToken)
        {
            string url = string.Format(@"http://api.themoviedb.org/3/search/person?api_key={1}&query={0}", WebUtility.UrlEncode(person.Name), MovieDbProvider.ApiKey);
            PersonSearchResults searchResult = null;

            try
            {
                using (Stream json = await HttpClient.Get(url, MovieDbProvider.Current.MovieDbResourcePool, cancellationToken).ConfigureAwait(false))
                {
                    searchResult = JsonSerializer.DeserializeFromStream<PersonSearchResults>(json);
                }
            }
            catch (HttpException)
            {
            }

            return searchResult != null && searchResult.Total_Results > 0 ? searchResult.Results[0].Id.ToString() : null;
        }

        /// <summary>
        /// Fetches the info.
        /// </summary>
        /// <param name="person">The person.</param>
        /// <param name="id">The id.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        private async Task FetchInfo(Person person, string id, CancellationToken cancellationToken)
        {
            string url = string.Format(@"http://api.themoviedb.org/3/person/{1}?api_key={0}", MovieDbProvider.ApiKey, id);
            PersonResult searchResult = null;

            try
            {
                using (Stream json = await HttpClient.Get(url, MovieDbProvider.Current.MovieDbResourcePool, cancellationToken).ConfigureAwait(false))
                {
                    if (json != null)
                    {
                        searchResult = JsonSerializer.DeserializeFromStream<PersonResult>(json);
                    }
                }
            }
            catch (HttpException)
            {
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (searchResult != null && searchResult.Biography != null)
            {
                ProcessInfo(person, searchResult);

                //save locally
                var memoryStream = new MemoryStream();

                JsonSerializer.SerializeToStream(searchResult, memoryStream);

                await ProviderManager.SaveToLibraryFilesystem(person, Path.Combine(person.MetaLocation, MetaFileName), memoryStream, cancellationToken);

                Logger.Debug("TmdbPersonProvider downloaded and saved information for {0}", person.Name);
            }
        }

        /// <summary>
        /// Processes the info.
        /// </summary>
        /// <param name="person">The person.</param>
        /// <param name="searchResult">The search result.</param>
        protected void ProcessInfo(Person person, PersonResult searchResult)
        {
            person.Overview = searchResult.Biography;

            DateTime date;

            if (DateTime.TryParseExact(searchResult.Birthday, "yyyy-MM-dd", new CultureInfo("en-US"), DateTimeStyles.None, out date))
            {
                person.PremiereDate = date.ToUniversalTime();
            }

            if (DateTime.TryParseExact(searchResult.Deathday, "yyyy-MM-dd", new CultureInfo("en-US"), DateTimeStyles.None, out date))
            {
                person.EndDate = date.ToUniversalTime();
            }

            if (!string.IsNullOrEmpty(searchResult.Homepage))
            {
                person.HomePageUrl = searchResult.Homepage;
            }

            if (!string.IsNullOrEmpty(searchResult.Place_Of_Birth))
            {
                person.AddProductionLocation(searchResult.Place_Of_Birth);
            }
            
            person.SetProviderId(MetadataProviders.Tmdb, searchResult.Id.ToString());
        }

        /// <summary>
        /// Fetches the images.
        /// </summary>
        /// <param name="person">The person.</param>
        /// <param name="id">The id.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        private async Task FetchImages(Person person, string id, CancellationToken cancellationToken)
        {
            string url = string.Format(@"http://api.themoviedb.org/3/person/{1}/images?api_key={0}", MovieDbProvider.ApiKey, id);

            PersonImages searchResult = null;

            try
            {
                using (Stream json = await HttpClient.Get(url, MovieDbProvider.Current.MovieDbResourcePool, cancellationToken).ConfigureAwait(false))
                {
                    if (json != null)
                    {
                        searchResult = JsonSerializer.DeserializeFromStream<PersonImages>(json);
                    }
                }
            }
            catch (HttpException)
            {
            }

            if (searchResult != null && searchResult.Profiles.Count > 0)
            {
                //get our language
                var profile =
                    searchResult.Profiles.FirstOrDefault(
                        p =>
                        !string.IsNullOrEmpty(p.Iso_639_1) &&
                        p.Iso_639_1.Equals(ConfigurationManager.Configuration.PreferredMetadataLanguage,
                                          StringComparison.OrdinalIgnoreCase));
                if (profile == null)
                {
                    //didn't find our language - try first null one
                    profile =
                        searchResult.Profiles.FirstOrDefault(
                            p =>
                                !string.IsNullOrEmpty(p.Iso_639_1) &&
                            p.Iso_639_1.Equals(ConfigurationManager.Configuration.PreferredMetadataLanguage,
                                              StringComparison.OrdinalIgnoreCase));

                }
                if (profile == null)
                {
                    //still nothing - just get first one
                    profile = searchResult.Profiles[0];
                }
                if (profile != null)
                {
                    var tmdbSettings = await MovieDbProvider.Current.TmdbSettings.ConfigureAwait(false);

                    var img = await DownloadAndSaveImage(person, tmdbSettings.images.base_url + ConfigurationManager.Configuration.TmdbFetchedProfileSize + profile.File_Path,
                                             "folder" + Path.GetExtension(profile.File_Path), cancellationToken).ConfigureAwait(false);

                    if (!string.IsNullOrEmpty(img))
                    {
                        person.PrimaryImagePath = img;
                    }
                }
            }
        }

        /// <summary>
        /// Downloads the and save image.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="source">The source.</param>
        /// <param name="targetName">Name of the target.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.String}.</returns>
        private async Task<string> DownloadAndSaveImage(BaseItem item, string source, string targetName, CancellationToken cancellationToken)
        {
            if (source == null) return null;

            //download and save locally (if not already there)
            var localPath = Path.Combine(item.MetaLocation, targetName);
            if (!item.ResolveArgs.ContainsMetaFileByName(targetName))
            {
                using (var sourceStream = await HttpClient.GetMemoryStream(source, MovieDbProvider.Current.MovieDbResourcePool, cancellationToken).ConfigureAwait(false))
                {
                    await ProviderManager.SaveToLibraryFilesystem(item, localPath, sourceStream, cancellationToken).ConfigureAwait(false);

                    Logger.Debug("TmdbPersonProvider downloaded and saved image for {0}", item.Name);
                }
            }
            return localPath;
        }

        #region Result Objects
        /// <summary>
        /// Class PersonSearchResult
        /// </summary>
        public class PersonSearchResult
        {
            /// <summary>
            /// Gets or sets a value indicating whether this <see cref="PersonSearchResult" /> is adult.
            /// </summary>
            /// <value><c>true</c> if adult; otherwise, <c>false</c>.</value>
            public bool Adult { get; set; }
            /// <summary>
            /// Gets or sets the id.
            /// </summary>
            /// <value>The id.</value>
            public int Id { get; set; }
            /// <summary>
            /// Gets or sets the name.
            /// </summary>
            /// <value>The name.</value>
            public string Name { get; set; }
            /// <summary>
            /// Gets or sets the profile_ path.
            /// </summary>
            /// <value>The profile_ path.</value>
            public string Profile_Path { get; set; }
        }

        /// <summary>
        /// Class PersonSearchResults
        /// </summary>
        public class PersonSearchResults
        {
            /// <summary>
            /// Gets or sets the page.
            /// </summary>
            /// <value>The page.</value>
            public int Page { get; set; }
            /// <summary>
            /// Gets or sets the results.
            /// </summary>
            /// <value>The results.</value>
            public List<PersonSearchResult> Results { get; set; }
            /// <summary>
            /// Gets or sets the total_ pages.
            /// </summary>
            /// <value>The total_ pages.</value>
            public int Total_Pages { get; set; }
            /// <summary>
            /// Gets or sets the total_ results.
            /// </summary>
            /// <value>The total_ results.</value>
            public int Total_Results { get; set; }
        }

        /// <summary>
        /// Class PersonResult
        /// </summary>
        public class PersonResult
        {
            /// <summary>
            /// Gets or sets a value indicating whether this <see cref="PersonResult" /> is adult.
            /// </summary>
            /// <value><c>true</c> if adult; otherwise, <c>false</c>.</value>
            public bool Adult { get; set; }
            /// <summary>
            /// Gets or sets the also_ known_ as.
            /// </summary>
            /// <value>The also_ known_ as.</value>
            public List<object> Also_Known_As { get; set; }
            /// <summary>
            /// Gets or sets the biography.
            /// </summary>
            /// <value>The biography.</value>
            public string Biography { get; set; }
            /// <summary>
            /// Gets or sets the birthday.
            /// </summary>
            /// <value>The birthday.</value>
            public string Birthday { get; set; }
            /// <summary>
            /// Gets or sets the deathday.
            /// </summary>
            /// <value>The deathday.</value>
            public string Deathday { get; set; }
            /// <summary>
            /// Gets or sets the homepage.
            /// </summary>
            /// <value>The homepage.</value>
            public string Homepage { get; set; }
            /// <summary>
            /// Gets or sets the id.
            /// </summary>
            /// <value>The id.</value>
            public int Id { get; set; }
            /// <summary>
            /// Gets or sets the name.
            /// </summary>
            /// <value>The name.</value>
            public string Name { get; set; }
            /// <summary>
            /// Gets or sets the place_ of_ birth.
            /// </summary>
            /// <value>The place_ of_ birth.</value>
            public string Place_Of_Birth { get; set; }
            /// <summary>
            /// Gets or sets the profile_ path.
            /// </summary>
            /// <value>The profile_ path.</value>
            public string Profile_Path { get; set; }
        }

        /// <summary>
        /// Class PersonProfile
        /// </summary>
        public class PersonProfile
        {
            /// <summary>
            /// Gets or sets the aspect_ ratio.
            /// </summary>
            /// <value>The aspect_ ratio.</value>
            public double Aspect_Ratio { get; set; }
            /// <summary>
            /// Gets or sets the file_ path.
            /// </summary>
            /// <value>The file_ path.</value>
            public string File_Path { get; set; }
            /// <summary>
            /// Gets or sets the height.
            /// </summary>
            /// <value>The height.</value>
            public int Height { get; set; }
            /// <summary>
            /// Gets or sets the iso_639_1.
            /// </summary>
            /// <value>The iso_639_1.</value>
            public string Iso_639_1 { get; set; }
            /// <summary>
            /// Gets or sets the width.
            /// </summary>
            /// <value>The width.</value>
            public int Width { get; set; }
        }

        /// <summary>
        /// Class PersonImages
        /// </summary>
        public class PersonImages
        {
            /// <summary>
            /// Gets or sets the id.
            /// </summary>
            /// <value>The id.</value>
            public int Id { get; set; }
            /// <summary>
            /// Gets or sets the profiles.
            /// </summary>
            /// <value>The profiles.</value>
            public List<PersonProfile> Profiles { get; set; }
        }

        #endregion
    }
}
