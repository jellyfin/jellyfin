using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.DTO;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Weather;

namespace MediaBrowser.ApiInteraction
{
    public class ApiClient : IDisposable
    {
        public ApiClient(HttpClientHandler handler)
        {
            handler.AutomaticDecompression = DecompressionMethods.Deflate;

            HttpClient = new HttpClient(handler);
        }

        /// <summary>
        /// Gets or sets the server host name (myserver or 192.168.x.x)
        /// </summary>
        public string ServerHostName { get; set; }

        /// <summary>
        /// Gets or sets the port number used by the API
        /// </summary>
        public int ServerApiPort { get; set; }

        /// <summary>
        /// Gets the current api url based on hostname and port.
        /// </summary>
        protected string ApiUrl
        {
            get
            {
                return string.Format("http://{0}:{1}/mediabrowser/api", ServerHostName, ServerApiPort);
            }
        }

        /// <summary>
        /// Gets the data format to request from the server
        /// </summary>
        private SerializationFormat SerializationFormat
        {
            get
            {
                // First try Protobuf since it has the best performance
                if (DataSerializer.CanDeserializeProtobuf)
                {
                    return ApiInteraction.SerializationFormat.Protobuf;
                }

                // Next best is jsv
                if (DataSerializer.CanDeserializeJsv)
                {
                    return ApiInteraction.SerializationFormat.Jsv;
                }

                return ApiInteraction.SerializationFormat.Json;
            }
        }

        public HttpClient HttpClient { get; private set; }
        public IDataSerializer DataSerializer { get; set; }

        /// <summary>
        /// Gets an image url that can be used to download an image from the api
        /// </summary>
        /// <param name="itemId">The Id of the item</param>
        /// <param name="imageType">The type of image requested</param>
        /// <param name="imageIndex">The image index, if there are multiple. Currently only applies to backdrops. Supply null or 0 for first backdrop.</param>
        /// <param name="width">Use if a fixed width is required. Aspect ratio will be preserved.</param>
        /// <param name="height">Use if a fixed height is required. Aspect ratio will be preserved.</param>
        /// <param name="maxWidth">Use if a max width is required. Aspect ratio will be preserved.</param>
        /// <param name="maxHeight">Use if a max height is required. Aspect ratio will be preserved.</param>
        /// <param name="quality">Quality level, from 0-100. Currently only applies to JPG. The default value should suffice.</param>
        public string GetImageUrl(Guid itemId, ImageType imageType, int? imageIndex = null, int? width = null, int? height = null, int? maxWidth = null, int? maxHeight = null, int? quality = null)
        {
            string url = ApiUrl + "/image";

            url += "?id=" + itemId.ToString();
            url += "&type=" + imageType.ToString();

            if (imageIndex.HasValue)
            {
                url += "&index=" + imageIndex;
            }
            if (width.HasValue)
            {
                url += "&width=" + width;
            }
            if (height.HasValue)
            {
                url += "&height=" + height;
            }
            if (maxWidth.HasValue)
            {
                url += "&maxWidth=" + maxWidth;
            }
            if (maxHeight.HasValue)
            {
                url += "&maxHeight=" + maxHeight;
            }
            if (quality.HasValue)
            {
                url += "&quality=" + quality;
            }

            return url;
        }

        /// <summary>
        /// Gets an image url that can be used to download an image from the api
        /// </summary>
        /// <param name="userId">The Id of the user</param>
        /// <param name="width">Use if a fixed width is required. Aspect ratio will be preserved.</param>
        /// <param name="height">Use if a fixed height is required. Aspect ratio will be preserved.</param>
        /// <param name="maxWidth">Use if a max width is required. Aspect ratio will be preserved.</param>
        /// <param name="maxHeight">Use if a max height is required. Aspect ratio will be preserved.</param>
        /// <param name="quality">Quality level, from 0-100. Currently only applies to JPG. The default value should suffice.</param>
        public string GetUserImageUrl(Guid userId, int? width = null, int? height = null, int? maxWidth = null, int? maxHeight = null, int? quality = null)
        {
            string url = ApiUrl + "/image";

            url += "?userId=" + userId.ToString();

            if (width.HasValue)
            {
                url += "&width=" + width;
            }
            if (height.HasValue)
            {
                url += "&height=" + height;
            }
            if (maxWidth.HasValue)
            {
                url += "&maxWidth=" + maxWidth;
            }
            if (maxHeight.HasValue)
            {
                url += "&maxHeight=" + maxHeight;
            }
            if (quality.HasValue)
            {
                url += "&quality=" + quality;
            }

            return url;
        }

