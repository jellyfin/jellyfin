using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Session;
using Microsoft.Extensions.Hosting;

namespace Emby.Server.Implementations.EntryPoints
{
    /// <summary>
    /// <see cref="IHostedService"/> responsible for notifying users when associated item data is updated.
    /// </summary>
    public sealed class UserDataChangeNotifier : IHostedService, IDisposable
    {
        private const int UpdateDuration = 500;

        private readonly ISessionManager _sessionManager;
        private readonly IUserDataManager _userDataManager;
        private readonly IUserManager _userManager;

        private readonly Dictionary<Guid, List<BaseItem>> _changedItems = new();
        private readonly Lock _syncLock = new();

        private Timer? _updateTimer;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserDataChangeNotifier"/> class.
        /// </summary>
        /// <param name="userDataManager">The <see cref="IUserDataManager"/>.</param>
        /// <param name="sessionManager">The <see cref="ISessionManager"/>.</param>
        /// <param name="userManager">The <see cref="IUserManager"/>.</param>
        public UserDataChangeNotifier(
            IUserDataManager userDataManager,
            ISessionManager sessionManager,
            IUserManager userManager)
        {
            _userDataManager = userDataManager;
            _sessionManager = sessionManager;
            _userManager = userManager;
        }

        /// <inheritdoc />
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _userDataManager.UserDataSaved += OnUserDataManagerUserDataSaved;

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _userDataManager.UserDataSaved -= OnUserDataManagerUserDataSaved;

            return Task.CompletedTask;
        }

        private void OnUserDataManagerUserDataSaved(object? sender, UserDataSaveEventArgs e)
        {
            if (e.SaveReason == UserDataSaveReason.PlaybackProgress)
            {
                return;
            }

            lock (_syncLock)
            {
                if (_updateTimer is null)
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

                if (!_changedItems.TryGetValue(e.UserId, out List<BaseItem>? keys))
                {
                    keys = new List<BaseItem>();
                    _changedItems[e.UserId] = keys;
                }

                keys.Add(e.Item);

                var baseItem = e.Item;

                // Go up one level for indicators
                if (baseItem is not null)
                {
                    var parent = baseItem.GetOwner() ?? baseItem.GetParent();

                    if (parent is not null)
                    {
                        keys.Add(parent);
                    }
                }
            }
        }

        private async void UpdateTimerCallback(object? state)
        {
            List<KeyValuePair<Guid, List<BaseItem>>> changes;
            lock (_syncLock)
            {
                // Remove dupes in case some were saved multiple times
                changes = _changedItems.ToList();
                _changedItems.Clear();

                if (_updateTimer is not null)
                {
                    _updateTimer.Dispose();
                    _updateTimer = null;
                }
            }

            foreach (var (userId, changedItems) in changes)
            {
                await _sessionManager.SendMessageToUserSessions(
                    [userId],
                    SessionMessageType.UserDataChanged,
                    () => GetUserDataChangeInfo(userId, changedItems),
                    default).ConfigureAwait(false);
            }
        }

        private UserDataChangeInfo GetUserDataChangeInfo(Guid userId, List<BaseItem> changedItems)
        {
            var user = _userManager.GetUserById(userId)
                ?? throw new ArgumentException("Invalid user ID", nameof(userId));

            return new UserDataChangeInfo
            {
                UserId = userId,
                UserDataList = changedItems
                    .DistinctBy(x => x.Id)
                    .Select(i =>
                    {
                        var dto = _userDataManager.GetUserDataDto(i, user);
                        if (dto is null)
                        {
                            return null!;
                        }

                        dto.ItemId = i.Id;
                        return dto;
                    })
                    .Where(e => e is not null)
                    .ToArray()
            };
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _updateTimer?.Dispose();
            _updateTimer = null;
        }
    }
}
