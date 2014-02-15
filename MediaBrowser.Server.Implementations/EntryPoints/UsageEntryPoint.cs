using MediaBrowser.Common;
using MediaBrowser.Common.Implementations.Security;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Logging;
using System;
using System.Threading;

namespace MediaBrowser.Server.Implementations.EntryPoints
{
    /// <summary>
    /// Class UsageEntryPoint
    /// </summary>
    public class UsageEntryPoint : IServerEntryPoint
    {
        private readonly IApplicationHost _applicationHost;
        private readonly INetworkManager _networkManager;
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;

        private Timer _timer;
        private readonly TimeSpan _frequency = TimeSpan.FromHours(24);

        public UsageEntryPoint(ILogger logger, IApplicationHost applicationHost, INetworkManager networkManager, IHttpClient httpClient)
        {
            _logger = logger;
            _applicationHost = applicationHost;
            _networkManager = networkManager;
            _httpClient = httpClient;
        }

        public void Run()
        {
            _timer = new Timer(OnTimerFired, null, TimeSpan.FromMilliseconds(5000), _frequency);
        }

        /// <summary>
        /// Called when [timer fired].
        /// </summary>
        /// <param name="state">The state.</param>
        private async void OnTimerFired(object state)
        {
            try
            {
                await new UsageReporter(_applicationHost, _networkManager, _httpClient).ReportUsage(CancellationToken.None)
                        .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error sending anonymous usage statistics.", ex);
            }
        }

        public void Dispose()
        {
            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }
        }
    }
}
