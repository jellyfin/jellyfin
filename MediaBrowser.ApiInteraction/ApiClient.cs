using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.System;
using MediaBrowser.Model.Tasks;
using MediaBrowser.Model.Weather;
using MediaBrowser.Model.Web;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.ApiInteraction
{
    /// <summary>
    /// Provides api methods centered around an HttpClient
    /// </summary>
    public class ApiClient : BaseApiClient
    {
        /// <summary>
        /// Gets the HTTP client.
        /// </summary>
        /// <value>The HTTP client.</value>
        protected IAsyncHttpClient HttpClient { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiClient" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="httpClient">The HTTP client.</param>
        /// <exception cref="System.ArgumentNullException">httpClient</exception>
        public ApiClient(ILogger logger, IAsyncHttpClient httpClient)
            : base(logger)
        {
            if (httpClient == null)
            {
                throw new ArgumentNullException("httpClient");
            }

            HttpClient = httpClient;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiClient" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public ApiClient(ILogger logger)
            : this(logger, new AsyncHttpClient())
        {
        }

        /// <summary>
        /// Sets the authorization header.
        /// </summary>
        /// <param name="header">The header.</param>
        protected override void SetAuthorizationHeader(string header)
        {
            HttpClient.SetAuthorizationHeader(header);
        }

        /// <summary>
        /// Gets an image stream based on a url
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns>Task{Stream}.</returns>
        /// <exception cref="System.ArgumentNullException">url</exception>
        public Task<Stream> GetImageStreamAsync(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentNullException("url");
            }

            return HttpClient.GetAsync(url, Logger, CancellationToken.None);
        }

        /// <summary>
        /// Gets a BaseItem
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="userId">The user id.</param>
        /// <returns>Task{BaseItemDto}.</returns>
        /// <exception cref="System.ArgumentNullException">id</exception>
        public async Task<BaseItemDto> GetItemAsync(string id, Guid userId)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException("id");
            }

            if (userId == Guid.Empty)
            {
                throw new ArgumentNullException("userId");
            }

            var url = GetApiUrl("Users/" + userId + "/Items/" + id);

            using (var stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<BaseItemDto>(stream);
            }
        }

        /// <summary>
        /// Gets the intros async.
        /// </summary>
        /// <param name="itemId">The item id.</param>
        /// <param name="userId">The user id.</param>
        /// <returns>Task{System.String[]}.</returns>
        /// <exception cref="System.ArgumentNullException">id</exception>
        public async Task<string[]> GetIntrosAsync(string itemId, Guid userId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                throw new ArgumentNullException("itemId");
            }

            if (userId == Guid.Empty)
            {
                throw new ArgumentNullException("userId");
            }

            var url = GetApiUrl("Users/" + userId + "/Items/" + itemId + "/Intros");

            using (var stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<string[]>(stream);
            }
        }

        /// <summary>
        /// Gets a BaseItem
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <returns>Task{BaseItemDto}.</returns>
        /// <exception cref="System.ArgumentNullException">userId</exception>
        public async Task<BaseItemDto> GetRootFolderAsync(Guid userId)
        {
            if (userId == Guid.Empty)
            {
                throw new ArgumentNullException("userId");
            }

            var url = GetApiUrl("Users/" + userId + "/Items/Root");

            using (var stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<BaseItemDto>(stream);
            }
        }

        /// <summary>
        /// Gets all Users
        /// </summary>
        /// <returns>Task{UserDto[]}.</returns>
        public async Task<UserDto[]> GetAllUsersAsync()
        {
            var url = GetApiUrl("Users");

            using (var stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<UserDto[]>(stream);
            }
        }

        /// <summary>
        /// Queries for items
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task{ItemsResult}.</returns>
        /// <exception cref="System.ArgumentNullException">query</exception>
        public async Task<ItemsResult> GetItemsAsync(ItemQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            var url = GetItemListUrl(query);

            using (var stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<ItemsResult>(stream);
            }
        }

        /// <summary>
        /// Gets all People
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="itemId">Optional itemId, to localize the search to a specific item or folder</param>
        /// <param name="personTypes">Use this to limit results to specific person types</param>
        /// <param name="startIndex">Used to skip over a given number of items. Use if paging.</param>
        /// <param name="limit">The maximum number of items to return</param>
        /// <param name="sortOrder">The sort order</param>
        /// <param name="recursive">if set to true items will be searched recursively.</param>
        /// <returns>Task{IbnItemsResult}.</returns>
        /// <exception cref="System.ArgumentNullException">userId</exception>
        public async Task<ItemsResult> GetAllPeopleAsync(
            Guid userId,
            string itemId = null,
            IEnumerable<string> personTypes = null,
            int? startIndex = null,
            int? limit = null,
             SortOrder? sortOrder = null,
            bool recursive = false)
        {
            if (userId == Guid.Empty)
            {
                throw new ArgumentNullException("userId");
            }

            var dict = new QueryStringDictionary();

            dict.AddIfNotNull("startIndex", startIndex);
            dict.AddIfNotNull("limit", limit);

            dict.Add("recursive", recursive);

            if (sortOrder.HasValue)
            {
                dict["sortOrder"] = sortOrder.Value.ToString();
            }

            dict.AddIfNotNull("personTypes", personTypes);

            var url = string.IsNullOrEmpty(itemId) ? "Users/" + userId + "/Items/Root/Persons" : "Users/" + userId + "/Items/" + itemId + "/Persons";
            url = GetApiUrl(url, dict);

            using (var stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<ItemsResult>(stream);
            }
        }

        /// <summary>
        /// Gets a studio
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>Task{BaseItemDto}.</returns>
        /// <exception cref="System.ArgumentNullException">userId</exception>
        public async Task<BaseItemDto> GetStudioAsync(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            var url = GetApiUrl("Library/Studios/" + name);

            using (var stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<BaseItemDto>(stream);
            }
        }

        /// <summary>
        /// Gets a genre
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>Task{BaseItemDto}.</returns>
        /// <exception cref="System.ArgumentNullException">userId</exception>
        public async Task<BaseItemDto> GetGenreAsync(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            var url = GetApiUrl("Library/Genres/" + name);

            using (var stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<BaseItemDto>(stream);
            }
        }

        /// <summary>
        /// Restarts the kernel or the entire server if necessary
        /// If the server application is restarting this request will fail to return, even if
        /// the operation is successful.
        /// </summary>
        /// <returns>Task.</returns>
        public Task PerformPendingRestartAsync()
        {
            var url = GetApiUrl("System/Restart");

            return PostAsync<EmptyRequestResult>(url, new QueryStringDictionary());
        }

        /// <summary>
        /// Gets the system status async.
        /// </summary>
        /// <returns>Task{SystemInfo}.</returns>
        public async Task<SystemInfo> GetSystemInfoAsync()
        {
            var url = GetApiUrl("System/Info");

            using (var stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<SystemInfo>(stream);
            }
        }

        /// <summary>
        /// Gets a person
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>Task{BaseItemDto}.</returns>
        /// <exception cref="System.ArgumentNullException">userId</exception>
        public async Task<BaseItemDto> GetPersonAsync(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            var url = GetApiUrl("Library/Persons/" + name);

            using (var stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<BaseItemDto>(stream);
            }
        }

        /// <summary>
        /// Gets a year
        /// </summary>
        /// <param name="year">The year.</param>
        /// <returns>Task{BaseItemDto}.</returns>
        /// <exception cref="System.ArgumentNullException">userId</exception>
        public async Task<BaseItemDto> GetYearAsync(int year)
        {
            var url = GetApiUrl("Library/Years/" + year);

            using (var stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<BaseItemDto>(stream);
            }
        }

        /// <summary>
        /// Gets a list of plugins installed on the server
        /// </summary>
        /// <returns>Task{PluginInfo[]}.</returns>
        public async Task<PluginInfo[]> GetInstalledPluginsAsync()
        {
            var url = GetApiUrl("Plugins");

            using (var stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<PluginInfo[]>(stream);
            }
        }

        /// <summary>
        /// Gets a list of plugins installed on the server
        /// </summary>
        /// <param name="plugin">The plugin.</param>
        /// <returns>Task{Stream}.</returns>
        /// <exception cref="System.ArgumentNullException">plugin</exception>
        public Task<Stream> GetPluginAssemblyAsync(PluginInfo plugin)
        {
            if (plugin == null)
            {
                throw new ArgumentNullException("plugin");
            }

            var url = GetApiUrl("Plugins/" + plugin.Id + "/Assembly");

            return HttpClient.GetAsync(url, Logger, CancellationToken.None);
        }

        /// <summary>
        /// Gets the current server configuration
        /// </summary>
        /// <returns>Task{ServerConfiguration}.</returns>
        public async Task<ServerConfiguration> GetServerConfigurationAsync()
        {
            var url = GetApiUrl("System/Configuration");

            using (var stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<ServerConfiguration>(stream);
            }
        }

        /// <summary>
        /// Gets the scheduled tasks.
        /// </summary>
        /// <returns>Task{TaskInfo[]}.</returns>
        public async Task<TaskInfo[]> GetScheduledTasksAsync()
        {
            var url = GetApiUrl("ScheduledTasks");

            using (var stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<TaskInfo[]>(stream);
            }
        }

        /// <summary>
        /// Gets the scheduled task async.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>Task{TaskInfo}.</returns>
        /// <exception cref="System.ArgumentNullException">id</exception>
        public async Task<TaskInfo> GetScheduledTaskAsync(Guid id)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentNullException("id");
            }

            var url = GetApiUrl("ScheduledTasks/" + id);

            using (var stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<TaskInfo>(stream);
            }
        }

        /// <summary>
        /// Gets the plugin configuration file in plain text.
        /// </summary>
        /// <param name="pluginId">The plugin id.</param>
        /// <returns>Task{Stream}.</returns>
        /// <exception cref="System.ArgumentNullException">assemblyFileName</exception>
        public async Task<Stream> GetPluginConfigurationFileAsync(Guid pluginId)
        {
            if (pluginId == Guid.Empty)
            {
                throw new ArgumentNullException("pluginId");
            }

            var url = GetApiUrl("Plugins/" + pluginId + "/ConfigurationFile");

            return await HttpClient.GetAsync(url, Logger, CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets a user by id
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>Task{UserDto}.</returns>
        /// <exception cref="System.ArgumentNullException">id</exception>
        public async Task<UserDto> GetUserAsync(Guid id)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentNullException("id");
            }

            var url = GetApiUrl("Users/" + id);

            using (var stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<UserDto>(stream);
            }
        }

        /// <summary>
        /// Gets the parental ratings async.
        /// </summary>
        /// <returns>Task{List{ParentalRating}}.</returns>
        public async Task<List<ParentalRating>> GetParentalRatingsAsync()
        {
            var url = GetApiUrl("Localization/ParentalRatings");

            using (var stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<List<ParentalRating>>(stream);
            }
        }

        /// <summary>
        /// Gets weather information for the default location as set in configuration
        /// </summary>
        /// <returns>Task{WeatherInfo}.</returns>
        public async Task<WeatherInfo> GetWeatherInfoAsync()
        {
            var url = GetApiUrl("Weather");

            using (var stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<WeatherInfo>(stream);
            }
        }

        /// <summary>
        /// Gets weather information for a specific location
        /// Location can be a US zipcode, or "city,state", "city,state,country", "city,country"
        /// It can also be an ip address, or "latitude,longitude"
        /// </summary>
        /// <param name="location">The location.</param>
        /// <returns>Task{WeatherInfo}.</returns>
        /// <exception cref="System.ArgumentNullException">location</exception>
        public async Task<WeatherInfo> GetWeatherInfoAsync(string location)
        {
            if (string.IsNullOrEmpty(location))
            {
                throw new ArgumentNullException("location");
            }

            var dict = new QueryStringDictionary();

            dict.Add("location", location);

            var url = GetApiUrl("Weather", dict);

            using (var stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<WeatherInfo>(stream);
            }
        }

        /// <summary>
        /// Gets local trailers for an item
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="itemId">The item id.</param>
        /// <returns>Task{ItemsResult}.</returns>
        /// <exception cref="System.ArgumentNullException">query</exception>
        public async Task<BaseItemDto[]> GetLocalTrailersAsync(Guid userId, string itemId)
        {
            if (userId == Guid.Empty)
            {
                throw new ArgumentNullException("userId");
            }
            if (string.IsNullOrEmpty(itemId))
            {
                throw new ArgumentNullException("itemId");
            }

            var url = GetApiUrl("Users/" + userId + "/Items/" + itemId + "/LocalTrailers");

            using (var stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<BaseItemDto[]>(stream);
            }
        }

        /// <summary>
        /// Gets special features for an item
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="itemId">The item id.</param>
        /// <returns>Task{BaseItemDto[]}.</returns>
        /// <exception cref="System.ArgumentNullException">userId</exception>
        public async Task<BaseItemDto[]> GetSpecialFeaturesAsync(Guid userId, string itemId)
        {
            if (userId == Guid.Empty)
            {
                throw new ArgumentNullException("userId");
            }
            if (string.IsNullOrEmpty(itemId))
            {
                throw new ArgumentNullException("itemId");
            }

            var url = GetApiUrl("Users/" + userId + "/Items/" + itemId + "/SpecialFeatures");

            using (var stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<BaseItemDto[]>(stream);
            }
        }

        /// <summary>
        /// Gets the cultures async.
        /// </summary>
        /// <returns>Task{CultureDto[]}.</returns>
        public async Task<CultureDto[]> GetCulturesAsync()
        {
            var url = GetApiUrl("Localization/Cultures");

            using (var stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<CultureDto[]>(stream);
            }
        }

        /// <summary>
        /// Gets the countries async.
        /// </summary>
        /// <returns>Task{CountryInfo[]}.</returns>
        public async Task<CountryInfo[]> GetCountriesAsync()
        {
            var url = GetApiUrl("Localization/Countries");

            using (var stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<CountryInfo[]>(stream);
            }
        }

        /// <summary>
        /// Marks an item as played or unplayed.
        /// This should not be used to update playstate following playback.
        /// There are separate playstate check-in methods for that. This should be used for a
        /// separate option to reset playstate.
        /// </summary>
        /// <param name="itemId">The item id.</param>
        /// <param name="userId">The user id.</param>
        /// <param name="wasPlayed">if set to <c>true</c> [was played].</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">itemId</exception>
        public Task UpdatePlayedStatusAsync(string itemId, Guid userId, bool wasPlayed)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                throw new ArgumentNullException("itemId");
            }
            if (userId == Guid.Empty)
            {
                throw new ArgumentNullException("userId");
            }

            var url = GetApiUrl("Users/" + userId + "/PlayedItems/" + itemId);

            if (wasPlayed)
            {
                return PostAsync<EmptyRequestResult>(url, new Dictionary<string, string>());
            }

            return HttpClient.DeleteAsync(url, Logger, CancellationToken.None);
        }

        /// <summary>
        /// Updates the favorite status async.
        /// </summary>
        /// <param name="itemId">The item id.</param>
        /// <param name="userId">The user id.</param>
        /// <param name="isFavorite">if set to <c>true</c> [is favorite].</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">itemId</exception>
        public Task UpdateFavoriteStatusAsync(string itemId, Guid userId, bool isFavorite)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                throw new ArgumentNullException("itemId");
            }
            if (userId == Guid.Empty)
            {
                throw new ArgumentNullException("userId");
            }

            var url = GetApiUrl("Users/" + userId + "/FavoriteItems/" + itemId);

            if (isFavorite)
            {
                return PostAsync<EmptyRequestResult>(url, new Dictionary<string, string>());
            }

            return HttpClient.DeleteAsync(url, Logger, CancellationToken.None);
        }

        /// <summary>
        /// Reports to the server that the user has begun playing an item
        /// </summary>
        /// <param name="itemId">The item id.</param>
        /// <param name="userId">The user id.</param>
        /// <returns>Task{UserItemDataDto}.</returns>
        /// <exception cref="System.ArgumentNullException">itemId</exception>
        public Task ReportPlaybackStartAsync(string itemId, Guid userId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                throw new ArgumentNullException("itemId");
            }

            if (userId == Guid.Empty)
            {
                throw new ArgumentNullException("userId");
            }

            var url = GetApiUrl("Users/" + userId + "/PlayingItems/" + itemId);

            return PostAsync<EmptyRequestResult>(url, new Dictionary<string, string>());
        }

        /// <summary>
        /// Reports playback progress to the server
        /// </summary>
        /// <param name="itemId">The item id.</param>
        /// <param name="userId">The user id.</param>
        /// <param name="positionTicks">The position ticks.</param>
        /// <returns>Task{UserItemDataDto}.</returns>
        /// <exception cref="System.ArgumentNullException">itemId</exception>
        public Task ReportPlaybackProgressAsync(string itemId, Guid userId, long? positionTicks)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                throw new ArgumentNullException("itemId");
            }

            if (userId == Guid.Empty)
            {
                throw new ArgumentNullException("userId");
            }

            var dict = new QueryStringDictionary();
            dict.AddIfNotNull("positionTicks", positionTicks);

            var url = GetApiUrl("Users/" + userId + "/PlayingItems/" + itemId + "/Progress", dict);

            return PostAsync<EmptyRequestResult>(url, new Dictionary<string, string>());
        }

        /// <summary>
        /// Reports to the server that the user has stopped playing an item
        /// </summary>
        /// <param name="itemId">The item id.</param>
        /// <param name="userId">The user id.</param>
        /// <param name="positionTicks">The position ticks.</param>
        /// <returns>Task{UserItemDataDto}.</returns>
        /// <exception cref="System.ArgumentNullException">itemId</exception>
        public Task ReportPlaybackStoppedAsync(string itemId, Guid userId, long? positionTicks)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                throw new ArgumentNullException("itemId");
            }

            if (userId == Guid.Empty)
            {
                throw new ArgumentNullException("userId");
            }

            var dict = new QueryStringDictionary();
            dict.AddIfNotNull("positionTicks", positionTicks);

            var url = GetApiUrl("Users/" + userId + "/PlayingItems/" + itemId, dict);

            return HttpClient.DeleteAsync(url, Logger, CancellationToken.None);
        }

        /// <summary>
        /// Clears a user's rating for an item
        /// </summary>
        /// <param name="itemId">The item id.</param>
        /// <param name="userId">The user id.</param>
        /// <returns>Task{UserItemDataDto}.</returns>
        /// <exception cref="System.ArgumentNullException">itemId</exception>
        public Task ClearUserItemRatingAsync(string itemId, Guid userId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                throw new ArgumentNullException("itemId");
            }

            if (userId == Guid.Empty)
            {
                throw new ArgumentNullException("userId");
            }

            var url = GetApiUrl("Users/" + userId + "/Items/" + itemId + "/Rating");

            return HttpClient.DeleteAsync(url, Logger, CancellationToken.None);
        }

        /// <summary>
        /// Updates a user's rating for an item, based on likes or dislikes
        /// </summary>
        /// <param name="itemId">The item id.</param>
        /// <param name="userId">The user id.</param>
        /// <param name="likes">if set to <c>true</c> [likes].</param>
        /// <returns>Task{UserItemDataDto}.</returns>
        /// <exception cref="System.ArgumentNullException">itemId</exception>
        public Task<UserItemDataDto> UpdateUserItemRatingAsync(string itemId, Guid userId, bool likes)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                throw new ArgumentNullException("itemId");
            }

            if (userId == Guid.Empty)
            {
                throw new ArgumentNullException("userId");
            }

            var dict = new QueryStringDictionary { };

            dict.Add("likes", likes);

            var url = GetApiUrl("Users/" + userId + "/Items/" + itemId + "/Rating", dict);

            return PostAsync<UserItemDataDto>(url, new Dictionary<string, string>());
        }

        /// <summary>
        /// Authenticates a user and returns the result
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="password">The password.</param>
        /// <exception cref="System.ArgumentNullException">userId</exception>
        public Task AuthenticateUserAsync(Guid userId, string password)
        {
            if (userId == Guid.Empty)
            {
                throw new ArgumentNullException("userId");
            }

            var url = GetApiUrl("Users/" + userId + "/Authenticate");

            var args = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(password))
            {
                args["password"] = password;
            }

            return PostAsync<EmptyRequestResult>(url, args);
        }

        /// <summary>
        /// Uploads the user image async.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="imageType">Type of the image.</param>
        /// <param name="image">The image.</param>
        /// <returns>Task{RequestResult}.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public Task UploadUserImageAsync(Guid userId, ImageType imageType, Stream image)
        {
            // Implement when needed
            throw new NotImplementedException();
        }

        /// <summary>
        /// Updates the server configuration async.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">configuration</exception>
        public Task UpdateServerConfigurationAsync(ServerConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            var url = GetApiUrl("System/Configuration");

            return PostAsync<ServerConfiguration, EmptyRequestResult>(url, configuration);
        }

        /// <summary>
        /// Updates the scheduled task triggers.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="triggers">The triggers.</param>
        /// <returns>Task{RequestResult}.</returns>
        /// <exception cref="System.ArgumentNullException">id</exception>
        public Task UpdateScheduledTaskTriggersAsync(Guid id, TaskTriggerInfo[] triggers)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentNullException("id");
            }

            if (triggers == null)
            {
                throw new ArgumentNullException("triggers");
            }

            var url = GetApiUrl("ScheduledTasks/" + id + "/Triggers");

            return PostAsync<TaskTriggerInfo[], EmptyRequestResult>(url, triggers);
        }

        /// <summary>
        /// Updates display preferences for a user
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="itemId">The item id.</param>
        /// <param name="displayPreferences">The display preferences.</param>
        /// <returns>Task{DisplayPreferences}.</returns>
        /// <exception cref="System.ArgumentNullException">userId</exception>
        public Task UpdateDisplayPreferencesAsync(Guid userId, string itemId, DisplayPreferences displayPreferences)
        {
            if (userId == Guid.Empty)
            {
                throw new ArgumentNullException("userId");
            }

            if (string.IsNullOrEmpty(itemId))
            {
                throw new ArgumentNullException("itemId");
            }

            if (displayPreferences == null)
            {
                throw new ArgumentNullException("displayPreferences");
            }

            var url = GetApiUrl("Users/" + userId + "/Items/" + itemId + "/DisplayPreferences");

            return PostAsync<DisplayPreferences, EmptyRequestResult>(url, displayPreferences);
        }

        /// <summary>
        /// Posts a set of data to a url, and deserializes the return stream into T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="url">The URL.</param>
        /// <param name="args">The args.</param>
        /// <returns>Task{``0}.</returns>
        private Task<T> PostAsync<T>(string url, Dictionary<string, string> args)
            where T : class
        {
            return PostAsync<T>(url, args, SerializationFormat);
        }

        /// <summary>
        /// Posts a set of data to a url, and deserializes the return stream into T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="url">The URL.</param>
        /// <param name="args">The args.</param>
        /// <param name="serializationFormat">The serialization format.</param>
        /// <returns>Task{``0}.</returns>
        private async Task<T> PostAsync<T>(string url, Dictionary<string, string> args, SerializationFormats serializationFormat)
            where T : class
        {
            url = AddDataFormat(url, serializationFormat);

            // Create the post body
            var strings = args.Keys.Select(key => string.Format("{0}={1}", key, args[key]));
            var postContent = string.Join("&", strings.ToArray());

            const string contentType = "application/x-www-form-urlencoded";

            using (var stream = await HttpClient.PostAsync(url, contentType, postContent, Logger, CancellationToken.None).ConfigureAwait(false))
            {
                return DeserializeFromStream<T>(stream);
            }
        }

        /// <summary>
        /// Posts an object of type TInputType to a given url, and deserializes the response into an object of type TOutputType
        /// </summary>
        /// <typeparam name="TInputType">The type of the T input type.</typeparam>
        /// <typeparam name="TOutputType">The type of the T output type.</typeparam>
        /// <param name="url">The URL.</param>
        /// <param name="obj">The obj.</param>
        /// <returns>Task{``1}.</returns>
        private Task<TOutputType> PostAsync<TInputType, TOutputType>(string url, TInputType obj)
            where TOutputType : class
        {
            return PostAsync<TInputType, TOutputType>(url, obj, SerializationFormat);
        }

        /// <summary>
        /// Posts an object of type TInputType to a given url, and deserializes the response into an object of type TOutputType
        /// </summary>
        /// <typeparam name="TInputType">The type of the T input type.</typeparam>
        /// <typeparam name="TOutputType">The type of the T output type.</typeparam>
        /// <param name="url">The URL.</param>
        /// <param name="obj">The obj.</param>
        /// <param name="serializationFormat">The serialization format.</param>
        /// <returns>Task{``1}.</returns>
        private async Task<TOutputType> PostAsync<TInputType, TOutputType>(string url, TInputType obj, SerializationFormats serializationFormat)
            where TOutputType : class
        {
            url = AddDataFormat(url, serializationFormat);

            const string contentType = "application/x-www-form-urlencoded";

            var postContent = SerializeToJson(obj);

            using (var stream = await HttpClient.PostAsync(url, contentType, postContent, Logger, CancellationToken.None).ConfigureAwait(false))
            {
                return DeserializeFromStream<TOutputType>(stream);
            }
        }

        /// <summary>
        /// This is a helper around getting a stream from the server that contains serialized data
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns>Task{Stream}.</returns>
        public Task<Stream> GetSerializedStreamAsync(string url)
        {
            return GetSerializedStreamAsync(url, SerializationFormat);
        }

        /// <summary>
        /// This is a helper around getting a stream from the server that contains serialized data
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="serializationFormat">The serialization format.</param>
        /// <returns>Task{Stream}.</returns>
        public Task<Stream> GetSerializedStreamAsync(string url, SerializationFormats serializationFormat)
        {
            url = AddDataFormat(url, serializationFormat);

            return HttpClient.GetAsync(url, Logger, CancellationToken.None);
        }


        /// <summary>
        /// Adds the data format.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="serializationFormat">The serialization format.</param>
        /// <returns>System.String.</returns>
        private string AddDataFormat(string url, SerializationFormats serializationFormat)
        {
            var format = serializationFormat == SerializationFormats.Protobuf ? "x-protobuf" : serializationFormat.ToString();

            if (url.IndexOf('?') == -1)
            {
                url += "?format=" + format;
            }
            else
            {
                url += "&format=" + format;
            }

            return url;
        }
    }
}
