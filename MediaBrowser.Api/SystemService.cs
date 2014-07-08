using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.System;
using ServiceStack;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MediaBrowser.Api
{
    /// <summary>
    /// Class GetSystemInfo
    /// </summary>
    [Route("/System/Info", "GET", Summary = "Gets information about the server")]
    [Authenticated]
    public class GetSystemInfo : IReturn<SystemInfo>
    {

    }

    /// <summary>
    /// Class RestartApplication
    /// </summary>
    [Route("/System/Restart", "POST", Summary = "Restarts the application, if needed")]
    [Authenticated]
    public class RestartApplication
    {
    }

    [Route("/System/Shutdown", "POST", Summary = "Shuts down the application")]
    [Authenticated]
    public class ShutdownApplication
    {
    }

    [Route("/System/Logs", "GET", Summary = "Gets a list of available server log files")]
    [Authenticated]
    public class GetServerLogs : IReturn<List<LogFile>>
    {
    }

    [Route("/System/Logs/Log", "GET", Summary = "Gets a log file")]
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

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemService" /> class.
        /// </summary>
        /// <param name="appHost">The app host.</param>
        /// <exception cref="System.ArgumentNullException">jsonSerializer</exception>
        public SystemService(IServerApplicationHost appHost, IApplicationPaths appPaths, IFileSystem fileSystem)
        {
            _appHost = appHost;
            _appPaths = appPaths;
            _fileSystem = fileSystem;
        }

        public object Get(GetServerLogs request)
        {
            List<FileInfo> files;

            try
            {
                files = new DirectoryInfo(_appPaths.LogDirectoryPath)
                    .EnumerateFiles("*", SearchOption.AllDirectories)
                    .Where(i => string.Equals(i.Extension, ".txt", System.StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            catch (DirectoryNotFoundException)
            {
                files = new List<FileInfo>();
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

        public object Get(GetLogFile request)
        {
            var file = new DirectoryInfo(_appPaths.LogDirectoryPath)
                .EnumerateFiles("*", SearchOption.AllDirectories)
                .First(i => string.Equals(i.Name, request.Name, System.StringComparison.OrdinalIgnoreCase));

            return ResultFactory.GetStaticFileResult(Request, file.FullName, FileShare.ReadWrite);
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

    }
}
