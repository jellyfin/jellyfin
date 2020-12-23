using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Attributes;
using Jellyfin.Api.Constants;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// The system controller.
    /// </summary>
    public class SystemController : BaseJellyfinApiController
    {
        private readonly IServerApplicationHost _appHost;
        private readonly IApplicationPaths _appPaths;
        private readonly IFileSystem _fileSystem;
        private readonly INetworkManager _network;
        private readonly ILogger<SystemController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemController"/> class.
        /// </summary>
        /// <param name="serverConfigurationManager">Instance of <see cref="IServerConfigurationManager"/> interface.</param>
        /// <param name="appHost">Instance of <see cref="IServerApplicationHost"/> interface.</param>
        /// <param name="fileSystem">Instance of <see cref="IFileSystem"/> interface.</param>
        /// <param name="network">Instance of <see cref="INetworkManager"/> interface.</param>
        /// <param name="logger">Instance of <see cref="ILogger{SystemController}"/> interface.</param>
        public SystemController(
            IServerConfigurationManager serverConfigurationManager,
            IServerApplicationHost appHost,
            IFileSystem fileSystem,
            INetworkManager network,
            ILogger<SystemController> logger)
        {
            _appPaths = serverConfigurationManager.ApplicationPaths;
            _appHost = appHost;
            _fileSystem = fileSystem;
            _network = network;
            _logger = logger;
        }

        /// <summary>
        /// Gets information about the server.
        /// </summary>
        /// <response code="200">Information retrieved.</response>
        /// <returns>A <see cref="SystemInfo"/> with info about the system.</returns>
        [HttpGet("Info")]
        [Authorize(Policy = Policies.FirstTimeSetupOrIgnoreParentalControl)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<SystemInfo> GetSystemInfo()
        {
            return _appHost.GetSystemInfo(Request.HttpContext.Connection.RemoteIpAddress ?? IPAddress.Loopback);
        }

        /// <summary>
        /// Gets public information about the server.
        /// </summary>
        /// <response code="200">Information retrieved.</response>
        /// <returns>A <see cref="PublicSystemInfo"/> with public info about the system.</returns>
        [HttpGet("Info/Public")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<PublicSystemInfo> GetPublicSystemInfo()
        {
            return _appHost.GetPublicSystemInfo(Request.HttpContext.Connection.RemoteIpAddress ?? IPAddress.Loopback);
        }

        /// <summary>
        /// Pings the system.
        /// </summary>
        /// <response code="200">Information retrieved.</response>
        /// <returns>The server name.</returns>
        [HttpGet("Ping", Name = "GetPingSystem")]
        [HttpPost("Ping", Name = "PostPingSystem")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<string> PingSystem()
        {
            return _appHost.Name;
        }

        /// <summary>
        /// Restarts the application.
        /// </summary>
        /// <response code="204">Server restarted.</response>
        /// <returns>No content. Server restarted.</returns>
        [HttpPost("Restart")]
        [Authorize(Policy = Policies.LocalAccessOrRequiresElevation)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult RestartApplication()
        {
            Task.Run(async () =>
            {
                await Task.Delay(100).ConfigureAwait(false);
                _appHost.Restart();
            });
            return NoContent();
        }

        /// <summary>
        /// Shuts down the application.
        /// </summary>
        /// <response code="204">Server shut down.</response>
        /// <returns>No content. Server shut down.</returns>
        [HttpPost("Shutdown")]
        [Authorize(Policy = Policies.RequiresElevation)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult ShutdownApplication()
        {
            Task.Run(async () =>
            {
                await Task.Delay(100).ConfigureAwait(false);
                await _appHost.Shutdown().ConfigureAwait(false);
            });
            return NoContent();
        }

        /// <summary>
        /// Gets a list of available server log files.
        /// </summary>
        /// <response code="200">Information retrieved.</response>
        /// <returns>An array of <see cref="LogFile"/> with the available log files.</returns>
        [HttpGet("Logs")]
        [Authorize(Policy = Policies.RequiresElevation)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<LogFile[]> GetServerLogs()
        {
            IEnumerable<FileSystemMetadata> files;

            try
            {
                files = _fileSystem.GetFiles(_appPaths.LogDirectoryPath, new[] { ".txt", ".log" }, true, false);
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Error getting logs");
                files = Enumerable.Empty<FileSystemMetadata>();
            }

            var result = files.Select(i => new LogFile
                {
                    DateCreated = _fileSystem.GetCreationTimeUtc(i),
                    DateModified = _fileSystem.GetLastWriteTimeUtc(i),
                    Name = i.Name,
                    Size = i.Length
                })
                .OrderByDescending(i => i.DateModified)
                .ThenByDescending(i => i.DateCreated)
                .ThenBy(i => i.Name)
                .ToArray();

            return result;
        }

        /// <summary>
        /// Gets information about the request endpoint.
        /// </summary>
        /// <response code="200">Information retrieved.</response>
        /// <returns><see cref="EndPointInfo"/> with information about the endpoint.</returns>
        [HttpGet("Endpoint")]
        [Authorize(Policy = Policies.DefaultAuthorization)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<EndPointInfo> GetEndpointInfo()
        {
            return new EndPointInfo
            {
                IsLocal = HttpContext.IsLocal(),
                IsInNetwork = _network.IsInLocalNetwork(HttpContext.GetNormalizedRemoteIp())
            };
        }

        /// <summary>
        /// Gets a log file.
        /// </summary>
        /// <param name="name">The name of the log file to get.</param>
        /// <response code="200">Log file retrieved.</response>
        /// <returns>The log file.</returns>
        [HttpGet("Logs/Log")]
        [Authorize(Policy = Policies.RequiresElevation)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesFile(MediaTypeNames.Text.Plain)]
        public ActionResult GetLogFile([FromQuery, Required] string name)
        {
            var file = _fileSystem.GetFiles(_appPaths.LogDirectoryPath)
                .First(i => string.Equals(i.Name, name, StringComparison.OrdinalIgnoreCase));

            // For older files, assume fully static
            var fileShare = file.LastWriteTimeUtc < DateTime.UtcNow.AddHours(-1) ? FileShare.Read : FileShare.ReadWrite;
            FileStream stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, fileShare);
            return File(stream, "text/plain");
        }

        /// <summary>
        /// Gets wake on lan information.
        /// </summary>
        /// <response code="200">Information retrieved.</response>
        /// <returns>An <see cref="IEnumerable{WakeOnLanInfo}"/> with the WakeOnLan infos.</returns>
        [HttpGet("WakeOnLanInfo")]
        [Authorize(Policy = Policies.DefaultAuthorization)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<WakeOnLanInfo>> GetWakeOnLanInfo()
        {
            var result = _appHost.GetWakeOnLanInfo();
            return Ok(result);
        }
    }
}
