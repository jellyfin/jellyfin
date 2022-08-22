#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Emby.Dlna.Common;
using Emby.Dlna.Ssdp;
using Microsoft.Extensions.Logging;

namespace Emby.Dlna.PlayTo
{
    public class Device : IDisposable
    {
        private readonly IHttpClientFactory _httpClientFactory;

        private readonly ILogger _logger;

        private readonly object _timerLock = new object();
        private Timer _timer;
        private int _muteVol;
        private int _volume;
        private DateTime _lastVolumeRefresh;
        private bool _volumeRefreshActive;
        private int _connectFailureCount;
        private bool _disposed;

        public Device(DeviceInfo deviceProperties, IHttpClientFactory httpClientFactory, ILogger logger)
        {
            Properties = deviceProperties;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public event EventHandler<PlaybackStartEventArgs> PlaybackStart;

        public event EventHandler<PlaybackProgressEventArgs> PlaybackProgress;

        public event EventHandler<PlaybackStoppedEventArgs> PlaybackStopped;

        public event EventHandler<MediaChangedEventArgs> MediaChanged;

        public DeviceInfo Properties { get; set; }

        public bool IsMuted { get; set; }

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

        public TransportState TransportState { get; private set; }

        public bool IsPlaying => TransportState == TransportState.PLAYING;

        public bool IsPaused => TransportState == TransportState.PAUSED_PLAYBACK;

        public bool IsStopped => TransportState == TransportState.STOPPED;

        public Action OnDeviceUnavailable { get; set; }

        private TransportCommands AvCommands { get; set; }

        private TransportCommands RendererCommands { get; set; }

        public UBaseObject CurrentMediaInfo { get; private set; }

        public void Start()
        {
            _logger.LogDebug("Dlna Device.Start");
            _timer = new Timer(TimerCallback, null, 1000, Timeout.Infinite);
        }

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

            var command = rendererCommands?.ServiceActions.FirstOrDefault(c => c.Name == "SetMute");
            if (command == null)
            {
                return false;
            }

            var service = GetServiceRenderingControl();

            if (service == null)
            {
                return false;
            }

            _logger.LogDebug("Setting mute");
            var value = mute ? 1 : 0;

            await new DlnaHttpClient(_logger, _httpClientFactory)
                .SendCommandAsync(
                    Properties.BaseUrl,
                    service,
                    command.Name,
                    rendererCommands.BuildPost(command, service.ServiceType, value),
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            IsMuted = mute;

            return true;
        }

        /// <summary>
        /// Sets volume on a scale of 0-100.
        /// </summary>
        /// <param name="value">The volume on a scale of 0-100.</param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task SetVolume(int value, CancellationToken cancellationToken)
        {
            var rendererCommands = await GetRenderingProtocolAsync(cancellationToken).ConfigureAwait(false);

            var command = rendererCommands?.ServiceActions.FirstOrDefault(c => c.Name == "SetVolume");
            if (command == null)
            {
                return;
            }

            var service = GetServiceRenderingControl();

            if (service == null)
            {
                throw new InvalidOperationException("Unable to find service");
            }

            // Set it early and assume it will succeed
            // Remote control will perform better
            Volume = value;

            await new DlnaHttpClient(_logger, _httpClientFactory)
                .SendCommandAsync(
                    Properties.BaseUrl,
                    service,
                    command.Name,
                    rendererCommands.BuildPost(command, service.ServiceType, value),
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task Seek(TimeSpan value, CancellationToken cancellationToken)
        {
            var avCommands = await GetAVProtocolAsync(cancellationToken).ConfigureAwait(false);

            var command = avCommands?.ServiceActions.FirstOrDefault(c => c.Name == "Seek");
            if (command == null)
            {
                return;
            }

            var service = GetAvTransportService();

            if (service == null)
            {
                throw new InvalidOperationException("Unable to find service");
            }

            await new DlnaHttpClient(_logger, _httpClientFactory)
                .SendCommandAsync(
                    Properties.BaseUrl,
                    service,
                    command.Name,
                    avCommands.BuildPost(command, service.ServiceType, string.Format(CultureInfo.InvariantCulture, "{0:hh}:{0:mm}:{0:ss}", value), "REL_TIME"),
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            RestartTimer(true);
        }

        public async Task SetAvTransport(string url, string header, string metaData, CancellationToken cancellationToken)
        {
            var avCommands = await GetAVProtocolAsync(cancellationToken).ConfigureAwait(false);

            url = url.Replace("&", "&amp;", StringComparison.Ordinal);

            _logger.LogDebug("{0} - SetAvTransport Uri: {1} DlnaHeaders: {2}", Properties.Name, url, header);

            var command = avCommands?.ServiceActions.FirstOrDefault(c => c.Name == "SetAVTransportURI");
            if (command == null)
            {
                return;
            }

            var dictionary = new Dictionary<string, string>
            {
                { "CurrentURI", url },
                { "CurrentURIMetaData", CreateDidlMeta(metaData) }
            };

            var service = GetAvTransportService();

            if (service == null)
            {
                throw new InvalidOperationException("Unable to find service");
            }

            var post = avCommands.BuildPost(command, service.ServiceType, url, dictionary);
            await new DlnaHttpClient(_logger, _httpClientFactory)
                .SendCommandAsync(
                    Properties.BaseUrl,
                    service,
                    command.Name,
                    post,
                    header: header,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            await Task.Delay(50, cancellationToken).ConfigureAwait(false);

            try
            {
                await SetPlay(avCommands, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // Some devices will throw an error if you tell it to play when it's already playing
                // Others won't
            }

            RestartTimer(true);
        }

        /*
         * SetNextAvTransport is used to specify to the DLNA device what is the next track to play.
         * Without that information, the next track command on the device does not work.
         */
        public async Task SetNextAvTransport(string url, string header, string metaData, CancellationToken cancellationToken = default)
        {
            var avCommands = await GetAVProtocolAsync(cancellationToken).ConfigureAwait(false);

            url = url.Replace("&", "&amp;", StringComparison.Ordinal);

            _logger.LogDebug("{PropertyName} - SetNextAvTransport Uri: {Url} DlnaHeaders: {Header}", Properties.Name, url, header);

            var command = avCommands.ServiceActions.FirstOrDefault(c => string.Equals(c.Name, "SetNextAVTransportURI", StringComparison.OrdinalIgnoreCase));
            if (command == null)
            {
                return;
            }

            var dictionary = new Dictionary<string, string>
            {
                { "NextURI", url },
                { "NextURIMetaData", CreateDidlMeta(metaData) }
            };

            var service = GetAvTransportService();

            if (service == null)
            {
                throw new InvalidOperationException("Unable to find service");
            }

            var post = avCommands.BuildPost(command, service.ServiceType, url, dictionary);
            await new DlnaHttpClient(_logger, _httpClientFactory)
                .SendCommandAsync(Properties.BaseUrl, service, command.Name, post, header, cancellationToken)
                .ConfigureAwait(false);
        }

        private static string CreateDidlMeta(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return SecurityElement.Escape(value);
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

            return new DlnaHttpClient(_logger, _httpClientFactory).SendCommandAsync(
                Properties.BaseUrl,
                service,
                command.Name,
                avCommands.BuildPost(command, service.ServiceType, 1),
                cancellationToken: cancellationToken);
        }

        public async Task SetPlay(CancellationToken cancellationToken)
        {
            var avCommands = await GetAVProtocolAsync(cancellationToken).ConfigureAwait(false);
            if (avCommands == null)
            {
                return;
            }

            await SetPlay(avCommands, cancellationToken).ConfigureAwait(false);

            RestartTimer(true);
        }

        public async Task SetStop(CancellationToken cancellationToken)
        {
            var avCommands = await GetAVProtocolAsync(cancellationToken).ConfigureAwait(false);

            var command = avCommands?.ServiceActions.FirstOrDefault(c => c.Name == "Stop");
            if (command == null)
            {
                return;
            }

            var service = GetAvTransportService();

            await new DlnaHttpClient(_logger, _httpClientFactory)
                .SendCommandAsync(
                    Properties.BaseUrl,
                    service,
                    command.Name,
                    avCommands.BuildPost(command, service.ServiceType, 1),
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            RestartTimer(true);
        }

        public async Task SetPause(CancellationToken cancellationToken)
        {
            var avCommands = await GetAVProtocolAsync(cancellationToken).ConfigureAwait(false);

            var command = avCommands?.ServiceActions.FirstOrDefault(c => c.Name == "Pause");
            if (command == null)
            {
                return;
            }

            var service = GetAvTransportService();

            await new DlnaHttpClient(_logger, _httpClientFactory)
                .SendCommandAsync(
                    Properties.BaseUrl,
                    service,
                    command.Name,
                    avCommands.BuildPost(command, service.ServiceType, 1),
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            TransportState = TransportState.PAUSED_PLAYBACK;

            RestartTimer(true);
        }

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
                    if (transportState.Value == TransportState.STOPPED)
                    {
                        UpdateMediaInfo(null, transportState.Value);
                    }
                    else
                    {
                        var tuple = await GetPositionInfo(avCommands, cancellationToken).ConfigureAwait(false);

                        var currentObject = tuple.Track;

                        if (tuple.Success && currentObject == null)
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
                    {
                        return;
                    }

                    // If we're not playing anything make sure we don't get data more often than necessary to keep the Session alive
                    if (transportState.Value == TransportState.STOPPED)
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
                {
                    return;
                }

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

            var command = rendererCommands?.ServiceActions.FirstOrDefault(c => c.Name == "GetVolume");
            if (command == null)
            {
                return;
            }

            var service = GetServiceRenderingControl();

            if (service == null)
            {
                return;
            }

            var result = await new DlnaHttpClient(_logger, _httpClientFactory).SendCommandAsync(
                Properties.BaseUrl,
                service,
                command.Name,
                rendererCommands.BuildPost(command, service.ServiceType),
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (result == null || result.Document == null)
            {
                return;
            }

            var volume = result.Document.Descendants(UPnpNamespaces.RenderingControl + "GetVolumeResponse").Select(i => i.Element("CurrentVolume")).FirstOrDefault(i => i != null);
            var volumeValue = volume?.Value;

            if (string.IsNullOrWhiteSpace(volumeValue))
            {
                return;
            }

            Volume = int.Parse(volumeValue, CultureInfo.InvariantCulture);

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

            var command = rendererCommands?.ServiceActions.FirstOrDefault(c => c.Name == "GetMute");
            if (command == null)
            {
                return;
            }

            var service = GetServiceRenderingControl();

            if (service == null)
            {
                return;
            }

            var result = await new DlnaHttpClient(_logger, _httpClientFactory).SendCommandAsync(
                Properties.BaseUrl,
                service,
                command.Name,
                rendererCommands.BuildPost(command, service.ServiceType),
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (result == null || result.Document == null)
            {
                return;
            }

            var valueNode = result.Document.Descendants(UPnpNamespaces.RenderingControl + "GetMuteResponse")
                                            .Select(i => i.Element("CurrentMute"))
                                            .FirstOrDefault(i => i != null);

            IsMuted = string.Equals(valueNode?.Value, "1", StringComparison.OrdinalIgnoreCase);
        }

        private async Task<TransportState?> GetTransportInfo(TransportCommands avCommands, CancellationToken cancellationToken)
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

            var result = await new DlnaHttpClient(_logger, _httpClientFactory).SendCommandAsync(
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
                result.Document.Descendants(UPnpNamespaces.AvTransport + "GetTransportInfoResponse").Select(i => i.Element("CurrentTransportState")).FirstOrDefault(i => i != null);

            var transportStateValue = transportState?.Value;

            if (transportStateValue != null
                && Enum.TryParse(transportStateValue, true, out TransportState state))
            {
                return state;
            }

            return null;
        }

        private async Task<UBaseObject> GetMediaInfo(TransportCommands avCommands, CancellationToken cancellationToken)
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
            if (rendererCommands == null)
            {
                return null;
            }

            var result = await new DlnaHttpClient(_logger, _httpClientFactory).SendCommandAsync(
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

            var e = track.Element(UPnpNamespaces.Items) ?? track;

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

            e = track.Element(UPnpNamespaces.Items) ?? track;

            elementString = (string)e;

            if (!string.IsNullOrWhiteSpace(elementString))
            {
                return new UBaseObject
                {
                    Url = elementString
                };
            }

            return null;
        }

        private async Task<(bool Success, UBaseObject Track)> GetPositionInfo(TransportCommands avCommands, CancellationToken cancellationToken)
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

            if (rendererCommands == null)
            {
                return (false, null);
            }

            var result = await new DlnaHttpClient(_logger, _httpClientFactory).SendCommandAsync(
                Properties.BaseUrl,
                service,
                command.Name,
                rendererCommands.BuildPost(command, service.ServiceType),
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (result == null || result.Document == null)
            {
                return (false, null);
            }

            var trackUriElem = result.Document.Descendants(UPnpNamespaces.AvTransport + "GetPositionInfoResponse").Select(i => i.Element("TrackURI")).FirstOrDefault(i => i != null);
            var trackUri = trackUriElem?.Value;

            var durationElem = result.Document.Descendants(UPnpNamespaces.AvTransport + "GetPositionInfoResponse").Select(i => i.Element("TrackDuration")).FirstOrDefault(i => i != null);
            var duration = durationElem?.Value;

            if (!string.IsNullOrWhiteSpace(duration)
                && !string.Equals(duration, "NOT_IMPLEMENTED", StringComparison.OrdinalIgnoreCase))
            {
                Duration = TimeSpan.Parse(duration, CultureInfo.InvariantCulture);
            }
            else
            {
                Duration = null;
            }

            var positionElem = result.Document.Descendants(UPnpNamespaces.AvTransport + "GetPositionInfoResponse").Select(i => i.Element("RelTime")).FirstOrDefault(i => i != null);
            var position = positionElem?.Value;

            if (!string.IsNullOrWhiteSpace(position) && !string.Equals(position, "NOT_IMPLEMENTED", StringComparison.OrdinalIgnoreCase))
            {
                Position = TimeSpan.Parse(position, CultureInfo.InvariantCulture);
            }

            var track = result.Document.Descendants("TrackMetaData").FirstOrDefault();

            if (track == null)
            {
                // If track is null, some vendors do this, use GetMediaInfo instead.
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

            var e = uPnpResponse.Element(UPnpNamespaces.Items);

            var uTrack = CreateUBaseObject(e, trackUri);

            return (true, uTrack);
        }

        private XElement ParseResponse(string xml)
        {
            // Handle different variations sent back by devices.
            try
            {
                return XElement.Parse(xml);
            }
            catch (XmlException)
            {
            }

            // first try to add a root node with a dlna namespace.
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
                return XElement.Parse(xml.Replace("&", "&amp;", StringComparison.Ordinal));
            }
            catch (XmlException)
            {
            }

            return null;
        }

        private static UBaseObject CreateUBaseObject(XElement container, string trackUri)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            var url = container.GetValue(UPnpNamespaces.Res);

            if (string.IsNullOrWhiteSpace(url))
            {
                url = trackUri;
            }

            return new UBaseObject
            {
                Id = container.GetAttributeValue(UPnpNamespaces.Id),
                ParentId = container.GetAttributeValue(UPnpNamespaces.ParentId),
                Title = container.GetValue(UPnpNamespaces.Title),
                IconUrl = container.GetValue(UPnpNamespaces.Artwork),
                SecondText = string.Empty,
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

            var resElement = container.Element(UPnpNamespaces.Res);

            if (resElement != null)
            {
                var info = resElement.Attribute(UPnpNamespaces.ProtocolInfo);

                if (info != null && !string.IsNullOrWhiteSpace(info.Value))
                {
                    return info.Value.Split(':');
                }
            }

            return new string[4];
        }

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

            var httpClient = new DlnaHttpClient(_logger, _httpClientFactory);

            var document = await httpClient.GetDataAsync(url, cancellationToken).ConfigureAwait(false);
            if (document == null)
            {
                return null;
            }

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

            var httpClient = new DlnaHttpClient(_logger, _httpClientFactory);
            _logger.LogDebug("Dlna Device.GetRenderingProtocolAsync");
            var document = await httpClient.GetDataAsync(url, cancellationToken).ConfigureAwait(false);
            if (document == null)
            {
                return null;
            }

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

            if (!url.Contains('/', StringComparison.Ordinal))
            {
                url = "/dmr/" + url;
            }

            if (!url.StartsWith('/'))
            {
                url = "/" + url;
            }

            return baseUrl + url;
        }

        public static async Task<Device> CreateuPnpDeviceAsync(Uri url, IHttpClientFactory httpClientFactory, ILogger logger, CancellationToken cancellationToken)
        {
            var ssdpHttpClient = new DlnaHttpClient(logger, httpClientFactory);

            var document = await ssdpHttpClient.GetDataAsync(url.ToString(), cancellationToken).ConfigureAwait(false);
            if (document == null)
            {
                return null;
            }

            var friendlyNames = new List<string>();

            var name = document.Descendants(UPnpNamespaces.Ud.GetName("friendlyName")).FirstOrDefault();
            if (name != null && !string.IsNullOrWhiteSpace(name.Value))
            {
                friendlyNames.Add(name.Value);
            }

            var room = document.Descendants(UPnpNamespaces.Ud.GetName("roomName")).FirstOrDefault();
            if (room != null && !string.IsNullOrWhiteSpace(room.Value))
            {
                friendlyNames.Add(room.Value);
            }

            var deviceProperties = new DeviceInfo()
            {
                Name = string.Join(' ', friendlyNames),
                BaseUrl = string.Format(CultureInfo.InvariantCulture, "http://{0}:{1}", url.Host, url.Port)
            };

            var model = document.Descendants(UPnpNamespaces.Ud.GetName("modelName")).FirstOrDefault();
            if (model != null)
            {
                deviceProperties.ModelName = model.Value;
            }

            var modelNumber = document.Descendants(UPnpNamespaces.Ud.GetName("modelNumber")).FirstOrDefault();
            if (modelNumber != null)
            {
                deviceProperties.ModelNumber = modelNumber.Value;
            }

            var uuid = document.Descendants(UPnpNamespaces.Ud.GetName("UDN")).FirstOrDefault();
            if (uuid != null)
            {
                deviceProperties.UUID = uuid.Value;
            }

            var manufacturer = document.Descendants(UPnpNamespaces.Ud.GetName("manufacturer")).FirstOrDefault();
            if (manufacturer != null)
            {
                deviceProperties.Manufacturer = manufacturer.Value;
            }

            var manufacturerUrl = document.Descendants(UPnpNamespaces.Ud.GetName("manufacturerURL")).FirstOrDefault();
            if (manufacturerUrl != null)
            {
                deviceProperties.ManufacturerUrl = manufacturerUrl.Value;
            }

            var presentationUrl = document.Descendants(UPnpNamespaces.Ud.GetName("presentationURL")).FirstOrDefault();
            if (presentationUrl != null)
            {
                deviceProperties.PresentationUrl = presentationUrl.Value;
            }

            var modelUrl = document.Descendants(UPnpNamespaces.Ud.GetName("modelURL")).FirstOrDefault();
            if (modelUrl != null)
            {
                deviceProperties.ModelUrl = modelUrl.Value;
            }

            var serialNumber = document.Descendants(UPnpNamespaces.Ud.GetName("serialNumber")).FirstOrDefault();
            if (serialNumber != null)
            {
                deviceProperties.SerialNumber = serialNumber.Value;
            }

            var modelDescription = document.Descendants(UPnpNamespaces.Ud.GetName("modelDescription")).FirstOrDefault();
            if (modelDescription != null)
            {
                deviceProperties.ModelDescription = modelDescription.Value;
            }

            var icon = document.Descendants(UPnpNamespaces.Ud.GetName("icon")).FirstOrDefault();
            if (icon != null)
            {
                deviceProperties.Icon = CreateIcon(icon);
            }

            foreach (var services in document.Descendants(UPnpNamespaces.Ud.GetName("serviceList")))
            {
                if (services == null)
                {
                    continue;
                }

                var servicesList = services.Descendants(UPnpNamespaces.Ud.GetName("service"));
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

            return new Device(deviceProperties, httpClientFactory, logger);
        }

#nullable enable
        private static DeviceIcon CreateIcon(XElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            var width = element.GetDescendantValue(UPnpNamespaces.Ud.GetName("width"));
            var height = element.GetDescendantValue(UPnpNamespaces.Ud.GetName("height"));

            _ = int.TryParse(width, NumberStyles.Integer, CultureInfo.InvariantCulture, out var widthValue);
            _ = int.TryParse(height, NumberStyles.Integer, CultureInfo.InvariantCulture, out var heightValue);

            return new DeviceIcon
            {
                Depth = element.GetDescendantValue(UPnpNamespaces.Ud.GetName("depth")) ?? string.Empty,
                Height = heightValue,
                MimeType = element.GetDescendantValue(UPnpNamespaces.Ud.GetName("mimetype")) ?? string.Empty,
                Url = element.GetDescendantValue(UPnpNamespaces.Ud.GetName("url")) ?? string.Empty,
                Width = widthValue
            };
        }

        private static DeviceService Create(XElement element)
            => new DeviceService()
            {
                ControlUrl = element.GetDescendantValue(UPnpNamespaces.Ud.GetName("controlURL")) ?? string.Empty,
                EventSubUrl = element.GetDescendantValue(UPnpNamespaces.Ud.GetName("eventSubURL")) ?? string.Empty,
                ScpdUrl = element.GetDescendantValue(UPnpNamespaces.Ud.GetName("SCPDURL")) ?? string.Empty,
                ServiceId = element.GetDescendantValue(UPnpNamespaces.Ud.GetName("serviceId")) ?? string.Empty,
                ServiceType = element.GetDescendantValue(UPnpNamespaces.Ud.GetName("serviceType")) ?? string.Empty
            };

        private void UpdateMediaInfo(UBaseObject? mediaInfo, TransportState state)
        {
            TransportState = state;

            var previousMediaInfo = CurrentMediaInfo;
            CurrentMediaInfo = mediaInfo;

            if (mediaInfo == null)
            {
                if (previousMediaInfo != null)
                {
                    OnPlaybackStop(previousMediaInfo);
                }
            }
            else if (previousMediaInfo == null)
            {
                if (state != TransportState.STOPPED)
                {
                    OnPlaybackStart(mediaInfo);
                }
            }
            else if (mediaInfo.Equals(previousMediaInfo))
            {
                OnPlaybackProgress(mediaInfo);
            }
            else
            {
                OnMediaChanged(previousMediaInfo, mediaInfo);
            }
        }

        private void OnPlaybackStart(UBaseObject mediaInfo)
        {
            if (string.IsNullOrWhiteSpace(mediaInfo.Url))
            {
                return;
            }

            PlaybackStart?.Invoke(this, new PlaybackStartEventArgs(mediaInfo));
        }

        private void OnPlaybackProgress(UBaseObject mediaInfo)
        {
            if (string.IsNullOrWhiteSpace(mediaInfo.Url))
            {
                return;
            }

            PlaybackProgress?.Invoke(this, new PlaybackProgressEventArgs(mediaInfo));
        }

        private void OnPlaybackStop(UBaseObject mediaInfo)
        {
            PlaybackStopped?.Invoke(this, new PlaybackStoppedEventArgs(mediaInfo));
        }

        private void OnMediaChanged(UBaseObject old, UBaseObject newMedia)
        {
            MediaChanged?.Invoke(this, new MediaChangedEventArgs(old, newMedia));
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and optionally managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
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

        /// <inheritdoc />
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} - {1}", Properties.Name, Properties.BaseUrl);
        }
    }
}
