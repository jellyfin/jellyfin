#pragma warning disable CS1591
#nullable enable

namespace Emby.Dlna.PlayTo
{
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
    using MediaBrowser.Model.Net;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Defines the <see cref="Device" />.
    /// </summary>
    public class Device : IDisposable
    {
        /// <summary>
        /// Defines the USERAGENT.
        /// </summary>
        private const string USERAGENT = "Microsoft-Windows/6.2 UPnP/1.0 Microsoft-DLNA DLNADOC/1.50";

        /// <summary>
        /// Defines the FriendlyName.
        /// </summary>
        private const string FriendlyName = "Jellyfin";

        /// <summary>
        /// Constants used in SendCommand.
        /// </summary>
        private const int TransportCommandsAV = 1;
        private const int TransportCommandsRender = 2;

        private const int Now = 1;
        private const int Never = 0;
        private const int Normal = -1;

        private static readonly CultureInfo _usCulture = new CultureInfo("en-US");

        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly PlayToManager _playToManager;
        private readonly object _timerLock = new object();
        private readonly object _queueLock = new object();

        /// <summary>
        /// Holds the URL for the Jellyfin web server.
        /// </summary>
        private readonly string _jellyfinUrl;

        /// <summary>
        /// Outbound events processing queue.
        /// </summary>
        private readonly List<KeyValuePair<string, object>> _queue = new List<KeyValuePair<string, object>>();

        private bool _disposed;
        private Timer? _timer;

        /// <summary>
        /// Connection failure retry counter.
        /// </summary>
        private int _connectFailureCount;

        /// <summary>
        /// Sound level prior to it being muted.
        /// </summary>
        private int _muteVol;

        /// <summary>
        /// True if this player is using subscription events.
        /// </summary>
        private bool _subscribed;

        /// <summary>
        /// Unique id used in subscription callbacks.
        /// </summary>
        private string? _sessionId;

        /// <summary>
        /// Transport service subscription SID value.
        /// </summary>
        private string? _transportSid;

        /// <summary>
        /// Render service subscription SID value.
        /// </summary>
        private string? _renderSid;

        /// <summary>
        /// Used by the volume control to stop DOS on volume queries.
        /// </summary>
        private DateTime _lastVolumeRefresh;
        private bool _volumeRefreshActive;
        private int _volume;

        /// <summary>
        /// Contains the item currently playing.
        /// </summary>
        private string _mediaPlaying = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="Device"/> class.
        /// </summary>
        /// <param name="playToManager">The playToManager<see cref="PlayToManager"/>.</param>
        /// <param name="deviceProperties">The deviceProperties<see cref="DeviceInfo"/>.</param>
        /// <param name="httpClient">The httpClient<see cref="IHttpClient"/>.</param>
        /// <param name="logger">The logger<see cref="ILogger"/>.</param>
        /// <param name="webUrl">The webUrl.</param>
        public Device(PlayToManager playToManager, DeviceInfo deviceProperties, IHttpClient httpClient, ILogger logger, string webUrl)
        {
            Properties = deviceProperties;
            _httpClient = httpClient;
            _logger = logger;
            TransportState = TransportState.NO_MEDIA_PRESENT;
            _jellyfinUrl = webUrl;
            _playToManager = playToManager;
        }

        /// <summary>
        /// Events called when playback starts.
        /// </summary>
        public event EventHandler<PlaybackStartEventArgs>? PlaybackStart;

        /// <summary>
        /// Events called during playback.
        /// </summary>
        public event EventHandler<PlaybackProgressEventArgs>? PlaybackProgress;

        /// <summary>
        /// Events called when playback stops.
        /// </summary>
        public event EventHandler<PlaybackStoppedEventArgs>? PlaybackStopped;

        /// <summary>
        /// Events called when the media changes.
        /// </summary>
        public event EventHandler<MediaChangedEventArgs>? MediaChanged;

