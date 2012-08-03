using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Users;

namespace MediaBrowser.ApiInteraction
{
    public class ApiClient : BaseClient
    {
        public IJsonSerializer JsonSerializer { get; set; }

        public ApiClient()
            : base()
        {
        }

        public ApiClient(HttpClientHandler handler)
            : base(handler)
        {
        }

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
        public string GetImageUrl(Guid itemId, ImageType imageType, int? imageIndex, int? width, int? height, int? maxWidth, int? maxHeight, int? quality)
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
        /// <param name="itemWrapper">A given item.</param>
        /// <param name="width">Use if a fixed width is required. Aspect ratio will be preserved.</param>
        /// <param name="height">Use if a fixed height is required. Aspect ratio will be preserved.</param>
        /// <param name="maxWidth">Use if a max width is required. Aspect ratio will be preserved.</param>
        /// <param name="maxHeight">Use if a max height is required. Aspect ratio will be preserved.</param>
        /// <param name="quality">Quality level, from 0-100. Currently only applies to JPG. The default value should suffice.</param>
        public IEnumerable<string> GetBackdropImageUrls(ApiBaseItemWrapper<ApiBaseItem> itemWrapper, int? width, int? height, int? maxWidth, int? maxHeight, int? quality)
        {
            Guid? backdropItemId = null;
            int backdropCount = 0;

            if (itemWrapper.Item.BackdropImagePaths == null || !itemWrapper.Item.BackdropImagePaths.Any())
            {
                backdropItemId = itemWrapper.ParentBackdropItemId;
                backdropCount = itemWrapper.ParentBackdropCount ?? 0;
            }
            else
            {
                backdropItemId = itemWrapper.Item.Id;
                backdropCount = itemWrapper.Item.BackdropImagePaths.Count();
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
        /// <param name="itemWrapper">A given item.</param>
        /// <param name="width">Use if a fixed width is required. Aspect ratio will be preserved.</param>
        /// <param name="height">Use if a fixed height is required. Aspect ratio will be preserved.</param>
        /// <param name="maxWidth">Use if a max width is required. Aspect ratio will be preserved.</param>
        /// <param name="maxHeight">Use if a max height is required. Aspect ratio will be preserved.</param>
        /// <param name="quality">Quality level, from 0-100. Currently only applies to JPG. The default value should suffice.</param>
        public string GetLogoImageUrl(ApiBaseItemWrapper<ApiBaseItem> itemWrapper, int? width, int? height, int? maxWidth, int? maxHeight, int? quality)
        {
            Guid? logoItemId = !string.IsNullOrEmpty(itemWrapper.Item.LogoImagePath) ? itemWrapper.Item.Id : itemWrapper.ParentLogoItemId;

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
        public async Task<ApiBaseItemWrapper<ApiBaseItem>> GetItemAsync(Guid id, Guid userId)
        {
            string url = ApiUrl + "/item?userId=" + userId.ToString();

            if (id != Guid.Empty)
            {
                url += "&id=" + id.ToString();
            }

            using (Stream stream = await HttpClient.GetStreamAsync(url))
            {
                return JsonSerializer.DeserializeFromStream<ApiBaseItemWrapper<ApiBaseItem>>(stream);
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
        public async Task<IEnumerable<CategoryInfo<Genre>>> GetAllGenresAsync(Guid userId)
        {
            string url = ApiUrl + "/genres?userId=" + userId.ToString();

            using (Stream stream = await HttpClient.GetStreamAsync(url))
            {
                return JsonSerializer.DeserializeFromStream<IEnumerable<CategoryInfo<Genre>>>(stream);
            }
        }

        /// <summary>
        /// Gets a Genre
        /// </summary>
        public async Task<CategoryInfo<Genre>> GetGenreAsync(string name, Guid userId)
        {
            string url = ApiUrl + "/genre?userId=" + userId.ToString() + "&name=" + name;

            using (Stream stream = await HttpClient.GetStreamAsync(url))
            {
                return JsonSerializer.DeserializeFromStream<CategoryInfo<Genre>>(stream);
            }
        }

        /// <summary>
        /// Gets all studious
        /// </summary>
        public async Task<IEnumerable<CategoryInfo<Studio>>> GetAllStudiosAsync(Guid userId)
        {
            string url = ApiUrl + "/studios?userId=" + userId.ToString();

            using (Stream stream = await HttpClient.GetStreamAsync(url))
            {
                return JsonSerializer.DeserializeFromStream<IEnumerable<CategoryInfo<Studio>>>(stream);
            }
        }

        /// <summary>
        /// Gets the current personalized configuration
        /// </summary>
        public async Task<UserConfiguration> GetUserConfigurationAsync(Guid userId)
        {
            string url = ApiUrl + "/userconfiguration?userId=" + userId.ToString();

            using (Stream stream = await HttpClient.GetStreamAsync(url))
            {
                return JsonSerializer.DeserializeFromStream<UserConfiguration>(stream);
            }
        }

        /// <summary>
        /// Gets a Studio
        /// </summary>
        public async Task<CategoryInfo<Studio>> GetStudioAsync(string name, Guid userId)
        {
            string url = ApiUrl + "/studio?userId=" + userId.ToString() + "&name=" + name;

            using (Stream stream = await HttpClient.GetStreamAsync(url))
            {
                return JsonSerializer.DeserializeFromStream<CategoryInfo<Studio>>(stream);
            }
        }
    }
}
