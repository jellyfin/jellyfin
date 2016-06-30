using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Security;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.System;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommonIO;
using MediaBrowser.Model.Net;

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
    public class PingSystem : IReturnVoid
    {

    }

    /// <summary>
    /// Class RestartApplication
    /// </summary>
    [Route("/System/Restart", "POST", Summary = "Restarts the application, if needed")]
    [Authenticated(Roles = "Admin")]
    public class RestartApplication
    {
    }

    /// <summary>
    /// This is currently not authenticated because the uninstaller needs to be able to shutdown the server.
    /// </summary>
    [Route("/System/Shutdown", "POST", Summary = "Shuts down the application")]
    public class ShutdownApplication
    {
        // TODO: This is not currently authenticated due to uninstaller
        // Improve later
    }

    [Route("/System/Logs", "GET", Summary = "Gets a list of available server log files")]
    [Authenticated(Roles = "Admin")]
    public class GetServerLogs : IReturn<List<LogFile>>
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

        private readonly ISecurityManager _security;

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemService" /> class.
        /// </summary>
        /// <param name="appHost">The app host.</param>
        /// <param name="appPaths">The application paths.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <exception cref="ArgumentNullException">jsonSerializer</exception>
        public SystemService(IServerApplicationHost appHost, IApplicationPaths appPaths, IFileSystem fileSystem, INetworkManager network, ISecurityManager security)
        {
            _appHost = appHost;
            _appPaths = appPaths;
            _fileSystem = fileSystem;
            _network = network;
            _security = security;
        }

        public object Post(PingSystem request)
        {
            return _appHost.Name;
        }

        public object Get(GetServerLogs request)
        {
            List<FileSystemMetadata> files;

            try
            {
                files = _fileSystem.GetFiles(_appPaths.LogDirectoryPath)
                    .Where(i => string.Equals(i.Extension, ".txt", StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            catch (DirectoryNotFoundException)
            {
                files = new List<FileSystemMetadata>();
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
                .ToList();

            return ToOptimizedResult(result);
        }

        public Task<object> Get(GetLogFile request)
        {
            var file = _fileSystem.GetFiles(_appPaths.LogDirectoryPath)
                .First(i => string.Equals(i.Name, request.Name, StringComparison.OrdinalIgnoreCase));

            return ResultFactory.GetStaticFileResult(Request, file.FullName, FileShare.ReadWrite);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public async Task<object> Get(GetSystemInfo request)
        {
            var result = await _appHost.GetSystemInfo().ConfigureAwait(false);

            return ToOptimizedResult(result);
        }

        public async Task<object> Get(GetPublicSystemInfo request)
        {
            var result = await _appHost.GetSystemInfo().ConfigureAwait(false);

            var publicInfo = new PublicSystemInfo
            {
                Id = result.Id,
                ServerName = result.ServerName,
                Version = result.Version,
                LocalAddress = result.LocalAddress,
                WanAddress = result.WanAddress,
                OperatingSystem = result.OperatingSystem
            };

            return ToOptimizedResult(publicInfo);
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
