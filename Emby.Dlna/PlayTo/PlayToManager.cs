#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Emby.Dlna.Ssdp;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Networking;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Session;
using Microsoft.Extensions.Logging;

namespace Emby.Dlna.PlayTo
{
    public sealed class PlayToManager : IDisposable
    {
        private readonly ILogger _logger;
        private readonly ISessionManager _sessionManager;

        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;
        private readonly IDlnaManager _dlnaManager;
        private readonly IServerApplicationHost _appHost;
        private readonly IImageProcessor _imageProcessor;
        private readonly IHttpClient _httpClient;
        private readonly IServerConfigurationManager _config;
        private readonly IUserDataManager _userDataManager;
        private readonly ILocalizationManager _localization;
        private readonly IDeviceDiscovery _deviceDiscovery;
        private readonly IMediaSourceManager _mediaSourceManager;
        private readonly IMediaEncoder _mediaEncoder;
        private readonly List<Device> _devices;

        private bool _disposed;
        private SemaphoreSlim _sessionLock = new SemaphoreSlim(1, 1);
        private CancellationTokenSource _disposeCancellationTokenSource = new CancellationTokenSource();

        public PlayToManager(ILogger logger, ISessionManager sessionManager, ILibraryManager libraryManager, IUserManager userManager, IDlnaManager dlnaManager, IServerApplicationHost appHost, IImageProcessor imageProcessor, IDeviceDiscovery? deviceDiscovery, IHttpClient httpClient, IServerConfigurationManager config, IUserDataManager userDataManager, ILocalizationManager localization, IMediaSourceManager mediaSourceManager, IMediaEncoder mediaEncoder)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
            _libraryManager = libraryManager ?? throw new ArgumentNullException(nameof(libraryManager));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _dlnaManager = dlnaManager ?? throw new ArgumentNullException(nameof(dlnaManager));
            _appHost = appHost ?? throw new ArgumentNullException(nameof(appHost));
            _imageProcessor = imageProcessor ?? throw new ArgumentNullException(nameof(imageProcessor));
            _deviceDiscovery = deviceDiscovery ?? throw new ArgumentNullException(nameof(deviceDiscovery));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _userDataManager = userDataManager ?? throw new ArgumentNullException(nameof(userDataManager));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _mediaSourceManager = mediaSourceManager ?? throw new ArgumentNullException(nameof(mediaSourceManager));
            _mediaEncoder = mediaEncoder ?? throw new ArgumentNullException(nameof(mediaEncoder));
            _devices = new List<Device>();
        }

        public void Start()
        {
            _deviceDiscovery.DeviceDiscovered += OnDeviceDiscoveryDeviceDiscovered;
            _deviceDiscovery.Start();
        }

        private async void OnDeviceDiscoveryDeviceDiscovered(object sender, GenericEventArgs<UpnpDeviceInfo> e)
        {
            if (_disposed)
            {
                return;
            }

            var info = e.Argument;

            if (!info.Headers.TryGetValue("USN", out string usn))
            {
                usn = string.Empty;
            }

            if (!info.Headers.TryGetValue("NT", out string nt))
            {
                nt = string.Empty;
            }

            string location = info.Location.ToString();

            // It has to report that it's a media renderer
            if (usn.IndexOf("MediaRenderer:", StringComparison.OrdinalIgnoreCase) == -1 &&
                     nt.IndexOf("MediaRenderer:", StringComparison.OrdinalIgnoreCase) == -1)
            {
                // _logger.LogDebug("Upnp device {0} does not contain a MediaRenderer device (0).", location);
                return;
            }

            var cancellationToken = _disposeCancellationTokenSource.Token;

            await _sessionLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (_disposed)
                {
                    return;
                }

                if (_sessionManager.Sessions.Any(i => usn.IndexOf(i.DeviceId, StringComparison.OrdinalIgnoreCase) != -1))
                {
                    return;
                }

                await AddDevice(info, location, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating PlayTo device.");
            }
            finally
            {
                _sessionLock.Release();
            }
        }

