using System;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Diagnostics;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Threading;
using System.Collections.Generic;
using System.Threading;

namespace MediaBrowser.Api
{
    /// <summary>
    /// Class ServerEntryPoint
    /// </summary>
    public class ApiEntryPoint : IServerEntryPoint
    {
        /// <summary>
        /// The instance
        /// </summary>
        public static ApiEntryPoint Instance;

        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        /// <value>The logger.</value>
        internal ILogger Logger { get; private set; }
        internal IHttpResultFactory ResultFactory { get; private set; }

        /// <summary>
        /// The application paths
        /// </summary>
        private readonly IServerConfigurationManager _config;

        private readonly ISessionManager _sessionManager;
        private readonly IFileSystem _fileSystem;
        private readonly IMediaSourceManager _mediaSourceManager;
        public readonly ITimerFactory TimerFactory;
        public readonly IProcessFactory ProcessFactory;

        private readonly Dictionary<string, SemaphoreSlim> _transcodingLocks =
            new Dictionary<string, SemaphoreSlim>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiEntryPoint" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="sessionManager">The session manager.</param>
        /// <param name="config">The configuration.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="mediaSourceManager">The media source manager.</param>
        public ApiEntryPoint(ILogger logger, ISessionManager sessionManager, IServerConfigurationManager config, IFileSystem fileSystem, IMediaSourceManager mediaSourceManager, ITimerFactory timerFactory, IProcessFactory processFactory, IHttpResultFactory resultFactory)
        {
            Logger = logger;
            _sessionManager = sessionManager;
            _config = config;
            _fileSystem = fileSystem;
            _mediaSourceManager = mediaSourceManager;
            TimerFactory = timerFactory;
            ProcessFactory = processFactory;
            ResultFactory = resultFactory;

            Instance = this;
        }

        public static string[] Split(string value, char separator, bool removeEmpty)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return new string[] { };
            }

            if (removeEmpty)
            {
                return value.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries);
            }

            return value.Split(separator);
        }

        public void Run()
        {
            
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
