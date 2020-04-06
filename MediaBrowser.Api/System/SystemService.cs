using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Services;
using MediaBrowser.Model.System;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Api.System
{
    /// <summary>
    /// Class GetSystemInfo
    /// </summary>
    [Route("/System/Info", "GET", Summary = "Gets information about the server")]
    [Authenticated(EscapeParentalControl = true, AllowBeforeStartupWizard = true)]
    public class GetSystemInfo : IReturn<SystemInfo>
    {

    }

    [Route("/System/Info/Public", "GET", Summary = "Gets public information about the server")]
    public class GetPublicSystemInfo : IReturn<PublicSystemInfo>
    {

    }

    [Route("/System/Ping", "POST")]
    [Route("/System/Ping", "GET")]
    public class PingSystem : IReturnVoid
    {

    }

    /// <summary>
    /// Class RestartApplication
    /// </summary>
    [Route("/System/Restart", "POST", Summary = "Restarts the application, if needed")]
    [Authenticated(Roles = "Admin", AllowLocal = true)]
    public class RestartApplication
    {
    }

    /// <summary>
    /// This is currently not authenticated because the uninstaller needs to be able to shutdown the server.
    /// </summary>
    [Route("/System/Shutdown", "POST", Summary = "Shuts down the application")]
    [Authenticated(Roles = "Admin", AllowLocal = true)]
    public class ShutdownApplication
    {
    }

    [Route("/System/Logs", "GET", Summary = "Gets a list of available server log files")]
    [Authenticated(Roles = "Admin")]
    public class GetServerLogs : IReturn<LogFile[]>
    {
    }

    [Route("/System/Endpoint", "GET", Summary = "Gets information about the request endpoint")]
    [Authenticated]
    public class GetEndpointInfo : IReturn<EndPointInfo>
    {
        public string Endpoint { get; set; }
    }

    [Route("/System/Logs/Log", "GET", Summary = "Gets a log file")]
    [Authenticated(Roles = "Admin")]
    public class GetLogFile
    {
        [ApiMember(Name = "Name", Description = "The log file name.", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string Name { get; set; }
    }

    [Route("/System/WakeOnLanInfo", "GET", Summary = "Gets wake on lan information")]
    [Authenticated]
    public class GetWakeOnLanInfo : IReturn<WakeOnLanInfo[]>
    {

    }

    /// <summary>
    /// Class SystemInfoService
    /// </summary>
    public class SystemService : BaseApiService
    {
        /// <summary>
        /// The _app host
        /// </summary>
        private readonly IServerApplicationHost _appHost;
        private readonly IApplicationPaths _appPaths;
        private readonly IFileSystem _fileSystem;

        private readonly INetworkManager _network;

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemService" /> class.
        /// </summary>
        /// <param name="appHost">The app host.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <exception cref="ArgumentNullException">jsonSerializer</exception>
        public SystemService(
            ILogger<SystemService> logger,
            IServerConfigurationManager serverConfigurationManager,
            IHttpResultFactory httpResultFactory,
            IServerApplicationHost appHost,
            IFileSystem fileSystem,
            INetworkManager network)
            : base(logger, serverConfigurationManager, httpResultFactory)
        {
            _appPaths = serverConfigurationManager.ApplicationPaths;
            _appHost = appHost;
            _fileSystem = fileSystem;
            _network = network;
        }

        public object Post(PingSystem request)
        {
            return _appHost.Name;
        }

        public object Get(GetWakeOnLanInfo request)
        {
            var result = _appHost.GetWakeOnLanInfo();

            return ToOptimizedResult(result);
        }

        public object Get(GetServerLogs request)
        {
            IEnumerable<FileSystemMetadata> files;

            try
            {
                files = _fileSystem.GetFiles(_appPaths.LogDirectoryPath, new[] { ".txt", ".log" }, true, false);
            }
            catch (IOException ex)
            {
                Logger.LogError(ex, "Error getting logs");
                files = Enumerable.Empty<FileSystemMetadata>();
            }

            var result = files.Select(i => new LogFile
            {
                DateCreated = _fileSystem.GetCreationTimeUtc(i),
                DateModified = _fileSystem.GetLastWriteTimeUtc(i),
                Name = i.Name,
                Size = i.Length

            }).OrderByDescending(i => i.DateModified)
                .ThenByDescending(i => i.DateCreated)
                .ThenBy(i => i.Name)
                .ToArray();

            return ToOptimizedResult(result);
        }

        public Task<object> Get(GetLogFile request)
        {
            var file = _fileSystem.GetFiles(_appPaths.LogDirectoryPath)
                .First(i => string.Equals(i.Name, request.Name, StringComparison.OrdinalIgnoreCase));

            // For older files, assume fully static
            var fileShare = file.LastWriteTimeUtc < DateTime.UtcNow.AddHours(-1) ? FileShare.Read : FileShare.ReadWrite;

            return ResultFactory.GetStaticFileResult(Request, file.FullName, fileShare);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public async Task<object> Get(GetSystemInfo request)
        {
            var result = await _appHost.GetSystemInfo(CancellationToken.None).ConfigureAwait(false);

            return ToOptimizedResult(result);
        }

        public async Task<object> Get(GetPublicSystemInfo request)
        {
            var result = await _appHost.GetPublicSystemInfo(CancellationToken.None).ConfigureAwait(false);

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(RestartApplication request)
        {
            _appHost.Restart();
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

        public object Get(GetEndpointInfo request)
        {
            return ToOptimizedResult(new EndPointInfo
            {
                IsLocal = Request.IsLocal,
                IsInNetwork = _network.IsInLocalNetwork(request.Endpoint ?? Request.RemoteIp)
            });
        }
    }
}
