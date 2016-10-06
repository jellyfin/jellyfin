using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Extensions;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Logging;
using System;
using System.Linq;
using System.Threading;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Server.Implementations.LiveTv.TunerHosts.HdHomerun
{
    public class HdHomerunDiscovery : IServerEntryPoint
    {
        private readonly IDeviceDiscovery _deviceDiscovery;
        private readonly IServerConfigurationManager _config;
        private readonly ILogger _logger;
        private readonly ILiveTvManager _liveTvManager;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _json;

        public HdHomerunDiscovery(IDeviceDiscovery deviceDiscovery, IServerConfigurationManager config, ILogger logger, ILiveTvManager liveTvManager, IHttpClient httpClient, IJsonSerializer json)
        {
            _deviceDiscovery = deviceDiscovery;
            _config = config;
            _logger = logger;
            _liveTvManager = liveTvManager;
            _httpClient = httpClient;
            _json = json;
        }

        public void Run()
        {
            _deviceDiscovery.DeviceDiscovered += _deviceDiscovery_DeviceDiscovered;
        }

        void _deviceDiscovery_DeviceDiscovered(object sender, GenericEventArgs<UpnpDeviceInfo> e)
        {
            string server = null;
            var info = e.Argument;

            if (info.Headers.TryGetValue("SERVER", out server) && server.IndexOf("HDHomeRun", StringComparison.OrdinalIgnoreCase) != -1)
            {
                string location;
                if (info.Headers.TryGetValue("Location", out location))
                {
                    //_logger.Debug("HdHomerun found at {0}", location);

                    // Just get the beginning of the url
                    Uri uri;
                    if (Uri.TryCreate(location, UriKind.Absolute, out uri))
                    {
                        var apiUrl = location.Replace(uri.LocalPath, String.Empty, StringComparison.OrdinalIgnoreCase)
                                .TrimEnd('/');

                        //_logger.Debug("HdHomerun api url: {0}", apiUrl);
                        AddDevice(apiUrl);
                    }
                }
            }
        }

        private async void AddDevice(string url)
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                var options = GetConfiguration();

                if (options.TunerHosts.Any(i =>
                            string.Equals(i.Type, HdHomerunHost.DeviceType, StringComparison.OrdinalIgnoreCase) &&
                            UriEquals(i.Url, url)))
                {
                    return;
                }

                // Strip off the port
                url = new Uri(url).GetComponents(UriComponents.AbsoluteUri & ~UriComponents.Port, UriFormat.UriEscaped).TrimEnd('/');

                // Test it by pulling down the lineup
                using (var stream = await _httpClient.Get(new HttpRequestOptions
                {
                    Url = string.Format("{0}/discover.json", url),
                    CancellationToken = CancellationToken.None,
                    BufferContent = false
                }))
                {
                    var response = _json.DeserializeFromStream<HdHomerunHost.DiscoverResponse>(stream);

                    var existing = GetConfiguration().TunerHosts
                        .FirstOrDefault(i => string.Equals(i.Type, HdHomerunHost.DeviceType, StringComparison.OrdinalIgnoreCase) && string.Equals(i.DeviceId, response.DeviceID, StringComparison.OrdinalIgnoreCase));

                    if (existing == null)
                    {
                        await _liveTvManager.SaveTunerHost(new TunerHostInfo
                        {
                            Type = HdHomerunHost.DeviceType,
                            Url = url,
                            DataVersion = 1,
                            DeviceId = response.DeviceID

                        }).ConfigureAwait(false);
                    }
                    else
                    {
                        if (!string.Equals(existing.Url, url, StringComparison.OrdinalIgnoreCase))
                        {
                            existing.Url = url;
                            await _liveTvManager.SaveTunerHost(existing).ConfigureAwait(false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error saving device", ex);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private bool UriEquals(string savedUri, string location)
        {
            return string.Equals(NormalizeUrl(location), NormalizeUrl(savedUri), StringComparison.OrdinalIgnoreCase);
        }

        private string NormalizeUrl(string url)
        {
            if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                url = "http://" + url;
            }

            url = url.TrimEnd('/');

            // Strip off the port
            return new Uri(url).GetComponents(UriComponents.AbsoluteUri & ~UriComponents.Port, UriFormat.UriEscaped);
        }

        private LiveTvOptions GetConfiguration()
        {
            return _config.GetConfiguration<LiveTvOptions>("livetv");
        }

        public void Dispose()
        {
        }
    }
}