        /// <summary>
        /// Gets an image url that can be used to download an image from the api
        /// </summary>
        /// <param name="name">The name of the person</param>
        /// <param name="width">Use if a fixed width is required. Aspect ratio will be preserved.</param>
        /// <param name="height">Use if a fixed height is required. Aspect ratio will be preserved.</param>
        /// <param name="maxWidth">Use if a max width is required. Aspect ratio will be preserved.</param>
        /// <param name="maxHeight">Use if a max height is required. Aspect ratio will be preserved.</param>
        /// <param name="quality">Quality level, from 0-100. Currently only applies to JPG. The default value should suffice.</param>
        public string GetPersonImageUrl(string name, int? width = null, int? height = null, int? maxWidth = null, int? maxHeight = null, int? quality = null)
        {
            string url = ApiUrl + "/image";

            url += "?personname=" + name;

            if (width.HasValue)
            {
                url += "&width=" + width;
            }
            if (height.HasValue)
            {
                url += "&height=" + height;
            }
            if (maxWidth.HasValue)
            {
                url += "&maxWidth=" + maxWidth;
            }
            if (maxHeight.HasValue)
            {
                url += "&maxHeight=" + maxHeight;
            }
            if (quality.HasValue)
            {
                url += "&quality=" + quality;
            }

            return url;
        }

        /// <summary>
        /// Gets an image url that can be used to download an image from the api
        /// </summary>
        /// <param name="year">The year</param>
        /// <param name="width">Use if a fixed width is required. Aspect ratio will be preserved.</param>
        /// <param name="height">Use if a fixed height is required. Aspect ratio will be preserved.</param>
        /// <param name="maxWidth">Use if a max width is required. Aspect ratio will be preserved.</param>
        /// <param name="maxHeight">Use if a max height is required. Aspect ratio will be preserved.</param>
        /// <param name="quality">Quality level, from 0-100. Currently only applies to JPG. The default value should suffice.</param>
        public string GetYearImageUrl(int year, int? width = null, int? height = null, int? maxWidth = null, int? maxHeight = null, int? quality = null)
        {
            string url = ApiUrl + "/image";

            url += "?year=" + year;

            if (width.HasValue)
            {
                url += "&width=" + width;
            }
            if (height.HasValue)
            {
                url += "&height=" + height;
            }
            if (maxWidth.HasValue)
            {
                url += "&maxWidth=" + maxWidth;
            }
            if (maxHeight.HasValue)
            {
                url += "&maxHeight=" + maxHeight;
            }
            if (quality.HasValue)
            {
                url += "&quality=" + quality;
            }

            return url;
        }

        /// <summary>
        /// Gets an image url that can be used to download an image from the api
        /// </summary>
        /// <param name="name">The name of the genre</param>
        /// <param name="width">Use if a fixed width is required. Aspect ratio will be preserved.</param>
        /// <param name="height">Use if a fixed height is required. Aspect ratio will be preserved.</param>
        /// <param name="maxWidth">Use if a max width is required. Aspect ratio will be preserved.</param>
        /// <param name="maxHeight">Use if a max height is required. Aspect ratio will be preserved.</param>
        /// <param name="quality">Quality level, from 0-100. Currently only applies to JPG. The default value should suffice.</param>
        public string GetGenreImageUrl(string name, int? width = null, int? height = null, int? maxWidth = null, int? maxHeight = null, int? quality = null)
        {
            string url = ApiUrl + "/image";

            url += "?genre=" + name;

            if (width.HasValue)
            {
                url += "&width=" + width;
            }
            if (height.HasValue)
            {
                url += "&height=" + height;
            }
            if (maxWidth.HasValue)
            {
                url += "&maxWidth=" + maxWidth;
            }
            if (maxHeight.HasValue)
            {
                url += "&maxHeight=" + maxHeight;
            }
            if (quality.HasValue)
            {
                url += "&quality=" + quality;
            }

            return url;
        }

