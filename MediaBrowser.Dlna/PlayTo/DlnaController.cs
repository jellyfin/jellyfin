using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;

namespace MediaBrowser.Dlna.PlayTo
{
    public class PlayToController : ISessionController, IDisposable
    {
        private Device _device;
        private BaseItem _currentItem = null;
        private readonly SessionInfo _session;
        private readonly ISessionManager _sessionManager;
        private readonly IItemRepository _itemRepository;
        private readonly ILibraryManager _libraryManager;
        private readonly INetworkManager _networkManager;
        private readonly ILogger _logger;
        private readonly IDlnaManager _dlnaManager;
        private bool _playbackStarted = false;

        public bool SupportsMediaRemoteControl
        {
            get { return true; }
        }

        public bool IsSessionActive
        {
            get
            {
                if (_device == null || _device.UpdateTime == default(DateTime))
                    return false;

                return DateTime.UtcNow <= _device.UpdateTime.AddSeconds(30);
            }
        }

        public PlayToController(SessionInfo session, ISessionManager sessionManager, IItemRepository itemRepository, ILibraryManager libraryManager, ILogger logger, INetworkManager networkManager, IDlnaManager dlnaManager)
        {
            _session = session;
            _itemRepository = itemRepository;
            _sessionManager = sessionManager;
            _libraryManager = libraryManager;
            _networkManager = networkManager;
            _dlnaManager = dlnaManager;
            _logger = logger;
        }

        public void Init(Device device)
        {
            _device = device;
            _device.PlaybackChanged += Device_PlaybackChanged;
            _device.CurrentIdChanged += Device_CurrentIdChanged;
            _device.Start();

            _updateTimer = new Timer(1000);
            _updateTimer.Elapsed += updateTimer_Elapsed;
            _updateTimer.Start();
        }

        #region Device EventHandlers & Update Timer

        Timer _updateTimer;

        async void Device_PlaybackChanged(object sender, TransportStateEventArgs e)
        {
            if (_currentItem == null)
                return;

            if (e.Stopped == false)
                await ReportProgress().ConfigureAwait(false);

            else if (e.Stopped && _playbackStarted)
            {
                _playbackStarted = false;

                await _sessionManager.OnPlaybackStopped(new PlaybackStopInfo
                {
                    Item = _currentItem,
                    SessionId = _session.Id,
                    PositionTicks = _device.Position.Ticks

                }).ConfigureAwait(false);

                await SetNext().ConfigureAwait(false);
            }
        }

