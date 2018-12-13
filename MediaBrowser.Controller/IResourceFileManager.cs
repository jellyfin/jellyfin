using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Extensions;
using Microsoft.Extensions.Logging;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Reflection;
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
