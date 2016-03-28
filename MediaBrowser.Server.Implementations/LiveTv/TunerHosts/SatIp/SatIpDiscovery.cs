using System;
using System.Collections.Generic;
using System.Globalization;
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
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Extensions;

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
                    Uri uri;
                    if (Uri.TryCreate(location, UriKind.Absolute, out uri))
                    {
                        var apiUrl = location.Replace(uri.LocalPath, String.Empty, StringComparison.OrdinalIgnoreCase)
                                .TrimEnd('/');

                        AddDevice(apiUrl, location);
                    }
                }
            }
        }

        private async void AddDevice(string deviceUrl, string infoUrl)
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                var options = GetConfiguration();

                if (options.TunerHosts.Any(i => string.Equals(i.Type, SatIpHost.DeviceType, StringComparison.OrdinalIgnoreCase) && UriEquals(i.Url, deviceUrl)))
                {
                    return;
                }

                _logger.Debug("Will attempt to add SAT device {0}", deviceUrl);
                var info = await GetInfo(infoUrl, CancellationToken.None).ConfigureAwait(false);

                var existing = GetConfiguration().TunerHosts
                    .FirstOrDefault(i => string.Equals(i.Type, SatIpHost.DeviceType, StringComparison.OrdinalIgnoreCase) && string.Equals(i.DeviceId, info.DeviceId, StringComparison.OrdinalIgnoreCase));

                if (existing == null)
                {
                    //if (string.IsNullOrWhiteSpace(info.M3UUrl))
                    //{
                    //    return;
                    //}

                    await _liveTvManager.SaveTunerHost(new TunerHostInfo
                    {
                        Type = SatIpHost.DeviceType,
                        Url = deviceUrl,
                        InfoUrl = infoUrl,
                        DataVersion = 1,
                        DeviceId = info.DeviceId,
                        FriendlyName = info.FriendlyName,
                        Tuners = info.Tuners,
                        M3UUrl = info.M3UUrl,
                        IsEnabled = true

                    }).ConfigureAwait(false);
                }
                else
                {
                    existing.Url = deviceUrl;
                    existing.InfoUrl = infoUrl;
                    existing.M3UUrl = info.M3UUrl;
                    existing.FriendlyName = info.FriendlyName;
                    existing.Tuners = info.Tuners;
                    await _liveTvManager.SaveTunerHost(existing).ConfigureAwait(false);
                }
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

            if (string.IsNullOrWhiteSpace(result.DeviceId))
            {
                throw new NotImplementedException();
            }

            // Device hasn't implemented an m3u list
            if (string.IsNullOrWhiteSpace(result.M3UUrl))
            {
                result.IsEnabled = false;
            }

            else if (!result.M3UUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
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
                    switch (reader.LocalName)
                    {
                        case "UDN":
                            {
                                info.DeviceId = reader.ReadElementContentAsString();
                                break;
                            }

                        case "friendlyName":
                            {
                                info.FriendlyName = reader.ReadElementContentAsString();
                                break;
                            }

                        case "satip:X_SATIPCAP":
                        case "X_SATIPCAP":
                            {
                                // <satip:X_SATIPCAP xmlns:satip="urn:ses-com:satip">DVBS2-2</satip:X_SATIPCAP>
                                var value = reader.ReadElementContentAsString() ?? string.Empty;
                                var parts = value.Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                                if (parts.Length == 2)
                                {
                                    int intValue;
                                    if (int.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out intValue))
                                    {
                                        info.TunersAvailable = intValue;
                                    }

                                    if (int.TryParse(parts[0].Substring(parts[0].Length - 1), NumberStyles.Any, CultureInfo.InvariantCulture, out intValue))
                                    {
                                        info.Tuners = intValue;
                                    }
                                }
                                break;
                            }

                        case "satip:X_SATIPM3U":
                        case "X_SATIPM3U":
                            {
                                // <satip:X_SATIPM3U xmlns:satip="urn:ses-com:satip">/channellist.lua?select=m3u</satip:X_SATIPM3U>
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
        public int TunersAvailable { get; set; }
    }
}
