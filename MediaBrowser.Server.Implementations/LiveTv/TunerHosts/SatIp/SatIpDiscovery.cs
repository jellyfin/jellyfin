using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Extensions;
using System.Xml.Linq;
using MediaBrowser.Model.Events;

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
        private int _tunerCountDVBS=0;
        private int _tunerCountDVBC=0;
        private int _tunerCountDVBT=0;
        private bool  _supportsDVBS=false;
        private bool  _supportsDVBC=false;
        private bool  _supportsDVBT=false;
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

        void _deviceDiscovery_DeviceDiscovered(object sender, GenericEventArgs<UpnpDeviceInfo> e)
        {
            var info = e.Argument;

            string st = null;
            string nt = null;
            info.Headers.TryGetValue("ST", out st);
            info.Headers.TryGetValue("NT", out nt);

            if (string.Equals(st, "urn:ses-com:device:SatIPServer:1", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(nt, "urn:ses-com:device:SatIPServer:1", StringComparison.OrdinalIgnoreCase))
            {
                string location;
                if (info.Headers.TryGetValue("Location", out location) && !string.IsNullOrWhiteSpace(location))
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

                    }, true).ConfigureAwait(false);
                }
                else
                {
                    existing.Url = deviceUrl;
                    existing.InfoUrl = infoUrl;
                    existing.M3UUrl = info.M3UUrl;
                    existing.FriendlyName = info.FriendlyName;
                    existing.Tuners = info.Tuners;
                    await _liveTvManager.SaveTunerHost(existing, false).ConfigureAwait(false);
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
 private void ReadCapability(string capability)
        {

            string[] cap = capability.Split('-');
            switch (cap[0].ToLower())
            {
                case "dvbs":
                case "dvbs2":
                    {
                        // Optional that you know what an device Supports can you add an flag 
                        _supportsDVBS = true;

                        for (int i = 0; i < int.Parse(cap[1]); i++)
                        {
                            //ToDo Create Digital Recorder / Tuner Capture Instance here for each with index FE param in Sat>Ip Spec for direct communication with this instance 
                        }
                        _tunerCountDVBS = int.Parse(cap[1]);
                        break;
                    }
                case "dvbc":
                case "dvbc2":
                    {
                        // Optional that you know what an device Supports can you add an flag 
                        _supportsDVBC = true;

                        for (int i = 0; i < int.Parse(cap[1]); i++)
                        {
                            //ToDo Create Digital Recorder / Tuner Capture Instance here for each with index FE param in Sat>Ip Spec for direct communication with this instance
                            
                        }
                        _tunerCountDVBC = int.Parse(cap[1]);
                        break;
                    }
                case "dvbt":
                case "dvbt2":
                    {
                        // Optional that you know what an device Supports can you add an flag 
                        _supportsDVBT = true;


                        for (int i = 0; i < int.Parse(cap[1]); i++)
                        {
                            //ToDo Create Digital Recorder / Tuner Capture Instance here for each with index FE param in Sat>Ip Spec for direct communication with this instance  

                        }
                        _tunerCountDVBT = int.Parse(cap[1]);
                        break;
                    }
            }

        }
        public async Task<SatIpTunerHostInfo> GetInfo(string url, CancellationToken cancellationToken)
        {
            Uri locationUri = new Uri(url);
            string devicetype = "";
            string friendlyname = "";
            string uniquedevicename = "";
            string manufacturer = "";
            string manufacturerurl = "";
            string modelname = "";
            string modeldescription = "";
            string modelnumber = "";
            string modelurl = "";
            string serialnumber = "";
            string presentationurl = "";
            //string capabilities = "";
            string m3u = "";
            var document = XDocument.Load(locationUri.AbsoluteUri);
            var xnm = new XmlNamespaceManager(new NameTable());
            XNamespace n1 = "urn:ses-com:satip";
            XNamespace n0 = "urn:schemas-upnp-org:device-1-0";
            xnm.AddNamespace("root", n0.NamespaceName);
            xnm.AddNamespace("satip:", n1.NamespaceName);
            if (document.Root != null)
            {
                var deviceElement = document.Root.Element(n0 + "device");
                if (deviceElement != null)
                {
                    var devicetypeElement = deviceElement.Element(n0 + "deviceType");
                    if (devicetypeElement != null)
                        devicetype = devicetypeElement.Value;
                    var friendlynameElement = deviceElement.Element(n0 + "friendlyName");
                    if (friendlynameElement != null)
                        friendlyname = friendlynameElement.Value;
                    var manufactureElement = deviceElement.Element(n0 + "manufacturer");
                    if (manufactureElement != null)
                        manufacturer = manufactureElement.Value;
                    var manufactureurlElement = deviceElement.Element(n0 + "manufacturerURL");
                    if (manufactureurlElement != null)
                        manufacturerurl = manufactureurlElement.Value;
                    var modeldescriptionElement = deviceElement.Element(n0 + "modelDescription");
                    if (modeldescriptionElement != null)
                        modeldescription = modeldescriptionElement.Value;
                    var modelnameElement = deviceElement.Element(n0 + "modelName");
                    if (modelnameElement != null)
                        modelname = modelnameElement.Value;
                    var modelnumberElement = deviceElement.Element(n0 + "modelNumber");
                    if (modelnumberElement != null)
                        modelnumber = modelnumberElement.Value;
                    var modelurlElement = deviceElement.Element(n0 + "modelURL");
                    if (modelurlElement != null)
                        modelurl = modelurlElement.Value;
                    var serialnumberElement = deviceElement.Element(n0 + "serialNumber");
                    if (serialnumberElement != null)
                        serialnumber = serialnumberElement.Value;
                    var uniquedevicenameElement = deviceElement.Element(n0 + "UDN");
                    if (uniquedevicenameElement != null) uniquedevicename = uniquedevicenameElement.Value;
                    var presentationUrlElement = deviceElement.Element(n0 + "presentationURL");
                    if (presentationUrlElement != null) presentationurl = presentationUrlElement.Value;
                    var capabilitiesElement = deviceElement.Element(n1 + "X_SATIPCAP");
                        if (capabilitiesElement != null)
                        {
                        //_capabilities = capabilitiesElement.Value;
                        if (capabilitiesElement.Value.Contains(','))
                        {
                            string[] capabilities = capabilitiesElement.Value.Split(',');
                            foreach (var capability in capabilities)
                            {
                                ReadCapability(capability);
                            }
                        }
                        else
                        {
                            ReadCapability(capabilitiesElement.Value);
                        }
                    }
                        else
                        {
                            _supportsDVBS = true;
                            _tunerCountDVBS =1;
                        }
                    var m3uElement = deviceElement.Element(n1 + "X_SATIPM3U");
                    if (m3uElement != null) m3u = m3uElement.Value;
                }
            }

            var result = new SatIpTunerHostInfo
            {
                Url = url,
                Id = uniquedevicename,
                IsEnabled = true,
                Type = SatIpHost.DeviceType,
                Tuners = _tunerCountDVBS,
                TunersAvailable = _tunerCountDVBS,
                M3UUrl = m3u
            };

            result.FriendlyName = friendlyname;
            if (string.IsNullOrWhiteSpace(result.Id))
            {
                throw new NotImplementedException();
            }

            else if (!result.M3UUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                var fullM3uUrl = url.Substring(0, url.LastIndexOf('/'));
                result.M3UUrl = fullM3uUrl + "/" + result.M3UUrl.TrimStart('/');
            }

            _logger.Debug("SAT device result: {0}", _json.SerializeToString(result));

            return result;
        }
    }

    public class SatIpTunerHostInfo : TunerHostInfo
    {
        public int TunersAvailable { get; set; }
    }
}