        /// <summary>
        /// Gets an image url that can be used to download an image from the api
        /// </summary>
        /// <param name="name">The name of the studio</param>
        /// <param name="width">Use if a fixed width is required. Aspect ratio will be preserved.</param>
        /// <param name="height">Use if a fixed height is required. Aspect ratio will be preserved.</param>
        /// <param name="maxWidth">Use if a max width is required. Aspect ratio will be preserved.</param>
        /// <param name="maxHeight">Use if a max height is required. Aspect ratio will be preserved.</param>
        /// <param name="quality">Quality level, from 0-100. Currently only applies to JPG. The default value should suffice.</param>
        public string GetStudioImageUrl(string name, int? width = null, int? height = null, int? maxWidth = null, int? maxHeight = null, int? quality = null)
        {
            string url = ApiUrl + "/image";

            url += "?studio=" + name;

            if (width.HasValue)
            {
                url += "&width=" + width;
            }
            if (height.HasValue)
            {
                url += "&height=" + height;
            }
            if (maxWidth.HasValue)
            {
                url += "&maxWidth=" + maxWidth;
            }
            if (maxHeight.HasValue)
            {
                url += "&maxHeight=" + maxHeight;
            }
            if (quality.HasValue)
            {
                url += "&quality=" + quality;
            }

            return url;
        }

        /// <summary>
        /// This is a helper to get a list of backdrop url's from a given ApiBaseItemWrapper. If the actual item does not have any backdrops it will return backdrops from the first parent that does.
        /// </summary>
        /// <param name="item">A given item.</param>
        /// <param name="width">Use if a fixed width is required. Aspect ratio will be preserved.</param>
        /// <param name="height">Use if a fixed height is required. Aspect ratio will be preserved.</param>
        /// <param name="maxWidth">Use if a max width is required. Aspect ratio will be preserved.</param>
        /// <param name="maxHeight">Use if a max height is required. Aspect ratio will be preserved.</param>
        /// <param name="quality">Quality level, from 0-100. Currently only applies to JPG. The default value should suffice.</param>
        public IEnumerable<string> GetBackdropImageUrls(DTOBaseItem item, int? width = null, int? height = null, int? maxWidth = null, int? maxHeight = null, int? quality = null)
        {
            Guid? backdropItemId = null;
            int backdropCount = 0;

            if (item.BackdropCount == 0)
            {
                backdropItemId = item.ParentBackdropItemId;
                backdropCount = item.ParentBackdropCount ?? 0;
            }
            else
            {
                backdropItemId = item.Id;
                backdropCount = item.BackdropCount;
            }

            if (backdropItemId == null)
            {
                return new string[] { };
            }

            List<string> files = new List<string>();

            for (int i = 0; i < backdropCount; i++)
            {
                files.Add(GetImageUrl(backdropItemId.Value, ImageType.Backdrop, i, width, height, maxWidth, maxHeight, quality));
            }

            return files;
        }

        /// <summary>
        /// This is a helper to get the logo image url from a given ApiBaseItemWrapper. If the actual item does not have a logo, it will return the logo from the first parent that does, or null.
        /// </summary>
        /// <param name="item">A given item.</param>
        /// <param name="width">Use if a fixed width is required. Aspect ratio will be preserved.</param>
        /// <param name="height">Use if a fixed height is required. Aspect ratio will be preserved.</param>
        /// <param name="maxWidth">Use if a max width is required. Aspect ratio will be preserved.</param>
        /// <param name="maxHeight">Use if a max height is required. Aspect ratio will be preserved.</param>
        /// <param name="quality">Quality level, from 0-100. Currently only applies to JPG. The default value should suffice.</param>
        public string GetLogoImageUrl(DTOBaseItem item, int? width = null, int? height = null, int? maxWidth = null, int? maxHeight = null, int? quality = null)
        {
            Guid? logoItemId = item.HasLogo ? item.Id : item.ParentLogoItemId;

            if (logoItemId.HasValue)
            {
                return GetImageUrl(logoItemId.Value, ImageType.Logo, null, width, height, maxWidth, maxHeight, quality);
            }

            return null;
        }

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
        public async Task<IEnumerable<DTOUser>> GetAllUsersAsync()
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
        public async Task<IEnumerable<IBNItem>> GetAllGenresAsync(Guid userId)
        {
            string url = ApiUrl + "/genres?userId=" + userId.ToString();

            using (Stream stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<IBNItem[]>(stream);
            }
        }

