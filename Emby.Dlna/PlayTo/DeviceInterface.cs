#pragma warning disable CA1031 // Catch a more specific exception type, or rethrow the exception.
#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Mime;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Emby.Dlna.Common;
using Emby.Dlna.Ssdp;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using Microsoft.Extensions.Logging;

namespace Emby.Dlna.PlayTo
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;
    using System.Xml.Linq;
    using Emby.Dlna.Common;
    using Emby.Dlna.PlayTo.EventArgs;
    using Emby.Dlna.Ssdp;
    using MediaBrowser.Common.Configuration;
    using MediaBrowser.Common.Net;
    using MediaBrowser.Model.Dlna;
    using MediaBrowser.Model.Net;
    using MediaBrowser.Model.Notifications;
    using Microsoft.Extensions.Logging;

    using XMLProperties = System.Collections.Generic.Dictionary<string, string>;

    /// <summary>
    /// Code for managing a DLNA PlayTo device.
    ///
    /// The core of the code is based around the three methods ProcessSubscriptionEvent, TimerCallback and ProcessQueue.
    ///
    /// All incoming user actions are queue into the _queue, which is subsequently
    /// actioned by ProcessQueue function. This not only provides a level of rate limiting,
    /// but also stops repeated duplicate commands from being sent to the devices.
    ///
    /// TimeCallback is the manual handler for the device if it doesn't support subscriptions.
    /// It periodically polls for the settings.
    /// ProcessSubscriptionEvent is the handler for events that the device sends us.
    ///
    /// Both these two methods work side by side getting constant updates, using mutual
    /// caching to ensure the device isn't polled too frequently.
    /// </summary>
    public class DeviceInterface : IDisposable
    {
        /// <summary>
        /// Defines the USERAGENT that we send to devices.
        /// </summary>
        private const string USERAGENT = "Microsoft-Windows/6.2 UPnP/1.0 Microsoft-DLNA DLNADOC/1.50";

        /// <summary>
        /// The frequency of the device polling (ms).
        /// </summary>
        private const int TIMERINTERVAL = 30000;

        /// <summary>
        /// The user queue processing frequency (ms).
        /// </summary>
        private const int QUEUEINTERVAL = 1000;

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

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger _logger;
        private readonly PlayToManager _playToManager;
        private readonly IServerConfigurationManager _config;
        private readonly object _timerLock = new object();
        private readonly object _queueLock = new object();

        /// <summary>
        /// Device's volume boundary values.
        /// </summary>
        private readonly ValueRange _volRange = new ValueRange();

        /// <summary>
        /// Holds the URL for the Jellyfin web server.
        /// </summary>
        private readonly string _jellyfinUrl;

        /// <summary>
        /// Outbound events processing queue.
        /// </summary>
        private readonly List<KeyValuePair<string, object>> _queue = new List<KeyValuePair<string, object>>();

        /// <summary>
        /// Host network response roundtime time.
        /// </summary>
        private TimeSpan _transportOffset = TimeSpan.Zero;

        /// <summary>
        /// Holds the current playback position.
        /// </summary>
        private TimeSpan _position = TimeSpan.Zero;

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
        private int _volume;

        /// <summary>
        /// Media Type that is currently "loaded".
        /// </summary>
        private DlnaProfileType? _mediaType;

        /// <summary>
        /// Hosts the last time we polled for the requests.
        /// </summary>
        private DateTime _lastVolumeRefresh;
        private DateTime _lastTransportRefresh;
        private DateTime _lastMetaRefresh;
        private DateTime _lastPositionRequest;
        private DateTime _lastMuteRefresh;

        /// <summary>
        /// Contains the item currently playing.
        /// </summary>
        private string _mediaPlaying = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceInterface"/> class.
        /// </summary>
        /// <param name="playToManager">Our playToManager<see cref="PlayToManager"/>.</param>
        /// <param name="deviceProperties">The deviceProperties<see cref="DeviceInfo"/>.</param>
        /// <param name="httpClientFactory">The <see cref="IHttpClientFactory"/>.</param>
        /// <param name="logger">The logger<see cref="ILogger"/>.</param>
        /// <param name="config">The IServerConfigurationManager instance.</param>
        /// <param name="webUrl">The webUrl.</param>
        public DeviceInterface(PlayToManager playToManager, DeviceInfo deviceProperties, IHttpClientFactory httpClientFactory, ILogger logger, IServerConfigurationManager config, string webUrl)
        {
            Properties = deviceProperties;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _jellyfinUrl = webUrl;
            _playToManager = playToManager;
            _config = config;
            _config.NamedConfigurationUpdated += OnNamedConfigurationUpdated;
            TransportState = TransportState.NoMediaPresent;
        }

        /// <summary>
        /// Events called when playback starts.
        /// </summary>
        public event EventHandler<PlaybackEventArgs>? PlaybackStart;

        /// <summary>
        /// Events called during playback.
        /// </summary>
        public event EventHandler<PlaybackEventArgs>? PlaybackProgress;

        /// <summary>
        /// Events called when playback stops.
        /// </summary>
        public event EventHandler<PlaybackEventArgs>? PlaybackStopped;

        /// <summary>
        /// Events called when the media changes.
        /// </summary>
        public event EventHandler<MediaChangedEventArgs>? MediaChanged;

        /// <summary>
        /// Gets the device's properties.
        /// </summary>
        public DeviceInfo Properties { get; }

        /// <summary>
        /// Gets a value indicating whether the sound is muted.
        /// </summary>
        public bool IsMuted { get; private set; }

        /// <summary>
        /// Gets the current media information.
        /// </summary>
        public UBaseObject? CurrentMediaInfo { get; private set; }

        /// <summary>
        /// Gets or sets the Volume.
        /// </summary>
        public int Volume
        {
            get
            {
                if (!_subscribed)
                {
                    try
                    {
                        RefreshVolumeIfNeeded().GetAwaiter().GetResult();
                    }
                    catch (ObjectDisposedException)
                    {
                        // Ignore.
                    }
                    catch (HttpException ex)
                    {
                        _logger.LogError(ex, "{0}: Error getting device volume.", Properties.Name);
                    }
                }

                int calculateVolume = (int)Math.Round(100 / _volRange.Range * _volume);

                _logger.LogDebug("{0}: Returning a volume setting of {1}.", Properties.Name, calculateVolume);
                return calculateVolume;
            }

            set
            {
                if (value >= 0 && value <= 100)
                {
                    // Make ratio adjustments as not all devices have volume level 100. (User range => Device range.)
                    int newValue = (int)Math.Round(_volRange.Range / 100 * value);
                    if (newValue != _volume)
                    {
                        QueueEvent("SetVolume", newValue);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the Duration.
        /// </summary>
        public TimeSpan? Duration { get; internal set; }

        /// <summary>
        /// Gets or sets the Position.
        /// </summary>
        public TimeSpan Position
        {
            get
            {
                return _position.Add(_transportOffset);
            }

            set
            {
                _position = value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether IsPlaying.
        /// </summary>
        public bool IsPlaying => TransportState == TransportState.Playing;

        /// <summary>
        /// Gets a value indicating whether IsPaused.
        /// </summary>
        public bool IsPaused => TransportState == TransportState.Paused || TransportState == TransportState.PausedPlayback;

        /// <summary>
        /// Gets a value indicating whether IsStopped.
        /// </summary>
        public bool IsStopped => TransportState == TransportState.Stopped;

        /// <summary>
        /// Gets or sets the OnDeviceUnavailable.
        /// </summary>
        public Action? OnDeviceUnavailable { get; set; }

        /// <summary>
        /// Gets the TransportState.
        /// </summary>
        protected TransportState TransportState { get; private set; }

        /// <summary>
        /// Gets or sets the AvCommands.
        /// </summary>
        private TransportCommands? AvCommands { get; set; }

        /// <summary>
        /// Gets or sets the RendererCommands.
        /// </summary>
        private TransportCommands? RendererCommands { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether trace information should be redirected to the logs.
        /// </summary>
        private static bool Tracing { get; set; }

        /// <summary>
        /// The CreateuPnpDeviceAsync.
        /// </summary>
        /// <param name="playToManager">The playToManager<see cref="PlayToManager"/>.</param>
        /// <param name="url">The url<see cref="Uri"/>.</param>
        /// <param name="httpClientFactory">The <see cref="IHttpClientFactory"/>.</param>
        /// <param name="logger">The logger<see cref="ILogger"/>.</param>
        /// <param name="config">The IServerConfigurationManager instance.</param>
        /// <param name="serverUrl">The url to use for the server.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        public static async Task<DeviceInterface?> CreateuPnpDeviceAsync(
            PlayToManager playToManager,
            Uri url,
            IHttpClientFactory httpClientFactory,
            ILogger logger,
            IServerConfigurationManager config,
            string serverUrl)
        {
            if (url == null)
            {
                throw new ArgumentNullException(nameof(url));
            }

            if (httpClientFactory == null)
            {
                throw new ArgumentNullException(nameof(httpClientFactory));
            }

            var document = await GetDataAsync(httpClientFactory, url.ToString(), logger).ConfigureAwait(false);

            var friendlyNames = new List<string>();

            var name = document.Descendants(UPnpNamespaces.Ud.GetName("friendlyName")).FirstOrDefault();
            if (name != null && !string.IsNullOrWhiteSpace(name.Value))
            {
                // Some devices include their MAC addresses as part of their name.
                var value = Regex.Replace(name.Value, "([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})", string.Empty)
                    .Replace("()", string.Empty, StringComparison.OrdinalIgnoreCase)
                    .Replace("[]", string.Empty, StringComparison.OrdinalIgnoreCase)
                    .Trim();
                friendlyNames.Add(value);
            }

            var room = document.Descendants(UPnpNamespaces.Ud.GetName("roomName")).FirstOrDefault();
            if (room != null && !string.IsNullOrWhiteSpace(room.Value))
            {
                friendlyNames.Add(room.Value);
            }

            var deviceProperties = new DeviceInfo()
            {
                Name = string.Join(" ", friendlyNames),
                BaseUrl = string.Format(_usCulture, "http://{0}:{1}", url.Host, url.Port)
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
                var width = icon.GetDescendantValue(UPnpNamespaces.Ud.GetName("width"));
                var height = icon.GetDescendantValue(UPnpNamespaces.Ud.GetName("height"));
                if (!int.TryParse(width, NumberStyles.Integer, _usCulture, out int widthValue))
                {
                    logger.LogDebug("{0} : Unable to parse icon width {1}.", deviceProperties.Name, width);
                    widthValue = 32;
                }

                if (!int.TryParse(height, NumberStyles.Integer, _usCulture, out int heightValue))
                {
                    logger.LogDebug("{0} : Unable to parse icon width {1}.", deviceProperties.Name, width);
                    heightValue = 32;
                }

                deviceProperties.Icon = new DeviceIcon
                {
                    Depth = icon.GetDescendantValue(UPnpNamespaces.Ud.GetName("depth")),
                    MimeType = icon.GetDescendantValue(UPnpNamespaces.Ud.GetName("mimetype")),
                    Url = icon.GetDescendantValue(UPnpNamespaces.Ud.GetName("url")),
                    Height = heightValue,
                    Width = widthValue
                };
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

            try
            {
                return new DeviceInterface(playToManager, deviceProperties, httpClientFactory, logger, config, serverUrl);
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
                // Reset the caching timings.
                _lastPositionRequest = DateTime.UtcNow.AddSeconds(-5);
                _lastVolumeRefresh = _lastPositionRequest;
                _lastTransportRefresh = _lastPositionRequest;
                _lastMetaRefresh = _lastPositionRequest;
                _lastMuteRefresh = _lastPositionRequest;

                // Make sure that the device doesn't have a range on the volume controls.
                try
                {
                    await GetStateVariableRange(GetServiceRenderingControl(), "Volume").ConfigureAwait(false);
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (HttpException ex)
                {
                    if (Tracing)
                    {
                        _logger.LogDebug(ex, "{0}: Attempting to get volume range. Error usually means NOT SUPPORTED.", Properties.Name);
                    }
                }

                try
                {
                    await SubscribeAsync().ConfigureAwait(false);
                    // Update the position, volume and subscript for events.
                    await GetPositionRequest().ConfigureAwait(false);
                    await GetVolume().ConfigureAwait(false);
                    await GetMute().ConfigureAwait(false);
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (HttpException ex)
                {
                    if (Tracing)
                    {
                        _logger.LogDebug(ex, "{0}: Error initialising device.", Properties.Name);
                    }
                }

                _logger.LogDebug("{0}: Starting timer.", Properties.Name);
                _timer = new Timer(TimerCallback, null, 500, Timeout.Infinite);

                // Start the user command queue processor.
                await ProcessQueue().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Called when the device becomes unavailable.
        /// </summary>
        /// <returns>Task.</returns>
        public async Task DeviceUnavailable()
        {
            if (_subscribed)
            {
                _logger.LogDebug("{0}: Killing the timer.", Properties.Name);

                await UnSubscribeAsync().ConfigureAwait(false);

                lock (_timerLock)
                {
                    _timer?.Dispose();
                    _timer = null;
                }
            }
        }

        /// <summary>
        /// Decreases the volume.
        /// </summary>
        /// <returns>Task.</returns>
        public Task VolumeDown()
        {
            QueueEvent("SetVolume", Math.Max(_volume - _volRange.FivePoints, _volRange.Min));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Increases the volume.
        /// </summary>
        /// <returns>Task.</returns>
        public Task VolumeUp()
        {
            QueueEvent("SetVolume", Math.Min(_volume + _volRange.FivePoints, _volRange.Max));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Toggles Mute.
        /// </summary>
        /// <returns>Task.</returns>
        public Task ToggleMute()
        {
            AddOrCancelIfQueued("ToggleMute");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Starts playback.
        /// </summary>
        /// <returns>Task.</returns>
        public Task Play()
        {
            QueueEvent("Play");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Stops playback.
        /// </summary>
        /// <returns>Task.</returns>
        public Task Stop()
        {
            QueueEvent("Stop");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Pauses playback.
        /// </summary>
        /// <returns>Task.</returns>
        public Task Pause()
        {
            QueueEvent("Pause");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Mutes the sound.
        /// </summary>
        /// <returns>Task.</returns>
        public Task Mute()
        {
            QueueEvent("Mute");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Resumes the sound.
        /// </summary>
        /// <returns>Task.</returns>
        public Task Unmute()
        {
            QueueEvent("Unmute");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Moves playback to a specific point.
        /// </summary>
        /// <param name="value">The point at which playback will resume.</param>
        /// <returns>Task.</returns>
        public Task Seek(TimeSpan value)
        {
            QueueEvent("Seek", value);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Specifies new media to play.
        /// </summary>
        /// <param name="mediatype">The type of media the url points to.</param>
        /// <param name="resetPlay">In we are already playing this item, do we restart from the beginning.</param>
        /// <param name="url">Url of media.</param>
        /// <param name="headers">Headers.</param>
        /// <param name="metadata">Media metadata.</param>
        /// <returns>Task.</returns>
        public Task SetAvTransport(DlnaProfileType mediatype, bool resetPlay, string url, string headers, string metadata)
        {
            QueueEvent("Queue", new MediaData
            {
                Url = url,
                Headers = headers,
                Metadata = metadata,
                MediaType = mediatype,
                ResetPlay = resetPlay
            });

            return Task.CompletedTask;
        }

        /// <summary>
        /// Disposes this object.
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
                _config.NamedConfigurationUpdated -= OnNamedConfigurationUpdated;
                if (_playToManager != null)
                {
                    _playToManager.DLNAEvents -= ProcessSubscriptionEvent;
                }
            }

            _disposed = true;
        }

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
        /// Creates a UBaseObject from the information provided.
        /// </summary>
        /// <param name="properties">The XML properties.</param>
        /// <returns>The <see cref="UBaseObject"/>.</returns>
        private static UBaseObject? CreateUBaseObject(XMLProperties properties)
        {
            var uBase = new UBaseObject();

            if (properties.TryGetValue("res.protocolInfo", out string value) && !string.IsNullOrEmpty(value))
            {
                uBase.ProtocolInfo = value.Split(':');
            }
            else
            {
                uBase.ProtocolInfo = new string[4];
            }

            if (properties.TryGetValue("item.id", out value))
            {
                uBase.Id = value;
            }

            if (properties.TryGetValue("item.parentID", out value))
            {
                uBase.ParentId = value;
            }

            if (properties.TryGetValue("title", out value))
            {
                uBase.Title = value;
            }

            if (properties.TryGetValue("albumArtURI", out value))
            {
                uBase.IconUrl = value;
            }

            if (properties.TryGetValue("res", out value))
            {
                if (string.IsNullOrEmpty(value))
                {
                    properties.TryGetValue("TrackURI", out value);
                }

                uBase.Url = value.Replace("&amp;", "&", StringComparison.OrdinalIgnoreCase);
            }

            if (properties.TryGetValue("album", out value))
            {
                // Chose album - was original empty.
                uBase.SecondText = value;
            }

            if (properties.TryGetValue("res", out value))
            {
                uBase.Url = value.Replace("&amp;", "&", StringComparison.OrdinalIgnoreCase);
            }

            if (uBase.Url == null)
            {
                return null;
            }

            // currentObject.MetaData = container.ToString()
            return uBase;
        }

        /// <summary>
        /// Creates a DeviceService from an XML element.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns>The <see cref="DeviceService"/>.</returns>
        private static DeviceService Create(XElement element)
        {
            var type = element.GetDescendantValue(UPnpNamespaces.Ud.GetName("serviceType"));
            var id = element.GetDescendantValue(UPnpNamespaces.Ud.GetName("serviceId"));
            var scpdUrl = element.GetDescendantValue(UPnpNamespaces.Ud.GetName("SCPDURL"));
            var controlURL = element.GetDescendantValue(UPnpNamespaces.Ud.GetName("controlURL"));
            var eventSubURL = element.GetDescendantValue(UPnpNamespaces.Ud.GetName("eventSubURL"));

            return new DeviceService
            {
                ControlUrl = controlURL,
                EventSubUrl = eventSubURL,
                ScpdUrl = scpdUrl,
                ServiceId = id,
                ServiceType = type
            };
        }

        /// <summary>
        /// Gets service information from the DLNA clients.
        /// </summary>
        /// <param name="httpClientFactory">The IhttpClientFactory instance. <see cref="IHttpClientFactory"/>.</param>
        /// <param name="url">The destination URL.</param>
        /// <param name="logger">ILogger instance.</param>
        /// <returns>The <see cref="Task{XDocument}"/>.</returns>
        private static async Task<XDocument> GetDataAsync(IHttpClientFactory httpClientFactory, string url, ILogger logger)
        {
            using var options = new HttpRequestMessage(HttpMethod.Get, url);
            options.Headers.UserAgent.ParseAdd(USERAGENT);
            options.Headers.TryAddWithoutValidation("FriendlyName.DLNA.ORG", FriendlyName);
            string reply = string.Empty;
            int attempt = 0;

            while (true)
            {
                try
                {
                    logger.LogDebug("GetDataAsync: Communicating with {0}", url);

                    using var response = await httpClientFactory
                        .CreateClient(NamedClient.Default)
                        .SendAsync(options, HttpCompletionOption.ResponseHeadersRead, default).ConfigureAwait(false);

                    await using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                    using var reader = new StreamReader(stream, Encoding.UTF8);
                    reply = await reader.ReadToEndAsync().ConfigureAwait(false);

                    if (Tracing)
                    {
                        logger.LogDebug("<- {0}\r\n{1}", url, reply);
                    }

                    return XDocument.Parse(reply);
                }
                catch (XmlException)
                {
                    logger.LogDebug("GetDataAsync: Invalid XML returned {0}", reply);
                    if (attempt++ > 3)
                    {
                        throw;
                    }
                }
                catch (HttpRequestException ex)
                {
                    logger.LogDebug("GetDataAsync: Failed with {0}", ex.Message);
                    if (attempt++ > 3)
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    // Show stack trace on other errors.
                    logger.LogDebug(ex, "GetDataAsync: Failed.");
                    if (attempt++ > 3)
                    {
                        throw;
                    }
                }

                await Task.Delay(100).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Enables the volume bar hovered over effect.
        /// </summary>
        /// <returns>Task.</returns>
        private async Task RefreshVolumeIfNeeded()
        {
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

        private void OnNamedConfigurationUpdated(object sender, ConfigurationUpdateEventArgs e)
        {
            if (string.Equals(e.Key, "dlna", StringComparison.OrdinalIgnoreCase))
            {
                Tracing = _config.GetDlnaConfiguration().EnablePlayToTracing;
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

                int delay = when == Never ? Timeout.Infinite : when == Now ? 100 : TIMERINTERVAL;
                _timer?.Change(delay, Timeout.Infinite);
            }
        }

        /// <summary>
        /// Adds an event to the user control queue.
        /// Later identical events overwrite earlier ones.
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
                    _logger.LogDebug("{0}: Replacing user event: {1} {2}", Properties.Name, command, value);
                    _queue.RemoveAt(index);
                }
                else
                {
                    _logger.LogDebug("{0}: Queuing user event: {1} {2}", Properties.Name, command, value);
                }

                _queue.Add(new KeyValuePair<string, object>(command, value ?? 0));
            }
        }

        /// <summary>
        /// Removes an event from the queue if it exists, or adds it if it doesn't.
        /// </summary>
        private void AddOrCancelIfQueued(string command)
        {
            lock (_queueLock)
            {
                int index = _queue.FindIndex(item => string.Equals(item.Key, command, StringComparison.OrdinalIgnoreCase));

                if (index != -1)
                {
                    _logger.LogDebug("{0}: Cancelling user event: {1}", Properties.Name, command);
                    _queue.RemoveAt(index);
                }
                else
                {
                    _logger.LogDebug("{0} : Queuing user event: {1}", Properties.Name, command);
                    _queue.Add(new KeyValuePair<string, object>(command, 0));
                }
            }
        }

        /// <summary>
        /// Gets the next item from the queue. (Threadsafe).
        /// </summary>
        /// <param name="action">Next item if true is returned, otherwise null.</param>
        /// <param name="defaultAction">The value to return if not successful.</param>
        /// <returns>Success of the operation.</returns>
        private bool TryPop(out KeyValuePair<string, object> action, KeyValuePair<string, object> defaultAction)
        {
            lock (_queueLock)
            {
                if (_queue.Count <= 0)
                {
                    action = defaultAction;
                    return false;
                }

                action = _queue[0];
                _queue.RemoveAt(0);
                return true;
            }
        }

        private async Task ProcessQueue()
        {
            // When TryPop is unsuccessful it still needs to return a value in "action".
            // To return null the KeyValuePair needs to be nullable.
            // Doing this requires ".GetValueOrDefault()." to be prefixed to action every time it's used.
            // In my opinion it makes the code look bad, implies a level of risk where there is none (action can never be null
            // so why imply it in the code?), and adds an extra level of processing that is totally unneccesary.
            // My work around is to default this default value that will never be used, except as a pass back when TryPop is false.

            var defaultValue = new KeyValuePair<string, object>(string.Empty, 0);

            // Infinite loop until dispose.
            while (!_disposed)
            {
                // Process items in the queue.
                while (TryPop(out KeyValuePair<string, object> action, defaultValue))
                {
                    // Ensure we are still subscribed.
                    await SubscribeAsync().ConfigureAwait(false);

                    try
                    {
                        _logger.LogDebug("{0}: Attempting action : {1}", Properties.Name, action.Key);

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
                                            var sendVolume = _muteVol <= 0 ?
                                                (int)Math.Round((double)(_volRange.Max - _volRange.Min) / 100 * 20) // 20% of maximum.
                                                : _muteVol;
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
                                        var sendVolume = _muteVol <= 0 ?
                                                (int)Math.Round((double)(_volRange.Max - _volRange.Min) / 100 * 20) // 20% of maximum.
                                                : _muteVol;
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
                                    var settings = (MediaData)action.Value;

                                    // Media Change event
                                    if (IsPlaying)
                                    {
                                        // Compare what is currently playing to what is being sent minus the time element.
                                        string thisMedia = Regex.Replace(_mediaPlaying, "&StartTimeTicks=\\d*", string.Empty);
                                        string newMedia = Regex.Replace(settings.Url, "&StartTimeTicks=\\d*", string.Empty);

                                        if (!thisMedia.Equals(newMedia, StringComparison.Ordinal))
                                        {
                                            _logger.LogDebug("{0}: Stopping current playback for transition.", Properties.Name);
                                            bool success = await SendStopRequest().ConfigureAwait(false);

                                            if (success)
                                            {
                                                // Save current progress.
                                                TransportState = TransportState.Transitioning;
                                                UpdateMediaInfo(null);
                                            }
                                        }

                                        if (settings.ResetPlay)
                                        {
                                            // Restart from the beginning.
                                            _logger.LogDebug("{0}: Resetting playback position.", Properties.Name);
                                            bool success = await SendSeekRequest(TimeSpan.Zero).ConfigureAwait(false);
                                            if (success)
                                            {
                                                // Save progress and restart time.
                                                UpdateMediaInfo(CurrentMediaInfo);
                                                RestartTimer(Normal);
                                                // We're finished. Nothing further to do.
                                                break;
                                            }
                                        }
                                    }

                                    await SendMediaRequest(settings).ConfigureAwait(false);

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

                await Task.Delay(QUEUEINTERVAL).ConfigureAwait(false);
            }
        }

        private async Task NotifyUser(string msg)
        {
            var notification = new NotificationRequest()
            {
                Name = string.Format(CultureInfo.InvariantCulture, msg, "DLNA PlayTo")
            };

            if (_mediaType == DlnaProfileType.Audio)
            {
                notification.NotificationType = NotificationType.AudioPlayback.ToString();
            }
            else if (_mediaType == DlnaProfileType.Video)
            {
                notification.NotificationType = NotificationType.AudioPlayback.ToString();
            }
            else
            {
                notification.NotificationType = NotificationType.TaskFailed.ToString();
            }

            _logger.LogDebug("{0}: User notification: {1}", Properties.Name, notification.NotificationType);

            await _playToManager.SendNotification(this, notification).ConfigureAwait(false);
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
        private async Task<XMLProperties?> SendCommandAsync(
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
            var stopWatch = new Stopwatch();
            string xmlResponse = string.Empty;

            try
            {
                using var options = new HttpRequestMessage(HttpMethod.Post, url);
                options.Headers.UserAgent.ParseAdd(USERAGENT);
                options.Headers.TryAddWithoutValidation("SOAPAction", $"\"{service.ServiceType}#{command}\"");
                options.Headers.TryAddWithoutValidation("Pragma", "no-cache");
                options.Headers.TryAddWithoutValidation("FriendlyName.DLNA.ORG", FriendlyName);

                if (!string.IsNullOrEmpty(header))
                {
                    options.Headers.TryAddWithoutValidation("contentFeatures.dlna.org", header);
                }

                options.Content = new StringContent(postData, Encoding.UTF8, MediaTypeNames.Text.Xml);

                if (Tracing)
                {
                    _logger.LogDebug("{0}:-> {1}\r\nHeader\r\n{2}\r\nData\r\n{3}", Properties.Name, url, options.Headers, postData);
                }

                stopWatch.Start();
                var response = await _httpClientFactory
                    .CreateClient(NamedClient.Default)
                    .SendAsync(options, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                    .ConfigureAwait(false);

                // Get the response.
                var buffer = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                xmlResponse = Encoding.UTF8.GetString(buffer);

                if (Tracing)
                {
                    _logger.LogDebug("{0}:<- {1}\r\n{2}", Properties.Name, url, xmlResponse);
                }

                // Some devices don't return valid xml - so we need to loosly parse it.
                XMLUtilities.ParseXML(xmlResponse, out XMLProperties results);
                stopWatch.Stop();

                // Calculate just under half of the round trip time so we can make the position slide more accurate.
                _transportOffset = stopWatch.Elapsed.Divide(1.8);

                return results;
            }
            catch (HttpException ex)
            {
                stopWatch.Stop();

                if (!string.IsNullOrEmpty(ex.ResponseText))
                {
                    if (Tracing)
                    {
                        _logger.LogDebug("{0}:<- {1}\r\n{2}", Properties.Name, url, ex.ResponseText);
                    }

                    string msg = string.Empty;
                    XMLUtilities.ParseXML(ex.ResponseText, out XMLProperties prop);
                    if (!prop.TryGetValue("errorDescription", out msg))
                    {
                        prop.TryGetValue("faultstring", out msg);
                    }

                    if (msg != null)
                    {
                        // Send the user notification, so they don't just sit clicking!
                        await NotifyUser(Properties.Name + ": " + msg).ConfigureAwait(false);
                        _logger.LogError("{0}: SendCommandAsync failed. Device returned '{1}'.", Properties.Name, msg);
                    }
                    else
                    {
                        _logger.LogError("{0}: SendCommandAsync failed. Unable to parse error. \r\n", Properties.Name, ex.ResponseText);
                    }
                }
                else
                {
                    _logger.LogError("{0}: SendCommandAsync failed with {1} to {2} ", Properties.Name, ex.Message.ToString(_usCulture), url);
                }

                return null;
            }
            catch (HttpRequestException ex)
            {
                stopWatch.Stop();
                _logger.LogError("{0}: SendCommandAsync failed with {1} to {2} ", Properties.Name, ex.ToString(), url);
                return null;
            }
            catch (XmlException)
            {
                stopWatch.Stop();
                _logger.LogError("{0}: SendCommandAsync responded with invalid XML. \r\n{1} ", Properties.Name, xmlResponse);
                return null;
            }
        }

        /// <summary>
        /// Sends a command to the device and waits for a response.
        /// </summary>
        /// <param name="transportCommandsType">The transport type.</param>
        /// <param name="actionCommand">The actionCommand.</param>
        /// <param name="name">The name.</param>
        /// <param name="commandParameter">The commandParameter.</param>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="header">The header.</param>
        /// <returns>The <see cref="Task{XDocument}"/>.</returns>
        private async Task<XMLProperties?> SendCommandResponseRequired(
            int transportCommandsType,
            string actionCommand,
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

                RendererCommands ??= await GetProtocolAsync(service).ConfigureAwait(false);
                if (RendererCommands == null)
                {
                    _logger.LogError("{0}: GetRenderingProtocolAsync returned null.", Properties.Name);
                    return null;
                }

                command = RendererCommands.ServiceActions.FirstOrDefault(c => c.Name == actionCommand);
                if (service == null || command == null)
                {
                    _logger.LogError("{0}: Command or service returned null.", Properties.Name);
                    return null;
                }

                commands = RendererCommands;
            }
            else
            {
                service = GetAvTransportService();
                AvCommands ??= await GetProtocolAsync(service).ConfigureAwait(false);
                if (AvCommands == null)
                {
                    _logger.LogError("{0}: GetAVProtocolAsync returned null.", Properties.Name);
                    return null;
                }

                command = AvCommands.ServiceActions.FirstOrDefault(c => c.Name == actionCommand);
                if (service == null || command == null)
                {
                    _logger.LogWarning("{0}: Command or service returned null.", Properties.Name);
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

            _logger.LogDebug("{0}: Transmitting {1} to device.", Properties.Name, command.Name);

            return await SendCommandAsync(Properties.BaseUrl, service, command.Name, postData, header).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends a command to the device, verifies receipt, and does return a response.
        /// </summary>
        /// <param name="transportCommandsType">The transport commands type.</param>
        /// <param name="actionCommand">The action command to use.</param>
        /// <param name="name">The name.</param>
        /// <param name="commandParameter">The command parameter.</param>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="header">The header.</param>
        /// <returns>Returns success of the task.</returns>
        private async Task<bool> SendCommand(
            int transportCommandsType,
            string actionCommand,
            object? name = null,
            string? commandParameter = null,
            Dictionary<string, string>? dictionary = null,
            string? header = null)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(string.Empty);
            }

            XMLProperties? result = await SendCommandResponseRequired(transportCommandsType, actionCommand, name, commandParameter, dictionary, header).ConfigureAwait(false);
            if (result != null && result.TryGetValue(actionCommand + "Response", out string _))
            {
                return true;
            }

            _logger.LogWarning("{0}: Sending {1} Failed!", Properties.Name, actionCommand);
            if (Tracing)
            {
                _logger.LogDebug("{0}: Response was: {1}", Properties.Name, result);
            }

            return false;
        }

        /// <summary>
        /// Retrieves the DNLA device description and parses the state variable info.
        /// </summary>
        /// <param name="renderService">Service to use.</param>
        /// <param name="wanted">State variable to return.</param>
        private async Task GetStateVariableRange(DeviceService renderService, string wanted)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(string.Empty);
            }

            string xmlstring = string.Empty;

            try
            {
                string url = NormalizeUrl(Properties.BaseUrl, renderService.ScpdUrl);
                if (Tracing)
                {
                    _logger.LogDebug("{0}:-> {1}", Properties.Name, url);
                }

                using var options = new HttpRequestMessage(HttpMethod.Get, url);
                options.Headers.UserAgent.ParseAdd(USERAGENT);
                options.Headers.TryAddWithoutValidation("FriendlyName.DLNA.ORG", FriendlyName);

                using var response = await _httpClientFactory
                    .CreateClient(NamedClient.Default)
                    .SendAsync(options, HttpCompletionOption.ResponseHeadersRead, default).ConfigureAwait(false);

                using var reader = new StreamReader(await response.Content.ReadAsStringAsync().ConfigureAwait(false), Encoding.UTF8);
                xmlstring = await reader.ReadToEndAsync().ConfigureAwait(false);

                if (Tracing)
                {
                    _logger.LogDebug("{0}:<- {1}", Properties.Name, xmlstring);
                }

                var dlnaDescripton = new XmlDocument();
                dlnaDescripton.LoadXml(xmlstring);

                // Use xpath to get to /stateVariable/allowedValueRange/minimum|maximum where /stateVariable/Name == wanted.
                XmlNamespaceManager xmlns = new XmlNamespaceManager(dlnaDescripton.NameTable);
                xmlns.AddNamespace("ns", "urn:schemas-upnp-org:service-1-0");

                XmlNode? minimum = dlnaDescripton.SelectSingleNode(
                    "//ns:stateVariable[ns:name/text()='"
                    + wanted
                    + "']/ns:allowedValueRange/ns:minimum/text()", xmlns);

                XmlNode? maximum = dlnaDescripton.SelectSingleNode(
                    "//ns:stateVariable[ns:name/text()='"
                    + wanted
                    + "']/ns:allowedValueRange/ns:maximum/text()", xmlns);

                // Populate the return value with what we have. Don't worry about null values.
                if (minimum.Value != null && maximum.Value != null)
                {
                    _volRange.Min = int.Parse(minimum.Value, _usCulture);
                    _volRange.Max = int.Parse(maximum.Value, _usCulture);
                    _volRange.Range = _volRange.Max - _volRange.Min;
                    _volRange.FivePoints = (int)Math.Round(_volRange.Range / 100 * 5);
                }
            }
            catch (XmlException)
            {
                _logger.LogError("{0}: Badly formed description document received XML", Properties.Name);
            }
            catch (HttpRequestException ex)
            {
                if (Tracing)
                {
                    _logger.LogDebug(ex, "{0}: Error getting StateVariableRange.", Properties.Name);
                }

                _logger.LogError("{0}: Unable to retrieve ssdp description.", Properties.Name);
            }
        }

        /// <summary>
        /// Checks to see if DLNA subscriptions are implemented, and if so subscribes to changes.
        /// </summary>
        /// <param name="service">The service<see cref="DeviceService"/>.</param>
        /// <param name="sid">The SID for renewal, or null for subscription.</param>
        /// <returns>Task.</returns>
        private async Task<string> SubscribeInternalAsync(DeviceService service, string? sid)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(string.Empty);
            }

            if (service.EventSubUrl != null)
            {
                var url = NormalizeUrl(Properties.BaseUrl, service.EventSubUrl);
                var options = new HttpRequestMessage(new HttpMethod("SUBSCRIBE"), url);
                options.Headers.UserAgent.ParseAdd(USERAGENT);

                // Renewal or subscription?
                if (string.IsNullOrEmpty(sid))
                {
                    if (string.IsNullOrEmpty(_sessionId))
                    {
                        // If we haven't got a GUID yet - get one.
                        _sessionId = Guid.NewGuid().ToString();
                    }

                    // Create a unique callback url based up our GUID.
                    options.Headers.TryAddWithoutValidation("CALLBACK", $"<{_jellyfinUrl}/Dlna/Eventing/{_sessionId}>");
                }
                else
                {
                    // Resubscription id.
                    options.Headers.TryAddWithoutValidation("SID", "uuid:{sid}");
                }

                options.Headers.TryAddWithoutValidation("HOST", _jellyfinUrl);
                options.Headers.TryAddWithoutValidation("NT", "upnp:event");
                options.Headers.TryAddWithoutValidation("TIMEOUT", "Second-60");

                // uPnP v2 permits variables that are to returned at events to be defined.
                options.Headers.TryAddWithoutValidation("STATEVAR", "Mute,Volume,CurrentTrackURI,CurrentTrackMetaData,CurrentTrackDuration,RelativeTimePosition,TransportState");

                if (Tracing)
                {
                    _logger.LogDebug("->{0} : {1}", url, options.Headers.ToString());
                }

                try
                {
                    using var response = await _httpClientFactory.CreateClient(NamedClient.Default)
                        .SendAsync(options, HttpCompletionOption.ResponseHeadersRead)
                        .ConfigureAwait(false);
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        if (!_subscribed)
                        {
                            return response.Headers.GetValues("SID").FirstOrDefault();
                        }
                    }
                    else
                    {
                        _logger.LogDebug("{0}: SUBSCRIBE failed: {1}", Properties.Name, response.Content);
                    }
                }
                catch (HttpException ex)
                {
                    _logger.LogError(ex, "{0}: SUBSCRIBE failed: {1}", Properties.Name, ex.StatusCode);
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
                    // Start listening to DLNA events that come via the url through the PlayToManger.
                    _playToManager.DLNAEvents += ProcessSubscriptionEvent;

                    // Subscribe to both AvTransport and RenderControl events.
                    _transportSid = await SubscribeInternalAsync(GetAvTransportService(), _transportSid).ConfigureAwait(false);
                    _logger.LogDebug("{0}: AVTransport SID {0}.", Properties.Name, _transportSid);

                    _renderSid = await SubscribeInternalAsync(GetServiceRenderingControl(), _renderSid).ConfigureAwait(false);
                    _logger.LogDebug("{0}: RenderControl SID {0}.", Properties.Name, _renderSid);

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
        /// Use in the event trigger, as an async wrapper.
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

            if (service != null && (service.EventSubUrl != null || string.IsNullOrEmpty(sid)))
            {
                var url = NormalizeUrl(Properties.BaseUrl, service.EventSubUrl ?? string.Empty);
                var options = new HttpRequestMessage(new HttpMethod("UNSUBSCRIBE"), url);
                options.Headers.UserAgent.ParseAdd(USERAGENT);
                options.Headers.TryAddWithoutValidation("SID", "uuid: {sid}");

                try
                {
                    using var response = await _httpClientFactory.CreateClient(NamedClient.Default)
                        .SendAsync(options, HttpCompletionOption.ResponseHeadersRead)
                        .ConfigureAwait(false);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        _logger.LogDebug("{0}: UNSUBSCRIBE succeeded.", Properties.Name);
                        return true;
                    }
                    else
                    {
                        _logger.LogDebug("{0}: UNSUBSCRIBE failed. {1}", Properties.Name, response.Content);
                    }
                }
                catch (HttpException ex)
                {
                    _logger.LogError(ex, "{0}: UNSUBSCRIBE failed.", Properties.Name);
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
                if (Tracing)
                {
                    _logger.LogDebug("{0}:<-Event received:\r\n{1}", Properties.Name, args.Response);
                }

                try
                {
                    if (XMLUtilities.ParseXML(args.Response, out XMLProperties reply))
                    {
                        _logger.LogDebug("{0}: Processing a subscription event.", Properties.Name);

                        // Render events.
                        if (reply.TryGetValue("Mute.val", out string value) && int.TryParse(value, out int mute))
                        {
                            _lastMuteRefresh = DateTime.UtcNow;
                            _logger.LogDebug("Muted: {0}", mute);
                            IsMuted = mute == 1;
                        }

                        if (reply.TryGetValue("Volume.val", out value) && int.TryParse(value, out int volume))
                        {
                            _logger.LogDebug("{0}: Volume: {1}", Properties.Name, volume);
                            _lastVolumeRefresh = DateTime.UtcNow;
                            _volume = volume;
                        }

                        if (reply.TryGetValue("TransportState.val", out value)
                            && Enum.TryParse(value, true, out TransportState ts))
                        {
                            _logger.LogDebug("{0} : TransportState: {1}", Properties.Name, ts);

                            // Mustn't process our own change playback event.
                            if (ts != TransportState && TransportState != TransportState.Transitioning)
                            {
                                TransportState = ts;

                                if (ts == TransportState.Stopped)
                                {
                                    _lastTransportRefresh = DateTime.UtcNow;
                                    UpdateMediaInfo(null);
                                    RestartTimer(Normal);
                                }
                            }
                        }

                        // If the position isn't in this update, try to get it.
                        if (TransportState == TransportState.Playing)
                        {
                            if (!reply.TryGetValue("RelativeTimePosition.val", out value))
                            {
                                _logger.LogDebug("{0} : Updating position as not included.", Properties.Name);
                                // Try and get the latest position update
                                await GetPositionRequest().ConfigureAwait(false);
                            }
                            else if (TimeSpan.TryParse(value, _usCulture, out TimeSpan rel))
                            {
                                _logger.LogDebug("{0}: RelativeTimePosition: {1}", Properties.Name, rel);
                                Position = rel;
                                _lastPositionRequest = DateTime.UtcNow;
                            }
                        }

                        if (reply.TryGetValue("CurrentTrackDuration.val", out value)
                            && TimeSpan.TryParse(value, _usCulture, out TimeSpan dur))
                        {
                            _logger.LogDebug("{0}: CurrentTrackDuration: {1}", Properties.Name, dur);
                            Duration = dur;
                        }

                        UBaseObject? currentObject = null;
                        // Have we parsed any item metadata?
                        if (!reply.ContainsKey("DIDL-Lite.xmlns"))
                        {
                            currentObject = await GetMediaInfo().ConfigureAwait(false);
                        }
                        else
                        {
                            currentObject = CreateUBaseObject(reply);
                        }

                        if (currentObject != null)
                        {
                            UpdateMediaInfo(currentObject);
                        }

                        _ = ResubscribeToEvents();
                    }
                    else
                    {
                        _logger.LogDebug("{0}: Received blank event data : ", Properties.Name);
                    }
                }
                catch (ObjectDisposedException)
                {
                    // Ignore.
                }
                catch (HttpException ex)
                {
                    _logger.LogError("{0}: Unable to parse event response.", Properties.Name);
                    _logger.LogDebug(ex, "{0}: Received: ", Properties.Name, args.Response);
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

            _logger.LogDebug("{0}: Timer firing.", Properties.Name);
            try
            {
                var transportState = await GetTransportStatus().ConfigureAwait(false);

                if (transportState == TransportState.Error)
                {
                    _logger.LogError("{0}: Unable to get TransportState.", Properties.Name);
                    // Assume it's a one off.
                    RestartTimer(Normal);
                }
                else
                {
                    TransportState = transportState;

                    if (transportState != TransportState.Error)
                    {
                        // If we're not playing anything make sure we don't get data more
                        // often than neccessary to keep the Session alive.
                        if (transportState == TransportState.Stopped)
                        {
                            UpdateMediaInfo(null);
                            RestartTimer(Never);
                        }
                        else
                        {
                            XMLProperties? response = await SendCommandResponseRequired(TransportCommandsAV, "GetPositionInfo").ConfigureAwait(false);
                            if (response == null || response.Count == 0)
                            {
                                RestartTimer(Normal);
                                return;
                            }

                            if (response.TryGetValue("TrackDuration", out string duration) && TimeSpan.TryParse(duration, _usCulture, out TimeSpan dur))
                            {
                                Duration = dur;
                            }

                            if (response.TryGetValue("RelTime", out string position) && TimeSpan.TryParse(position, _usCulture, out TimeSpan rel))
                            {
                                Position = rel;
                            }

                            // Get current media info.
                            UBaseObject? currentObject = null;

                            // Have we parsed any item metadata?
                            if (!response.ContainsKey("DIDL-Lite.xmlns"))
                            {
                                // If not get some.
                                currentObject = await GetMediaInfo().ConfigureAwait(false);
                            }
                            else
                            {
                                currentObject = CreateUBaseObject(response);
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

                _logger.LogError(ex, "{0}: Error updating device info.", Properties.Name);
                if (_connectFailureCount++ >= 3)
                {
                    _logger.LogDebug("{0}: Disposing device due to loss of connection.", Properties.Name);
                    OnDeviceUnavailable?.Invoke();
                    return;
                }

                RestartTimer(Normal);
            }
        }

        private async Task<bool> SendVolumeRequest(int value)
        {
            var result = false;
            if (_volume != value)
            {
                // Adjust for items that don't have a volume range 0..100.
                result = await SendCommand(TransportCommandsRender, "SetVolume", value).ConfigureAwait(false);
                if (result)
                {
                    _volume = value;
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

            if (_lastVolumeRefresh.AddSeconds(5) <= _lastVolumeRefresh)
            {
                return true;
            }

            string volume = string.Empty;
            try
            {
                XMLProperties? response = await SendCommandResponseRequired(TransportCommandsRender, "GetVolume").ConfigureAwait(false);
                if (response != null && response.TryGetValue("GetVolumeResponse", out volume))
                {
                    if (response.TryGetValue("CurrentVolume", out volume) && int.TryParse(volume, out int value))
                    {
                        _volume = value;
                        if (_volume > 0)
                        {
                            _muteVol = _volume;
                        }

                        _lastVolumeRefresh = DateTime.UtcNow;
                    }

                    return true;
                }

                _logger.LogWarning("{0} : GetVolume Failed.", Properties.Name);
            }
            catch (ObjectDisposedException)
            {
                // Ignore.
            }
            catch (FormatException)
            {
                _logger.LogError("{0} : Error parsing GetVolume {1}.", Properties.Name, volume);
            }

            return false;
        }

        private async Task<bool> SendPauseRequest()
        {
            var result = false;
            if (!IsPaused)
            {
                result = await SendCommand(TransportCommandsAV, "Pause", 1).ConfigureAwait(false);
                if (result)
                {
                    // Stop user from issuing multiple commands.
                    TransportState = TransportState.Paused;
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
                result = await SendCommand(TransportCommandsAV, "Play", 1).ConfigureAwait(false);
                if (result)
                {
                    // Stop user from issuing multiple commands.
                    TransportState = TransportState.Playing;
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
                result = await SendCommand(TransportCommandsAV, "Stop", 1).ConfigureAwait(false);
                if (result)
                {
                    // Stop user from issuing multiple commands.
                    TransportState = TransportState.Stopped;
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

            if (_lastTransportRefresh.AddSeconds(5) >= DateTime.UtcNow)
            {
                return TransportState;
            }

            XMLProperties? response = await SendCommandResponseRequired(TransportCommandsAV, "GetTransportInfo").ConfigureAwait(false);
            if (response != null && response.ContainsKey("GetTransportInfoResponse"))
            {
                if (response.TryGetValue("CurrentTransportState", out string transportState))
                {
                    if (Enum.TryParse(transportState, true, out TransportState state))
                    {
                        _lastTransportRefresh = DateTime.UtcNow;
                        return state;
                    }
                }
            }

            _logger.LogWarning("GetTransportInfo failed.");
            return TransportState.Error;
        }

        private async Task<bool> SendMuteRequest(bool value)
        {
            var result = false;

            if (IsMuted != value)
            {
                result = await SendCommand(TransportCommandsRender, "SetMute", value ? 1 : 0).ConfigureAwait(false);
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

            if (_lastMuteRefresh.AddSeconds(5) <= _lastMuteRefresh)
            {
                return true;
            }

            XMLProperties? response = await SendCommandResponseRequired(TransportCommandsRender, "GetMute").ConfigureAwait(false);
            if (response != null && response.ContainsKey("GetMuteResponse"))
            {
                if (response.TryGetValue("CurrentMute", out string muted))
                {
                    IsMuted = string.Equals(muted, "1", StringComparison.OrdinalIgnoreCase);
                    return true;
                }
            }

            _logger.LogWarning("{0} : GetMute failed.", Properties.Name);
            return false;
        }

        private async Task<bool> SendMediaRequest(MediaData settings)
        {
            var result = false;

            if (!string.IsNullOrEmpty(settings.Url))
            {
                _logger.LogDebug(
                    "{0} : {1} - SetAvTransport Uri: {2} DlnaHeaders: {3}",
                    Properties.Name,
                    settings.Metadata,
                    settings.Url,
                    settings.Headers);

                var dictionary = new Dictionary<string, string>
                {
                    { "CurrentURI", settings.Url },
                    { "CurrentURIMetaData", SecurityElement.Escape(settings.Metadata) }
                };

                result = await SendCommand(
                    TransportCommandsAV,
                    "SetAVTransportURI",
                    settings.Url,
                    dictionary: dictionary,
                    header: settings.Headers).ConfigureAwait(false);

                if (result)
                {
                    await Task.Delay(50).ConfigureAwait(false);

                    result = await SendPlayRequest().ConfigureAwait(false);
                    if (result)
                    {
                        // Update what is playing.
                        _mediaPlaying = settings.Url;
                        _mediaType = settings.MediaType;
                        RestartTimer(Now);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Runs the GetMediaInfo command.
        /// </summary>
        /// <returns>The <see cref="Task{UBaseObject}"/>.</returns>
        private async Task<UBaseObject?> GetMediaInfo()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(string.Empty);
            }

            if (_lastMetaRefresh.AddSeconds(5) >= DateTime.UtcNow)
            {
                return CurrentMediaInfo;
            }

            XMLProperties? response = await SendCommandResponseRequired(TransportCommandsAV, "GetMediaInfo").ConfigureAwait(false);
            if (response != null && response.ContainsKey("GetMediaInfoResponse"))
            {
                _lastMetaRefresh = DateTime.UtcNow;
                RestartTimer(Normal);

                var retVal = new UBaseObject();
                if (response.TryGetValue("item.id", out string value))
                {
                    retVal.Id = value;
                }

                if (response.TryGetValue("item.parentId", out value))
                {
                    retVal.ParentId = value;
                }

                if (response.TryGetValue("title", out value))
                {
                    retVal.Title = value;
                }

                if (response.TryGetValue("albumArtURI", out value))
                {
                    retVal.IconUrl = value;
                }

                if (response.TryGetValue("class", out value))
                {
                    retVal.UpnpClass = value;
                }

                if (response.TryGetValue("CurrentURI", out value) && !string.IsNullOrWhiteSpace(value))
                {
                    retVal.Url = value.Replace("&amp;", "&", StringComparison.OrdinalIgnoreCase);
                }

                return retVal;
            }
            else
            {
                _logger.LogWarning("{0} : GetMediaInfo failed.", Properties.Name);
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

            if (_lastPositionRequest.AddSeconds(5) >= DateTime.UtcNow)
            {
                return true;
            }

            // Update position information.
            try
            {
                XMLProperties? response = await SendCommandResponseRequired(TransportCommandsAV, "GetPositionInfo").ConfigureAwait(false);
                if (response != null && response.ContainsKey("GetPositionInfoResponse"))
                {
                    if (response.TryGetValue("TrackDuration", out string value) && TimeSpan.TryParse(value, _usCulture, out TimeSpan d))
                    {
                        Duration = d;
                    }

                    if (response.TryGetValue("RelTime", out value) && TimeSpan.TryParse(value, _usCulture, out TimeSpan r))
                    {
                        Position = r;
                        _lastPositionRequest = DateTime.Now;
                    }

                    RestartTimer(Normal);
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
        /// <returns>The <see cref="Task{TransportCommands}"/>.</returns>
        private async Task<TransportCommands?> GetProtocolAsync(DeviceService services)
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

            XDocument document = await GetDataAsync(_httpClientFactory, url, _logger).ConfigureAwait(false);
            return TransportCommands.Create(document);
        }

        /// <summary>
        /// Updates the media info, firing events.
        /// </summary>
        /// <param name="mediaInfo">The mediaInfo<see cref="UBaseObject"/>.</param>
        private void UpdateMediaInfo(UBaseObject? mediaInfo)
        {
            var previousMediaInfo = CurrentMediaInfo;
            CurrentMediaInfo = mediaInfo;
            try
            {
                if (mediaInfo != null)
                {
                    if (previousMediaInfo == null)
                    {
                        if (TransportState != TransportState.Stopped && !string.IsNullOrWhiteSpace(mediaInfo.Url))
                        {
                            _logger.LogDebug("{0} : Firing playback started event.", Properties.Name);
                            PlaybackStart?.Invoke(this, new PlaybackEventArgs
                            {
                                MediaInfo = mediaInfo
                            });
                        }
                    }
                    else if (mediaInfo.Equals(previousMediaInfo))
                    {
                        if (!string.IsNullOrWhiteSpace(mediaInfo?.Url))
                        {
                            _logger.LogDebug("{0} : Firing playback progress event.", Properties.Name);
                            PlaybackProgress?.Invoke(this, new PlaybackEventArgs
                            {
                                MediaInfo = mediaInfo
                            });
                        }
                    }
                    else
                    {
                        _logger.LogDebug("{0} : Firing media change event.", Properties.Name);
                        MediaChanged?.Invoke(this, new MediaChangedEventArgs
                        {
                            OldMediaInfo = previousMediaInfo,
                            NewMediaInfo = mediaInfo
                        });
                    }
                }
                else if (previousMediaInfo != null)
                {
                    _logger.LogDebug("{0} : Firing playback stopped event.", Properties.Name);
                    PlaybackStopped?.Invoke(this, new PlaybackEventArgs
                    {
                        MediaInfo = previousMediaInfo
                    });
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types : Don't let errors in the events affect us.
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                _logger.LogError(ex, "{0} : UpdateMediaInfo errored.", Properties.Name);
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

#pragma warning disable SA1401 // Fields should be private
#pragma warning disable IDE1006 // Naming Styles

        internal class ValueRange
        {
            internal double FivePoints;
            internal double Range;
            internal int Min;
            internal int Max = 100;
        }

        internal class MediaData
        {
            internal bool ResetPlay;
            internal string Url = string.Empty;
            internal string Metadata = string.Empty;
            internal string Headers = string.Empty;
            internal DlnaProfileType MediaType = DlnaProfileType.Audio;
        }
    }
}
