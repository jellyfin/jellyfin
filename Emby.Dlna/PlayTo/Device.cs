#pragma warning disable CS1591
#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
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
        private const string USERAGENT = "Microsoft-Windows/6.2 UPnP/1.0 Microsoft-DLNA DLNADOC/1.50";
        private const string FriendlyName = "Jellyfin";

        private const int TransportCommandsAV = 1;
        private const int TransportCommandsRender = 2;

        private static readonly CultureInfo _usCulture = new CultureInfo("en-US");

        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly PlayToManager _playToManager;
        private readonly object _timerLock = new object();
        private readonly string _jellyfinUrl;

        private bool _disposed;
        private int _connectFailureCount;
        private Timer? _timer;
        private int _volume;
        private int _muteVol;
        private DateTime _lastVolumeRefresh;
        private bool _volumeRefreshActive;
        private bool _eventing;
        private string? _sessionId;
        private string? _eventSid;

        public Device(PlayToManager playToManager, DeviceInfo deviceProperties, IHttpClient httpClient, ILogger logger, string webUrl)
        {
            Properties = deviceProperties;
            _httpClient = httpClient;
            _logger = logger;
            TransportState = TransportState.NO_MEDIA_PRESENT;
            _jellyfinUrl = webUrl;
            _playToManager = playToManager;
        }

        public event EventHandler<PlaybackStartEventArgs>? PlaybackStart;

        public event EventHandler<PlaybackProgressEventArgs>? PlaybackProgress;

        public event EventHandler<PlaybackStoppedEventArgs>? PlaybackStopped;

        public event EventHandler<MediaChangedEventArgs>? MediaChanged;

        public DeviceInfo Properties { get; set; }

        public bool IsMuted { get; set; }

        public uBaseObject? CurrentMediaInfo { get; private set; }

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

        public bool IsPaused => TransportState == TransportState.PAUSED || TransportState == TransportState.PAUSED_PLAYBACK;

        public bool IsStopped => TransportState == TransportState.STOPPED;

        public Action? OnDeviceUnavailable { get; set; }

        private TransportCommands? AvCommands { get; set; }

        private TransportCommands? RendererCommands { get; set; }

        public static async Task<Device> CreateuPnpDeviceAsync(
            PlayToManager playToManager,
            Uri url,
            IHttpClient httpClient,
            ILogger logger,
            string serverUrl,
            CancellationToken cancellationToken)
        {
            if (url == null)
            {
                throw new ArgumentNullException(nameof(url));
            }

            if (httpClient == null)
            {
                throw new ArgumentNullException(nameof(httpClient));
            }

            var document = await GetDataAsync(httpClient, url.ToString(), cancellationToken).ConfigureAwait(false);

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
                BaseUrl = string.Format(CultureInfo.InvariantCulture, "http://{0}:{1}", url.Host, url.Port)
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

            return new Device(playToManager, deviceProperties, httpClient, logger, serverUrl);
        }

        public void Start()
        {
            _logger.LogDebug("Dlna Device.Start");
            _timer = new Timer(TimerCallback, null, 1000, Timeout.Infinite);
        }

        public Task<bool> VolumeDown(CancellationToken cancellationToken)
        {
            return SetVolume(Math.Max(Volume - 5, 0), cancellationToken);
        }

        public Task<bool> VolumeUp(CancellationToken cancellationToken)
        {
            return SetVolume(Math.Min(Volume + 5, 100), cancellationToken);
        }

        public Task<bool> ToggleMute(CancellationToken cancellationToken)
        {
            if (IsMuted)
            {
                return Unmute(cancellationToken);
            }

            return Mute(cancellationToken);
        }

        public async Task<bool> SetPlay(CancellationToken cancellationToken)
        {
            if (!IsPlaying)
            {
                _logger.LogDebug("Playing.");
                var result = await SendCommand(TransportCommandsAV, "Play", cancellationToken, 1).ConfigureAwait(false);
                if (result)
                {
                    // Stop user from issuing multiple commands.
                    TransportState = TransportState.PLAYING;
                    RestartTimer(true);
                }

                await SubscribeAsync().ConfigureAwait(false);

                return result;
            }

            return false;
        }

        public async Task<bool> SetStop(CancellationToken cancellationToken)
        {
            if (IsPlaying || IsPaused)
            {
                _logger.LogDebug("Stopping.");
                var result = await SendCommand(TransportCommandsAV, "Stop", cancellationToken, 1).ConfigureAwait(false);
                if (result)
                {
                    // Stop user from issuing multiple commands.
                    TransportState = TransportState.STOPPED;
                    RestartTimer(true);
                }

                await UnSubscribe().ConfigureAwait(false);

                return result;
            }

            return false;
        }

        public async Task<bool> SetPause(CancellationToken cancellationToken)
        {
            if (!IsPaused)
            {
                _logger.LogDebug("Pausing.");
                var result = await SendCommand(TransportCommandsAV, "Pause", cancellationToken, 1).ConfigureAwait(false);
                if (result)
                {
                    // Stop user from issuing multiple commands.
                    TransportState = TransportState.PAUSED;
                    RestartTimer(true);
                }

                return result;
            }

            return false;
        }

        public async Task<bool> Mute(CancellationToken cancellationToken)
        {
            var success = await SetMute(true, cancellationToken).ConfigureAwait(true);

            if (!success)
            {
                return await SetVolume(0, cancellationToken).ConfigureAwait(false);
            }

            return false;
        }

        public async Task<bool> Unmute(CancellationToken cancellationToken)
        {
            var success = await SetMute(false, cancellationToken).ConfigureAwait(true);

            if (!success)
            {
                var sendVolume = _muteVol <= 0 ? 20 : _muteVol;
                return await SetVolume(sendVolume, cancellationToken).ConfigureAwait(false);
            }

            return false;
        }

        /// <summary>
        /// Sets volume on a scale of 0-100.
        /// </summary>
        /// <param name="value">Volume level.</param>
        /// <param name="cancellationToken">CancellationToken.</param>
        /// <returns>>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<bool> SetVolume(int value, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Setting volume {0}.", value);
            var result = await SendCommand(TransportCommandsRender, "SetVolume", cancellationToken, value).ConfigureAwait(false);

            if (result)
            {
                Volume = value;
            }

            return result;
        }

        public async Task<bool> Seek(TimeSpan value, CancellationToken cancellationToken)
        {
            if (IsPlaying || IsPaused)
            {
                _logger.LogDebug("Seeking to {0}.", value);
                var result = await SendCommand(
                    TransportCommandsAV,
                    "Seek",
                    cancellationToken,
                    string.Format(CultureInfo.InvariantCulture, "{0:hh}:{0:mm}:{0:ss}", value),
                    commandParameter: "REL_TIME").ConfigureAwait(false);

                if (result)
                {
                    RestartTimer(true);
                }

                return result;
            }

            return false;
        }

        public async Task<bool> SetAvTransport(string url, string header, string metaData, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(url))
            {
                return false;
            }

            url = url.Replace("&", "&amp;", StringComparison.OrdinalIgnoreCase);
            _logger.LogDebug("{0} - SetAvTransport Uri: {1} DlnaHeaders: {2}", Properties.Name, url, header);

            var dictionary = new Dictionary<string, string>
            {
                { "CurrentURI", url },
                { "CurrentURIMetaData", CreateDidlMeta(metaData) }
            };

            _logger.LogDebug("Setting transport to {0}.", dictionary);
            var result = await SendCommand(TransportCommandsAV, "SetAVTransportURI", cancellationToken, url, dictionary: dictionary, header: header).ConfigureAwait(false);

            if (result)
            {
                await Task.Delay(50).ConfigureAwait(false);

                result = await SetPlay(CancellationToken.None).ConfigureAwait(false);

                if (result)
                {
                    RestartTimer(true);
                }
            }

            return result;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} - {1}", Properties.Name, Properties.BaseUrl);
        }

        protected virtual async void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _timer?.Dispose();
                if (_eventing)
                {
                    await UnSubscribe().ConfigureAwait(false);
                }

                if (_playToManager != null)
                {
                    _playToManager.DLNAEvents -= ProcessEvent;
                }
            }

            _disposed = true;
        }

        private static async Task<XDocument> GetDataAsync(IHttpClient httpClient, string url, CancellationToken cancellationToken)
        {
            var options = new HttpRequestOptions
            {
                Url = url,
                UserAgent = USERAGENT,
                LogErrorResponseBody = true,
                BufferContent = false,

                CancellationToken = cancellationToken
            };

            options.RequestHeaders["FriendlyName.DLNA.ORG"] = FriendlyName;

            using var response = await httpClient.SendAsync(options, HttpMethod.Get).ConfigureAwait(false);
            using var stream = response.Content;
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return XDocument.Parse(
                await reader.ReadToEndAsync().ConfigureAwait(false),
                LoadOptions.PreserveWhitespace);
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

        private static string NormalizeServiceUrl(string baseUrl, string serviceUrl)
        {
            // If it's already a complete url, don't stick anything onto the front of it
            if (serviceUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                return serviceUrl;
            }

            if (!serviceUrl.StartsWith("/", StringComparison.Ordinal))
            {
                serviceUrl = "/" + serviceUrl;
            }

            return baseUrl + serviceUrl;
        }

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

            var widthValue = int.Parse(width, NumberStyles.Integer, _usCulture);
            var heightValue = int.Parse(height, NumberStyles.Integer, _usCulture);

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

        private void UpdateMediaInfo(uBaseObject? mediaInfo, TransportState state)
        {
            TransportState = state;

            var previousMediaInfo = CurrentMediaInfo;
            CurrentMediaInfo = mediaInfo;

            if (mediaInfo != null)
            {
                if (previousMediaInfo == null)
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
            else if (previousMediaInfo != null)
            {
                OnPlaybackStop(previousMediaInfo);
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

        private async Task<XDocument> SendCommandAsync(
            string baseUrl,
            DeviceService service,
            string command,
            string postData,
            string? header = null,
            CancellationToken cancellationToken = default)
        {
            var url = NormalizeServiceUrl(baseUrl, service.ControlUrl);
            using var response = await PostSoapDataAsync(
                url,
                $"\"{service.ServiceType}#{command}\"",
                postData,
                header,
                cancellationToken)
                .ConfigureAwait(false);
            using var stream = response.Content;
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return XDocument.Parse(await reader.ReadToEndAsync().ConfigureAwait(false), LoadOptions.PreserveWhitespace);
        }

        /// <summary>
        /// Checks to see if DLNA subscriptions are implemented, and if so subscribes to changes.
        /// </summary>
        /// <returns>Task.</returns>
        private async Task SubscribeAsync()
        {
            var avServices = GetAvTransportService();
            if (avServices.EventSubUrl != null)
            {
                var options = new HttpRequestOptions
                {
                    Url = NormalizeServiceUrl(Properties.BaseUrl, avServices.EventSubUrl),
                    UserAgent = USERAGENT,
                    LogErrorResponseBody = true,
                    BufferContent = false,
                };

                if (!_eventing)
                {
                    // Subscription.
                    if (string.IsNullOrEmpty(_sessionId))
                    {
                        _sessionId = Guid.NewGuid().ToString();
                    }

                    options.RequestHeaders["CALLBACK"] = $"<{_jellyfinUrl}/Dlna/Eventing/{_sessionId}>";
                    options.RequestHeaders["NT"] = "upnp:event";
                }
                else
                {
                    // Resubscription.
                    options.RequestHeaders["SID"] = "uuid:{_eventSid}";
                }

                options.RequestHeaders["TIMEOUT"] = "Second-10"; // TODO: get from into profile.

                using var response = await _httpClient.SendAsync(options, new HttpMethod("SUBSCRIBE")).ConfigureAwait(false);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    if (!_eventing)
                    {
                        _eventSid = response.Headers.GetValues("SID").FirstOrDefault();
                        _eventing = !string.IsNullOrEmpty(_eventSid);

                        // Add us into the events list.
                        _playToManager.DLNAEvents += ProcessEvent;
                    }
                }
            }
            else
            {
                _eventing = false;
            }
        }

        private async Task UnSubscribe()
        {
            if (_eventing)
            {
                var avServices = GetAvTransportService();
                if (avServices.EventSubUrl != null)
                {
                    var options = new HttpRequestOptions
                    {
                        Url = NormalizeServiceUrl(Properties.BaseUrl, avServices.EventSubUrl),
                        UserAgent = USERAGENT,
                        LogErrorResponseBody = true,
                        BufferContent = false,
                    };

                    options.RequestHeaders["SID"] = "uuid: {_eventSid}";
                    using var response = await _httpClient.SendAsync(options, new HttpMethod("UNSUBSCRIBE")).ConfigureAwait(false);
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        _eventSid = string.Empty;
                        _playToManager.DLNAEvents -= ProcessEvent;
                    }
                }

                _eventing = false;
            }
        }

        private void ProcessEvent(object sender, DlnaEventArgs args)
        {
            if (args.Id == _sessionId)
            {
                try
                {
                    var response = XDocument.Parse(args.Response);
                    _ = ProcessEventAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError("Unable to parse Subscription response {0}.", Device.FriendlyName);
                    _logger.LogDebug(ex, "Received: {0}", args.Response);
                }
            }
        }

        private async Task ProcessEventAsync()
        {
            await ProcessChange().ConfigureAwait(false);
            await SubscribeAsync().ConfigureAwait(false);
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

        private async Task<bool> RefreshVolume(CancellationToken cancellationToken = default)
        {
            if (_disposed)
            {
                return false;
            }

            var result = await GetVolume(cancellationToken).ConfigureAwait(false);
            if (result)
            {
                result = await GetMute(cancellationToken).ConfigureAwait(false);
            }

            return result;
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
                _timer?.Change(time, Timeout.Infinite);
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
                _timer?.Change(Timeout.Infinite, Timeout.Infinite);
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

        private async Task<XDocument?> SendCommandResponseRequired(
            int transportCommandsType,
            string actionCommand,
            CancellationToken cancellationToken,
            object? name = null,
            string? commandParameter = null,
            Dictionary<string, string>? dictionary = null,
            string? header = null)
        {
            TransportCommands? commands = null;
            ServiceAction? command = null;
            DeviceService? service = null;
            string? postData = string.Empty;

            if (transportCommandsType == TransportCommandsRender)
            {
                commands = await GetRenderingProtocolAsync(cancellationToken).ConfigureAwait(false);
                if (commands == null)
                {
                    _logger.LogWarning("GetRenderingProtocolAsync returned null.");
                    return null;
                }

                command = commands.ServiceActions.FirstOrDefault(c => c.Name == actionCommand);
                service = GetServiceRenderingControl();

                if (service == null || command == null)
                {
                    _logger.LogWarning("Command or service returned null.");
                    return null;
                }
            }
            else
            {
                commands = await GetAVProtocolAsync(cancellationToken).ConfigureAwait(false);
                if (commands == null)
                {
                    _logger.LogWarning("GetAVProtocolAsync returned null.");
                    return null;
                }

                command = commands.ServiceActions.FirstOrDefault(c => c.Name == actionCommand);
                service = GetAvTransportService();
                if (service == null || command == null)
                {
                    _logger.LogWarning("Command or service returned null.");
                    return null;
                }
            }

            if (commandParameter != null)
            {
                postData = commands.BuildPost(command, service.ServiceType, name, commandParameter);
            }
            else if (dictionary != null)
            {
                postData = commands.BuildPost(command, service.ServiceType, name, dictionary);
            }
            else if (name != null)
            {
                postData = commands.BuildPost(command, service.ServiceType, name);
            }
            else
            {
                postData = commands.BuildPost(command, service.ServiceType);
            }

            return await SendCommandAsync(Properties.BaseUrl, service, command.Name, postData, header).ConfigureAwait(false);
        }

        private async Task<bool> SendCommand(
            int transportCommandsType,
            string actionCommand,
            CancellationToken cancellationToken,
            object? name = null,
            string? commandParameter = null,
            Dictionary<string, string>? dictionary = null,
            string? header = null)
        {
            var result = await SendCommandResponseRequired(transportCommandsType, actionCommand, cancellationToken, name, commandParameter, dictionary, header).ConfigureAwait(false);
            var response = ParseResponse(result?.Document, actionCommand + "Response");

            if (response.TryGetValue(actionCommand + "Response", out string _))
            {
                return true;
            }

            _logger.LogDebug("Failed!");
            return false;
        }

        private async Task<bool> SetMute(bool mute, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Setting mute {0}.", mute);
            return await SendCommand(TransportCommandsRender, "SetMute", cancellationToken, mute ? 1 : 0).ConfigureAwait(false);
        }

        private string CreateDidlMeta(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return DescriptionXmlBuilder.Escape(value);
        }

        private async Task ProcessChange()
        {
            if (TransportState != TransportState.ERROR)
            {
                var cancellationToken = CancellationToken.None;

                // If we're not playing anything no need to get additional data
                if (TransportState == TransportState.STOPPED)
                {
                    UpdateMediaInfo(null, TransportState);
                }
                else
                {
                    var tuple = await GetPositionInfo(cancellationToken).ConfigureAwait(false);

                    var currentObject = tuple.Item2;

                    if (tuple.Item1 && currentObject == null)
                    {
                        currentObject = await GetMediaInfo(cancellationToken).ConfigureAwait(false);
                    }

                    if (currentObject != null)
                    {
                        UpdateMediaInfo(currentObject, TransportState);
                    }
                }
            }
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
                var transportState = await GetTransportInfo(cancellationToken).ConfigureAwait(false);
                if (_disposed)
                {
                    return;
                }

                if (transportState == TransportState.ERROR)
                {
                    _logger.LogError("Unable to get TransportState for {DeviceName}", Properties.Name);
                    // Assume it's a one off.
                    RestartTimer();
                }
                else
                {
                    _connectFailureCount = 0;

                    TransportState = transportState;

                    await ProcessChange().ConfigureAwait(false);

                    if (_disposed)
                    {
                        return;
                    }

                    // If we're not playing anything make sure we don't get data more often than neccessry to keep the Session alive
                    if (TransportState == TransportState.STOPPED)
                    {
                        RestartTimerInactive();
                    }
                    else
                    {
                        RestartTimer();
                    }
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

        /// <summary>
        /// Requests the volume setting from the client.
        /// </summary>
        /// <param name="cancellationToken">CancellationToken.</param>
        /// <returns>Task.</returns>
        private async Task<bool> GetVolume(CancellationToken cancellationToken)
        {
            if (_disposed)
            {
                return false;
            }

            var result = await SendCommandResponseRequired(TransportCommandsRender, "GetVolume", cancellationToken).ConfigureAwait(false);
            var response = ParseResponse(result?.Document, "GetVolumeResponse"); // CurrentVolumeResponse

            if (response.TryGetValue("CurrentVolume", out string volume))
            {
                Volume = int.Parse(volume, _usCulture);
                if (Volume > 0)
                {
                    _muteVol = Volume;
                }

                return true;
            }

            _logger.LogWarning("GetVolume Failed.");
            return false;
        }

        /// <summary>
        /// Gets the mute setting from the client.
        /// </summary>
        /// <param name="cancellationToken">CancellationToken.</param>
        /// <returns>Task.</returns>
        private async Task<bool> GetMute(CancellationToken cancellationToken)
        {
            if (_disposed)
            {
                return false;
            }

            var result = await SendCommandResponseRequired(TransportCommandsRender, "GetMute", cancellationToken).ConfigureAwait(false);
            var response = ParseResponse(result?.Document, "GetMuteResponse");

            if (response.TryGetValue("CurrentMute", out string muted))
            {
                IsMuted = string.Equals(muted, "1", StringComparison.OrdinalIgnoreCase);
                return true;
            }

            _logger.LogWarning("GetMute failed.");
            return false;
        }

        /// <summary>
        /// Returns information associated with the current transport state of the specified instance.
        /// </summary>
        /// <param name="cancellationToken">CancellationToken.</param>
        /// <returns>Task.</returns>
        private async Task<TransportState> GetTransportInfo(CancellationToken cancellationToken)
        {
            if (_disposed)
            {
                return TransportState.ERROR;
            }

            var result = await SendCommandResponseRequired(TransportCommandsAV, "GetTransportInfo", cancellationToken).ConfigureAwait(false);
            var response = ParseResponse(result?.Document, "GetTransportInfoResponse");

            if (response.TryGetValue("CurrentTransportState", out string transportState))
            {
                if (Enum.TryParse(transportState, true, out TransportState state))
                {
                    return state;
                }
            }

            _logger.LogWarning("GetTransportInfo failed.");
            return TransportState.ERROR;
        }

        private async Task<uBaseObject?> GetMediaInfo(CancellationToken cancellationToken)
        {
            if (_disposed)
            {
                return null;
            }

            var result = await SendCommandResponseRequired(TransportCommandsAV, "GetMediaInfo", cancellationToken).ConfigureAwait(false);
            if (result != null)
            {
                var track = result.Document.Descendants("CurrentURIMetaData").FirstOrDefault();
                var e = track?.Element(uPnpNamespaces.items) ?? track;
                if (!string.IsNullOrWhiteSpace(e?.Value))
                {
                    return UpnpContainer.Create(e);
                }

                track = result.Document.Descendants("CurrentURI").FirstOrDefault();
                e = track?.Element(uPnpNamespaces.items) ?? track;
                if (!string.IsNullOrWhiteSpace(e?.Value))
                {
                    return new uBaseObject
                    {
                        Url = e.Value
                    };
                }
            }
            else
            {
                _logger.LogWarning("GetMediaInfo failed.");
            }

            return null;
        }

        private async Task<(bool, uBaseObject?)> GetPositionInfo(CancellationToken cancellationToken)
        {
            if (_disposed)
            {
                return (false, null);
            }

            var result = await SendCommandResponseRequired(TransportCommandsAV, "GetPositionInfo", cancellationToken).ConfigureAwait(false);
            var response = ParseResponse(result?.Document, "GetPositionInfoResponse");

            if (response.Count == 0)
            {
                return (false, null);
            }

            response.TryGetValue("TrackDuration", out string duration);
            Duration = TimeSpan.TryParse(duration, _usCulture, out TimeSpan dur) ? dur : TimeSpan.Zero;

            response.TryGetValue("relTime", out string position);
            Position = TimeSpan.TryParse(position, _usCulture, out TimeSpan rel) ? rel : Position;

            if (!response.TryGetValue("TrackMetaData", out string track) || string.IsNullOrEmpty(track))
            {
                // If track is null, some vendors do this, use GetMediaInfo instead
                return (true, null);
            }

            XElement? uPnpResponse = ParseNodeResponse(track);
            if (uPnpResponse == null)
            {
                _logger.LogError("Failed to parse xml: \n {Xml}", track);
                return (true, null);
            }

            var e = uPnpResponse.Element(uPnpNamespaces.items);

            response.TryGetValue("TrackURI", out string trackUri);
            var uTrack = CreateUBaseObject(e, trackUri);

            return (true, uTrack);
        }

        /// <summary>
        /// Parses a response into a dictionary.
        /// </summary>
        /// <param name="document">Response to parse.</param>
        /// <param name="action">Action to extract.</param>
        /// <returns>Dictionary contains the arguments and values.</returns>
        private Dictionary<string, string> ParseResponse(XDocument? document, string action)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            if (document != null)
            {
                var nodes = document.Descendants(uPnpNamespaces.AvTransport + action);
                if (nodes != null)
                {
                    foreach (var node in nodes)
                    {
                        if (node.HasElements)
                        {
                            foreach (var childNode in node.Elements())
                            {
                                string value = childNode.Value;

                                if (string.IsNullOrWhiteSpace(value) && string.Equals(value, "NOT_IMPLEMENTED", StringComparison.OrdinalIgnoreCase))
                                {
                                    value = string.Empty;
                                }

                                result.Add(childNode.Name.LocalName, value);
                            }
                        }
                        else
                        {
                            string value = node.Value;

                            if (string.IsNullOrWhiteSpace(value) && string.Equals(value, "NOT_IMPLEMENTED", StringComparison.OrdinalIgnoreCase))
                            {
                                value = string.Empty;
                            }

                            result.Add(node.Name.LocalName, value);
                        }
                    }
                }
            }

            return result;
        }

        private XElement? ParseNodeResponse(string xml)
        {
            // Handle different variations sent back by devices
            try
            {
                return XElement.Parse(xml);
            }
            catch (XmlException)
            {
                // Wasn't this flavour.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Uncaught exception while parsing xml: method 1.");
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
                // Wasn't this flavour.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Uncaught exception while parsing xml: method 2.");
            }

            // some devices send back invalid xml
            try
            {
                return XElement.Parse(xml.Replace("&", "&amp;", StringComparison.OrdinalIgnoreCase));
            }
            catch (XmlException)
            {
                // Wasn't this flavour.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Uncaught exception while parsing xml: method 3.");
            }

            return null;
        }

        private async Task<TransportCommands?> GetAVProtocolAsync(CancellationToken cancellationToken)
        {
            if (AvCommands != null)
            {
                return AvCommands;
            }

            if (_disposed)
            {
                return null;
            }

            var avService = GetAvTransportService();
            if (avService == null)
            {
                return null;
            }

            string url = NormalizeUrl(Properties.BaseUrl, avService.ScpdUrl);

            var document = await GetDataAsync(_httpClient, url, cancellationToken).ConfigureAwait(false);
            AvCommands = TransportCommands.Create(document);
            return AvCommands;
        }

        private async Task<TransportCommands?> GetRenderingProtocolAsync(CancellationToken cancellationToken)
        {
            if (RendererCommands != null)
            {
                return RendererCommands;
            }

            if (_disposed)
            {
                return null;
            }

            var avService = GetServiceRenderingControl();
            if (avService == null)
            {
                return null;
            }

            string url = NormalizeUrl(Properties.BaseUrl, avService.ScpdUrl);

            _logger.LogDebug("Dlna Device.GetRenderingProtocolAsync");
            var document = await GetDataAsync(_httpClient, url, cancellationToken).ConfigureAwait(false);

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

            if (!url.Contains("/", StringComparison.OrdinalIgnoreCase))
            {
                url = "/dmr/" + url;
            }

            if (!url.StartsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                url = "/" + url;
            }

            return baseUrl + url;
        }

        private Task<HttpResponseInfo> PostSoapDataAsync(
            string url,
            string soapAction,
            string postData,
            string? header,
            CancellationToken cancellationToken)
        {
            if (soapAction[0] != '\"')
            {
                soapAction = $"\"{soapAction}\"";
            }

            var options = new HttpRequestOptions
            {
                Url = url,
                UserAgent = USERAGENT,
                LogErrorResponseBody = true,
                BufferContent = false,

                CancellationToken = cancellationToken
            };

            options.RequestHeaders["SOAPAction"] = soapAction;
            options.RequestHeaders["Pragma"] = "no-cache";
            options.RequestHeaders["FriendlyName.DLNA.ORG"] = FriendlyName;

            if (!string.IsNullOrEmpty(header))
            {
                options.RequestHeaders["contentFeatures.dlna.org"] = header;
            }

            options.RequestContentType = "text/xml";
            options.RequestContent = postData;

            return _httpClient.Post(options);
        }
    }
}
