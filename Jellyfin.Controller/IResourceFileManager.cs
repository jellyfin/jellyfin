using System;
using System.IO;
using System.Threading.Tasks;
using Jellyfin.Model.Services;

namespace Jellyfin.Controller
{
    public interface IResourceFileManager
    {
        Task<object> GetStaticFileResult(IRequest request, string basePath, string virtualPath, string contentType, TimeSpan? cacheDuration);

        Stream GetResourceFileStream(string basePath, string virtualPath);

        string ReadAllText(string basePath, string virtualPath);
    }
}
