using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Emby.Dlna.Configuration;
using Emby.Dlna.PlayTo.EventArgs;
using Jellyfin.Data.Events;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Notifications;
using MediaBrowser.Model.Session;
using Microsoft.Extensions.Logging;

namespace Emby.Dlna.PlayTo
{
    /// <summary>
    /// Definition for the <see cref="PlayToManager"/> class.
    /// </summary>
    public sealed class PlayToManager : IDisposable, IPlayToManager
    {
        private readonly object _syncLock;
        private readonly ILogger _logger;
        private readonly ISessionManager _sessionManager;
        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;
        private readonly IDlnaManager _dlnaManager;
        private readonly IServerApplicationHost _appHost;
        private readonly IImageProcessor _imageProcessor;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IServerConfigurationManager _configurationManager;
        private readonly IUserDataManager _userDataManager;
        private readonly ILocalizationManager _localization;
        private readonly IMediaSourceManager _mediaSourceManager;
        private readonly IMediaEncoder _mediaEncoder;
        private readonly INotificationManager _notificationManager;
        private readonly INetworkManager _networkManager;
        private readonly SemaphoreSlim _sessionLock = new SemaphoreSlim(1, 1);
        private readonly CancellationTokenSource _disposeCancellationTokenSource = new CancellationTokenSource();
        private readonly List<PlayToDevice> _devices = new List<PlayToDevice>();
        private readonly ILoggerFactory _loggerFactory;

        private ISsdpPlayToLocator? _playToLocator;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayToManager"/> class.
        /// </summary>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> instance.</param>
        /// <param name="sessionManager">The <see cref="ISessionManager"/> instance.</param>
        /// <param name="libraryManager">The <see cref="ILibraryManager"/> instance.</param>
        /// <param name="userManager">The <see cref="IUserManager"/> instance.</param>
        /// <param name="dlnaManager">The <see cref="IDlnaManager"/> instance.</param>
        /// <param name="appHost">The <see cref="IServerApplicationHost"/> instance.</param>
        /// <param name="imageProcessor">The <see cref="IImageProcessor"/> instance.</param>
        /// <param name="httpClientFactory">The <see cref="IHttpClientFactory"/> instance.</param>
        /// <param name="configurationManager">The <see cref="IServerConfigurationManager"/> instance.</param>
        /// <param name="userDataManager">The <see cref="IUserDataManager"/> instance.</param>
        /// <param name="localization">The <see cref="ILocalizationManager"/> instance.</param>
        /// <param name="mediaSourceManager">The <see cref="IMediaSourceManager"/> instance.</param>
        /// <param name="mediaEncoder">The <see cref="IMediaEncoder"/> instance.</param>
        /// <param name="notificationManager">The <see cref="INotificationManager"/> instance.</param>
        /// <param name="networkManager">The <see cref="INetworkManager"/> instance.</param>
        public PlayToManager(
            ILoggerFactory loggerFactory,
            ISessionManager sessionManager,
            ILibraryManager libraryManager,
            IUserManager userManager,
            IDlnaManager dlnaManager,
            IServerApplicationHost appHost,
            IImageProcessor imageProcessor,
            IHttpClientFactory httpClientFactory,
            IServerConfigurationManager configurationManager,
            IUserDataManager userDataManager,
            ILocalizationManager localization,
            IMediaSourceManager mediaSourceManager,
            IMediaEncoder mediaEncoder,
            INotificationManager notificationManager,
            INetworkManager networkManager)
        {
            _logger = loggerFactory.CreateLogger<PlayToManager>();
            _loggerFactory = loggerFactory;
            _sessionManager = sessionManager;
            _libraryManager = libraryManager;
            _userManager = userManager;
            _dlnaManager = dlnaManager;
            _appHost = appHost;
            _imageProcessor = imageProcessor;
            _httpClientFactory = httpClientFactory;
            _configurationManager = configurationManager ?? throw new NullReferenceException(nameof(configurationManager));
            _userDataManager = userDataManager;
            _localization = localization;
            _mediaSourceManager = mediaSourceManager;
            _mediaEncoder = mediaEncoder;
            _notificationManager = notificationManager;
            _networkManager = networkManager;
            _syncLock = new object();
            _configurationManager.NamedConfigurationUpdated += NamedConfigurationUpdated;
            CheckComponents();
        }

