#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Emby.Dlna.Common;
using Emby.Dlna.Server;
using Emby.Dlna.Ssdp;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using Microsoft.Extensions.Logging;

namespace Emby.Dlna.PlayTo
{
    public class Device : IDisposable
    {
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
                RefreshVolumeIfNeeded().GetAwaiter().GetResult();
                return _volume;
            }
            set => _volume = value;
        }

        public TimeSpan? Duration { get; set; }

        public TimeSpan Position { get; set; } = TimeSpan.FromSeconds(0);

        public TRANSPORTSTATE TransportState { get; private set; }

        public bool IsPlaying => TransportState == TRANSPORTSTATE.PLAYING;

        public bool IsPaused => TransportState == TRANSPORTSTATE.PAUSED || TransportState == TRANSPORTSTATE.PAUSED_PLAYBACK;

        public bool IsStopped => TransportState == TRANSPORTSTATE.STOPPED;

        #endregion

        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly IServerConfigurationManager _config;

        public Action OnDeviceUnavailable { get; set; }

        public Device(DeviceInfo deviceProperties, IHttpClient httpClient, ILogger logger, IServerConfigurationManager config)
        {
            Properties = deviceProperties;
            _httpClient = httpClient;
            _logger = logger;
            _config = config;
        }

        public void Start()
        {
            _logger.LogDebug("Dlna Device.Start");
            _timer = new Timer(TimerCallback, null, 1000, Timeout.Infinite);
        }

        private DateTime _lastVolumeRefresh;
        private bool _volumeRefreshActive;
        private Task RefreshVolumeIfNeeded()
        {
            if (_volumeRefreshActive
                && DateTime.UtcNow >= _lastVolumeRefresh.AddSeconds(5))
            {
                _lastVolumeRefresh = DateTime.UtcNow;
                return RefreshVolume();
            }

            return Task.CompletedTask;
        }

        private async Task RefreshVolume(CancellationToken cancellationToken = default)
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                await GetVolume(cancellationToken).ConfigureAwait(false);
                await GetMute(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating device volume info for {DeviceName}", Properties.Name);
            }
        }

        private readonly object _timerLock = new object();
        private void RestartTimer(bool immediate = false)
        {
            lock (_timerLock)
            {
                if (_disposed)
                {
                    return;
                }

                _volumeRefreshActive = true;

                var time = immediate ? 100 : 10000;
                _timer.Change(time, Timeout.Infinite);
            }
        }

        /// <summary>
        /// Restarts the timer in inactive mode.
        /// </summary>
        private void RestartTimerInactive()
        {
            lock (_timerLock)
            {
                if (_disposed)
                {
                    return;
                }

                _volumeRefreshActive = false;

                _timer.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }

        #region Commanding

        public Task VolumeDown(CancellationToken cancellationToken)
        {
            var sendVolume = Math.Max(Volume - 5, 0);

            return SetVolume(sendVolume, cancellationToken);
        }

        public Task VolumeUp(CancellationToken cancellationToken)
        {
            var sendVolume = Math.Min(Volume + 5, 100);

            return SetVolume(sendVolume, cancellationToken);
        }

        public Task ToggleMute(CancellationToken cancellationToken)
        {
            if (IsMuted)
            {
                return Unmute(cancellationToken);
            }

            return Mute(cancellationToken);
        }

        public async Task Mute(CancellationToken cancellationToken)
        {
            var success = await SetMute(true, cancellationToken).ConfigureAwait(true);

            if (!success)
            {
                await SetVolume(0, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task Unmute(CancellationToken cancellationToken)
        {
            var success = await SetMute(false, cancellationToken).ConfigureAwait(true);

            if (!success)
            {
                var sendVolume = _muteVol <= 0 ? 20 : _muteVol;

                await SetVolume(sendVolume, cancellationToken).ConfigureAwait(false);
            }
        }

        private DeviceService GetServiceRenderingControl()
        {
            var services = Properties.Services;

            return services.FirstOrDefault(s => string.Equals(s.ServiceType, "urn:schemas-upnp-org:service:RenderingControl:1", StringComparison.OrdinalIgnoreCase)) ??
                services.FirstOrDefault(s => (s.ServiceType ?? string.Empty).StartsWith("urn:schemas-upnp-org:service:RenderingControl", StringComparison.OrdinalIgnoreCase));
        }

        private DeviceService GetAvTransportService()
        {
            var services = Properties.Services;

            return services.FirstOrDefault(s => string.Equals(s.ServiceType, "urn:schemas-upnp-org:service:AVTransport:1", StringComparison.OrdinalIgnoreCase)) ??
                services.FirstOrDefault(s => (s.ServiceType ?? string.Empty).StartsWith("urn:schemas-upnp-org:service:AVTransport", StringComparison.OrdinalIgnoreCase));
        }

        private async Task<bool> SetMute(bool mute, CancellationToken cancellationToken)
        {
            var rendererCommands = await GetRenderingProtocolAsync(cancellationToken).ConfigureAwait(false);

            var command = rendererCommands.ServiceActions.FirstOrDefault(c => c.Name == "SetMute");
            if (command == null)
                return false;

            var service = GetServiceRenderingControl();

            if (service == null)
            {
                return false;
            }

            _logger.LogDebug("Setting mute");
            var value = mute ? 1 : 0;

            await new SsdpHttpClient(_httpClient).SendCommandAsync(Properties.BaseUrl, service, command.Name, rendererCommands.BuildPost(command, service.ServiceType, value))
                .ConfigureAwait(false);

            IsMuted = mute;

            return true;
        }

        /// <summary>
        /// Sets volume on a scale of 0-100
        /// </summary>
        public async Task SetVolume(int value, CancellationToken cancellationToken)
        {
            var rendererCommands = await GetRenderingProtocolAsync(cancellationToken).ConfigureAwait(false);

            var command = rendererCommands.ServiceActions.FirstOrDefault(c => c.Name == "SetVolume");
            if (command == null)
                return;

            var service = GetServiceRenderingControl();

            if (service == null)
            {
                throw new InvalidOperationException("Unable to find service");
            }

            // Set it early and assume it will succeed
            // Remote control will perform better
            Volume = value;

            await new SsdpHttpClient(_httpClient).SendCommandAsync(Properties.BaseUrl, service, command.Name, rendererCommands.BuildPost(command, service.ServiceType, value))
                .ConfigureAwait(false);
        }

        public async Task Seek(TimeSpan value, CancellationToken cancellationToken)
        {
            var avCommands = await GetAVProtocolAsync(cancellationToken).ConfigureAwait(false);

            var command = avCommands.ServiceActions.FirstOrDefault(c => c.Name == "Seek");
            if (command == null)
                return;

            var service = GetAvTransportService();

            if (service == null)
            {
                throw new InvalidOperationException("Unable to find service");
            }

            await new SsdpHttpClient(_httpClient).SendCommandAsync(Properties.BaseUrl, service, command.Name, avCommands.BuildPost(command, service.ServiceType, string.Format("{0:hh}:{0:mm}:{0:ss}", value), "REL_TIME"))
                .ConfigureAwait(false);

            RestartTimer(true);
        }

        public async Task SetAvTransport(string url, string header, string metaData, CancellationToken cancellationToken)
        {
            var avCommands = await GetAVProtocolAsync(cancellationToken).ConfigureAwait(false);

            url = url.Replace("&", "&amp;");

            _logger.LogDebug("{0} - SetAvTransport Uri: {1} DlnaHeaders: {2}", Properties.Name, url, header);

            var command = avCommands.ServiceActions.FirstOrDefault(c => c.Name == "SetAVTransportURI");
            if (command == null)
                return;

            var dictionary = new Dictionary<string, string>
            {
                {"CurrentURI", url},
                {"CurrentURIMetaData", CreateDidlMeta(metaData)}
            };

            var service = GetAvTransportService();

            if (service == null)
            {
                throw new InvalidOperationException("Unable to find service");
            }

            var post = avCommands.BuildPost(command, service.ServiceType, url, dictionary);
            await new SsdpHttpClient(_httpClient).SendCommandAsync(Properties.BaseUrl, service, command.Name, post, header: header)
                .ConfigureAwait(false);

            await Task.Delay(50).ConfigureAwait(false);

            try
            {
                await SetPlay(avCommands, CancellationToken.None).ConfigureAwait(false);
            }
            catch
            {
                // Some devices will throw an error if you tell it to play when it's already playing
                // Others won't
            }

            RestartTimer(true);
        }

        private string CreateDidlMeta(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return DescriptionXmlBuilder.Escape(value);
        }

        private Task SetPlay(TransportCommands avCommands, CancellationToken cancellationToken)
        {
            var command = avCommands.ServiceActions.FirstOrDefault(c => c.Name == "Play");
            if (command == null)
            {
                return Task.CompletedTask;
            }

            var service = GetAvTransportService();
            if (service == null)
            {
                throw new InvalidOperationException("Unable to find service");
            }

            return new SsdpHttpClient(_httpClient).SendCommandAsync(
                Properties.BaseUrl,
                service,
                command.Name,
                avCommands.BuildPost(command, service.ServiceType, 1),
                cancellationToken: cancellationToken);
        }

        public async Task SetPlay(CancellationToken cancellationToken)
        {
            var avCommands = await GetAVProtocolAsync(cancellationToken).ConfigureAwait(false);

            await SetPlay(avCommands, cancellationToken).ConfigureAwait(false);

            RestartTimer(true);
        }

        public async Task SetStop(CancellationToken cancellationToken)
        {
            var avCommands = await GetAVProtocolAsync(cancellationToken).ConfigureAwait(false);

            var command = avCommands.ServiceActions.FirstOrDefault(c => c.Name == "Stop");
            if (command == null)
            {
                return;
            }

            var service = GetAvTransportService();

            await new SsdpHttpClient(_httpClient).SendCommandAsync(Properties.BaseUrl, service, command.Name, avCommands.BuildPost(command, service.ServiceType, 1))
                .ConfigureAwait(false);

            RestartTimer(true);
        }

        public async Task SetPause(CancellationToken cancellationToken)
        {
            var avCommands = await GetAVProtocolAsync(cancellationToken).ConfigureAwait(false);

            var command = avCommands.ServiceActions.FirstOrDefault(c => c.Name == "Pause");
            if (command == null)
            {
                return;
            }

            var service = GetAvTransportService();

            await new SsdpHttpClient(_httpClient).SendCommandAsync(Properties.BaseUrl, service, command.Name, avCommands.BuildPost(command, service.ServiceType, 1))
                .ConfigureAwait(false);

            TransportState = TRANSPORTSTATE.PAUSED;

            RestartTimer(true);
        }

        #endregion

        #region Get data

        private int _connectFailureCount;
        private async void TimerCallback(object sender)
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                var cancellationToken = CancellationToken.None;

                var avCommands = await GetAVProtocolAsync(cancellationToken).ConfigureAwait(false);

                if (avCommands == null)
                {
                    return;
                }

                var transportState = await GetTransportInfo(avCommands, cancellationToken).ConfigureAwait(false);

                if (_disposed)
                {
                    return;
                }

                if (transportState.HasValue)
                {
                    // If we're not playing anything no need to get additional data
                    if (transportState.Value == TRANSPORTSTATE.STOPPED)
                    {
                        UpdateMediaInfo(null, transportState.Value);
                    }
                    else
                    {
                        var tuple = await GetPositionInfo(avCommands, cancellationToken).ConfigureAwait(false);

                        var currentObject = tuple.Item2;

                        if (tuple.Item1 && currentObject == null)
                        {
                            currentObject = await GetMediaInfo(avCommands, cancellationToken).ConfigureAwait(false);
                        }

                        if (currentObject != null)
                        {
                            UpdateMediaInfo(currentObject, transportState.Value);
                        }
                    }

                    _connectFailureCount = 0;

                    if (_disposed)
                        return;

                    // If we're not playing anything make sure we don't get data more often than neccessry to keep the Session alive
                    if (transportState.Value == TRANSPORTSTATE.STOPPED)
                    {
                        RestartTimerInactive();
                    }
                    else
                    {
                        RestartTimer();
                    }
                }
                else
                {
                    RestartTimerInactive();
                }
            }
            catch (Exception ex)
            {
                if (_disposed)
                    return;

                _logger.LogError(ex, "Error updating device info for {DeviceName}", Properties.Name);

                _connectFailureCount++;

                if (_connectFailureCount >= 3)
                {
                    var action = OnDeviceUnavailable;
                    if (action != null)
                    {
                        _logger.LogDebug("Disposing device due to loss of connection");
                        action();
                        return;
                    }
                }
                RestartTimerInactive();
            }
        }

        private async Task GetVolume(CancellationToken cancellationToken)
        {
            if (_disposed)
            {
                return;
            }

            var rendererCommands = await GetRenderingProtocolAsync(cancellationToken).ConfigureAwait(false);

            var command = rendererCommands.ServiceActions.FirstOrDefault(c => c.Name == "GetVolume");
            if (command == null)
            {
                return;
            }

            var service = GetServiceRenderingControl();

            if (service == null)
            {
                return;
            }

            var result = await new SsdpHttpClient(_httpClient).SendCommandAsync(
                Properties.BaseUrl,
                service,
                command.Name,
                rendererCommands.BuildPost(command, service.ServiceType),
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (result == null || result.Document == null)
            {
                return;
            }

            var volume = result.Document.Descendants(uPnpNamespaces.RenderingControl + "GetVolumeResponse").Select(i => i.Element("CurrentVolume")).FirstOrDefault(i => i != null);
            var volumeValue = volume?.Value;

            if (string.IsNullOrWhiteSpace(volumeValue))
            {
                return;
            }

            Volume = int.Parse(volumeValue, UsCulture);

            if (Volume > 0)
            {
                _muteVol = Volume;
            }
        }

        private async Task GetMute(CancellationToken cancellationToken)
        {
            if (_disposed)
            {
                return;
            }

            var rendererCommands = await GetRenderingProtocolAsync(cancellationToken).ConfigureAwait(false);

            var command = rendererCommands.ServiceActions.FirstOrDefault(c => c.Name == "GetMute");
            if (command == null)
            {
                return;
            }

            var service = GetServiceRenderingControl();

            if (service == null)
            {
                return;
            }

            var result = await new SsdpHttpClient(_httpClient).SendCommandAsync(
                Properties.BaseUrl,
                service,
                command.Name,
                rendererCommands.BuildPost(command, service.ServiceType),
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (result == null || result.Document == null)
                return;

            var valueNode = result.Document.Descendants(uPnpNamespaces.RenderingControl + "GetMuteResponse")
                                            .Select(i => i.Element("CurrentMute"))
                                            .FirstOrDefault(i => i != null);

            IsMuted = string.Equals(valueNode?.Value, "1", StringComparison.OrdinalIgnoreCase);
        }

        private async Task<TRANSPORTSTATE?> GetTransportInfo(TransportCommands avCommands, CancellationToken cancellationToken)
        {
            var command = avCommands.ServiceActions.FirstOrDefault(c => c.Name == "GetTransportInfo");
            if (command == null)
            {
                return null;
            }

            var service = GetAvTransportService();
            if (service == null)
            {
                return null;
            }

            var result = await new SsdpHttpClient(_httpClient).SendCommandAsync(
                Properties.BaseUrl,
                service,
                command.Name,
                avCommands.BuildPost(command, service.ServiceType),
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (result == null || result.Document == null)
            {
                return null;
            }

            var transportState =
                result.Document.Descendants(uPnpNamespaces.AvTransport + "GetTransportInfoResponse").Select(i => i.Element("CurrentTransportState")).FirstOrDefault(i => i != null);

            var transportStateValue = transportState?.Value;

            if (transportStateValue != null
                && Enum.TryParse(transportStateValue, true, out TRANSPORTSTATE state))
            {
                return state;
            }

            return null;
        }

        private async Task<uBaseObject> GetMediaInfo(TransportCommands avCommands, CancellationToken cancellationToken)
        {
            var command = avCommands.ServiceActions.FirstOrDefault(c => c.Name == "GetMediaInfo");
            if (command == null)
            {
                return null;
            }

            var service = GetAvTransportService();
            if (service == null)
            {
                throw new InvalidOperationException("Unable to find service");
            }

            var rendererCommands = await GetRenderingProtocolAsync(cancellationToken).ConfigureAwait(false);

            var result = await new SsdpHttpClient(_httpClient).SendCommandAsync(
                Properties.BaseUrl,
                service,
                command.Name,
                rendererCommands.BuildPost(command, service.ServiceType),
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (result == null || result.Document == null)
            {
                return null;
            }

            var track = result.Document.Descendants("CurrentURIMetaData").FirstOrDefault();

            if (track == null)
            {
                return null;
            }

            var e = track.Element(uPnpNamespaces.items) ?? track;

            var elementString = (string)e;

            if (!string.IsNullOrWhiteSpace(elementString))
            {
                return UpnpContainer.Create(e);
            }

            track = result.Document.Descendants("CurrentURI").FirstOrDefault();

            if (track == null)
            {
                return null;
            }

            e = track.Element(uPnpNamespaces.items) ?? track;

            elementString = (string)e;

            if (!string.IsNullOrWhiteSpace(elementString))
            {
                return new uBaseObject
                {
                    Url = elementString
                };
            }

            return null;
        }

        private async Task<(bool, uBaseObject)> GetPositionInfo(TransportCommands avCommands, CancellationToken cancellationToken)
        {
            var command = avCommands.ServiceActions.FirstOrDefault(c => c.Name == "GetPositionInfo");
            if (command == null)
            {
                return (false, null);
            }

            var service = GetAvTransportService();

            if (service == null)
            {
                throw new InvalidOperationException("Unable to find service");
            }

            var rendererCommands = await GetRenderingProtocolAsync(cancellationToken).ConfigureAwait(false);

            var result = await new SsdpHttpClient(_httpClient).SendCommandAsync(
                Properties.BaseUrl,
                service,
                command.Name,
                rendererCommands.BuildPost(command, service.ServiceType),
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (result == null || result.Document == null)
            {
                return (false, null);
            }

            var trackUriElem = result.Document.Descendants(uPnpNamespaces.AvTransport + "GetPositionInfoResponse").Select(i => i.Element("TrackURI")).FirstOrDefault(i => i != null);
            var trackUri = trackUriElem == null ? null : trackUriElem.Value;

            var durationElem = result.Document.Descendants(uPnpNamespaces.AvTransport + "GetPositionInfoResponse").Select(i => i.Element("TrackDuration")).FirstOrDefault(i => i != null);
            var duration = durationElem == null ? null : durationElem.Value;

            if (!string.IsNullOrWhiteSpace(duration)
                && !string.Equals(duration, "NOT_IMPLEMENTED", StringComparison.OrdinalIgnoreCase))
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
                return (true, null);
            }

            var trackString = (string)track;

            if (string.IsNullOrWhiteSpace(trackString) || string.Equals(trackString, "NOT_IMPLEMENTED", StringComparison.OrdinalIgnoreCase))
            {
                return (true, null);
            }

            XElement uPnpResponse = null;

            try
            {
                uPnpResponse = ParseResponse(trackString);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Uncaught exception while parsing xml");
            }

            if (uPnpResponse == null)
            {
                _logger.LogError("Failed to parse xml: \n {Xml}", trackString);
                return (true, null);
            }

            var e = uPnpResponse.Element(uPnpNamespaces.items);

            var uTrack = CreateUBaseObject(e, trackUri);

            return (true, uTrack);
        }

        private XElement ParseResponse(string xml)
        {
            // Handle different variations sent back by devices
            try
            {
                return XElement.Parse(xml);
            }
            catch (XmlException)
            {

            }

            // first try to add a root node with a dlna namesapce
            try
            {
                return XElement.Parse("<data xmlns:dlna=\"urn:schemas-dlna-org:device-1-0\">" + xml + "</data>")
                                .Descendants()
                                .First();
            }
            catch (XmlException)
            {

            }

            // some devices send back invalid xml
            try
            {
                return XElement.Parse(xml.Replace("&", "&amp;"));
            }
            catch (XmlException)
            {

            }

            return null;
        }

        private static uBaseObject CreateUBaseObject(XElement container, string trackUri)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
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
                throw new ArgumentNullException(nameof(container));
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

        private async Task<TransportCommands> GetAVProtocolAsync(CancellationToken cancellationToken)
        {
            if (AvCommands != null)
            {
                return AvCommands;
            }

            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            var avService = GetAvTransportService();
            if (avService == null)
            {
                return null;
            }

            string url = NormalizeUrl(Properties.BaseUrl, avService.ScpdUrl);

            var httpClient = new SsdpHttpClient(_httpClient);

            var document = await httpClient.GetDataAsync(url, cancellationToken).ConfigureAwait(false);

            AvCommands = TransportCommands.Create(document);
            return AvCommands;
        }

        private async Task<TransportCommands> GetRenderingProtocolAsync(CancellationToken cancellationToken)
        {
            if (RendererCommands != null)
            {
                return RendererCommands;
            }

            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            var avService = GetServiceRenderingControl();
            if (avService == null)
            {
                throw new ArgumentException("Device AvService is null");
            }

            string url = NormalizeUrl(Properties.BaseUrl, avService.ScpdUrl);

            var httpClient = new SsdpHttpClient(_httpClient);
            _logger.LogDebug("Dlna Device.GetRenderingProtocolAsync");
            var document = await httpClient.GetDataAsync(url, cancellationToken).ConfigureAwait(false);

            RendererCommands = TransportCommands.Create(document);
            return RendererCommands;
        }

        private string NormalizeUrl(string baseUrl, string url)
        {
            // If it's already a complete url, don't stick anything onto the front of it
            if (url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                return url;
            }

            if (!url.Contains("/"))
            {
                url = "/dmr/" + url;
            }

            if (!url.StartsWith("/"))
            {
                url = "/" + url;
            }

            return baseUrl + url;
        }

        private TransportCommands AvCommands { get; set; }

        private TransportCommands RendererCommands { get; set; }

        public static async Task<Device> CreateuPnpDeviceAsync(Uri url, IHttpClient httpClient, IServerConfigurationManager config, ILogger logger, CancellationToken cancellationToken)
        {
            var ssdpHttpClient = new SsdpHttpClient(httpClient);

            var document = await ssdpHttpClient.GetDataAsync(url.ToString(), cancellationToken).ConfigureAwait(false);

            var friendlyNames = new List<string>();

            var name = document.Descendants(uPnpNamespaces.ud.GetName("friendlyName")).FirstOrDefault();
            if (name != null && !string.IsNullOrWhiteSpace(name.Value))
            {
                friendlyNames.Add(name.Value);
            }

            var room = document.Descendants(uPnpNamespaces.ud.GetName("roomName")).FirstOrDefault();
            if (room != null && !string.IsNullOrWhiteSpace(room.Value))
            {
                friendlyNames.Add(room.Value);
            }

            var deviceProperties = new DeviceInfo()
            {
                Name = string.Join(" ", friendlyNames),
                BaseUrl = string.Format("http://{0}:{1}", url.Host, url.Port)
            };

            var model = document.Descendants(uPnpNamespaces.ud.GetName("modelName")).FirstOrDefault();
            if (model != null)
            {
                deviceProperties.ModelName = model.Value;
            }

            var modelNumber = document.Descendants(uPnpNamespaces.ud.GetName("modelNumber")).FirstOrDefault();
            if (modelNumber != null)
            {
                deviceProperties.ModelNumber = modelNumber.Value;
            }

            var uuid = document.Descendants(uPnpNamespaces.ud.GetName("UDN")).FirstOrDefault();
            if (uuid != null)
            {
                deviceProperties.UUID = uuid.Value;
            }

            var manufacturer = document.Descendants(uPnpNamespaces.ud.GetName("manufacturer")).FirstOrDefault();
            if (manufacturer != null)
            {
                deviceProperties.Manufacturer = manufacturer.Value;
            }

            var manufacturerUrl = document.Descendants(uPnpNamespaces.ud.GetName("manufacturerURL")).FirstOrDefault();
            if (manufacturerUrl != null)
            {
                deviceProperties.ManufacturerUrl = manufacturerUrl.Value;
            }

            var presentationUrl = document.Descendants(uPnpNamespaces.ud.GetName("presentationURL")).FirstOrDefault();
            if (presentationUrl != null)
            {
                deviceProperties.PresentationUrl = presentationUrl.Value;
            }

            var modelUrl = document.Descendants(uPnpNamespaces.ud.GetName("modelURL")).FirstOrDefault();
            if (modelUrl != null)
            {
                deviceProperties.ModelUrl = modelUrl.Value;
            }

            var serialNumber = document.Descendants(uPnpNamespaces.ud.GetName("serialNumber")).FirstOrDefault();
            if (serialNumber != null)
            {
                deviceProperties.SerialNumber = serialNumber.Value;
            }

            var modelDescription = document.Descendants(uPnpNamespaces.ud.GetName("modelDescription")).FirstOrDefault();
            if (modelDescription != null)
            {
                deviceProperties.ModelDescription = modelDescription.Value;
            }

            var icon = document.Descendants(uPnpNamespaces.ud.GetName("icon")).FirstOrDefault();
            if (icon != null)
            {
                deviceProperties.Icon = CreateIcon(icon);
            }

            foreach (var services in document.Descendants(uPnpNamespaces.ud.GetName("serviceList")))
            {
                if (services == null)
                {
                    continue;
                }

                var servicesList = services.Descendants(uPnpNamespaces.ud.GetName("service"));
                if (servicesList == null)
                {
                    continue;
                }

                foreach (var element in servicesList)
                {
                    var service = Create(element);

                    if (service != null)
                    {
                        deviceProperties.Services.Add(service);
                    }
                }
            }

            return new Device(deviceProperties, httpClient, logger, config);
        }

        #endregion

        private static readonly CultureInfo UsCulture = new CultureInfo("en-US");
        private static DeviceIcon CreateIcon(XElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            var mimeType = element.GetDescendantValue(uPnpNamespaces.ud.GetName("mimetype"));
            var width = element.GetDescendantValue(uPnpNamespaces.ud.GetName("width"));
            var height = element.GetDescendantValue(uPnpNamespaces.ud.GetName("height"));
            var depth = element.GetDescendantValue(uPnpNamespaces.ud.GetName("depth"));
            var url = element.GetDescendantValue(uPnpNamespaces.ud.GetName("url"));

            var widthValue = int.Parse(width, NumberStyles.Integer, UsCulture);
            var heightValue = int.Parse(height, NumberStyles.Integer, UsCulture);

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
            if (string.IsNullOrWhiteSpace(mediaInfo.Url))
            {
                return;
            }

            PlaybackStart?.Invoke(this, new PlaybackStartEventArgs
            {
                MediaInfo = mediaInfo
            });
        }

        private void OnPlaybackProgress(uBaseObject mediaInfo)
        {
            if (string.IsNullOrWhiteSpace(mediaInfo.Url))
            {
                return;
            }

            PlaybackProgress?.Invoke(this, new PlaybackProgressEventArgs
            {
                MediaInfo = mediaInfo
            });
        }

        private void OnPlaybackStop(uBaseObject mediaInfo)
        {
            PlaybackStopped?.Invoke(this, new PlaybackStoppedEventArgs
            {
                MediaInfo = mediaInfo
            });
        }

        private void OnMediaChanged(uBaseObject old, uBaseObject newMedia)
        {
            MediaChanged?.Invoke(this, new MediaChangedEventArgs
            {
                OldMediaInfo = old,
                NewMediaInfo = newMedia
            });
        }

        #region IDisposable

        bool _disposed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _timer?.Dispose();
            }

            _timer = null;
            Properties = null;

            _disposed = true;
        }

        #endregion

        public override string ToString()
        {
            return string.Format("{0} - {1}", Properties.Name, Properties.BaseUrl);
        }
    }
}
