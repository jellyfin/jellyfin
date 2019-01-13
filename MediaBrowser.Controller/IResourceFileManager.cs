using System;
using System.IO;
using System.Threading.Tasks;
using MediaBrowser.Model.Services;

namespace MediaBrowser.Controller
{
    public interface IResourceFileManager
    {
        Task<object> GetStaticFileResult(IRequest request, string basePath, string virtualPath, string contentType, TimeSpan? cacheDuration);

        Stream GetResourceFileStream(string basePath, string virtualPath);

        string ReadAllText(string basePath, string virtualPath);
    }
}
