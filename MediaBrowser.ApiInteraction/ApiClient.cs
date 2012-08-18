using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using MediaBrowser.Model.DTO;
using MediaBrowser.Model.Entities;

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

        public HttpClient HttpClient { get; private set; }
        public IJsonSerializer JsonSerializer { get; set; }

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
        public async Task<Stream> GetImageStreamAsync(string url)
        {
            return await HttpClient.GetStreamAsync(url);
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

            using (Stream stream = await HttpClient.GetStreamAsync(url))
            {
                return JsonSerializer.DeserializeFromStream<DTOBaseItem>(stream);
            }
        }

        /// <summary>
        /// Gets all Users
        /// </summary>
        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            string url = ApiUrl + "/users";

            using (Stream stream = await HttpClient.GetStreamAsync(url))
            {
                return JsonSerializer.DeserializeFromStream<IEnumerable<User>>(stream);
            }
        }

        /// <summary>
        /// Gets all Genres
        /// </summary>
        public async Task<IEnumerable<IBNItem<Genre>>> GetAllGenresAsync(Guid userId)
        {
            string url = ApiUrl + "/genres?userId=" + userId.ToString();

            using (Stream stream = await HttpClient.GetStreamAsync(url))
            {
                return JsonSerializer.DeserializeFromStream<IEnumerable<IBNItem<Genre>>>(stream);
            }
        }

        /// <summary>
        /// Gets all Years
        /// </summary>
        public async Task<IEnumerable<IBNItem<Year>>> GetAllYearsAsync(Guid userId)
        {
            string url = ApiUrl + "/years?userId=" + userId.ToString();

            using (Stream stream = await HttpClient.GetStreamAsync(url))
            {
                return JsonSerializer.DeserializeFromStream<IEnumerable<IBNItem<Year>>>(stream);
            }
        }

        /// <summary>
        /// Gets all items that contain a given Year
        /// </summary>
        public async Task<IEnumerable<DTOBaseItem>> GetItemsWithYearAsync(string name, Guid userId)
        {
            string url = ApiUrl + "/itemlist?listtype=itemswithyear&userId=" + userId.ToString() + "&name=" + name;

            using (Stream stream = await HttpClient.GetStreamAsync(url))
            {
                return JsonSerializer.DeserializeFromStream<IEnumerable<DTOBaseItem>>(stream);
            }
        }

        /// <summary>
        /// Gets all items that contain a given Genre
        /// </summary>
        public async Task<IEnumerable<DTOBaseItem>> GetItemsWithGenreAsync(string name, Guid userId)
        {
            string url = ApiUrl + "/itemlist?listtype=itemswithgenre&userId=" + userId.ToString() + "&name=" + name;

            using (Stream stream = await HttpClient.GetStreamAsync(url))
            {
                return JsonSerializer.DeserializeFromStream<IEnumerable<DTOBaseItem>>(stream);
            }
        }

        /// <summary>
        /// Gets all items that contain a given Person
        /// </summary>
        public async Task<IEnumerable<DTOBaseItem>> GetItemsWithPersonAsync(string name, Guid userId)
        {
            string url = ApiUrl + "/itemlist?listtype=itemswithperson&userId=" + userId.ToString() + "&name=" + name;

            using (Stream stream = await HttpClient.GetStreamAsync(url))
            {
                return JsonSerializer.DeserializeFromStream<IEnumerable<DTOBaseItem>>(stream);
            }
        }
        
        /// <summary>
        /// Gets all items that contain a given Person
        /// </summary>
        public async Task<IEnumerable<DTOBaseItem>> GetItemsWithPersonAsync(string name, string personType, Guid userId)
        {
            string url = ApiUrl + "/itemlist?listtype=itemswithperson&userId=" + userId.ToString() + "&name=" + name;

            url += "&persontype=" + personType;

            using (Stream stream = await HttpClient.GetStreamAsync(url))
            {
                return JsonSerializer.DeserializeFromStream<IEnumerable<DTOBaseItem>>(stream);
            }
        }

        /// <summary>
        /// Gets all studious
        /// </summary>
        public async Task<IEnumerable<IBNItem<Studio>>> GetAllStudiosAsync(Guid userId)
        {
            string url = ApiUrl + "/studios?userId=" + userId.ToString();

            using (Stream stream = await HttpClient.GetStreamAsync(url))
            {
                return JsonSerializer.DeserializeFromStream<IEnumerable<IBNItem<Studio>>>(stream);
            }
        }

        /// <summary>
        /// Gets all items that contain a given Studio
        /// </summary>
        public async Task<IEnumerable<DTOBaseItem>> GetItemsWithStudioAsync(string name, Guid userId)
        {
            string url = ApiUrl + "/itemlist?listtype=itemswithstudio&userId=" + userId.ToString() + "&name=" + name;

            using (Stream stream = await HttpClient.GetStreamAsync(url))
            {
                return JsonSerializer.DeserializeFromStream<IEnumerable<DTOBaseItem>>(stream);
            }
        }

        /// <summary>
        /// Gets a studio
        /// </summary>
        public async Task<IBNItem<Studio>> GetStudioAsync(Guid userId, string name)
        {
            string url = ApiUrl + "/studio?userId=" + userId.ToString() + "&name=" + name;

            using (Stream stream = await HttpClient.GetStreamAsync(url))
            {
                return JsonSerializer.DeserializeFromStream<IBNItem<Studio>>(stream);
            }
        }

        /// <summary>
        /// Gets a genre
        /// </summary>
        public async Task<IBNItem<Genre>> GetGenreAsync(Guid userId, string name)
        {
            string url = ApiUrl + "/genre?userId=" + userId.ToString() + "&name=" + name;

            using (Stream stream = await HttpClient.GetStreamAsync(url))
            {
                return JsonSerializer.DeserializeFromStream<IBNItem<Genre>>(stream);
            }
        }

        /// <summary>
        /// Gets a person
        /// </summary>
        public async Task<IBNItem<Person>> GetPersonAsync(Guid userId, string name)
        {
            string url = ApiUrl + "/person?userId=" + userId.ToString() + "&name=" + name;

            using (Stream stream = await HttpClient.GetStreamAsync(url))
            {
                return JsonSerializer.DeserializeFromStream<IBNItem<Person>>(stream);
            }
        }

        /// <summary>
        /// Gets a year
        /// </summary>
        public async Task<IBNItem<Year>> GetYearAsync(Guid userId, int year)
        {
            string url = ApiUrl + "/year?userId=" + userId.ToString() + "&year=" + year;

            using (Stream stream = await HttpClient.GetStreamAsync(url))
            {
                return JsonSerializer.DeserializeFromStream<IBNItem<Year>>(stream);
            }
        }

        public void Dispose()
        {
            HttpClient.Dispose();
        }
    }
}