        /// <summary>
        /// Gets or sets the device's properties.
        /// </summary>
        public DeviceInfo Properties { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the sound is muted.
        /// </summary>
        public bool IsMuted { get; set; }

        /// <summary>
        /// Gets the current media information.
        /// </summary>
        public uBaseObject? CurrentMediaInfo { get; private set; }

        /// <summary>
        /// Gets the Volume.
        /// </summary>
        public int Volume
        {
            get
            {
                try
                {
                    // _logger.LogDebug("Volume first.");
                    // RefreshVolumeIfNeeded().GetAwaiter().GetResult();
                }
                catch (ObjectDisposedException)
                {
                    // Ignore.
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating device volume info for {DeviceName}", Properties.Name);
                }

                return _volume;
            }

            internal set => _volume = value;
        }

        /// <summary>
        /// Gets or sets the Duration.
        /// </summary>
        public TimeSpan? Duration { get; set; }

        /// <summary>
        /// Gets or sets the Position.
        /// </summary>
        public TimeSpan Position { get; set; } = TimeSpan.FromSeconds(0);

        /// <summary>
        /// Gets the TransportState.
        /// </summary>
        public TransportState TransportState { get; private set; }

        /// <summary>
        /// Gets a value indicating whether IsPlaying.
        /// </summary>
        public bool IsPlaying => TransportState == TransportState.PLAYING;

        /// <summary>
        /// Gets a value indicating whether IsPaused.
        /// </summary>
        public bool IsPaused => TransportState == TransportState.PAUSED || TransportState == TransportState.PAUSED_PLAYBACK;

        /// <summary>
        /// Gets a value indicating whether IsStopped.
        /// </summary>
        public bool IsStopped => TransportState == TransportState.STOPPED;

        /// <summary>
        /// Gets or sets the OnDeviceUnavailable.
        /// </summary>
        public Action? OnDeviceUnavailable { get; set; }

        /// <summary>
        /// Gets or sets the AvCommands.
        /// </summary>
        private TransportCommands? AvCommands { get; set; }

        /// <summary>
        /// Gets or sets the RendererCommands.
        /// </summary>
        private TransportCommands? RendererCommands { get; set; }

        /// <summary>
        /// The CreateuPnpDeviceAsync.
        /// </summary>
        /// <param name="playToManager">The playToManager<see cref="PlayToManager"/>.</param>
        /// <param name="url">The url<see cref="Uri"/>.</param>
        /// <param name="httpClient">The httpClient<see cref="IHttpClient"/>.</param>
        /// <param name="logger">The logger<see cref="ILogger"/>.</param>
        /// <param name="serverUrl">The serverUrl.</param>
        /// <param name="cancellationToken">The cancellationToken.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        public static async Task<Device?> CreateuPnpDeviceAsync(
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

            try
            {
                var document = await GetDataAsync(httpClient, url.ToString(), logger, cancellationToken).ConfigureAwait(false);
                // var data = ParseResponse(document, "");
                if (document == null)
                {
                    return null;
                }

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
#pragma warning disable CA1031 // Do not catch general exception types : Don't let our errors affect our owners.
            catch
#pragma warning restore CA1031 // Do not catch general exception types
            {
                return null;
            }
        }

        /// <summary>
        /// Starts the monitoring of the device.
        /// </summary>
        /// <returns>Task.</returns>
        public async Task DeviceInitialise()
        {
            if (_timer == null)
            {
                _logger.LogDebug("Starting timer.");
                _timer = new Timer(TimerCallback, null, 1000, Timeout.Infinite);
                try
                {
                    _lastVolumeRefresh = DateTime.UtcNow;

                    // Refresh the volume.
                    await GetPositionRequest().ConfigureAwait(false); // TODO: do we need the other work around?
                    await RefreshVolumeIfNeeded().ConfigureAwait(false);
                    await SubscribeAsync().ConfigureAwait(false);
                }
                catch (ObjectDisposedException)
                {
                    // Ignore.
                }
                catch (HttpException)
                {
                    // Ignore.
                }

                // Start the queue processor.
                await ProcessQueue().ConfigureAwait(false);
            }
        }

        public async Task DeviceUnavailable()
        {
            if (_subscribed)
            {
                _logger.LogDebug("Killing the timer.");

                await UnSubscribeAsync().ConfigureAwait(false);

                lock (_timerLock)
                {
                    _timer?.Dispose();
                    _timer = null;
                }
            }
        }

        /// <summary>
        /// Enables the volume bar hovered over effect.
        /// </summary>
        /// <returns>Task.</returns>
        private async Task RefreshVolumeIfNeeded()
        {
            if (_volumeRefreshActive && DateTime.UtcNow >= _lastVolumeRefresh.AddSeconds(5))
            {
                _lastVolumeRefresh = DateTime.UtcNow;
                try
                {
                    await GetVolume().ConfigureAwait(false);
                    await GetMute().ConfigureAwait(false);
                }
                catch (ObjectDisposedException)
                {
                    // Ignore.
                }
            }
        }

        /// <summary>
        /// Restart the polling timer.
        /// </summary>
        /// <param name="when">When to restart the timer. Less than 0 = never, 0 = instantly, greater than 0 in 1 second.</param>
        private void RestartTimer(int when)
        {
            lock (_timerLock)
            {
                if (_disposed)
                {
                    return;
                }

                _volumeRefreshActive = when != Never;

                int delay = when == Never ? Timeout.Infinite : when == Now ? 100 : 10000;
                _timer?.Change(delay, Timeout.Infinite);
            }
        }

        /// <summary>
        /// Adds an event to the queue.
        /// </summary>
        /// <param name="command">Command to queue.</param>
        /// <param name="value">Command parameter.</param>
        private void QueueEvent(string command, object? value = null)
        {
            lock (_queueLock)
            {
                // Does this action exist in the queue ?
                int index = _queue.FindIndex(item => string.Equals(item.Key, command, StringComparison.OrdinalIgnoreCase));

                if (index != -1)
                {
                    _logger.LogDebug("Replacing user event: {0} {1}", command, value);
                    _queue.RemoveAt(index);
                }
                else
                {
                    _logger.LogDebug("Queuing user event: {0} {1}", command, value);
                }

                _queue.Add(new KeyValuePair<string, object>(command, value ?? 0));
            }
        }

        /// <summary>
        /// Removes an event from the queue.
        /// </summary>
        private void AddOrCancelIfQueued(string command)
        {
            lock (_queueLock)
            {
                int index = _queue.FindIndex(item => string.Equals(item.Key, command, StringComparison.OrdinalIgnoreCase));

                if (index != -1)
                {
                    _logger.LogDebug("Cancelling user event: {0}", command);
                    _queue.RemoveAt(index);
                }
                else
                {
                    _logger.LogDebug("Queuing user event: {0}", command);
                    _queue.Add(new KeyValuePair<string, object>(command, 0));
                }
            }
        }

        private async Task ProcessQueue()
        {
            while (!_disposed)
            {
                while (_queue.Count > 0)
                {
                    // Ensure we are subscribed.
                    await SubscribeAsync().ConfigureAwait(false);

                    KeyValuePair<string, object> action;

                    lock (_queueLock)
                    {
                        action = _queue[0];
                        _queue.RemoveAt(0);
                    }

                    try
                    {
                        _logger.LogDebug("Attempting action : {0}", action.Key);

                        switch (action.Key)
                        {
                            case "SetVolume":
                                {
                                    await SendVolumeRequest((int)action.Value).ConfigureAwait(false);

                                    break;
                                }

                            case "ToggleMute":
                                {
                                    if ((int)action.Value == 1)
                                    {
                                        var success = await SendMuteRequest(false).ConfigureAwait(true);
                                        if (!success)
                                        {
                                            var sendVolume = _muteVol <= 0 ? 20 : _muteVol;
                                            await SendVolumeRequest(sendVolume).ConfigureAwait(false);
                                        }
                                    }
                                    else
                                    {
                                        var success = await SendMuteRequest(true).ConfigureAwait(true);
                                        if (!success)
                                        {
                                            await SendVolumeRequest(0).ConfigureAwait(false);
                                        }
                                    }

                                    break;
                                }

                            case "Play":
                                {
                                    await SendPlayRequest().ConfigureAwait(false);
                                    break;
                                }

                            case "Stop":
                                {
                                    await SendStopRequest().ConfigureAwait(false);
                                    break;
                                }

                            case "Pause":
                                {
                                    await SendPauseRequest().ConfigureAwait(false);
                                    break;
                                }

                            case "Mute":
                                {
                                    var success = await SendMuteRequest(true).ConfigureAwait(true);
                                    if (!success)
                                    {
                                        await SendVolumeRequest(0).ConfigureAwait(false);
                                    }
                                }

                                break;

                            case "Unmute":
                                {
                                    var success = await SendMuteRequest(false).ConfigureAwait(true);
                                    if (!success)
                                    {
                                        var sendVolume = _muteVol <= 0 ? 20 : _muteVol;
                                        await SendVolumeRequest(sendVolume).ConfigureAwait(false);
                                    }

                                    break;
                                }

                            case "Seek":
                                {
                                    await SendSeekRequest((TimeSpan)action.Value).ConfigureAwait(false);
                                    break;
                                }

                            case "Queue":
                                {
                                    var settings = (Tuple<string, string, string>)action.Value;
                                    bool success = true;

                                    if (IsPlaying)
                                    {
                                        // Has user requested a media change?
                                        if (_mediaPlaying != settings.Item1)
                                        {
                                            _logger.LogDebug("Stopping current playback.");
                                            success = await SendStopRequest().ConfigureAwait(false);

                                            if (success)
                                            {
                                                TransportState = TransportState.TRANSITIONING;
                                                UpdateMediaInfo(null);
                                            }

                                            success = true; // Attempt to load up next track. May work on some systems.
                                        }
                                        else
                                        {
                                            // Restart from the beginning.
                                            _logger.LogDebug("Restarting playback.");
                                            success = await SendSeekRequest(TimeSpan.Zero).ConfigureAwait(false);
                                            if (success)
                                            {
                                                UpdateMediaInfo(CurrentMediaInfo);
                                                RestartTimer(Normal);
                                                success = false; // Nothing to do here.
                                            }
                                        }
                                    }

                                    if (success)
                                    {
                                        await SendMediaRequest(settings).ConfigureAwait(false);
                                    }

                                    break;
                                }
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        return;
                    }
                    catch (HttpException)
                    {
                        // Ignore.
                    }
                }

                await Task.Delay(1000).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Sends a command to the DLNA device.
        /// </summary>
        /// <param name="baseUrl">baseUrl to use..</param>
        /// <param name="service">Service to use.<see cref="DeviceService"/>.</param>
        /// <param name="command">Command to send..</param>
        /// <param name="postData">Information to post..</param>
        /// <param name="header">Headers to include..</param>
        /// <param name="cancellationToken">The cancellationToken.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        private async Task<XDocument?> SendCommandAsync(
            string baseUrl,
            DeviceService service,
            string command,
            string postData,
            string? header = null,
            CancellationToken cancellationToken = default)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(string.Empty);
            }

            var url = NormalizeUrl(baseUrl, service.ControlUrl);
            try
            {
                var options = new HttpRequestOptions
                {
                    Url = url,
                    UserAgent = USERAGENT,
                    LogErrorResponseBody = true,
                    BufferContent = false,

                    CancellationToken = cancellationToken
                };

                options.RequestHeaders["SOAPAction"] = $"\"{service.ServiceType}#{command}\"";
                options.RequestHeaders["Pragma"] = "no-cache";
                options.RequestHeaders["FriendlyName.DLNA.ORG"] = FriendlyName;

                if (!string.IsNullOrEmpty(header))
                {
                    options.RequestHeaders["contentFeatures.dlna.org"] = header;
                }

                options.RequestContentType = "text/xml";
                options.RequestContent = postData;

                HttpResponseInfo response = await _httpClient.Post(options).ConfigureAwait(true);

                using var stream = response.Content;
                using var reader = new StreamReader(stream, Encoding.UTF8);
                return XDocument.Parse(await reader.ReadToEndAsync().ConfigureAwait(false), LoadOptions.PreserveWhitespace);
            }
            catch (HttpException ex)
            {
                _logger.LogError("SendCommandAsync failed with {0} to {1} ", ex.ToString(), url);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError("SendCommandAsync failed with {0} to {1} ", ex.ToString(), url);
            }

            return null;
        }

        /// <summary>
        /// Sends a command to the device and waits for a response.
        /// </summary>
        /// <param name="transportCommandsType">The transport type.</param>
        /// <param name="actionCommand">The actionCommand.</param>
        /// <param name="cancellationToken">The cancellationToken.</param>
        /// <param name="name">The name.</param>
        /// <param name="commandParameter">The commandParameter.</param>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="header">The header.</param>
        /// <returns>The <see cref="Task{XDocument}"/>.</returns>
        private async Task<XDocument?> SendCommandResponseRequired(
            int transportCommandsType,
            string actionCommand,
            CancellationToken cancellationToken,
            object? name = null,
            string? commandParameter = null,
            Dictionary<string, string>? dictionary = null,
            string? header = null)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(string.Empty);
            }

