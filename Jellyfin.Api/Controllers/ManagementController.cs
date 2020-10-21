using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
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
    /// The management controller.
    /// </summary>
    [Management]
    public class ManagementController : BaseJellyfinApiController
    {
        private readonly IServerApplicationHost _appHost;
        private readonly IApplicationPaths _appPaths;
        private readonly IFileSystem _fileSystem;
        private readonly INetworkManager _network;
        private readonly ILogger<ManagementController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ManagementController"/> class.
        /// </summary>
        /// <param name="serverConfigurationManager">Instance of <see cref="IServerConfigurationManager"/> interface.</param>
        /// <param name="appHost">Instance of <see cref="IServerApplicationHost"/> interface.</param>
        /// <param name="fileSystem">Instance of <see cref="IFileSystem"/> interface.</param>
        /// <param name="network">Instance of <see cref="INetworkManager"/> interface.</param>
        /// <param name="logger">Instance of <see cref="ILogger{SystemController}"/> interface.</param>
        public ManagementController(
            IServerConfigurationManager serverConfigurationManager,
            IServerApplicationHost appHost,
            IFileSystem fileSystem,
            INetworkManager network,
            ILogger<ManagementController> logger)
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
        [HttpGet("Test")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<int> GetTest()
        {
            return 123456; // secret
        }
    }
}
