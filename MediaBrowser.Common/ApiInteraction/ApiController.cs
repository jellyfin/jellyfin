using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using MediaBrowser.Common.Json;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Users;

namespace MediaBrowser.Common.ApiInteraction
{
    public class ApiController
    {
        public string ApiUrl { get; set; }

        private WebClient WebClient { get; set; }

        public ApiController()
        {
            WebClient = new WebClient();
        }

        public async Task<ApiBaseItemWrapper<ApiBaseItem>> GetRootItem(Guid userId)
        {
            string url = ApiUrl + "/item?userId=" + userId.ToString();

            using (Stream stream = await WebClient.OpenReadTaskAsync(url))
            {
                using (GZipStream gzipStream = new GZipStream(stream, CompressionMode.Decompress, false))
                {
                    return DeserializeBaseItemWrapper(gzipStream);
                }
            }
        }

        public async Task<ApiBaseItemWrapper<ApiBaseItem>> GetItem(Guid id, Guid userId)
        {
            string url = ApiUrl + "/item?userId=" + userId.ToString();

            if (id != Guid.Empty)
            {
                url += "&id=" + id.ToString();
            }

            using (Stream stream = await WebClient.OpenReadTaskAsync(url))
            {
                using (GZipStream gzipStream = new GZipStream(stream, CompressionMode.Decompress, false))
                {
                    return DeserializeBaseItemWrapper(gzipStream);
                }
            }
        }

        public async Task<IEnumerable<User>> GetAllUsers()
        {
            string url = ApiUrl + "/users";

            using (Stream stream = await WebClient.OpenReadTaskAsync(url))
            {
                using (GZipStream gzipStream = new GZipStream(stream, CompressionMode.Decompress, false))
                {
                    return JsonSerializer.DeserializeFromStream<IEnumerable<User>>(gzipStream);
                }
            }
        }

        public async Task<IEnumerable<CategoryInfo>> GetAllGenres(Guid userId)
        {
            string url = ApiUrl + "/genres?userId=" + userId.ToString();

            using (Stream stream = await WebClient.OpenReadTaskAsync(url))
            {
                using (GZipStream gzipStream = new GZipStream(stream, CompressionMode.Decompress, false))
                {
                    return JsonSerializer.DeserializeFromStream<IEnumerable<CategoryInfo>>(gzipStream);
                }
            }
        }

        public async Task<CategoryInfo> GetGenre(string name, Guid userId)
        {
            string url = ApiUrl + "/genre?userId=" + userId.ToString() + "&name=" + name;

            using (Stream stream = await WebClient.OpenReadTaskAsync(url))
            {
                using (GZipStream gzipStream = new GZipStream(stream, CompressionMode.Decompress, false))
                {
                    return JsonSerializer.DeserializeFromStream<CategoryInfo>(gzipStream);
                }
            }
        }

        public async Task<IEnumerable<CategoryInfo>> GetAllStudios(Guid userId)
        {
            string url = ApiUrl + "/studios?userId=" + userId.ToString();

            using (Stream stream = await WebClient.OpenReadTaskAsync(url))
            {
                using (GZipStream gzipStream = new GZipStream(stream, CompressionMode.Decompress, false))
                {
                    return JsonSerializer.DeserializeFromStream<IEnumerable<CategoryInfo>>(gzipStream);
                }
            }
        }

        public async Task<CategoryInfo> GetStudio(string name, Guid userId)
        {
            string url = ApiUrl + "/studio?userId=" + userId.ToString() + "&name=" + name;

            using (Stream stream = await WebClient.OpenReadTaskAsync(url))
            {
                using (GZipStream gzipStream = new GZipStream(stream, CompressionMode.Decompress, false))
                {
                    return JsonSerializer.DeserializeFromStream<CategoryInfo>(gzipStream);
                }
            }
        }

        private static ApiBaseItemWrapper<ApiBaseItem> DeserializeBaseItemWrapper(Stream stream)
        {
            ApiBaseItemWrapper<ApiBaseItem> data = JsonSerializer.DeserializeFromStream<ApiBaseItemWrapper<ApiBaseItem>>(stream);

            return data;
        }
    }
}