            TransportCommands? commands;
            ServiceAction? command;
            DeviceService? service;
            string? postData = string.Empty;

            if (transportCommandsType == TransportCommandsRender)
            {
                service = GetServiceRenderingControl();

                RendererCommands ??= await GetProtocolAsync(service, cancellationToken).ConfigureAwait(false);
                if (RendererCommands == null)
                {
                    _logger.LogWarning("GetRenderingProtocolAsync returned null.");
                    return null;
                }

                command = RendererCommands.ServiceActions.FirstOrDefault(c => c.Name == actionCommand);
                if (service == null || command == null)
                {
                    _logger.LogWarning("Command or service returned null.");
                    return null;
                }

                commands = RendererCommands;
            }
            else
            {
                service = GetAvTransportService();
                AvCommands ??= await GetProtocolAsync(service, cancellationToken).ConfigureAwait(false);
                if (AvCommands == null)
                {
                    _logger.LogWarning("GetAVProtocolAsync returned null.");
                    return null;
                }

                command = AvCommands.ServiceActions.FirstOrDefault(c => c.Name == actionCommand);
                if (service == null || command == null)
                {
                    _logger.LogWarning("Command or service returned null.");
                    return null;
                }

                commands = AvCommands;
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

        /// <summary>
        /// Sends a command to the device, verifies receipt, and does return a response.
        /// </summary>
        /// <param name="transportCommandsType">The transport commands type.</param>
        /// <param name="actionCommand">The action command to use.</param>
        /// <param name="cancellationToken">The cancellationToken.</param>
        /// <param name="name">The name.</param>
        /// <param name="commandParameter">The command parameter.</param>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="header">The header.</param>
        /// <returns>Returns success of the task.</returns>
        private async Task<bool> SendCommand(
            int transportCommandsType,
            string actionCommand,
            CancellationToken cancellationToken,
            object? name = null,
            string? commandParameter = null,
            Dictionary<string, string>? dictionary = null,
            string? header = null)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(string.Empty);
            }