        private string GetUuid(string usn)
        {
            var found = false;
            var index = usn.IndexOf("uuid:", StringComparison.OrdinalIgnoreCase);
            if (index != -1)
            {
                usn = usn.Substring(index);
                found = true;
            }

            index = usn.IndexOf("::", StringComparison.OrdinalIgnoreCase);
            if (index != -1)
            {
                usn = usn.Substring(0, index);
            }

            if (found)
            {
                return usn;
            }

            return usn.GetMD5().ToString("N", CultureInfo.InvariantCulture);
        }

        private async Task AddDevice(UpnpDeviceInfo info, string location, CancellationToken cancellationToken)
        {
            var uri = info.Location;
            _logger.LogDebug("Attempting to create PlayToController from location {0}", location);

            _logger.LogDebug("Logging session activity from location {0}", location);
            if (info.Headers.TryGetValue("USN", out string uuid))
            {
                uuid = GetUuid(uuid);
            }
            else
            {
                uuid = location.GetMD5().ToString("N", CultureInfo.InvariantCulture);
            }

            var sessionInfo = _sessionManager.LogSessionActivity("DLNA", _appHost.ApplicationVersionString, uuid, null, uri.OriginalString, null);

            var controller = sessionInfo.SessionControllers.OfType<PlayToController>().FirstOrDefault();

            if (controller == null)
            {
                var device = await Device.CreateuPnpDeviceAsync(uri, _httpClient, _config, _logger, cancellationToken).ConfigureAwait(false);
                _devices.Add(device);

                string deviceName = device.Properties.Name;

                _sessionManager.UpdateDeviceName(sessionInfo.Id, deviceName);

                controller = new PlayToController(
                    sessionInfo,
                    _sessionManager,
                    _libraryManager,
                    _logger,
                    _dlnaManager,
                    _userManager,
                    _imageProcessor,
                    _appHost.GetSmartApiUrl(info.LocalIpAddress),
                    null,
                    _deviceDiscovery,
                    _userDataManager,
                    _localization,
                    _mediaSourceManager,
                    _config,
                    _mediaEncoder);

                sessionInfo.AddController(controller);

                controller.Init(device);

                var profile = _dlnaManager.GetProfile(device.Properties.ToDeviceIdentification()) ??
                              _dlnaManager.GetDefaultProfile();

                _sessionManager.ReportCapabilities(sessionInfo.Id, new ClientCapabilities
                {
                    PlayableMediaTypes = profile.GetSupportedMediaTypes(),

                    SupportedCommands = new string[]
                    {
                            GeneralCommandType.VolumeDown.ToString(),
                            GeneralCommandType.VolumeUp.ToString(),
                            GeneralCommandType.Mute.ToString(),
                            GeneralCommandType.Unmute.ToString(),
                            GeneralCommandType.ToggleMute.ToString(),
                            GeneralCommandType.SetVolume.ToString(),
                            GeneralCommandType.SetAudioStreamIndex.ToString(),
                            GeneralCommandType.SetSubtitleStreamIndex.ToString(),
                            GeneralCommandType.PlayMediaSource.ToString()
                    },

                    SupportsMediaControl = true
                });
                _logger.LogInformation("DLNA Session created for {0} - {1}", device.Properties.Name, device.Properties.ModelName);
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _logger.LogDebug("Disposed.");

            _deviceDiscovery.DeviceDiscovered -= OnDeviceDiscoveryDeviceDiscovered;
            _deviceDiscovery?.Dispose();

            try
            {
                _disposeCancellationTokenSource.Cancel();
            }
            catch
            {
            }

            // Dispose the CreateuPnpDeviceAsync created in AddDevice.
            foreach (var device in _devices)
            {
                device?.Dispose();
            }

            // Stop any active sessions and dispose of the PlayToControllers created.
            _sessionManager.Sessions.ToList().ForEach(s =>
                {
                    s.StopController<PlayToController>();
                    _sessionManager.ReportSessionEnded(s.Id);
                });

            _disposeCancellationTokenSource.Dispose();

            _disposed = true;
        }
    }
}
