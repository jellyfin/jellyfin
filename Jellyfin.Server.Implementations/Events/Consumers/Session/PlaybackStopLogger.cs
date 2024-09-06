using System;
using System.Globalization;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Notifications;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Implementations.Events.Consumers.Session
{
    /// <summary>
    /// Creates an activity log entry whenever a user stops playback.
    /// </summary>
    public class PlaybackStopLogger : IEventConsumer<PlaybackStopEventArgs>
    {
        private readonly ILogger<PlaybackStopLogger> _logger;
        private readonly ILocalizationManager _localizationManager;
        private readonly IActivityManager _activityManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlaybackStopLogger"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="localizationManager">The localization manager.</param>
        /// <param name="activityManager">The activity manager.</param>
        public PlaybackStopLogger(ILogger<PlaybackStopLogger> logger, ILocalizationManager localizationManager, IActivityManager activityManager)
        {
            _logger = logger;
            _localizationManager = localizationManager;
            _activityManager = activityManager;
        }

        /// <inheritdoc />
        public async Task OnEvent(PlaybackStopEventArgs eventArgs)
        {
            var item = eventArgs.MediaInfo;

            if (item is null)
            {
                _logger.LogWarning("PlaybackStopped reported with null media info.");
                return;
            }

            if (eventArgs.Item is not null && eventArgs.Item.IsThemeMedia)
            {
                // Don't report theme song or local trailer playback
                return;
            }

            if (eventArgs.Users.Count == 0)
            {
                return;
            }

            var user = eventArgs.Users[0];

            var notificationType = GetPlaybackStoppedNotificationType(item.MediaType);
            if (notificationType is null)
            {
                return;
            }

            await _activityManager.CreateAsync(new ActivityLog(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        _localizationManager.GetLocalizedString("UserStoppedPlayingItemWithValues"),
                        user.Username,
                        GetItemName(item),
                        eventArgs.DeviceName),
                    notificationType,
                    user.Id)
                {
                    ItemId = eventArgs.Item?.Id.ToString("N", CultureInfo.InvariantCulture),
                })
                .ConfigureAwait(false);
        }

        private static string GetItemName(BaseItemDto item)
        {
            var name = item.Name;

            if (!string.IsNullOrEmpty(item.SeriesName))
            {
                name = item.SeriesName + " - " + name;
            }

            if (item.Artists is not null && item.Artists.Count > 0)
            {
                name = item.Artists[0] + " - " + name;
            }

            return name;
        }

        private static string? GetPlaybackStoppedNotificationType(MediaType mediaType)
        {
            if (mediaType == MediaType.Audio)
            {
                return NotificationType.AudioPlaybackStopped.ToString();
            }

            if (mediaType == MediaType.Video)
            {
                return NotificationType.VideoPlaybackStopped.ToString();
            }

            return null;
        }
    }
}
