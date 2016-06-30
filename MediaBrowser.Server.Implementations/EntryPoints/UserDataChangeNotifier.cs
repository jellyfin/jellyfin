using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Session;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.EntryPoints
{
    class UserDataChangeNotifier : IServerEntryPoint
    {
        private readonly ISessionManager _sessionManager;
        private readonly ILogger _logger;
        private readonly IUserDataManager _userDataManager;
        private readonly IUserManager _userManager;

        private readonly object _syncLock = new object();
        private Timer UpdateTimer { get; set; }
        private const int UpdateDuration = 500;

        private readonly Dictionary<Guid, List<IHasUserData>> _changedItems = new Dictionary<Guid, List<IHasUserData>>();

        public UserDataChangeNotifier(IUserDataManager userDataManager, ISessionManager sessionManager, ILogger logger, IUserManager userManager)
        {
            _userDataManager = userDataManager;
            _sessionManager = sessionManager;
            _logger = logger;
            _userManager = userManager;
        }

        public void Run()
        {
            _userDataManager.UserDataSaved += _userDataManager_UserDataSaved;
        }

        void _userDataManager_UserDataSaved(object sender, UserDataSaveEventArgs e)
        {
            if (e.SaveReason == UserDataSaveReason.PlaybackProgress)
            {
                return;
            }

            lock (_syncLock)
            {
                if (UpdateTimer == null)
                {
                    UpdateTimer = new Timer(UpdateTimerCallback, null, UpdateDuration,
                                                   Timeout.Infinite);
                }
                else
                {
                    UpdateTimer.Change(UpdateDuration, Timeout.Infinite);
                }

                List<IHasUserData> keys;

                if (!_changedItems.TryGetValue(e.UserId, out keys))
                {
                    keys = new List<IHasUserData>();
                    _changedItems[e.UserId] = keys;
                }

                keys.Add(e.Item);

                var baseItem = e.Item as BaseItem;

                // Go up one level for indicators
                if (baseItem != null)
                {
                    var parent = baseItem.GetParent();

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

                SendNotifications(changes, CancellationToken.None);

                if (UpdateTimer != null)
                {
                    UpdateTimer.Dispose();
                    UpdateTimer = null;
                }
            }
        }

        private async Task SendNotifications(IEnumerable<KeyValuePair<Guid, List<IHasUserData>>> changes, CancellationToken cancellationToken)
        {
            foreach (var pair in changes)
            {
                var userId = pair.Key;
                var userSessions = _sessionManager.Sessions
                    .Where(u => u.ContainsUser(userId) && u.SessionController != null && u.IsActive)
                    .ToList();

                if (userSessions.Count > 0)
                {
                    var user = _userManager.GetUserById(userId);

                    var dtoList = pair.Value
                        .DistinctBy(i => i.Id)
                        .Select(i =>
                        {
                            var dto = _userDataManager.GetUserDataDto(i, user).Result;
                            dto.ItemId = i.Id.ToString("N");
                            return dto;
                        })
                        .ToList();

                    var info = new UserDataChangeInfo
                    {
                        UserId = userId.ToString("N"),

                        UserDataList = dtoList
                    };

                    foreach (var userSession in userSessions)
                    {
                        try
                        {
                            await userSession.SessionController.SendUserDataChangeInfo(info, cancellationToken).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            _logger.ErrorException("Error sending UserDataChanged message", ex);
                        }
                    }
                }

            }
        }

        public void Dispose()
        {
            if (UpdateTimer != null)
            {
                UpdateTimer.Dispose();
                UpdateTimer = null;
            }

            _userDataManager.UserDataSaved -= _userDataManager_UserDataSaved;
        }
    }
}