        /// <summary>
        /// An event handler that is triggered on reciept of a PlayTo client subscription event.
        /// </summary>
        public event EventHandler<DlnaEventArgs>? DLNAEvents;

        /// <summary>
        /// Gets a value indicating whether gets the current status of DLNA playTo is enabled.
        /// </summary>
        public bool IsPlayToEnabled => _configurationManager.GetDlnaConfiguration().EnablePlayTo;

        /// <summary>
        /// Method that triggers a DLNAEvents event.
        /// </summary>
        /// <param name="args">A DlnaEventArgs instance containing the event message.</param>
        /// <returns>An awaitable <see cref="Task"/>.</returns>
        public Task NotifyDevice(DlnaEventArgs args)
        {
            DLNAEvents?.Invoke(this, args);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Sends a client notification message.
        /// </summary>
        /// <param name="device">Device sending the notification.</param>
        /// <param name="notification">The notification to send.</param>
        /// <returns>Task.</returns>
        public async Task SendNotification(PlayToDevice device, NotificationRequest notification)
        {
            try
            {
                await _notificationManager.SendNotification(notification, null, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{0} : Error sending notification.", device?.Properties.Name);
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_disposed)
            {
                _ = Dispose(true);
            }

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// The GetUuid.
        /// </summary>
        /// <param name="usn">The usn<see cref="string"/>.</param>
        /// <returns>The <see cref="string"/>.</returns>
        private static string GetUuid(string usn)
        {
            const string UuidStr = "uuid:";
            const string UuidColonStr = "::";

            var index = usn.IndexOf(UuidStr, StringComparison.OrdinalIgnoreCase);
            if (index != -1)
            {
                return usn.Substring(index + UuidStr.Length);
            }

            index = usn.IndexOf(UuidColonStr, StringComparison.OrdinalIgnoreCase);
            if (index != -1)
            {
                usn = usn.Substring(0, index + UuidColonStr.Length);
            }

            return usn.GetMD5().ToString("N", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Triggerer every time the configuration is updated.
        /// </summary>
        /// <param name="sender">Configuration instance.</param>
        /// <param name="e">Configuration that was updated.</param>
        private void NamedConfigurationUpdated(object sender, ConfigurationUpdateEventArgs e)
        {
            if (string.Equals(e.Key, "dlna", StringComparison.OrdinalIgnoreCase))
            {
                CheckComponents();
            }
        }

        /// <summary>
        /// The CheckComponents.
        /// </summary>
        private void CheckComponents()
        {
            lock (_syncLock)
            {
                if (IsPlayToEnabled)
                {
                    if (_playToLocator == null)
                    {
                        _logger.LogDebug("DLNA PlayTo: Starting Device Discovery.");
                        _playToLocator = new SsdpPlayToLocator(_loggerFactory.CreateLogger<SsdpPlayToLocator>(), _networkManager, _configurationManager, _appHost);
                        _playToLocator.DeviceDiscovered += OnDeviceDiscoveryDeviceDiscovered;
                        _playToLocator.Start();
                    }
                }
                else if (_playToLocator != null)
                {
                    _logger.LogDebug("DLNA PlayTo: Stopping Service.");
                    lock (_syncLock)
                    {
                        _playToLocator.DeviceDiscovered -= OnDeviceDiscoveryDeviceDiscovered;
                        _playToLocator?.Dispose();
                        _playToLocator = null;
                    }
                }
            }
        }

        /// <summary>
        /// Override this method and dispose any objects you own the lifetime of if disposing is true.
        /// </summary>
        /// <param name="disposing">True if managed objects should be disposed, if false, only unmanaged resources should be released.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        private async Task Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _logger.LogDebug("Disposing instance.");

                    _configurationManager.NamedConfigurationUpdated -= NamedConfigurationUpdated;

                    if (_playToLocator != null)
                    {
                        _playToLocator.DeviceDiscovered -= OnDeviceDiscoveryDeviceDiscovered;
                        _playToLocator?.Dispose();
                        _playToLocator = null;
                    }

                    var cancellationToken = _disposeCancellationTokenSource.Token;
                    await _sessionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
                    try
                    {
                        // Stop any active sessions and dispose of the PlayToControllers created.
                        _sessionManager.Sessions.ToList().ForEach(s =>
                            {
                                s.StopController<PlayToController>();
                                _sessionManager.ReportSessionEnded(s.Id);
                            });
                    }
                    catch (OperationCanceledException)
                    {
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error disposing of PlayToControllers.");
                    }
                    finally
                    {
                        _sessionLock.Release();
                    }

                    try
                    {
                        _disposeCancellationTokenSource.Cancel();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Error while disposing PlayToManager");
                    }

                    _sessionLock.Dispose();

                    _disposeCancellationTokenSource.Dispose();

                    // Dispose the PlayToDevices created in AddDevice.
                    foreach (var device in _devices)
                    {
                        device?.Dispose();
                    }
                }

                _disposed = true;
            }
        }

        /// <summary>
        /// The OnDeviceDiscoveryDeviceDiscovered.
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/>.</param>
        /// <param name="e">The e<see cref="GenericEventArgs{UpnpDeviceInfo}"/>.</param>
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
            if (!usn.Contains("MediaRenderer:", StringComparison.OrdinalIgnoreCase) && !nt.Contains("MediaRenderer:", StringComparison.OrdinalIgnoreCase))
            {
                // _logger.LogDebug("Upnp device {0} does not contain a MediaRenderer device {1} {2}.", location, usn, nt);
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

                _logger.LogDebug("Adding device found at {0} : ", info.LocalIpAddress);
                await AddDevice(info, location).ConfigureAwait(false);
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

        /// <summary>
        /// The AddDevice.
        /// </summary>
        /// <param name="info">The info<see cref="UpnpDeviceInfo"/>.</param>
        /// <param name="location">The location<see cref="string"/>.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        private async Task AddDevice(UpnpDeviceInfo info, string location)
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
                string serverAddress = _appHost.GetSmartApiUrl(info.LocalIpAddress);

                var device = await PlayToDevice.CreateDevice(
                    this,
                    uri,
                    _httpClientFactory,
                    _logger,
                    _configurationManager,
                    serverAddress).ConfigureAwait(false);
                if (device == null)
                {
                    return;
                }

                _devices.Add(device);

                _sessionManager.UpdateDeviceName(sessionInfo.Id, device.Properties.Name);

#pragma warning disable CA2000 // Dispose objects before losing scope: This object is disposed of in the dispose section.
                controller = new PlayToController(
                    sessionInfo,
                    _sessionManager,
                    _libraryManager,
                    _logger,
                    _dlnaManager,
                    _userManager,
                    _imageProcessor,
                    serverAddress,
                    null,
                    _playToLocator ?? throw new NullReferenceException(nameof(_playToLocator)),
                    _userDataManager,
                    _localization,
                    _mediaSourceManager,
                    _configurationManager,
                    _mediaEncoder,
                    device);
#pragma warning restore CA2000 // Dispose objects before losing scope

                sessionInfo.AddController(controller);

                var profile = _dlnaManager.GetProfile(device.Properties) ??
                              _dlnaManager.GetDefaultProfile(device.Properties);

                _sessionManager.ReportCapabilities(sessionInfo.Id, new ClientCapabilities
                {
                    PlayableMediaTypes = profile.GetSupportedMediaTypes(),

                    SupportedCommands = new[]
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

                return;
            }

            return;
        }
    }
}
