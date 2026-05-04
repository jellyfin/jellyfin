using System;
using System.Globalization;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Events;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Notifications;

namespace Jellyfin.Server.Implementations.Events.Consumers.Updates
{
    public abstract class PluginActivityLogConsumer<TEventArgs> : IEventConsumer<TEventArgs>
        where TEventArgs : EventArgs
    {
        private readonly ILocalizationManager _localizationManager;
        private readonly IActivityManager _activityManager;
        private readonly string _nameLocalizationKey;
        private readonly NotificationType _notificationType;
        private readonly Func<TEventArgs, string> _getName;
        private readonly Func<TEventArgs, object?>? _getVersion;
        private readonly Func<TEventArgs, string?>? _getOverview;

        protected PluginActivityLogConsumer(
            ILocalizationManager localizationManager,
            IActivityManager activityManager,
            string nameLocalizationKey,
            NotificationType notificationType,
            Func<TEventArgs, string> getName,
            Func<TEventArgs, object?>? getVersion = null,
            Func<TEventArgs, string?>? getOverview = null)
        {
            _localizationManager = localizationManager;
            _activityManager = activityManager;
            _nameLocalizationKey = nameLocalizationKey;
            _notificationType = notificationType;
            _getName = getName;
            _getVersion = getVersion;
            _getOverview = getOverview;
        }

        public async Task OnEvent(TEventArgs eventArgs)
        {
            var activityLog = new ActivityLog(
                string.Format(
                    CultureInfo.InvariantCulture,
                    _localizationManager.GetLocalizedString(_nameLocalizationKey),
                    _getName(eventArgs)),
                _notificationType.ToString(),
                Guid.Empty);

            if (_getVersion is not null)
            {
                activityLog.ShortOverview = string.Format(
                    CultureInfo.InvariantCulture,
                    _localizationManager.GetLocalizedString("VersionNumber"),
                    _getVersion(eventArgs));
            }

            if (_getOverview is not null)
            {
                activityLog.Overview = _getOverview(eventArgs);
            }

            await _activityManager.CreateAsync(activityLog).ConfigureAwait(false);
        }
    }
}
