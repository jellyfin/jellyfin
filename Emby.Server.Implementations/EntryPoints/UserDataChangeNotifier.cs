#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Session;

namespace Emby.Server.Implementations.EntryPoints
{
    public sealed class UserDataChangeNotifier : IServerEntryPoint
    {
        private const int UpdateDuration = 500;

        private readonly ISessionManager _sessionManager;
        private readonly IUserDataManager _userDataManager;
        private readonly IUserManager _userManager;

        private readonly Dictionary<Guid, List<BaseItem>> _changedItems = new Dictionary<Guid, List<BaseItem>>();

        private readonly object _syncLock = new object();
        private Timer _updateTimer;


        public UserDataChangeNotifier(IUserDataManager userDataManager, ISessionManager sessionManager, IUserManager userManager)
        {
            _userDataManager = userDataManager;
            _sessionManager = sessionManager;
            _userManager = userManager;
        }

        public Task RunAsync()
        {
            _userDataManager.UserDataSaved += OnUserDataManagerUserDataSaved;

            return Task.CompletedTask;
        }

        void OnUserDataManagerUserDataSaved(object sender, UserDataSaveEventArgs e)
        {
            if (e.SaveReason == UserDataSaveReason.PlaybackProgress)
            {
                return;
            }

            lock (_syncLock)
            {
                if (_updateTimer == null)
                {
                    _updateTimer = new Timer(
                        UpdateTimerCallback,
                        null,
                        UpdateDuration,
                        Timeout.Infinite);
                }
                else
                {
                    _updateTimer.Change(UpdateDuration, Timeout.Infinite);
                }

                if (!_changedItems.TryGetValue(e.UserId, out List<BaseItem> keys))
                {
                    keys = new List<BaseItem>();
                    _changedItems[e.UserId] = keys;
                }

                keys.Add(e.Item);

                var baseItem = e.Item;

                // Go up one level for indicators
                if (baseItem != null)
                {
                    var parent = baseItem.GetOwner() ?? baseItem.GetParent();

                    if (parent != null)
                    {
                        keys.Add(parent);
                    }
                }
            }
        }

        private void UpdateTimerCallback(object state)
        {
            lock (_syncLock)
            {
                // Remove dupes in case some were saved multiple times
                var changes = _changedItems.ToList();
                _changedItems.Clear();

                var task = SendNotifications(changes, CancellationToken.None);

                if (_updateTimer != null)
                {
                    _updateTimer.Dispose();
                    _updateTimer = null;
                }
            }
        }

        private async Task SendNotifications(List<KeyValuePair<Guid, List<BaseItem>>> changes, CancellationToken cancellationToken)
        {
            foreach (var pair in changes)
            {
                await SendNotifications(pair.Key, pair.Value, cancellationToken).ConfigureAwait(false);
            }
        }

        private Task SendNotifications(Guid userId, List<BaseItem> changedItems, CancellationToken cancellationToken)
        {
            return _sessionManager.SendMessageToUserSessions(new List<Guid> { userId }, "UserDataChanged", () => GetUserDataChangeInfo(userId, changedItems), cancellationToken);
        }

        private UserDataChangeInfo GetUserDataChangeInfo(Guid userId, List<BaseItem> changedItems)
        {
            var user = _userManager.GetUserById(userId);

            var dtoList = changedItems
                .GroupBy(x => x.Id)
                .Select(x => x.First())
                .Select(i =>
                {
                    var dto = _userDataManager.GetUserDataDto(i, user);
                    dto.ItemId = i.Id.ToString("N", CultureInfo.InvariantCulture);
                    return dto;
                })
                .ToArray();

            var userIdString = userId.ToString("N", CultureInfo.InvariantCulture);

            return new UserDataChangeInfo
            {
                UserId = userIdString,

                UserDataList = dtoList
            };
        }

        public void Dispose()
        {
            if (_updateTimer != null)
            {
                _updateTimer.Dispose();
                _updateTimer = null;
            }

            _userDataManager.UserDataSaved -= OnUserDataManagerUserDataSaved;
        }
    }
}