            var result = await SendCommandResponseRequired(transportCommandsType, actionCommand, cancellationToken, name, commandParameter, dictionary, header).ConfigureAwait(false);
            var response = ParseResponse(result?.Document, actionCommand + "Response");

            if (response.TryGetValue(actionCommand + "Response", out string _))
            {
                return true;
            }

            _logger.LogWarning("Sending {0} Failed!", actionCommand);
            return false;
        }

        /// <summary>
        /// Checks to see if DLNA subscriptions are implemented, and if so subscribes to changes.
        /// </summary>
        /// <param name="service">The service<see cref="DeviceService"/>.</param>
        /// <param name="sid">The sid.</param>
        /// <returns>Task.</returns>
        private async Task<string> SubscribeInternalAsync(DeviceService service, string? sid)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(string.Empty);
            }

            if (service.EventSubUrl != null)
            {
                var options = new HttpRequestOptions
                {
                    Url = NormalizeUrl(Properties.BaseUrl, service.EventSubUrl),
                    UserAgent = USERAGENT,
                    LogErrorResponseBody = true,
                    BufferContent = false,
                };

                if (string.IsNullOrEmpty(sid))
                {
                    if (string.IsNullOrEmpty(_sessionId))
                    {
                        // If we haven't got a GUID yet - get one.
                        _sessionId = Guid.NewGuid().ToString();
                    }

                    // Create a unique callback url based up our GUID.
                    options.RequestHeaders["CALLBACK"] = $"<{_jellyfinUrl}/Dlna/Eventing/{_sessionId}>";
                }
                else
                {
                    // Resubscription id.
                    options.RequestHeaders["SID"] = "uuid:{sid}";
                }

                options.RequestHeaders["NT"] = "upnp:event";
                options.RequestHeaders["TIMEOUT"] = "Second-30"; // Wait 5 seconds before timing out.
                // TODO: check what happens at timeout.

                try
                {
                    using var response = await _httpClient.SendAsync(options, new HttpMethod("SUBSCRIBE")).ConfigureAwait(false);
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        if (!_subscribed)
                        {
                            return response.Headers.GetValues("SID").FirstOrDefault();
                        }
                    }
                }
                catch (HttpException ex)
                {
                    _logger.LogError(ex, "SUBSCRIBE failed.");
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Attempts to subscribe to the multiple services of a DLNA client.
        /// </summary>
        /// <returns>The <see cref="Task"/>.</returns>
        private async Task SubscribeAsync()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(string.Empty);
            }

            if (!_subscribed)
            {
                try
                {
                    // Start listening to DLNA events.
                    _playToManager.DLNAEvents += ProcessSubscriptionEvent;

                    _transportSid = await SubscribeInternalAsync(GetAvTransportService(), _transportSid).ConfigureAwait(false);

                    _logger.LogDebug("AVTransport SID {0}.", _transportSid);

                    _renderSid = await SubscribeInternalAsync(GetServiceRenderingControl(), _renderSid).ConfigureAwait(false);
                    _logger.LogDebug("RenderControl SID {0}.", _renderSid);

                    _subscribed = true;
                }
                catch (ObjectDisposedException)
                {
                    // Ignore.
                }
            }
        }

        /// <summary>
        /// Resubscribe to DLNA evemts.
        /// </summary>
        /// <returns>The <see cref="Task"/>.</returns>
        private async Task ResubscribeToEvents()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(string.Empty);
            }

            await SubscribeAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Attempts to unsubscribe from a DLNA client.
        /// </summary>
        /// <param name="service">The service<see cref="DeviceService"/>.</param>
        /// <param name="sid">The sid.</param>
        /// <returns>Returns success of the task.</returns>
        private async Task<bool> UnSubscribeInternalAsync(DeviceService service, string? sid)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(string.Empty);
            }

            if (service?.EventSubUrl != null || string.IsNullOrEmpty(sid))
            {
                var options = new HttpRequestOptions
                {
#pragma warning disable CS8602 // Dereference of a possibly null reference. Erroneous warning: service cannot be null here.
                    Url = NormalizeUrl(Properties.BaseUrl, service.EventSubUrl),
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                    UserAgent = USERAGENT,
                    LogErrorResponseBody = true,
                    BufferContent = false,
                };

                options.RequestHeaders["SID"] = "uuid: {sid}";
                try
                {
                    using var response = await _httpClient.SendAsync(options, new HttpMethod("UNSUBSCRIBE")).ConfigureAwait(false);
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        _logger.LogDebug("UNSUBSCRIBE succeeded.");
                        return true;
                    }
                }
                catch (HttpException ex)
                {
                    _logger.LogError(ex, "UNSUBSCRIBE failed.");
                }
            }

            return false;
        }

        /// <summary>
        /// Attempts to unsubscribe from the multiple services of the DLNA client.
        /// </summary>
        /// <returns>The <see cref="Task"/>.</returns>
        private async Task UnSubscribeAsync()
        {
            if (_subscribed)
            {
                try
                {
                    // stop processing events.
                    _playToManager.DLNAEvents -= ProcessSubscriptionEvent;

                    var success = await UnSubscribeInternalAsync(GetAvTransportService(), _transportSid).ConfigureAwait(false);
                    if (success)
                    {
                        // Keep Sid in case the user interacts with this device.
                        _transportSid = string.Empty;
                    }

                    success = await UnSubscribeInternalAsync(GetServiceRenderingControl(), _renderSid).ConfigureAwait(false);
                    if (success)
                    {
                        _renderSid = string.Empty;
                    }

                    // TODO: should we attempt to unsubscribe again if they fail?

                    _subscribed = false;
                }
                catch (ObjectDisposedException)
                {
                    // Ignore.
                }
            }
        }

        /// <summary>
        /// This method gets called with the information the DLNA clients have passed through eventing.
        /// </summary>
        /// <param name="sender">PlayToController object.</param>
        /// <param name="args">Arguments passed from DLNA player.</param>
        private async void ProcessSubscriptionEvent(object sender, DlnaEventArgs args)
        {
            if (args.Id == _sessionId)
            {
                try
                {
                    var response = XDocument.Parse(System.Web.HttpUtility.HtmlDecode(args.Response));
                    if (response != null)
                    {
                        var reply = ParseResponse(response, "InstanceID");
                        _logger.LogDebug("Processing a subscription event.");

                        if (reply.TryGetValue("Mute", out string value) && int.TryParse(value, out int mute))
                        {
                            _logger.LogDebug("Muted: {0}", mute);
                            IsMuted = mute == 1;
                        }

                        if (reply.TryGetValue("Volume", out value) && int.TryParse(value, out int volume))
                        {
                            _logger.LogDebug("Volume: {0}", volume);
                            Volume = volume;
                        }

                        if (reply.TryGetValue("CurrentTrackDuration", out value) && TimeSpan.TryParse(value, _usCulture, out TimeSpan dur))
                        {
                            _logger.LogDebug("CurrentTrackDuration: {0}", dur);
                            Duration = dur;
                        }

                        if (reply.TryGetValue("RelativeTimePosition", out value) && TimeSpan.TryParse(value, _usCulture, out TimeSpan rel))
                        {
                            _logger.LogDebug("RelativeTimePosition: {0}", rel);
                            Position = rel;
                        }

                        // If this is an AVTransport event and the position isn't in this update, try to get it.
                        if (reply.TryGetValue("AVTransportURI", out value) && !reply.ContainsKey("RelativeTimePosition"))
                        {
                            _logger.LogDebug("Updating position as not included.");
                            // Try and get the latest position update
                            await GetPositionRequest().ConfigureAwait(false);
                        }

                        if (reply.TryGetValue("TransportState", out value) && Enum.TryParse(value, true, out TransportState ts))
                        {
                            _logger.LogDebug("TransportState: {0}", ts);
                            // Mustn't process our own change playback event.
                            if (ts != TransportState && TransportState != TransportState.TRANSITIONING)
                            {
                                TransportState = ts;

                                if (ts == TransportState.STOPPED)
                                {
                                    UpdateMediaInfo(null);
                                    RestartTimer(Normal);
                                }
                            }
                        }

                        if (reply.TryGetValue("CurrentTrackMetaData", out value) && !string.IsNullOrEmpty(value))
                        {
                            XElement? uPnpResponse = ParseNodeResponse(value);
                            var e = uPnpResponse?.Element(uPnpNamespaces.items);

                            if (reply.TryGetValue("CurrentTrackURI", out value) && !string.IsNullOrEmpty(value))
                            {
                                uBaseObject? uTrack = CreateUBaseObject(e, value);

                                if (uTrack != null)
                                {
                                    UpdateMediaInfo(uTrack);
                                    RestartTimer(Normal);
                                }
                            }
                        }

                        _ = ResubscribeToEvents();
                    }
                    else
                    {
                        _logger.LogDebug("Received blank event data : ");
                    }
                }
                catch (ObjectDisposedException)
                {
                    // Ignore.
                }
                catch (HttpException ex)
                {
                    _logger.LogError("Unable to parse event response.");
                    _logger.LogDebug(ex, "Received: {0}", args.Response);
                }
            }
        }

        /// <summary>
        /// Timer Callback function that polls the DLNA status.
        /// </summary>
        /// <param name="sender">The sender.</param>
        private async void TimerCallback(object sender)
        {
            if (_disposed)
            {
                return;
            }

            _logger.LogDebug("Timer event");
            try
            {
                var cancellationToken = CancellationToken.None;
                var transportState = await GetTransportStatus().ConfigureAwait(false);

                if (transportState == TransportState.ERROR)
                {
                    _logger.LogError("Unable to get TransportState for {DeviceName}", Properties.Name);
                    // Assume it's a one off.
                    RestartTimer(Normal);
                }
                else
                {
                    TransportState = transportState;

                    if (transportState != TransportState.ERROR)
                    {
                        // If we're not playing anything make sure we don't get data more often than neccessry to keep the Session alive
                        if (transportState == TransportState.STOPPED)
                        {
                            UpdateMediaInfo(null);
                            RestartTimer(Never);
                        }
                        else
                        {
                            var result = await SendCommandResponseRequired(TransportCommandsAV, "GetPositionInfo", cancellationToken).ConfigureAwait(false);
                            var response = ParseResponse(result?.Document, "GetPositionInfoResponse");
                            if (response.Count == 0)
                            {
                                RestartTimer(Normal);
                                return;
                            }

                            if (response.TryGetValue("TrackDuration", out string duration) && TimeSpan.TryParse(duration, _usCulture, out TimeSpan dur))
                            {
                                Duration = dur;
                            }

                            if (response.TryGetValue("relTime", out string position) && TimeSpan.TryParse(position, _usCulture, out TimeSpan rel))
                            {
                                Position = rel;
                            }

                            uBaseObject? currentObject = null;
                            if (!response.TryGetValue("TrackMetaData", out string track) || string.IsNullOrEmpty(track))
                            {
                                // If track is null, some vendors do this, use GetMediaInfo instead
                                currentObject = await GetMediaInfo(cancellationToken).ConfigureAwait(false);
                            }

                            XElement? uPnpResponse = ParseNodeResponse(track);
                            if (uPnpResponse == null)
                            {
                                _logger.LogError("Failed to parse xml: \n {Xml}", track);
                                currentObject = await GetMediaInfo(cancellationToken).ConfigureAwait(false);
                            }

                            if (currentObject == null)
                            {
                                var e = uPnpResponse?.Element(uPnpNamespaces.items);
                                if (response.TryGetValue("TrackURI", out string trackUri))
                                {
                                    currentObject = CreateUBaseObject(e, trackUri);
                                }
                            }

                            if (currentObject != null)
                            {
                                UpdateMediaInfo(currentObject);
                            }

                            RestartTimer(Normal);
                        }
                    }

                    _connectFailureCount = 0;
                }
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (HttpException ex)
            {
                if (_disposed)
                {
                    return;
                }

                _logger.LogError(ex, "Error updating device info for {DeviceName}", Properties.Name);
                if (_connectFailureCount++ >= 3)
                {
                    _logger.LogDebug("Disposing device due to loss of connection");
                    OnDeviceUnavailable?.Invoke();
                    return;
                }

                RestartTimer(Normal);
            }
        }

        private async Task<bool> SendVolumeRequest(int value)
        {
            var result = false;
            if (Volume != value)
            {
                result = await SendCommand(TransportCommandsRender, "SetVolume", default, value).ConfigureAwait(false);
                if (result)
                {
                    Volume = value;
                }
            }

            return result;
        }

        /// <summary>
        /// Requests the volume setting from the client.
        /// </summary>
        /// <returns>Task.</returns>
        private async Task<bool> GetVolume()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(string.Empty);
            }

            try
            {
                var result = await SendCommandResponseRequired(TransportCommandsRender, "GetVolume", default).ConfigureAwait(false);
                var response = ParseResponse(result?.Document, "GetVolumeResponse");

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
            }
            catch (ObjectDisposedException)
            {
                // Ignore.
            }

            return false;
        }

        private async Task<bool> SendPauseRequest()
        {
            var result = false;
            if (!IsPaused)
            {
                result = await SendCommand(TransportCommandsAV, "Pause", default, 1).ConfigureAwait(false);
                if (result)
                {
                    // Stop user from issuing multiple commands.
                    TransportState = TransportState.PAUSED;
                    RestartTimer(Now);
                }
            }

            return result;
        }

        private async Task<bool> SendPlayRequest()
        {
            var result = false;
            if (!IsPlaying)
            {
                result = await SendCommand(TransportCommandsAV, "Play", default, 1).ConfigureAwait(false);
                if (result)
                {
                    // Stop user from issuing multiple commands.
                    TransportState = TransportState.PLAYING;
                    RestartTimer(Now);
                }
            }

            return result;
        }

        private async Task<bool> SendStopRequest()
        {
            var result = false;
            if (IsPlaying || IsPaused)
            {
                result = await SendCommand(TransportCommandsAV, "Stop", default, 1).ConfigureAwait(false);
                if (result)
                {
                    // Stop user from issuing multiple commands.
                    TransportState = TransportState.STOPPED;
                    RestartTimer(Now);
                }
            }

            return result;
        }

        /// <summary>
        /// Returns information associated with the current transport state of the specified instance.
        /// </summary>
        /// <returns>Task.</returns>
        private async Task<TransportState> GetTransportStatus()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(string.Empty);
            }

            var result = await SendCommandResponseRequired(TransportCommandsAV, "GetTransportInfo", default).ConfigureAwait(false);
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

        private async Task<TransportState> GetPositionStatus()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(string.Empty);
            }

            var result = await SendCommandResponseRequired(TransportCommandsAV, "GetTransportInfo", default).ConfigureAwait(false);
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

        private async Task<bool> SendMuteRequest(bool value)
        {
            var result = false;

            if (IsMuted != value)
            {
                result = await SendCommand(TransportCommandsRender, "SetMute", default, value ? 1 : 0).ConfigureAwait(false);
            }

            return result;
        }

        /// <summary>
        /// Gets the mute setting from the client.
        /// </summary>
        /// <returns>Task.</returns>
        private async Task<bool> GetMute()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(string.Empty);
            }

            var result = await SendCommandResponseRequired(TransportCommandsRender, "GetMute", default).ConfigureAwait(false);
            var response = ParseResponse(result?.Document, "GetMuteResponse");

            if (response.TryGetValue("CurrentMute", out string muted))
            {
                IsMuted = string.Equals(muted, "1", StringComparison.OrdinalIgnoreCase);
                return true;
            }

            _logger.LogWarning("GetMute failed.");
            return false;
        }

        private async Task<bool> SendMediaRequest(Tuple<string, string, string> settings)
        {
            string url = settings.Item1;
            var result = false;

            if (!string.IsNullOrEmpty(url))
            {
                url = url.Replace("&", "&amp;", StringComparison.OrdinalIgnoreCase);
                _logger.LogDebug("{0} - SetAvTransport Uri: {1} DlnaHeaders: {2}", Properties.Name, url, settings.Item2);

                var dictionary = new Dictionary<string, string>
                {
                    { "CurrentURI", settings.Item1 },
                    { "CurrentURIMetaData", DescriptionXmlBuilder.Escape(settings.Item3) }
                };

                result = await SendCommand(
                    TransportCommandsAV,
                    "SetAVTransportURI",
                    default,
                    settings.Item1,
                    dictionary: dictionary,
                    header: settings.Item2).ConfigureAwait(false);

                if (result)
                {
                    await Task.Delay(50).ConfigureAwait(false);

                    result = await SendPlayRequest().ConfigureAwait(false);

                    if (result)
                    {
                        // Update what is playing.
                        _mediaPlaying = url;
                        RestartTimer(Now);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// The GetMediaInfo.
        /// </summary>
        /// <param name="cancellationToken">The cancellationToken.</param>
        /// <returns>The <see cref="Task{uBaseObject}"/>.</returns>
        private async Task<uBaseObject?> GetMediaInfo(CancellationToken cancellationToken)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(string.Empty);
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

        private async Task<bool> SendSeekRequest(TimeSpan value)
        {
            var result = false;
            if (IsPlaying || IsPaused)
            {
                result = await SendCommand(
                    TransportCommandsAV,
                    "Seek",
                    default,
                    string.Format(CultureInfo.InvariantCulture, "{0:hh}:{0:mm}:{0:ss}", value),
                    commandParameter: "REL_TIME").ConfigureAwait(false);

                if (result)
                {
                    Position = value;
                    RestartTimer(Now);
                }
            }

            return result;
        }

        private async Task<bool> GetPositionRequest()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(string.Empty);
            }

            // Update position information.
            try
            {
                var result = await SendCommandResponseRequired(TransportCommandsAV, "GetPositionInfo", default).ConfigureAwait(false);
                var position = ParseResponse(result?.Document, "GetPositionInfoResponse");
                if (position.Count != 0)
                {
                    if (position.TryGetValue("TrackDuration", out string value) && TimeSpan.TryParse(value, _usCulture, out TimeSpan d))
                    {
                        Duration = d;
                    }

                    if (position.TryGetValue("relTime", out value) && TimeSpan.TryParse(value, _usCulture, out TimeSpan r))
                    {
                        Position = r;
                    }

                    return true;
                }
            }
            catch (ObjectDisposedException)
            {
                // Ignore.
            }

            return false;
        }

        /// <summary>
        /// Retreives SSDP protocol information.
        /// </summary>
        /// <param name="services">The service to extract.</param>
        /// <param name="cancellationToken">The cancellationToken.</param>
        /// <returns>The <see cref="Task{TransportCommands}"/>.</returns>
        private async Task<TransportCommands?> GetProtocolAsync(DeviceService services, CancellationToken cancellationToken)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(string.Empty);
            }

            if (services == null)
            {
                return null;
            }

            string url = NormalizeUrl(Properties.BaseUrl, services.ScpdUrl, true);

            var document = await GetDataAsync(_httpClient, url, _logger, cancellationToken).ConfigureAwait(false);
            return TransportCommands.Create(document);
        }

        /// <summary>
        /// Gets service information from the DLNA clients.
        /// </summary>
        /// <param name="httpClient">The HttpClient to use. <see cref="IHttpClient"/>.</param>
        /// <param name="url">The destination URL..</param>
        /// <param name="logger">The logger to use.<see cref="ILogger"/>.</param>
        /// <param name="cancellationToken">The cancellationToken.</param>
        /// <returns>The <see cref="Task{XDocument}"/>.</returns>
        private static async Task<XDocument?> GetDataAsync(IHttpClient httpClient, string url, ILogger logger, CancellationToken cancellationToken)
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
            try
            {
                logger?.LogDebug("GetDataAsync: Communicating with {0}", url);
                using var response = await httpClient.SendAsync(options, HttpMethod.Get).ConfigureAwait(false);
                using var stream = response.Content;
                using var reader = new StreamReader(stream, Encoding.UTF8);
                return XDocument.Parse(
                    await reader.ReadToEndAsync().ConfigureAwait(false),
                    LoadOptions.PreserveWhitespace);
            }
            catch (HttpRequestException ex)
            {
                logger?.LogDebug("GetDataAsync: Failed with {0}", ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                // Show stack trace on other errors.
                logger?.LogDebug(ex, "GetDataAsync: Failed.");
                throw;
            }
        }

        public Task VolumeDown()
        {
            QueueEvent("SetVolume", Math.Max(Volume - 5, 0));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Sets or increases the volume. (No value will increase the volume by 5.)
        /// </summary>
        /// <param name="value">A value will set the volume.</param>
        /// <returns>Task.</returns>
        public Task VolumeUp(int value = -1)
        {
            if (value == -1)
            {
                QueueEvent("SetVolume", Math.Min(Volume + 5, 100));
            }
            else if (value >= 0 && value <= 100)
            {
                QueueEvent("SetVolume", value);
            }

            return Task.CompletedTask;
        }

        public Task ToggleMute()
        {
            AddOrCancelIfQueued("ToggleMute");
            return Task.CompletedTask;
        }

        public Task Play()
        {
            QueueEvent("Play");
            return Task.CompletedTask;
        }

        public Task Stop()
        {
            QueueEvent("Stop");
            return Task.CompletedTask;
        }

        public Task Pause()
        {
            QueueEvent("Pause");
            return Task.CompletedTask;
        }

        public Task Mute()
        {
            QueueEvent("Mute");
            return Task.CompletedTask;
        }

        public Task Unmute()
        {
            QueueEvent("Unmute");
            return Task.CompletedTask;
        }

        public Task Seek(TimeSpan value)
        {
            QueueEvent("Seek", value);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Attempts to set the AV transport.
        /// </summary>
        /// <param name="url">The url.</param>
        /// <param name="header">The header.</param>
        /// <param name="metaData">The metaData.</param>
        /// <returns>Task.</returns>
        public Task SetAvTransport(string url, string header, string metaData)
        {
            QueueEvent("Queue", new Tuple<string, string, string>(url, header, metaData));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        /// <returns>The .</returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} - {1}", Properties.Name, Properties.BaseUrl);
        }

        /// <summary>
        /// Diposes this object.
        /// </summary>
        /// <param name="disposing">The disposing<see cref="bool"/>.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _timer?.Dispose();

                if (_playToManager != null)
                {
                    _playToManager.DLNAEvents -= ProcessSubscriptionEvent;
                }
            }

            _disposed = true;
        }

        /// <summary>
        /// Updates the media info, firing events.
        /// </summary>
        /// <param name="mediaInfo">The mediaInfo<see cref="uBaseObject"/>.</param>
        private void UpdateMediaInfo(uBaseObject? mediaInfo)
        {
            var previousMediaInfo = CurrentMediaInfo;
            CurrentMediaInfo = mediaInfo;
            try
            {
                if (mediaInfo != null)
                {
                    if (previousMediaInfo == null)
                    {
                        if (TransportState != TransportState.STOPPED && !string.IsNullOrWhiteSpace(mediaInfo.Url))
                        {
                            _logger.LogDebug("Firing playback started.");
                            PlaybackStart?.Invoke(this, new PlaybackStartEventArgs
                            {
                                MediaInfo = mediaInfo
                            });
                        }
                    }
                    else if (mediaInfo.Equals(previousMediaInfo))
                    {
                        if (!string.IsNullOrWhiteSpace(mediaInfo?.Url))
                        {
                            _logger.LogDebug("Firing playback progress.");
                            PlaybackProgress?.Invoke(this, new PlaybackProgressEventArgs
                            {
                                MediaInfo = mediaInfo
                            });
                        }
                    }
                    else
                    {
                        _logger.LogDebug("Firing media change.");
                        MediaChanged?.Invoke(this, new MediaChangedEventArgs
                        {
                            OldMediaInfo = previousMediaInfo,
                            NewMediaInfo = mediaInfo
                        });
                    }
                }
                else if (previousMediaInfo != null)
                {
                    _logger.LogDebug("Firing playback stopped.");
                    PlaybackStopped?.Invoke(this, new PlaybackStoppedEventArgs
                    {
                        MediaInfo = previousMediaInfo
                    });
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types : Don't let errors in the events affect us.
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                _logger.LogError(ex, "UpdateMediaInfo errored.");
            }
        }

        /// <summary>
        /// Returns the ServiceRenderingControl element of the device.
        /// </summary>
        /// <returns>The ServiceRenderingControl <see cref="DeviceService"/>.</returns>
        private DeviceService GetServiceRenderingControl()
        {
            var services = Properties.Services;

            return services.FirstOrDefault(s => string.Equals(s.ServiceType, "urn:schemas-upnp-org:service:RenderingControl:1", StringComparison.OrdinalIgnoreCase)) ??
                services.FirstOrDefault(s => (s.ServiceType ?? string.Empty).StartsWith("urn:schemas-upnp-org:service:RenderingControl", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Returns the AvTransportService element of the device.
        /// </summary>
        /// <returns>The AvTransportService <see cref="DeviceService"/>.</returns>
        private DeviceService GetAvTransportService()
        {
            var services = Properties.Services;

            return services.FirstOrDefault(s => string.Equals(s.ServiceType, "urn:schemas-upnp-org:service:AVTransport:1", StringComparison.OrdinalIgnoreCase)) ??
                services.FirstOrDefault(s => (s.ServiceType ?? string.Empty).StartsWith("urn:schemas-upnp-org:service:AVTransport", StringComparison.OrdinalIgnoreCase));
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
                var nodes = document.Descendants()
                    .Where(p => p.Name.LocalName == action);
                if (nodes != null)
                {
                    foreach (var node in nodes)
                    {
                        if (node.HasElements)
                        {
                            foreach (var childNode in node.Elements())
                            {
                                string? value = childNode.Value;
                                if (string.IsNullOrWhiteSpace(value))
                                {
                                    // Some responses are stores in the val property.
                                    value = childNode.Attribute("val")?.Value;
                                }

                                if (string.IsNullOrWhiteSpace(value) || string.Equals(value, "NOT_IMPLEMENTED", StringComparison.OrdinalIgnoreCase))
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

        /// <summary>
        /// The ParseNodeResponse.
        /// </summary>
        /// <param name="xml">The xml.</param>
        /// <returns>The .</returns>
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

            // some devices send back invalid xml
            try
            {
                return XElement.Parse(xml.Replace("&", "&amp;", StringComparison.OrdinalIgnoreCase));
            }
            catch (XmlException)
            {
                // Wasn't this flavour.
            }

            return null;
        }

        /// <summary>
        /// Normalizes a Url.
        /// </summary>
        /// <param name="baseUrl">The base Url.</param>
        /// <param name="url">The service Url.</param>
        /// <param name="dmr">When true, prepends 'dmr' if not present in the url.</param>
        /// <returns>A normalised Url.</returns>
        private static string NormalizeUrl(string baseUrl, string url, bool dmr = false)
        {
            // If it's already a complete url, don't stick anything onto the front of it
            if (url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                return url;
            }

            if (dmr && !url.Contains("/", StringComparison.OrdinalIgnoreCase))
            {
                url = "/dmr/" + url;
            }
            else if (!url.StartsWith("/", StringComparison.Ordinal))
            {
                url = "/" + url;
            }

            return baseUrl + url;
        }

        /// <summary>
        /// Creates a uBaseObject from the information provided.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="trackUri">The trackUri.</param>
        /// <returns>The <see cref="uBaseObject"/>.</returns>
        private static uBaseObject CreateUBaseObject(XElement? container, string trackUri)
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

            var resElement = container.Element(uPnpNamespaces.Res);
            var protocolInfo = new string[4];

            if (resElement != null)
            {
                var info = resElement.Attribute(uPnpNamespaces.ProtocolInfo);

                if (info != null && !string.IsNullOrWhiteSpace(info.Value))
                {
                    protocolInfo = info.Value.Split(':');
                }
            }

            return new uBaseObject
            {
                Id = container.GetAttributeValue(uPnpNamespaces.Id),
                ParentId = container.GetAttributeValue(uPnpNamespaces.ParentId),
                Title = container.GetValue(uPnpNamespaces.title),
                IconUrl = container.GetValue(uPnpNamespaces.Artwork),
                SecondText = string.Empty,
                Url = url,
                ProtocolInfo = protocolInfo,
                MetaData = container.ToString()
            };
        }

        /// <summary>
        /// The CreateIcon.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns>The <see cref="DeviceIcon"/>.</returns>
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

        /// <summary>
        /// Creates a DeviceService from an XML element.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns>The <see cref="DeviceService"/>.</returns>
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
    }
}