        /// <summary>
        /// Gets all Years
        /// </summary>
        public async Task<IEnumerable<IBNItem>> GetAllYearsAsync(Guid userId)
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
        public async Task<IEnumerable<DTOBaseItem>> GetItemsWithYearAsync(string name, Guid userId)
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
        public async Task<IEnumerable<DTOBaseItem>> GetItemsWithGenreAsync(string name, Guid userId)
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
        public async Task<IEnumerable<DTOBaseItem>> GetItemsWithPersonAsync(string name, Guid userId)
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
        public async Task<IEnumerable<DTOBaseItem>> GetItemsWithPersonAsync(string name, string personType, Guid userId)
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
        public async Task<IEnumerable<IBNItem>> GetAllStudiosAsync(Guid userId)
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
        public async Task<IEnumerable<DTOBaseItem>> GetItemsWithStudioAsync(string name, Guid userId)
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
        public async Task<PluginInfo[]> GetInstalledPlugins()
        {
            string url = ApiUrl + "/plugins";

            using (Stream stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<PluginInfo[]>(stream);
            }
        }
        
        /// <summary>
        /// Gets weather information for the default location as set in configuration
        /// </summary>
        public async Task<ServerConfiguration> GetServerConfigurationAsync()
        {
            string url = ApiUrl + "/ServerConfiguration";

            using (Stream stream = await GetSerializedStreamAsync(url, ApiInteraction.SerializationFormat.Json).ConfigureAwait(false))
            {
                return DeserializeFromStream<ServerConfiguration>(stream, ApiInteraction.SerializationFormat.Json);
            }
        }

        /// <summary>
        /// Gets weather information for the default location as set in configuration
        /// </summary>
        public async Task<DTOUser> GetDefaultUserAsync()
        {
            string url = ApiUrl + "/defaultuser";

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
        /// This is a helper around getting a stream from the server that contains serialized data
        /// </summary>
        private Task<Stream> GetSerializedStreamAsync(string url)
        {
            return GetSerializedStreamAsync(url, SerializationFormat);
        }

        /// <summary>
        /// This is a helper around getting a stream from the server that contains serialized data
        /// </summary>
        private Task<Stream> GetSerializedStreamAsync(string url, SerializationFormat serializationFormat)
        {
            if (url.IndexOf('?') == -1)
            {
                url += "?dataformat=" + serializationFormat.ToString().ToLower();
            }
            else
            {
                url += "&dataformat=" + serializationFormat.ToString().ToLower();
            }

            return GetStreamAsync(url);
        }
        
        private T DeserializeFromStream<T>(Stream stream)
        {
            return DeserializeFromStream<T>(stream, SerializationFormat);
        }

        private T DeserializeFromStream<T>(Stream stream, SerializationFormat format)
        {
            if (format == ApiInteraction.SerializationFormat.Protobuf)
            {
                return DataSerializer.DeserializeProtobufFromStream<T>(stream);
            }
            if (format == ApiInteraction.SerializationFormat.Jsv)
            {
                return DataSerializer.DeserializeJsvFromStream<T>(stream);
            }

            return DataSerializer.DeserializeJsonFromStream<T>(stream);
        }

        /// <summary>
        /// This is just a helper around HttpClient
        /// </summary>
        private Task<Stream> GetStreamAsync(string url)
        {
            return HttpClient.GetStreamAsync(url);
        }

        public void Dispose()
        {
            HttpClient.Dispose();
        }
    }

    public enum SerializationFormat
    {
        Json,
        Jsv,
        Protobuf
    }
}
