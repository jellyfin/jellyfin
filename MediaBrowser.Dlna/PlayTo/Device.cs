using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Dlna.Common;
using MediaBrowser.Dlna.Ssdp;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MediaBrowser.Dlna.PlayTo
{
    public class Device : IDisposable
    {
        const string ServiceAvtransportType = "urn:schemas-upnp-org:service:AVTransport:1";
        const string ServiceRenderingType = "urn:schemas-upnp-org:service:RenderingControl:1";

        #region Fields & Properties

        private Timer _timer;

        public DeviceInfo Properties { get; set; }

        private int _muteVol;
        public bool IsMuted { get; set; }

        private int _volume;

        public int Volume
        {
            get
            {
                RefreshVolumeIfNeeded();
                return _volume;
            }
            set
            {
                _volume = value;
            }
        }

        public TimeSpan? Duration { get; set; }

        private TimeSpan _position = TimeSpan.FromSeconds(0);
        public TimeSpan Position
        {
            get
            {
                return _position;
            }
            set
            {
                _position = value;
            }
        }

        public TRANSPORTSTATE TransportState { get; private set; }

        public bool IsPlaying
        {
            get
            {
                return TransportState == TRANSPORTSTATE.PLAYING;
            }
        }

        public bool IsPaused
        {
            get
            {
                return TransportState == TRANSPORTSTATE.PAUSED || TransportState == TRANSPORTSTATE.PAUSED_PLAYBACK;
            }
        }

        public bool IsStopped
        {
            get
            {
                return TransportState == TRANSPORTSTATE.STOPPED;
            }
        }

        #endregion

        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly IServerConfigurationManager _config;

        public DateTime DateLastActivity { get; private set; }

        public Device(DeviceInfo deviceProperties, IHttpClient httpClient, ILogger logger, IServerConfigurationManager config)
        {
            Properties = deviceProperties;
            _httpClient = httpClient;
            _logger = logger;
            _config = config;
        }

        private int GetPlaybackTimerIntervalMs()
        {
            return 1000;
        }

        private int GetInactiveTimerIntervalMs()
        {
            return 20000;
        }

        public void Start()
        {
            _timer = new Timer(TimerCallback, null, GetPlaybackTimerIntervalMs(), GetInactiveTimerIntervalMs());

            _timerActive = false;
        }

        private DateTime _lastVolumeRefresh;
        private void RefreshVolumeIfNeeded()
        {
            if (!_timerActive)
            {
                return;
            }

            if (DateTime.UtcNow >= _lastVolumeRefresh.AddSeconds(5))
            {
                _lastVolumeRefresh = DateTime.UtcNow;
                RefreshVolume();
            }
        }

        private async void RefreshVolume()
        {
            try
            {
                await GetVolume().ConfigureAwait(false);
                await GetMute().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error updating device volume info for {0}", ex, Properties.Name);
            }
        }

        private readonly object _timerLock = new object();
        private bool _timerActive;
        private void RestartTimer()
        {
            if (!_timerActive)
            {
                lock (_timerLock)
                {
                    if (!_timerActive)
                    {
                        _logger.Debug("RestartTimer");
                        _timer.Change(10, GetPlaybackTimerIntervalMs());
                    }

                    _timerActive = true;
                }
            }
        }

        /// <summary>
        /// Restarts the timer in inactive mode.
        /// </summary>
        private void RestartTimerInactive()
        {
            if (_timerActive)
            {
                lock (_timerLock)
                {
                    if (_timerActive)
                    {
                        _logger.Debug("RestartTimerInactive");
                        var interval = GetInactiveTimerIntervalMs();

                        if (_timer != null)
                        {
                            _timer.Change(interval, interval);
                        }
                    }

                    _timerActive = false;
                }
            }
        }

        #region Commanding

        public Task VolumeDown()
        {
            var sendVolume = Math.Max(Volume - 5, 0);

            return SetVolume(sendVolume);
        }

        public Task VolumeUp()
        {
            var sendVolume = Math.Min(Volume + 5, 100);

            return SetVolume(sendVolume);
        }

        public Task ToggleMute()
        {
            if (IsMuted)
            {
                return Unmute();
            }

            return Mute();
        }

        public async Task Mute()
        {
            var success = await SetMute(true).ConfigureAwait(true);

            if (!success)
            {
                await SetVolume(0).ConfigureAwait(false);
            }
        }

        public async Task Unmute()
        {
            var success = await SetMute(false).ConfigureAwait(true);

            if (!success)
            {
                var sendVolume = _muteVol <= 0 ? 20 : _muteVol;

                await SetVolume(sendVolume).ConfigureAwait(false);
            }
        }

        private async Task<bool> SetMute(bool mute)
        {
            var command = RendererCommands.ServiceActions.FirstOrDefault(c => c.Name == "SetMute");
            if (command == null)
                return false;

            var service = Properties.Services.FirstOrDefault(s => s.ServiceType == ServiceRenderingType);

            if (service == null)
            {
                return false;
            }

            _logger.Debug("Setting mute");
            var value = mute ? 1 : 0;

            await new SsdpHttpClient(_httpClient, _config).SendCommandAsync(Properties.BaseUrl, service, command.Name, RendererCommands.BuildPost(command, service.ServiceType, value))
                .ConfigureAwait(false);

            IsMuted = mute;

            return true;
        }

        /// <summary>
        /// Sets volume on a scale of 0-100
        /// </summary>
        public async Task SetVolume(int value)
        {
            var command = RendererCommands.ServiceActions.FirstOrDefault(c => c.Name == "SetVolume");
            if (command == null)
                return;

            var service = Properties.Services.FirstOrDefault(s => s.ServiceType == ServiceRenderingType);

            if (service == null)
            {
                throw new InvalidOperationException("Unable to find service");
            }

            // Set it early and assume it will succeed
            // Remote control will perform better
            Volume = value;

            await new SsdpHttpClient(_httpClient, _config).SendCommandAsync(Properties.BaseUrl, service, command.Name, RendererCommands.BuildPost(command, service.ServiceType, value))
                .ConfigureAwait(false);
        }

        public async Task Seek(TimeSpan value)
        {
            var command = AvCommands.ServiceActions.FirstOrDefault(c => c.Name == "Seek");
            if (command == null)
                return;

            var service = Properties.Services.FirstOrDefault(s => s.ServiceType == ServiceAvtransportType);

            if (service == null)
            {
                throw new InvalidOperationException("Unable to find service");
            }

            await new SsdpHttpClient(_httpClient, _config).SendCommandAsync(Properties.BaseUrl, service, command.Name, AvCommands.BuildPost(command, service.ServiceType, String.Format("{0:hh}:{0:mm}:{0:ss}", value), "REL_TIME"))
                .ConfigureAwait(false);
        }

        public async Task SetAvTransport(string url, string header, string metaData)
        {
            _logger.Debug("{0} - SetAvTransport Uri: {1} DlnaHeaders: {2}", Properties.Name, url, header);

            var command = AvCommands.ServiceActions.FirstOrDefault(c => c.Name == "SetAVTransportURI");
            if (command == null)
                return;

            var dictionary = new Dictionary<string, string>
            {
                {"CurrentURI", url},
                {"CurrentURIMetaData", CreateDidlMeta(metaData)}
            };

            var service = Properties.Services.FirstOrDefault(s => s.ServiceType == ServiceAvtransportType);

            if (service == null)
            {
                throw new InvalidOperationException("Unable to find service");
            }

            var post = AvCommands.BuildPost(command, service.ServiceType, url, dictionary);
            await new SsdpHttpClient(_httpClient, _config).SendCommandAsync(Properties.BaseUrl, service, command.Name, post, header: header)
                .ConfigureAwait(false);

            await Task.Delay(50).ConfigureAwait(false);

            try
            {
                await SetPlay().ConfigureAwait(false);
            }
            catch
            {
                // Some devices will throw an error if you tell it to play when it's already playing
                // Others won't
            }

            RestartTimer();
        }

        private string CreateDidlMeta(string value)
        {
            if (string.IsNullOrEmpty(value))
                return String.Empty;

            return SecurityElement.Escape(value);
        }

        public async Task SetPlay()
        {
            var command = AvCommands.ServiceActions.FirstOrDefault(c => c.Name == "Play");
            if (command == null)
                return;

            var service = Properties.Services.FirstOrDefault(s => s.ServiceType == ServiceAvtransportType);

            if (service == null)
            {
                throw new InvalidOperationException("Unable to find service");
            }

            await new SsdpHttpClient(_httpClient, _config).SendCommandAsync(Properties.BaseUrl, service, command.Name, AvCommands.BuildPost(command, service.ServiceType, 1))
                .ConfigureAwait(false);
        }

        public async Task SetStop()
        {
            var command = AvCommands.ServiceActions.FirstOrDefault(c => c.Name == "Stop");
            if (command == null)
                return;

            var service = Properties.Services.First(s => s.ServiceType == ServiceAvtransportType);

            await new SsdpHttpClient(_httpClient, _config).SendCommandAsync(Properties.BaseUrl, service, command.Name, AvCommands.BuildPost(command, service.ServiceType, 1))
                .ConfigureAwait(false);
        }

        public async Task SetPause()
        {
            var command = AvCommands.ServiceActions.FirstOrDefault(c => c.Name == "Pause");
            if (command == null)
                return;

            var service = Properties.Services.First(s => s.ServiceType == ServiceAvtransportType);

            await new SsdpHttpClient(_httpClient, _config).SendCommandAsync(Properties.BaseUrl, service, command.Name, AvCommands.BuildPost(command, service.ServiceType, 1))
                .ConfigureAwait(false);

            TransportState = TRANSPORTSTATE.PAUSED;
        }

        #endregion

        #region Get data

        private int _successiveStopCount;
        private async void TimerCallback(object sender)
        {
            if (_disposed)
                return;

            const int maxSuccessiveStopReturns = 5;

            try
            {
                var transportState = await GetTransportInfo().ConfigureAwait(false);

                DateLastActivity = DateTime.UtcNow;

                if (transportState.HasValue)
                {
                    // If we're not playing anything no need to get additional data
                    if (transportState.Value == TRANSPORTSTATE.STOPPED)
                    {
                        UpdateMediaInfo(null, transportState.Value);
                    }
                    else
                    {
                        var tuple = await GetPositionInfo().ConfigureAwait(false);

                        var currentObject = tuple.Item2;

                        if (tuple.Item1 && currentObject == null)
                        {
                            currentObject = await GetMediaInfo().ConfigureAwait(false);
                        }

                        if (currentObject != null)
                        {
                            UpdateMediaInfo(currentObject, transportState.Value);
                        }
                    }

                    if (_disposed)
                        return;

                    // If we're not playing anything make sure we don't get data more often than neccessry to keep the Session alive
                    if (transportState.Value == TRANSPORTSTATE.STOPPED)
                    {
                        _successiveStopCount++;

                        if (_successiveStopCount >= maxSuccessiveStopReturns)
                        {
                            RestartTimerInactive();
                        }
                    }
                    else
                    {
                        _successiveStopCount = 0;
                        RestartTimer();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error updating device info for {0}", ex, Properties.Name);

                _successiveStopCount++;

                if (_successiveStopCount >= maxSuccessiveStopReturns)
                {
                    RestartTimerInactive();
                }
            }
        }

        private async Task GetVolume()
        {
            var command = RendererCommands.ServiceActions.FirstOrDefault(c => c.Name == "GetVolume");
            if (command == null)
                return;

            var service = Properties.Services.FirstOrDefault(s => s.ServiceType == ServiceRenderingType);

            if (service == null)
            {
                return;
            }

            var result = await new SsdpHttpClient(_httpClient, _config).SendCommandAsync(Properties.BaseUrl, service, command.Name, RendererCommands.BuildPost(command, service.ServiceType), true)
                .ConfigureAwait(false);

            if (result == null || result.Document == null)
                return;

            var volume = result.Document.Descendants(uPnpNamespaces.RenderingControl + "GetVolumeResponse").Select(i => i.Element("CurrentVolume")).FirstOrDefault(i => i != null);
            var volumeValue = volume == null ? null : volume.Value;

            if (string.IsNullOrWhiteSpace(volumeValue))
                return;

            Volume = int.Parse(volumeValue, UsCulture);

            if (Volume > 0)
            {
                _muteVol = Volume;
            }
        }

        private async Task GetMute()
        {
            var command = RendererCommands.ServiceActions.FirstOrDefault(c => c.Name == "GetMute");
            if (command == null)
                return;

            var service = Properties.Services.FirstOrDefault(s => s.ServiceType == ServiceRenderingType);

            if (service == null)
            {
                return;
            }

            var result = await new SsdpHttpClient(_httpClient, _config).SendCommandAsync(Properties.BaseUrl, service, command.Name, RendererCommands.BuildPost(command, service.ServiceType), true)
                .ConfigureAwait(false);

            if (result == null || result.Document == null)
                return;

            var valueNode = result.Document.Descendants(uPnpNamespaces.RenderingControl + "GetMuteResponse").Select(i => i.Element("CurrentMute")).FirstOrDefault(i => i != null);
            var value = valueNode == null ? null : valueNode.Value;

            IsMuted = string.Equals(value, "1", StringComparison.OrdinalIgnoreCase);
        }

        private async Task<TRANSPORTSTATE?> GetTransportInfo()
        {
            var command = AvCommands.ServiceActions.FirstOrDefault(c => c.Name == "GetTransportInfo");
            if (command == null)
                return null;

            var service = Properties.Services.FirstOrDefault(s => s.ServiceType == ServiceAvtransportType);
            if (service == null)
                return null;

            var result = await new SsdpHttpClient(_httpClient, _config).SendCommandAsync(Properties.BaseUrl, service, command.Name, AvCommands.BuildPost(command, service.ServiceType), false)
                .ConfigureAwait(false);

            if (result == null || result.Document == null)
                return null;

            var transportState =
                result.Document.Descendants(uPnpNamespaces.AvTransport + "GetTransportInfoResponse").Select(i => i.Element("CurrentTransportState")).FirstOrDefault(i => i != null);

            var transportStateValue = transportState == null ? null : transportState.Value;

            if (transportStateValue != null)
            {
                TRANSPORTSTATE state;

                if (Enum.TryParse(transportStateValue, true, out state))
                {
                    return state;
                }
            }

            return null;
        }

        private async Task<uBaseObject> GetMediaInfo()
        {
            var command = AvCommands.ServiceActions.FirstOrDefault(c => c.Name == "GetMediaInfo");
            if (command == null)
                return null;

            var service = Properties.Services.FirstOrDefault(s => s.ServiceType == ServiceAvtransportType);

            if (service == null)
            {
                throw new InvalidOperationException("Unable to find service");
            }

            var result = await new SsdpHttpClient(_httpClient, _config).SendCommandAsync(Properties.BaseUrl, service, command.Name, RendererCommands.BuildPost(command, service.ServiceType), false)
                .ConfigureAwait(false);

            if (result == null || result.Document == null)
                return null;

            var track = result.Document.Descendants("CurrentURIMetaData").FirstOrDefault();

            if (track == null)
            {
                return null;
            }

            var e = track.Element(uPnpNamespaces.items) ?? track;

            return UpnpContainer.Create(e);
        }

        private async Task<Tuple<bool, uBaseObject>> GetPositionInfo()
        {
            var command = AvCommands.ServiceActions.FirstOrDefault(c => c.Name == "GetPositionInfo");
            if (command == null)
                return new Tuple<bool, uBaseObject>(false, null);

            var service = Properties.Services.FirstOrDefault(s => s.ServiceType == ServiceAvtransportType);

            if (service == null)
            {
                throw new InvalidOperationException("Unable to find service");
            }

            var result = await new SsdpHttpClient(_httpClient, _config).SendCommandAsync(Properties.BaseUrl, service, command.Name, RendererCommands.BuildPost(command, service.ServiceType), false)
                .ConfigureAwait(false);

            if (result == null || result.Document == null)
                return new Tuple<bool, uBaseObject>(false, null);

            var trackUriElem = result.Document.Descendants(uPnpNamespaces.AvTransport + "GetPositionInfoResponse").Select(i => i.Element("TrackURI")).FirstOrDefault(i => i != null);
            var trackUri = trackUriElem == null ? null : trackUriElem.Value;

            var durationElem = result.Document.Descendants(uPnpNamespaces.AvTransport + "GetPositionInfoResponse").Select(i => i.Element("TrackDuration")).FirstOrDefault(i => i != null);
            var duration = durationElem == null ? null : durationElem.Value;

            if (!string.IsNullOrWhiteSpace(duration) &&
                !string.Equals(duration, "NOT_IMPLEMENTED", StringComparison.OrdinalIgnoreCase))
            {
                Duration = TimeSpan.Parse(duration, UsCulture);
            }
            else
            {
                Duration = null;
            }

            var positionElem = result.Document.Descendants(uPnpNamespaces.AvTransport + "GetPositionInfoResponse").Select(i => i.Element("RelTime")).FirstOrDefault(i => i != null);
            var position = positionElem == null ? null : positionElem.Value;

            if (!string.IsNullOrWhiteSpace(position) && !string.Equals(position, "NOT_IMPLEMENTED", StringComparison.OrdinalIgnoreCase))
            {
                Position = TimeSpan.Parse(position, UsCulture);
            }

            var track = result.Document.Descendants("TrackMetaData").FirstOrDefault();

            if (track == null)
            {
                //If track is null, some vendors do this, use GetMediaInfo instead                    
                return new Tuple<bool, uBaseObject>(true, null);
            }

            var trackString = (string)track;

            if (string.IsNullOrWhiteSpace(trackString) || string.Equals(trackString, "NOT_IMPLEMENTED", StringComparison.OrdinalIgnoreCase))
            {
                return new Tuple<bool, uBaseObject>(false, null);
            }

            XElement uPnpResponse;
            
            // Handle different variations sent back by devices
            try
            {
                uPnpResponse = XElement.Parse(trackString);
            }
            catch (Exception)
            {
                // first try to add a root node with a dlna namesapce
                try
                {
                    uPnpResponse = XElement.Parse("<data xmlns:dlna=\"urn:schemas-dlna-org:device-1-0\">" + trackString + "</data>");
                    uPnpResponse = uPnpResponse.Descendants().First();
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Unable to parse xml {0}", ex, trackString);
                    return new Tuple<bool, uBaseObject>(true, null);
                }
            }

            var e = uPnpResponse.Element(uPnpNamespaces.items);

            var uTrack = CreateUBaseObject(e, trackUri);

            return new Tuple<bool, uBaseObject>(true, uTrack);
        }

        private static uBaseObject CreateUBaseObject(XElement container, string trackUri)
        {
            if (container == null)
            {
                throw new ArgumentNullException("container");
            }

            var url = container.GetValue(uPnpNamespaces.Res);

            if (string.IsNullOrWhiteSpace(url))
            {
                url = trackUri;
            }

            return new uBaseObject
            {
                Id = container.GetAttributeValue(uPnpNamespaces.Id),
                ParentId = container.GetAttributeValue(uPnpNamespaces.ParentId),
                Title = container.GetValue(uPnpNamespaces.title),
                IconUrl = container.GetValue(uPnpNamespaces.Artwork),
                SecondText = "",
                Url = url,
                ProtocolInfo = GetProtocolInfo(container),
                MetaData = container.ToString()
            };
        }

        private static string[] GetProtocolInfo(XElement container)
        {
            if (container == null)
            {
                throw new ArgumentNullException("container");
            }

            var resElement = container.Element(uPnpNamespaces.Res);

            if (resElement != null)
            {
                var info = resElement.Attribute(uPnpNamespaces.ProtocolInfo);

                if (info != null && !string.IsNullOrWhiteSpace(info.Value))
                {
                    return info.Value.Split(':');
                }
            }

            return new string[4];
        }

        #endregion

        #region From XML

        private async Task GetAVProtocolAsync()
        {
            var avService = Properties.Services.FirstOrDefault(s => s.ServiceType == ServiceAvtransportType);
            if (avService == null)
                return;

            string url = NormalizeUrl(Properties.BaseUrl, avService.ScpdUrl);

            var httpClient = new SsdpHttpClient(_httpClient, _config);
            var document = await httpClient.GetDataAsync(url);

            AvCommands = TransportCommands.Create(document);
        }

        private async Task GetRenderingProtocolAsync()
        {
            var avService = Properties.Services.FirstOrDefault(s => s.ServiceType == ServiceRenderingType);

            if (avService == null)
                return;
            string url = NormalizeUrl(Properties.BaseUrl, avService.ScpdUrl);

            var httpClient = new SsdpHttpClient(_httpClient, _config);
            var document = await httpClient.GetDataAsync(url);

            RendererCommands = TransportCommands.Create(document);
        }

        private string NormalizeUrl(string baseUrl, string url)
        {
            // If it's already a complete url, don't stick anything onto the front of it
            if (url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                return url;
            }

            if (!url.Contains("/"))
                url = "/dmr/" + url;
            if (!url.StartsWith("/"))
                url = "/" + url;

            return baseUrl + url;
        }

        private TransportCommands AvCommands
        {
            get;
            set;
        }

        internal TransportCommands RendererCommands
        {
            get;
            set;
        }

        public static async Task<Device> CreateuPnpDeviceAsync(Uri url, IHttpClient httpClient, IServerConfigurationManager config, ILogger logger)
        {
            var ssdpHttpClient = new SsdpHttpClient(httpClient, config);

            var document = await ssdpHttpClient.GetDataAsync(url.ToString()).ConfigureAwait(false);

            var deviceProperties = new DeviceInfo();

            var friendlyNames = new List<string>();

            var name = document.Descendants(uPnpNamespaces.ud.GetName("friendlyName")).FirstOrDefault();
            if (name != null && !string.IsNullOrWhiteSpace(name.Value))
                friendlyNames.Add(name.Value);

            var room = document.Descendants(uPnpNamespaces.ud.GetName("roomName")).FirstOrDefault();
            if (room != null && !string.IsNullOrWhiteSpace(room.Value))
                friendlyNames.Add(room.Value);

            deviceProperties.Name = string.Join(" ", friendlyNames.ToArray());

            var model = document.Descendants(uPnpNamespaces.ud.GetName("modelName")).FirstOrDefault();
            if (model != null)
                deviceProperties.ModelName = model.Value;

            var modelNumber = document.Descendants(uPnpNamespaces.ud.GetName("modelNumber")).FirstOrDefault();
            if (modelNumber != null)
                deviceProperties.ModelNumber = modelNumber.Value;

            var uuid = document.Descendants(uPnpNamespaces.ud.GetName("UDN")).FirstOrDefault();
            if (uuid != null)
                deviceProperties.UUID = uuid.Value;

            var manufacturer = document.Descendants(uPnpNamespaces.ud.GetName("manufacturer")).FirstOrDefault();
            if (manufacturer != null)
                deviceProperties.Manufacturer = manufacturer.Value;

            var manufacturerUrl = document.Descendants(uPnpNamespaces.ud.GetName("manufacturerURL")).FirstOrDefault();
            if (manufacturerUrl != null)
                deviceProperties.ManufacturerUrl = manufacturerUrl.Value;

            var presentationUrl = document.Descendants(uPnpNamespaces.ud.GetName("presentationURL")).FirstOrDefault();
            if (presentationUrl != null)
                deviceProperties.PresentationUrl = presentationUrl.Value;

            var modelUrl = document.Descendants(uPnpNamespaces.ud.GetName("modelURL")).FirstOrDefault();
            if (modelUrl != null)
                deviceProperties.ModelUrl = modelUrl.Value;

            var serialNumber = document.Descendants(uPnpNamespaces.ud.GetName("serialNumber")).FirstOrDefault();
            if (serialNumber != null)
                deviceProperties.SerialNumber = serialNumber.Value;

            var modelDescription = document.Descendants(uPnpNamespaces.ud.GetName("modelDescription")).FirstOrDefault();
            if (modelDescription != null)
                deviceProperties.ModelDescription = modelDescription.Value;

            deviceProperties.BaseUrl = String.Format("http://{0}:{1}", url.Host, url.Port);

            var icon = document.Descendants(uPnpNamespaces.ud.GetName("icon")).FirstOrDefault();

            if (icon != null)
            {
                deviceProperties.Icon = CreateIcon(icon);
            }

            var isRenderer = false;

            foreach (var services in document.Descendants(uPnpNamespaces.ud.GetName("serviceList")))
            {
                if (services == null)
                    continue;

                var servicesList = services.Descendants(uPnpNamespaces.ud.GetName("service"));

                if (servicesList == null)
                    continue;

                foreach (var element in servicesList)
                {
                    var service = Create(element);

                    if (service != null)
                    {
                        deviceProperties.Services.Add(service);
                        if (service.ServiceType == ServiceAvtransportType)
                        {
                            isRenderer = true;
                        }
                    }
                }
            }

            var device = new Device(deviceProperties, httpClient, logger, config);

            if (isRenderer)
            {
                await device.GetRenderingProtocolAsync().ConfigureAwait(false);
                await device.GetAVProtocolAsync().ConfigureAwait(false);
            }

            return device;
        }

        #endregion

        private static readonly CultureInfo UsCulture = new CultureInfo("en-US");
        private static DeviceIcon CreateIcon(XElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            var mimeType = element.GetDescendantValue(uPnpNamespaces.ud.GetName("mimetype"));
            var width = element.GetDescendantValue(uPnpNamespaces.ud.GetName("width"));
            var height = element.GetDescendantValue(uPnpNamespaces.ud.GetName("height"));
            var depth = element.GetDescendantValue(uPnpNamespaces.ud.GetName("depth"));
            var url = element.GetDescendantValue(uPnpNamespaces.ud.GetName("url"));

            var widthValue = int.Parse(width, NumberStyles.Any, UsCulture);
            var heightValue = int.Parse(height, NumberStyles.Any, UsCulture);

            return new DeviceIcon
            {
                Depth = depth,
                Height = heightValue,
                MimeType = mimeType,
                Url = url,
                Width = widthValue
            };
        }

        private static DeviceService Create(XElement element)
        {
            var type = element.GetDescendantValue(uPnpNamespaces.ud.GetName("serviceType"));
            var id = element.GetDescendantValue(uPnpNamespaces.ud.GetName("serviceId"));
            var scpdUrl = element.GetDescendantValue(uPnpNamespaces.ud.GetName("SCPDURL"));
            var controlURL = element.GetDescendantValue(uPnpNamespaces.ud.GetName("controlURL"));
            var eventSubURL = element.GetDescendantValue(uPnpNamespaces.ud.GetName("eventSubURL"));

            return new DeviceService
            {
                ControlUrl = controlURL,
                EventSubUrl = eventSubURL,
                ScpdUrl = scpdUrl,
                ServiceId = id,
                ServiceType = type
            };
        }

        public event EventHandler<PlaybackStartEventArgs> PlaybackStart;
        public event EventHandler<PlaybackProgressEventArgs> PlaybackProgress;
        public event EventHandler<PlaybackStoppedEventArgs> PlaybackStopped;
        public event EventHandler<MediaChangedEventArgs> MediaChanged;

        public uBaseObject CurrentMediaInfo { get; private set; }

        private void UpdateMediaInfo(uBaseObject mediaInfo, TRANSPORTSTATE state)
        {
            TransportState = state;

            var previousMediaInfo = CurrentMediaInfo;
            CurrentMediaInfo = mediaInfo;

            if (previousMediaInfo == null && mediaInfo != null)
            {
                if (state != TRANSPORTSTATE.STOPPED)
                {
                    OnPlaybackStart(mediaInfo);
                }
            }
            else if (mediaInfo != null && previousMediaInfo != null && !mediaInfo.Equals(previousMediaInfo))
            {
                OnMediaChanged(previousMediaInfo, mediaInfo);
            }
            else if (mediaInfo == null && previousMediaInfo != null)
            {
                OnPlaybackStop(previousMediaInfo);
            }
            else if (mediaInfo != null && mediaInfo.Equals(previousMediaInfo))
            {
                OnPlaybackProgress(mediaInfo);
            }
        }

        private void OnPlaybackStart(uBaseObject mediaInfo)
        {
            if (PlaybackStart != null)
            {
                PlaybackStart.Invoke(this, new PlaybackStartEventArgs
                {
                    MediaInfo = mediaInfo
                });
            }
        }

        private void OnPlaybackProgress(uBaseObject mediaInfo)
        {
            if (PlaybackProgress != null)
            {
                PlaybackProgress.Invoke(this, new PlaybackProgressEventArgs
                {
                    MediaInfo = mediaInfo
                });
            }
        }

        private void OnPlaybackStop(uBaseObject mediaInfo)
        {
            if (PlaybackStopped != null)
            {
                PlaybackStopped.Invoke(this, new PlaybackStoppedEventArgs
                {
                    MediaInfo = mediaInfo
                });
            }
        }

        private void OnMediaChanged(uBaseObject old, uBaseObject newMedia)
        {
            if (MediaChanged != null)
            {
                MediaChanged.Invoke(this, new MediaChangedEventArgs
                {
                    OldMediaInfo = old,
                    NewMediaInfo = newMedia
                });
            }
        }

        #region IDisposable

        bool _disposed;
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                DisposeTimer();
            }
        }

        private void DisposeTimer()
        {
            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }
        }

        #endregion

        public override string ToString()
        {
            return String.Format("{0} - {1}", Properties.Name, Properties.BaseUrl);
        }
    }
}
