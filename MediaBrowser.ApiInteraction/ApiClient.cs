using MediaBrowser.Model.Authentication;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.DTO;
using MediaBrowser.Model.Weather;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowser.ApiInteraction
{
    public class ApiClient : BaseApiClient
    {
        public ApiClient(HttpClientHandler handler)
            : base()
        {
            handler.AutomaticDecompression = DecompressionMethods.Deflate;

            HttpClient = new HttpClient(handler);
        }

        private HttpClient HttpClient { get; set; }

        /// <summary>
        /// Gets an image stream based on a url
        /// </summary>
        public Task<Stream> GetImageStreamAsync(string url)
        {
            return GetStreamAsync(url);
        }

        /// <summary>
        /// Gets a BaseItem
        /// </summary>
        public async Task<DTOBaseItem> GetItemAsync(Guid id, Guid userId)
        {
            string url = ApiUrl + "/item?userId=" + userId.ToString();

            if (id != Guid.Empty)
            {
                url += "&id=" + id.ToString();
            }

            using (Stream stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<DTOBaseItem>(stream);
            }
        }

        /// <summary>
        /// Gets all Users
        /// </summary>
        public async Task<DTOUser[]> GetAllUsersAsync()
        {
            string url = ApiUrl + "/users";

            using (Stream stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<DTOUser[]>(stream);
            }
        }

        /// <summary>
        /// Gets all Genres
        /// </summary>
        public async Task<IBNItem[]> GetAllGenresAsync(Guid userId)
        {
            string url = ApiUrl + "/genres?userId=" + userId.ToString();

            using (Stream stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<IBNItem[]>(stream);
            }
        }

        /// <summary>
        /// Gets in-progress items
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="folderId">(Optional) Specify a folder Id to localize the search to a specific folder.</param>
        public async Task<DTOBaseItem[]> GetInProgressItemsItemsAsync(Guid userId, Guid? folderId = null)
        {
            string url = ApiUrl + "/itemlist?listtype=inprogressitems&userId=" + userId.ToString();

            if (folderId.HasValue)
            {
                url += "&id=" + folderId.ToString();
            }

            using (Stream stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<DTOBaseItem[]>(stream);
            }
        }

        /// <summary>
        /// Gets recently added items
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="folderId">(Optional) Specify a folder Id to localize the search to a specific folder.</param>
        public async Task<DTOBaseItem[]> GetRecentlyAddedItemsAsync(Guid userId, Guid? folderId = null)
        {
            string url = ApiUrl + "/itemlist?listtype=recentlyaddeditems&userId=" + userId.ToString();

            if (folderId.HasValue)
            {
                url += "&id=" + folderId.ToString();
            }

            using (Stream stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<DTOBaseItem[]>(stream);
            }
        }

        /// <summary>
        /// Gets recently added items that are unplayed.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="folderId">(Optional) Specify a folder Id to localize the search to a specific folder.</param>
        public async Task<DTOBaseItem[]> GetRecentlyAddedUnplayedItemsAsync(Guid userId, Guid? folderId = null)
        {
            string url = ApiUrl + "/itemlist?listtype=recentlyaddedunplayeditems&userId=" + userId.ToString();

            if (folderId.HasValue)
            {
                url += "&id=" + folderId.ToString();
            }

            using (Stream stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<DTOBaseItem[]>(stream);
            }
        }

        /// <summary>
        /// Gets all Years
        /// </summary>
        public async Task<IBNItem[]> GetAllYearsAsync(Guid userId)
        {
            string url = ApiUrl + "/years?userId=" + userId.ToString();

            using (Stream stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<IBNItem[]>(stream);
            }
        }

        /// <summary>
        /// Gets all items that contain a given Year
        /// </summary>
        public async Task<DTOBaseItem[]> GetItemsWithYearAsync(string name, Guid userId)
        {
            string url = ApiUrl + "/itemlist?listtype=itemswithyear&userId=" + userId.ToString() + "&name=" + name;

            using (Stream stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<DTOBaseItem[]>(stream);
            }
        }

        /// <summary>
        /// Gets all items that contain a given Genre
        /// </summary>
        public async Task<DTOBaseItem[]> GetItemsWithGenreAsync(string name, Guid userId)
        {
            string url = ApiUrl + "/itemlist?listtype=itemswithgenre&userId=" + userId.ToString() + "&name=" + name;

            using (Stream stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<DTOBaseItem[]>(stream);
            }
        }

        /// <summary>
        /// Gets all items that contain a given Person
        /// </summary>
        public async Task<DTOBaseItem[]> GetItemsWithPersonAsync(string name, Guid userId)
        {
            string url = ApiUrl + "/itemlist?listtype=itemswithperson&userId=" + userId.ToString() + "&name=" + name;

            using (Stream stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<DTOBaseItem[]>(stream);
            }
        }

        /// <summary>
        /// Gets all items that contain a given Person
        /// </summary>
        public async Task<DTOBaseItem[]> GetItemsWithPersonAsync(string name, string personType, Guid userId)
        {
            string url = ApiUrl + "/itemlist?listtype=itemswithperson&userId=" + userId.ToString() + "&name=" + name;

            url += "&persontype=" + personType;

            using (Stream stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<DTOBaseItem[]>(stream);
            }
        }

        /// <summary>
        /// Gets all studious
        /// </summary>
        public async Task<IBNItem[]> GetAllStudiosAsync(Guid userId)
        {
            string url = ApiUrl + "/studios?userId=" + userId.ToString();

            using (Stream stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<IBNItem[]>(stream);
            }
        }

        /// <summary>
        /// Gets all items that contain a given Studio
        /// </summary>
        public async Task<DTOBaseItem[]> GetItemsWithStudioAsync(string name, Guid userId)
        {
            string url = ApiUrl + "/itemlist?listtype=itemswithstudio&userId=" + userId.ToString() + "&name=" + name;

            using (Stream stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<DTOBaseItem[]>(stream);
            }
        }

        /// <summary>
        /// Gets a studio
        /// </summary>
        public async Task<IBNItem> GetStudioAsync(Guid userId, string name)
        {
            string url = ApiUrl + "/studio?userId=" + userId.ToString() + "&name=" + name;

            using (Stream stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<IBNItem>(stream);
            }
        }

        /// <summary>
        /// Gets a genre
        /// </summary>
        public async Task<IBNItem> GetGenreAsync(Guid userId, string name)
        {
            string url = ApiUrl + "/genre?userId=" + userId.ToString() + "&name=" + name;

            using (Stream stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<IBNItem>(stream);
            }
        }

        /// <summary>
        /// Gets a person
        /// </summary>
        public async Task<IBNItem> GetPersonAsync(Guid userId, string name)
        {
            string url = ApiUrl + "/person?userId=" + userId.ToString() + "&name=" + name;

            using (Stream stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<IBNItem>(stream);
            }
        }

        /// <summary>
        /// Gets a year
        /// </summary>
        public async Task<IBNItem> GetYearAsync(Guid userId, int year)
        {
            string url = ApiUrl + "/year?userId=" + userId.ToString() + "&year=" + year;

            using (Stream stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<IBNItem>(stream);
            }
        }

        /// <summary>
        /// Gets a list of plugins installed on the server
        /// </summary>
        public async Task<PluginInfo[]> GetInstalledPluginsAsync()
        {
            string url = ApiUrl + "/plugins";

            using (Stream stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<PluginInfo[]>(stream);
            }
        }

        /// <summary>
        /// Gets a list of plugins installed on the server
        /// </summary>
        public Task<Stream> GetPluginAssemblyAsync(PluginInfo plugin)
        {
            string url = ApiUrl + "/pluginassembly?assemblyfilename=" + plugin.AssemblyFileName;

            return GetStreamAsync(url);
        }

        /// <summary>
        /// Gets the current server configuration
        /// </summary>
        public async Task<ServerConfiguration> GetServerConfigurationAsync()
        {
            string url = ApiUrl + "/ServerConfiguration";

            // At the moment this can't be retrieved in protobuf format
            SerializationFormats format = DataSerializer.CanDeSerializeJsv ? SerializationFormats.Jsv : SerializationFormats.Json;

            using (Stream stream = await GetSerializedStreamAsync(url, format).ConfigureAwait(false))
            {
                return DataSerializer.DeserializeFromStream<ServerConfiguration>(stream, format);
            }
        }

        /// <summary>
        /// Gets weather information for the default location as set in configuration
        /// </summary>
        public async Task<object> GetPluginConfigurationAsync(PluginInfo plugin, Type configurationType)
        {
            string url = ApiUrl + "/PluginConfiguration?assemblyfilename=" + plugin.AssemblyFileName;

            // At the moment this can't be retrieved in protobuf format
            SerializationFormats format = DataSerializer.CanDeSerializeJsv ? SerializationFormats.Jsv : SerializationFormats.Json;

            using (Stream stream = await GetSerializedStreamAsync(url, format).ConfigureAwait(false))
            {
                return DataSerializer.DeserializeFromStream(stream, format, configurationType);
            }
        }

        /// <summary>
        /// Gets the default user
        /// </summary>
        public async Task<DTOUser> GetDefaultUserAsync()
        {
            string url = ApiUrl + "/user";

            using (Stream stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<DTOUser>(stream);
            }
        }

        /// <summary>
        /// Gets a user by id
        /// </summary>
        public async Task<DTOUser> GetUserAsync(Guid id)
        {
            string url = ApiUrl + "/user?id=" + id.ToString();

            using (Stream stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<DTOUser>(stream);
            }
        }

        /// <summary>
        /// Gets weather information for the default location as set in configuration
        /// </summary>
        public async Task<WeatherInfo> GetWeatherInfoAsync()
        {
            string url = ApiUrl + "/weather";

            using (Stream stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<WeatherInfo>(stream);
            }
        }

        /// <summary>
        /// Gets weather information for a specific zip code
        /// </summary>
        public async Task<WeatherInfo> GetWeatherInfoAsync(string zipCode)
        {
            string url = ApiUrl + "/weather?zipcode=" + zipCode;

            using (Stream stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<WeatherInfo>(stream);
            }
        }

        /// <summary>
        /// Authenticates a user and returns the result
        /// </summary>
        public async Task<AuthenticationResult> AuthenticateUserAsync(Guid userId, string password)
        {
            string url = ApiUrl + "/UserAuthentication?dataformat=" + SerializationFormat.ToString();

            // Create the post body
            string postContent = string.Format("userid={0}", userId);

            if (!string.IsNullOrEmpty(password))
            {
                postContent += "&password=" + password;
            }

            HttpContent content = new StringContent(postContent, Encoding.UTF8, "application/x-www-form-urlencoded");

            HttpResponseMessage msg = await HttpClient.PostAsync(url, content).ConfigureAwait(false);

            using (Stream stream = await msg.Content.ReadAsStreamAsync().ConfigureAwait(false))
            {
                return DeserializeFromStream<AuthenticationResult>(stream);
            }
        }

        /// <summary>
        /// This is a helper around getting a stream from the server that contains serialized data
        /// </summary>
        private Task<Stream> GetSerializedStreamAsync(string url)
        {
            return GetSerializedStreamAsync(url, SerializationFormat);
        }

        /// <summary>
        /// This is a helper around getting a stream from the server that contains serialized data
        /// </summary>
        private Task<Stream> GetSerializedStreamAsync(string url, SerializationFormats serializationFormat)
        {
            if (url.IndexOf('?') == -1)
            {
                url += "?dataformat=" + serializationFormat.ToString();
            }
            else
            {
                url += "&dataformat=" + serializationFormat.ToString();
            }

            return GetStreamAsync(url);
        }

        /// <summary>
        /// This is just a helper around HttpClient
        /// </summary>
        private Task<Stream> GetStreamAsync(string url)
        {
            return HttpClient.GetStreamAsync(url);
        }

        public override void Dispose()
        {
            HttpClient.Dispose();
        }
    }
}
