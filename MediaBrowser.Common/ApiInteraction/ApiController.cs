using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading.Tasks;
using MediaBrowser.Common.Json;
using MediaBrowser.Model.Users;
using MediaBrowser.Model.Entities;

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

        public async Task<DictionaryBaseItem> GetRootItem(Guid userId)
        {
            string url = ApiUrl + "/item?userId=" + userId.ToString();

            Stream stream = await WebClient.OpenReadTaskAsync(url);

            using (GZipStream gzipStream = new GZipStream(stream, CompressionMode.Decompress, false))
            {
                return DictionaryBaseItem.FromApiOutput(gzipStream);
            }
        }

        public async Task<DictionaryBaseItem> GetItem(Guid id, Guid userId)
        {
            string url = ApiUrl + "/item?userId=" + userId.ToString();

            if (id != Guid.Empty)
            {
                url += "&id=" + id.ToString();
            }

            Stream stream = await WebClient.OpenReadTaskAsync(url);

            using (GZipStream gzipStream = new GZipStream(stream, CompressionMode.Decompress, false))
            {
                return DictionaryBaseItem.FromApiOutput(gzipStream);
            }
        }

        public async Task<IEnumerable<User>> GetAllUsers()
        {
            string url = ApiUrl + "/users";

            Stream stream = await WebClient.OpenReadTaskAsync(url);

            using (GZipStream gzipStream = new GZipStream(stream, CompressionMode.Decompress, false))
            {
                return JsonSerializer.DeserializeFromStream<IEnumerable<User>>(gzipStream);
            }
        }

        public async Task<IEnumerable<CategoryInfo>> GetAllGenres(Guid userId)
        {
            string url = ApiUrl + "/genres?userId=" + userId.ToString();

            Stream stream = await WebClient.OpenReadTaskAsync(url);

            using (GZipStream gzipStream = new GZipStream(stream, CompressionMode.Decompress, false))
            {
                return JsonSerializer.DeserializeFromStream<IEnumerable<CategoryInfo>>(gzipStream);
            }
        }

        public async Task<CategoryInfo> GetGenre(string name, Guid userId)
        {
            string url = ApiUrl + "/genre?userId=" + userId.ToString() + "&name=" + name;

            Stream stream = await WebClient.OpenReadTaskAsync(url);

            using (GZipStream gzipStream = new GZipStream(stream, CompressionMode.Decompress, false))
            {
                return JsonSerializer.DeserializeFromStream<CategoryInfo>(gzipStream);
            }
        }

        public async Task<IEnumerable<CategoryInfo>> GetAllStudios(Guid userId)
        {
            string url = ApiUrl + "/studios?userId=" + userId.ToString();

            Stream stream = await WebClient.OpenReadTaskAsync(url);

            using (GZipStream gzipStream = new GZipStream(stream, CompressionMode.Decompress, false))
            {
                return JsonSerializer.DeserializeFromStream<IEnumerable<CategoryInfo>>(gzipStream);
            }
        }

        public async Task<CategoryInfo> GetStudio(string name, Guid userId)
        {
            string url = ApiUrl + "/studio?userId=" + userId.ToString() + "&name=" + name;

            Stream stream = await WebClient.OpenReadTaskAsync(url);

            using (GZipStream gzipStream = new GZipStream(stream, CompressionMode.Decompress, false))
            {
                return JsonSerializer.DeserializeFromStream<CategoryInfo>(gzipStream);
            }
        }
    }
}
