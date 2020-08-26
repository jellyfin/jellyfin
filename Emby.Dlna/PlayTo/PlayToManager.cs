#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Dlna.PlayTo.EventArgs;
using Emby.Dlna.Ssdp;
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
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Notifications;
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
        private readonly INotificationManager _notificationManager;
        private readonly SemaphoreSlim _sessionLock = new SemaphoreSlim(1, 1);
        private readonly CancellationTokenSource _disposeCancellationTokenSource = new CancellationTokenSource();
        private bool _disposed;

        public PlayToManager(
            ILogger logger,
            ISessionManager sessionManager,
            ILibraryManager libraryManager,
            IUserManager userManager,
            IDlnaManager dlnaManager,
            IServerApplicationHost appHost,
            IImageProcessor imageProcessor,
            IDeviceDiscovery deviceDiscovery,
            IHttpClient httpClient,
            IServerConfigurationManager config,
            IUserDataManager userDataManager,
            ILocalizationManager localization,
            IMediaSourceManager mediaSourceManager,
            IMediaEncoder mediaEncoder,
            INotificationManager notificationManager)
        {
            _logger = logger;
            _sessionManager = sessionManager;
            _libraryManager = libraryManager;
            _userManager = userManager;
            _dlnaManager = dlnaManager;
            _appHost = appHost;
            _imageProcessor = imageProcessor;
            _deviceDiscovery = deviceDiscovery;
            _httpClient = httpClient;
            _config = config;
            _userDataManager = userDataManager;
            _localization = localization;
            _mediaSourceManager = mediaSourceManager;
            _mediaEncoder = mediaEncoder;
            _notificationManager = notificationManager;

            _deviceDiscovery.DeviceDiscovered += OnDeviceDiscoveryDeviceDiscovered;
            _deviceDiscovery.Start();
        }

        public event EventHandler<DlnaEventArgs> DLNAEvents;

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
        public async Task SendNotification(DeviceInterface device, NotificationRequest notification)
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

        private static string GetUuid(string usn)
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

        private async Task<bool> AddDevice(UpnpDeviceInfo info, string location)
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

                var device = await DeviceInterface.CreateuPnpDeviceAsync(this, uri, _httpClient, _logger, serverAddress).ConfigureAwait(false);
                if (device == null)
                {
                    return false;
                }

                string deviceName = device.Properties.Name;

                _sessionManager.UpdateDeviceName(sessionInfo.Id, deviceName);

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
                    _deviceDiscovery,
                    _userDataManager,
                    _localization,
                    _mediaSourceManager,
                    _config,
                    _mediaEncoder);
#pragma warning restore CA2000 // Dispose objects before losing scope

                sessionInfo.AddController(controller);

                controller.Init(device);

                var profile = _dlnaManager.GetProfile(device.Properties.ToDeviceIdentification()) ??
                              _dlnaManager.GetDefaultProfile();

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

                return true;
            }

            return false;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _deviceDiscovery.DeviceDiscovered -= OnDeviceDiscoveryDeviceDiscovered;

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

            _sessionLock?.Dispose();
            _deviceDiscovery?.Dispose();

            _disposed = true;
        }
    }
}
