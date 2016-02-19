using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Extensions;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Server.Implementations.LiveTv.TunerHosts.SatIp
{
    public class SatIpDiscovery : IServerEntryPoint
    {
        private readonly IDeviceDiscovery _deviceDiscovery;
        private readonly IServerConfigurationManager _config;
        private readonly ILogger _logger;
        private readonly ILiveTvManager _liveTvManager;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _json;

        public static SatIpDiscovery Current;

        private readonly List<TunerHostInfo> _discoveredHosts = new List<TunerHostInfo>();

        public List<TunerHostInfo> DiscoveredHosts
        {
            get { return _discoveredHosts.ToList(); }
        }

        public SatIpDiscovery(IDeviceDiscovery deviceDiscovery, IServerConfigurationManager config, ILogger logger, ILiveTvManager liveTvManager, IHttpClient httpClient, IJsonSerializer json)
        {
            _deviceDiscovery = deviceDiscovery;
            _config = config;
            _logger = logger;
            _liveTvManager = liveTvManager;
            _httpClient = httpClient;
            _json = json;
            Current = this;
        }

        public void Run()
        {
            _deviceDiscovery.DeviceDiscovered += _deviceDiscovery_DeviceDiscovered;
        }

        void _deviceDiscovery_DeviceDiscovered(object sender, SsdpMessageEventArgs e)
        {
            string st = null;
            string nt = null;
            e.Headers.TryGetValue("ST", out st);
            e.Headers.TryGetValue("NT", out nt);

            if (string.Equals(st, "urn:ses-com:device:SatIPServer:1", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(nt, "urn:ses-com:device:SatIPServer:1", StringComparison.OrdinalIgnoreCase))
            {
                string location;
                if (e.Headers.TryGetValue("Location", out location) && !string.IsNullOrWhiteSpace(location))
                {
                    _logger.Debug("SAT IP found at {0}", location);

                    // Just get the beginning of the url
                    AddDevice(location);
                }
            }
        }

        private async void AddDevice(string location)
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                if (_discoveredHosts.Any(i => string.Equals(i.Type, SatIpHost.DeviceType, StringComparison.OrdinalIgnoreCase) && string.Equals(location, i.Url, StringComparison.OrdinalIgnoreCase)))
                {
                    return;
                }

                _logger.Debug("Will attempt to add SAT device {0}", location);
                var info = await GetInfo(location, CancellationToken.None).ConfigureAwait(false);

                _discoveredHosts.Add(info);
            }
            catch (OperationCanceledException)
            {

            }
            catch (NotImplementedException)
            {
                
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

        public void Dispose()
        {
        }

        public async Task<SatIpTunerHostInfo> GetInfo(string url, CancellationToken cancellationToken)
        {
            var result = new SatIpTunerHostInfo
            {
                Url = url,
                IsEnabled = true,
                Type = SatIpHost.DeviceType,
                Tuners = 1,
                TunersAvailable = 1
            };

            using (var stream = await _httpClient.Get(url, cancellationToken).ConfigureAwait(false))
            {
                using (var streamReader = new StreamReader(stream))
                {
                    // Use XmlReader for best performance
                    using (var reader = XmlReader.Create(streamReader))
                    {
                        reader.MoveToContent();

                        // Loop through each element
                        while (reader.Read())
                        {
                            if (reader.NodeType == XmlNodeType.Element)
                            {
                                switch (reader.Name)
                                {
                                    case "device":
                                        using (var subtree = reader.ReadSubtree())
                                        {
                                            FillFromDeviceNode(result, subtree);
                                        }
                                        break;
                                    default:
                                        reader.Skip();
                                        break;
                                }
                            }
                        }
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(result.Id))
            {
                throw new NotImplementedException();
            }

            if (string.IsNullOrWhiteSpace(result.M3UUrl))
            {
                throw new NotImplementedException();
            }

            if (!result.M3UUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                var fullM3uUrl = url.Substring(0, url.LastIndexOf('/'));
                result.M3UUrl = fullM3uUrl + "/" + result.M3UUrl.TrimStart('/');
            }

            _logger.Debug("SAT device result: {0}", _json.SerializeToString(result));

            return result;
        }

        private void FillFromDeviceNode(SatIpTunerHostInfo info, XmlReader reader)
        {
            reader.MoveToContent();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "UDN":
                            {
                                info.Id = reader.ReadElementContentAsString();
                                break;
                            }

                        case "X_SATIPCAP":
                            {
                                var value = reader.ReadElementContentAsString();
                                // TODO
                                break;
                            }

                        case "X_SATIPM3U":
                            {
                                info.M3UUrl = reader.ReadElementContentAsString();
                                break;
                            }

                        default:
                            reader.Skip();
                            break;
                    }
                }
            }
        }
    }

    public class SatIpTunerHostInfo : TunerHostInfo
    {
        public int Tuners { get; set; }
        public int TunersAvailable { get; set; }
        public string M3UUrl { get; set; }
    }
}