        async void Device_CurrentIdChanged(object sender, CurrentIdEventArgs e)
        {
            if (e.Id != Guid.Empty)
            {
                if (_currentItem != null && _currentItem.Id == e.Id)
                {
                    return;
                }

                var item = _libraryManager.GetItemById(e.Id);

                if (item != null)
                {
                    _logger.Debug("{0} - CurrentId {1}", _session.DeviceName, item.Id);
                    _currentItem = item;
                    _playbackStarted = false;

                    await ReportProgress().ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Handles the Elapsed event of the updateTimer control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ElapsedEventArgs"/> instance containing the event data.</param>
        async void updateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (_disposed)
                return;

            ((Timer)sender).Stop();


            if (!IsSessionActive)
            {
                //Session is inactive, mark it for Disposal and don't start the elapsed timer.
                await _sessionManager.ReportSessionEnded(this._session.Id);
                return;
            }

            await ReportProgress().ConfigureAwait(false);

            if (!_disposed && IsSessionActive)
                ((Timer)sender).Start();
        }

        /// <summary>
        /// Reports the playback progress.
        /// </summary>
        /// <returns></returns>
        private async Task ReportProgress()
        {
            if (_currentItem == null || _device.IsStopped)
                return;

            if (!_playbackStarted)
            {
                await _sessionManager.OnPlaybackStart(new PlaybackInfo { Item = _currentItem, SessionId = _session.Id, CanSeek = true, QueueableMediaTypes = new List<string> { "Audio", "Video" } }).ConfigureAwait(false);
                _playbackStarted = true;
            }

            if ((_device.IsPlaying || _device.IsPaused))
            {
                var playlistItem = Playlist.FirstOrDefault(p => p.PlayState == 1);
                if (playlistItem != null && playlistItem.Transcode)
                {
                    await _sessionManager.OnPlaybackProgress(new PlaybackProgressInfo
                    {
                        Item = _currentItem,
                        SessionId = _session.Id,
                        PositionTicks = _device.Position.Ticks + playlistItem.StartPositionTicks,
                        IsMuted = _device.IsMuted,
                        IsPaused = _device.IsPaused

                    }).ConfigureAwait(false);
                }
                else if (_currentItem != null)
                {
                    await _sessionManager.OnPlaybackProgress(new PlaybackProgressInfo
                    {
                        Item = _currentItem,
                        SessionId = _session.Id,
                        PositionTicks = _device.Position.Ticks,
                        IsMuted = _device.IsMuted,
                        IsPaused = _device.IsPaused

                    }).ConfigureAwait(false);
                }
            }
        }

        #endregion

        #region SendCommands

        public Task SendPlayCommand(PlayRequest command, CancellationToken cancellationToken)
        {
            _logger.Debug("{0} - Received PlayRequest: {1}", this._session.DeviceName, command.PlayCommand);

            var items = new List<BaseItem>();
            foreach (string id in command.ItemIds)
            {
                AddItemFromId(Guid.Parse(id), items);
            }

            var playlist = new List<PlaylistItem>();
            var isFirst = true;

            var serverAddress = GetServerAddress();

            foreach (var item in items)
            {
                if (isFirst && command.StartPositionTicks.HasValue)
                {
                    playlist.Add(CreatePlaylistItem(item, command.StartPositionTicks.Value, serverAddress));
                    isFirst = false;
                }
                else
                {
                    playlist.Add(CreatePlaylistItem(item, 0, serverAddress));
                }
            }

            _logger.Debug("{0} - Playlist created", _session.DeviceName);

            if (command.PlayCommand == PlayCommand.PlayLast)
            {
                AddItemsToPlaylist(playlist);
                return Task.FromResult(true);
            }
            if (command.PlayCommand == PlayCommand.PlayNext)
            {
                AddItemsToPlaylist(playlist);
                return Task.FromResult(true);
            }

            _logger.Debug("{0} - Playing {1} items", _session.DeviceName, playlist.Count);
            return PlayItems(playlist);
        }

        public Task SendPlaystateCommand(PlaystateRequest command, CancellationToken cancellationToken)
        {
            switch (command.Command)
            {
                case PlaystateCommand.Stop:
                    Playlist.Clear();
                    return _device.SetStop();

                case PlaystateCommand.Pause:
                    return _device.SetPause();

                case PlaystateCommand.Unpause:
                    return _device.SetPlay();

                case PlaystateCommand.Seek:
                    var playlistItem = Playlist.FirstOrDefault(p => p.PlayState == 1);
                    if (playlistItem != null && playlistItem.Transcode && playlistItem.IsVideo && _currentItem != null)
                    {
                        var newItem = CreatePlaylistItem(_currentItem, command.SeekPositionTicks ?? 0, GetServerAddress());
                        playlistItem.StartPositionTicks = newItem.StartPositionTicks;
                        playlistItem.StreamUrl = newItem.StreamUrl;
                        playlistItem.Didl = newItem.Didl;
                        return _device.SetAvTransport(playlistItem.StreamUrl, playlistItem.DlnaHeaders, playlistItem.Didl);

                    }
                    return _device.Seek(TimeSpan.FromTicks(command.SeekPositionTicks ?? 0));


                case PlaystateCommand.NextTrack:
                    _currentItem = null;
                    return SetNext();

                case PlaystateCommand.PreviousTrack:
                    _currentItem = null;
                    return SetPrevious();
            }

            return Task.FromResult(true);
        }

        public Task SendSystemCommand(SystemCommand command, CancellationToken cancellationToken)
        {
            switch (command)
            {
                case SystemCommand.VolumeDown:
                    return _device.VolumeDown();
                case SystemCommand.VolumeUp:
                    return _device.VolumeUp();
                case SystemCommand.Mute:
                    return _device.VolumeDown(true);
                case SystemCommand.Unmute:
                    return _device.VolumeUp(true);
                case SystemCommand.ToggleMute:
                    return _device.ToggleMute();
                default:
                    return Task.FromResult(true);
            }
        }

        public Task SendUserDataChangeInfo(UserDataChangeInfo info, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public Task SendRestartRequiredNotification(CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public Task SendServerRestartNotification(CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public Task SendServerShutdownNotification(CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public Task SendBrowseCommand(BrowseRequest command, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public Task SendLibraryUpdateInfo(LibraryUpdateInfo info, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public Task SendMessageCommand(MessageCommand command, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        #endregion

        #region Playlist

        private List<PlaylistItem> _playlist = new List<PlaylistItem>();

        private List<PlaylistItem> Playlist
        {
            get
            {
                return _playlist;
            }
            set
            {
                _playlist = value;
            }
        }

        private void AddItemFromId(Guid id, List<BaseItem> list)
        {
            var item = _libraryManager.GetItemById(id);
            if (item.IsFolder)
            {
                foreach (var childId in _itemRepository.GetChildren(item.Id))
                {
                    AddItemFromId(childId, list);
                }
            }
            else
            {
                if (item.MediaType == MediaType.Audio || item.MediaType == MediaType.Video)
                {
                    list.Add(item);
                }
            }
        }

        private string GetServerAddress()
        {
            return string.Format("{0}://{1}:{2}/mediabrowser",

                "http",
                _networkManager.GetLocalIpAddresses().FirstOrDefault() ?? "localhost",
                "8096"
                );
        }

        private PlaylistItem CreatePlaylistItem(BaseItem item, long startPostionTicks, string serverAddress)
        {
            var streams = _itemRepository.GetMediaStreams(new MediaStreamQuery { ItemId = item.Id }).ToList();

            var deviceInfo = _device.Properties;

            var playlistItem = PlaylistItem.Create(item, _dlnaManager.GetProfile(deviceInfo.Name, deviceInfo.ModelName, deviceInfo.ModelNumber));
            playlistItem.StartPositionTicks = startPostionTicks;

            if (playlistItem.IsAudio)
                playlistItem.StreamUrl = StreamHelper.GetAudioUrl(playlistItem, serverAddress);
            else
            {
                playlistItem.StreamUrl = StreamHelper.GetVideoUrl(_device.Properties, playlistItem, streams, serverAddress);
            }

            var didl = DidlBuilder.Build(item, _session.UserId.ToString(), serverAddress, playlistItem.StreamUrl, streams);
            playlistItem.Didl = didl;

            var header = StreamHelper.GetDlnaHeaders(playlistItem);
            playlistItem.DlnaHeaders = header;
            return playlistItem;
        }

        /// <summary>
        /// Plays the items.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <returns></returns>
        private async Task<bool> PlayItems(IEnumerable<PlaylistItem> items)
        {
            Playlist.Clear();
            Playlist.AddRange(items);
            await SetNext();
            return true;
        }

        /// <summary>
        /// Adds the items to playlist.
        /// </summary>
        /// <param name="items">The items.</param>
        private void AddItemsToPlaylist(IEnumerable<PlaylistItem> items)
        {
            Playlist.AddRange(items);
        }

        private async Task<bool> SetNext()
        {
            if (!Playlist.Any() || Playlist.All(i => i.PlayState != 0))
            {
                return true;
            }
            var currentitem = Playlist.FirstOrDefault(i => i.PlayState == 1);

            if (currentitem != null)
            {
                currentitem.PlayState = 2;
            }

            var nextTrack = Playlist.FirstOrDefault(i => i.PlayState == 0);
            if (nextTrack == null)
            {
                await _device.SetStop();
                return true;
            }
            nextTrack.PlayState = 1;
            await _device.SetAvTransport(nextTrack.StreamUrl, nextTrack.DlnaHeaders, nextTrack.Didl);
            if (nextTrack.StartPositionTicks > 0 && !nextTrack.Transcode)
                await _device.Seek(TimeSpan.FromTicks(nextTrack.StartPositionTicks));
            return true;
        }

        public Task<bool> SetPrevious()
        {
            if (!Playlist.Any() || Playlist.All(i => i.PlayState != 2))
                return Task.FromResult(false);

            var currentitem = Playlist.FirstOrDefault(i => i.PlayState == 1);

            var prevTrack = Playlist.LastOrDefault(i => i.PlayState == 2);

            if (currentitem != null)
            {
                currentitem.PlayState = 0;
            }

            if (prevTrack == null)
                return Task.FromResult(false);

            prevTrack.PlayState = 1;
            return _device.SetAvTransport(prevTrack.StreamUrl, prevTrack.DlnaHeaders, prevTrack.Didl);
        }

        #endregion

        private bool _disposed;

        public void Dispose()
        {
            if (!_disposed)
            {
                _updateTimer.Stop();
                _disposed = true;
                _device.Dispose();
                _logger.Log(LogSeverity.Debug, "PlayTo - Controller disposed");
            }
        }
    }
}

