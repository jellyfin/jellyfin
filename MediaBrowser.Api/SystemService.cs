using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.IO;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.System;
using ServiceStack;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MediaBrowser.Api
{
    /// <summary>
    /// Class GetSystemInfo
    /// </summary>
    [Route("/System/Info", "GET")]
    [Api(Description = "Gets information about the server")]
    public class GetSystemInfo : IReturn<SystemInfo>
    {

    }

    /// <summary>
    /// Class RestartApplication
    /// </summary>
    [Route("/System/Restart", "POST")]
    [Api(("Restarts the application, if needed"))]
    public class RestartApplication
    {
    }

    [Route("/System/Shutdown", "POST")]
    [Api(("Shuts down the application"))]
    public class ShutdownApplication
    {
    }
    
    /// <summary>
    /// Class GetConfiguration
    /// </summary>
    [Route("/System/Configuration", "GET")]
    [Api(("Gets application configuration"))]
    public class GetConfiguration : IReturn<ServerConfiguration>
    {

    }

    /// <summary>
    /// Class UpdateConfiguration
    /// </summary>
    [Route("/System/Configuration", "POST")]
    [Api(("Updates application configuration"))]
    public class UpdateConfiguration : ServerConfiguration, IReturnVoid
    {
    }

    /// <summary>
    /// Class SystemInfoService
    /// </summary>
    public class SystemService : BaseApiService
    {
        /// <summary>
        /// The _json serializer
        /// </summary>
        private readonly IJsonSerializer _jsonSerializer;

        /// <summary>
        /// The _app host
        /// </summary>
        private readonly IServerApplicationHost _appHost;

        /// <summary>
        /// The _configuration manager
        /// </summary>
        private readonly IServerConfigurationManager _configurationManager;

        private readonly IFileSystem _fileSystem;


        /// <summary>
        /// Initializes a new instance of the <see cref="SystemService" /> class.
        /// </summary>
        /// <param name="jsonSerializer">The json serializer.</param>
        /// <param name="appHost">The app host.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <exception cref="System.ArgumentNullException">jsonSerializer</exception>
        public SystemService(IJsonSerializer jsonSerializer, IServerApplicationHost appHost, IServerConfigurationManager configurationManager, IFileSystem fileSystem)
            : base()
        {
            if (jsonSerializer == null)
            {
                throw new ArgumentNullException("jsonSerializer");
            }
            if (appHost == null)
            {
                throw new ArgumentNullException("appHost");
            }

            _appHost = appHost;
            _configurationManager = configurationManager;
            _fileSystem = fileSystem;
            _jsonSerializer = jsonSerializer;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetSystemInfo request)
        {
            var result = _appHost.GetSystemInfo();

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetConfiguration request)
        {
            var configPath = _configurationManager.ApplicationPaths.SystemConfigurationFilePath;

            var dateModified = _fileSystem.GetLastWriteTimeUtc(configPath);

            var cacheKey = (configPath + dateModified.Ticks).GetMD5();

            return ToOptimizedResultUsingCache(cacheKey, dateModified, null, () => _configurationManager.Configuration);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(RestartApplication request)
        {
            Task.Run(async () =>
            {
                await Task.Delay(100).ConfigureAwait(false);
                await _appHost.Restart().ConfigureAwait(false);
            });
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(ShutdownApplication request)
        {
            Task.Run(async () =>
            {
                await Task.Delay(100).ConfigureAwait(false);
                await _appHost.Shutdown().ConfigureAwait(false);
            });
        }

        /// <summary>
        /// Posts the specified configuraiton.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(UpdateConfiguration request)
        {
            // Silly, but we need to serialize and deserialize or the XmlSerializer will write the xml with an element name of UpdateConfiguration

            var json = _jsonSerializer.SerializeToString(request);

            var config = _jsonSerializer.DeserializeFromString<ServerConfiguration>(json);

            _configurationManager.ReplaceConfiguration(config);
        }
    }
}
