using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Themes;
using MediaBrowser.Model.Themes;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MediaBrowser.Api
{
    [Route("/Themes", "GET", Summary = "Gets a list of available themes for an app")]
    public class GetAppThemes : IReturn<List<AppThemeInfo>>
    {
        [ApiMember(Name = "App", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string App { get; set; }
    }

    [Route("/Themes/Info", "GET", Summary = "Gets an app theme")]
    public class GetAppTheme : IReturn<AppTheme>
    {
        [ApiMember(Name = "App", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string App { get; set; }

        [ApiMember(Name = "Name", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string Name { get; set; }
    }

    [Route("/Themes/Images", "GET", Summary = "Gets an app theme")]
    public class GetAppThemeImage
    {
        [ApiMember(Name = "App", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string App { get; set; }

        [ApiMember(Name = "Theme", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string Theme { get; set; }

        [ApiMember(Name = "Name", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string Name { get; set; }

        [ApiMember(Name = "CacheTag", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string CacheTag { get; set; }
    }

    [Route("/Themes", "POST", Summary = "Saves a theme")]
    public class SaveTheme : AppTheme, IReturnVoid
    {
    }

    [Authenticated]
    public class AppThemeService : BaseApiService
    {
        private readonly IAppThemeManager _themeManager;
        private readonly IFileSystem _fileSystem;

        public AppThemeService(IAppThemeManager themeManager, IFileSystem fileSystem)
        {
            _themeManager = themeManager;
            _fileSystem = fileSystem;
        }

        public object Get(GetAppThemes request)
        {
            var result = _themeManager.GetThemes(request.App).ToList();

            return ToOptimizedResult(result);
        }

        public object Get(GetAppTheme request)
        {
            var result = _themeManager.GetTheme(request.App, request.Name);

            return ToOptimizedResult(result);
        }

        public void Post(SaveTheme request)
        {
            _themeManager.SaveTheme(request);
        }

        public object Get(GetAppThemeImage request)
        {
            var info = _themeManager.GetImageImageInfo(request.App, request.Theme, request.Name);

            var cacheGuid = new Guid(info.CacheTag);

            TimeSpan? cacheDuration = null;

            if (!string.IsNullOrEmpty(request.CacheTag) && cacheGuid == new Guid(request.CacheTag))
            {
                cacheDuration = TimeSpan.FromDays(365);
            }

            var contentType = MimeTypes.GetMimeType(info.Path);

            return ToCachedResult(cacheGuid, info.DateModified, cacheDuration, () => _fileSystem.GetFileStream(info.Path, FileMode.Open, FileAccess.Read, FileShare.Read), contentType);
        }
    }
}
